using System.IO.Compression;

namespace UmbrashiftECS.Serialization;

public class UmbrFile : IDisposable, IAsyncDisposable
{
    public enum OpenMode
    {
        Ram,
        Temp,
        User
    }

    private readonly OpenMode _openMode;
    private readonly bool _isReadOnly;
    private readonly string? _workingDirectory;
    private readonly Dictionary<string, byte[]>? _ramEntries;
    private bool _disposed;

    private static string UserDataDirectory
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                localAppData = Path.Combine(userProfile, ".local", "share");
            }

            return Path.Combine(localAppData, "UmbrashiftECS");
        }
    }

    private UmbrFile(OpenMode openMode, bool isReadOnly, string? workingDirectory, Dictionary<string, byte[]>? ramEntries)
    {
        _openMode = openMode;
        _isReadOnly = isReadOnly;
        _workingDirectory = workingDirectory;
        _ramEntries = ramEntries;
    }

    public static UmbrFile OpenReadonly(string path, OpenMode openMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        

        return CreateInstance(path, openMode, isReadOnly: true, isAsync: false).GetAwaiter().GetResult();
    }

    public static async Task<UmbrFile> OpenReadonlyAsync(string path, OpenMode openMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return await CreateInstance(path, openMode, isReadOnly: true, isAsync: true, cancellationToken);
    }

    public static UmbrFile OpenReadWrite(string path, OpenMode openMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return CreateInstance(path, openMode, isReadOnly: false, isAsync: false).GetAwaiter().GetResult();
    }

    public static async Task<UmbrFile> OpenReadWriteAsync(string path, OpenMode openMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return await CreateInstance(path, openMode, isReadOnly: false, isAsync: true, cancellationToken);
    }

    public static UmbrFile CreateReadWrite(OpenMode openMode)
    {
        return CreateInstance(sourcePath: null, openMode, isReadOnly: false, isAsync: false).GetAwaiter().GetResult();
    }

    public static Task<UmbrFile> CreateReadWriteAsync(OpenMode openMode, CancellationToken cancellationToken = default)
    {
        return CreateInstance(sourcePath: null, openMode, isReadOnly: false, isAsync: true, cancellationToken);
    }

    private static async Task<UmbrFile> CreateInstance(string? sourcePath, OpenMode openMode, bool isReadOnly, bool isAsync, CancellationToken cancellationToken = default)
    {
        if (sourcePath != null && !File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Archive file '{sourcePath}' not found.", sourcePath);
        }
        switch (openMode)
        {
            case OpenMode.Ram:
            {
                var ramEntries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                if (sourcePath != null)
                {
                    if (isAsync)
                    {
                        await LoadZipIntoRamAsync(sourcePath, ramEntries, cancellationToken);
                    }
                    else
                    {
                        LoadZipIntoRam(sourcePath, ramEntries);
                    }
                }

                return new UmbrFile(openMode, isReadOnly, workingDirectory: null, ramEntries);
            }

            case OpenMode.Temp:
            {
                var workingDir = Path.Combine(Path.GetTempPath(), "UmbrashiftECS", "temp_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workingDir);
                if (sourcePath != null)
                {
                    if (isAsync)
                    {
                        await ExtractZipToDirectoryAsync(sourcePath, workingDir, cancellationToken);
                    }
                    else
                    {
                        ExtractZipToDirectory(sourcePath, workingDir);
                    }
                }

                return new UmbrFile(openMode, isReadOnly, workingDir, ramEntries: null);
            }

            case OpenMode.User:
            {
                var workingDir = Path.Combine(UserDataDirectory, "userdata_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workingDir);
                if (sourcePath != null)
                {
                    if (isAsync)
                    {
                        await ExtractZipToDirectoryAsync(sourcePath, workingDir, cancellationToken);
                    }
                    else
                    {
                        ExtractZipToDirectory(sourcePath, workingDir);
                    }
                }

                return new UmbrFile(openMode, isReadOnly, workingDir, ramEntries: null);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(openMode), openMode, "Unknown OpenMode.");
        }
    }

    private static void LoadZipIntoRam(string zipPath, Dictionary<string, byte[]> ramEntries)
    {
        using var fileStream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            var normalizedKey = NormalizeEntryPath(entry.FullName);
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ramEntries[normalizedKey] = ms.ToArray();
        }
    }

    private static async Task LoadZipIntoRamAsync(string zipPath, Dictionary<string, byte[]> ramEntries, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            var normalizedKey = NormalizeEntryPath(entry.FullName);
            await using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms, cancellationToken);
            ramEntries[normalizedKey] = ms.ToArray();
        }
    }

    private static void ExtractZipToDirectory(string zipPath, string targetDirectory)
    {
        using var fileStream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            var destPath = GetDiskPath(targetDirectory, entry.FullName);
            var parentDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            using var entryStream = entry.Open();
            using var destStream = File.Create(destPath);
            entryStream.CopyTo(destStream);
        }
    }

    private static async Task ExtractZipToDirectoryAsync(string zipPath, string targetDirectory, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            var destPath = GetDiskPath(targetDirectory, entry.FullName);
            var parentDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            await using var entryStream = entry.Open();
            await using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
            await entryStream.CopyToAsync(destStream, cancellationToken);
        }
    }

    public void Export(string path)
    {
        ExportAsync(path).GetAwaiter().GetResult();
    }

    public async Task ExportAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullExportPath = Path.GetFullPath(path);
        var exportDir = Path.GetDirectoryName(fullExportPath);
        if (!string.IsNullOrEmpty(exportDir))
        {
            Directory.CreateDirectory(exportDir);
        }

        var tempExportPath = Path.Combine(
            exportDir ?? Path.GetTempPath(),
            $".export_{Guid.NewGuid():N}.tmp"
        );

        try
        {
            await using (var fileStream = new FileStream(tempExportPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                if (_openMode == OpenMode.Ram)
                {
                    foreach (var (entryName, data) in _ramEntries!)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        await using var entryStream = entry.Open();
                        await entryStream.WriteAsync(data, cancellationToken);
                    }
                }
                else
                {
                    if (Directory.Exists(_workingDirectory))
                    {
                        foreach (var filePath in Directory.EnumerateFiles(_workingDirectory, "*", SearchOption.AllDirectories))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var relativePath = Path.GetRelativePath(_workingDirectory, filePath).Replace('\\', '/');
                            var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                            await using var entryStream = entry.Open();
                            await using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
                            await sourceStream.CopyToAsync(entryStream, cancellationToken);
                        }
                    }
                }
            }

            File.Move(tempExportPath, fullExportPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempExportPath))
            {
                try { File.Delete(tempExportPath); } catch { /* best effort */ }
            }
        }
    }

    public Stream OpenEntryRead(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (_openMode == OpenMode.Ram)
        {
            var key = NormalizeEntryPath(path);
            if (!_ramEntries!.TryGetValue(key, out var bytes))
            {
                throw new FileNotFoundException($"Entry '{path}' was not found in the archive.");
            }

            return new MemoryStream(bytes, writable: false);
        }
        else
        {
            var fullPath = GetDiskPath(_workingDirectory!, path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Entry '{path}' was not found.", fullPath);
            }

            return File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }

    public Task<Stream> OpenEntryReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenEntryRead(path));
    }

    public Stream OpenEntryWrite(string path)
    {
        ThrowIfDisposed();
        ThrowIfReadOnly();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (_openMode == OpenMode.Ram)
        {
            var key = NormalizeEntryPath(path);
            _ramEntries![key] = [];
            return new TrackingMemoryStream(bytes => _ramEntries[key] = bytes);
        }
        else
        {
            var fullPath = GetDiskPath(_workingDirectory!, path);
            var parentDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            return File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }
    }

    public Task<Stream> OpenEntryWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenEntryWrite(path));
    }

    public Stream OpenEntryReadWrite(string path)
    {
        ThrowIfDisposed();
        ThrowIfReadOnly();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (_openMode == OpenMode.Ram)
        {
            var key = NormalizeEntryPath(path);
            var existingData = _ramEntries!.TryGetValue(key, out var bytes) ? bytes : [];
            _ramEntries[key] = existingData;
            return new TrackingMemoryStream(existingData, bytes => _ramEntries[key] = bytes);
        }
        else
        {
            var fullPath = GetDiskPath(_workingDirectory!, path);
            var parentDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            return File.Open(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
    }

    public Task<Stream> OpenEntryReadWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenEntryReadWrite(path));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _ramEntries?.Clear();
        }

        if (_openMode == OpenMode.Temp && _workingDirectory != null)
        {
            try
            {
                if (Directory.Exists(_workingDirectory))
                {
                    Directory.Delete(_workingDirectory, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup of temporary directory
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("Cannot modify entries in a read-only UmbrFile.");
        }
    }

    private static string NormalizeEntryPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = path.Replace('\\', '/').Trim().TrimStart('/');
        if (string.IsNullOrEmpty(normalized) || normalized.EndsWith('/'))
        {
            throw new ArgumentException("Entry path cannot be empty or a directory.", nameof(path));
        }

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var depth = 0;
        foreach (var part in parts)
        {
            if (part == "..")
            {
                depth--;
                if (depth < 0)
                {
                    throw new ArgumentException("Path traversal outside archive is not allowed.", nameof(path));
                }
            }
            else if (part != ".")
            {
                depth++;
            }
        }

        return normalized;
    }

    private static string GetDiskPath(string rootDirectory, string path)
    {
        var normalized = NormalizeEntryPath(path);
        var osRelativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, osRelativePath));

        var rootWithSep = rootDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? rootDirectory
            : rootDirectory + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            throw new ArgumentException("Path traversal outside working directory is not allowed.", nameof(path));
        }

        return fullPath;
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        private readonly Action<byte[]> _onUpdate;

        public TrackingMemoryStream(Action<byte[]> onUpdate)
        {
            _onUpdate = onUpdate;
        }

        public TrackingMemoryStream(byte[] initialData, Action<byte[]> onUpdate)
        {
            _onUpdate = onUpdate;
            if (initialData.Length > 0)
            {
                Write(initialData, 0, initialData.Length);
                Position = 0;
            }
        }

        public override void Flush()
        {
            base.Flush();
            _onUpdate(ToArray());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onUpdate(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}
