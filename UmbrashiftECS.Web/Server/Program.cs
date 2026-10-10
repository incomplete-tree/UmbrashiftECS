using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".wasm"] = "application/wasm";
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true
});

app.MapPost("/feedback", async (Feedback feedback) =>
{
    var entry = $"[{DateTime.UtcNow:u}] jump/drag: {feedback.JumpOrDrag ?? "(none)"} | suggestion: {feedback.Suggestion ?? "(none)"}\n";
    await File.AppendAllTextAsync("Data/feedback.txt", entry);
    return Results.Ok();
});

app.Run();

record Feedback(string? JumpOrDrag, string? Suggestion);
