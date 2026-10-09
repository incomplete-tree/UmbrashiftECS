using System.Drawing;

namespace UmbrashiftECS.Rendering;

public record struct RenderCommand(
    string TexturePath,
    int? X = null,
    int? Y = null,
    Rectangle? DestRect = null,
    Rectangle? SourceRect = null,
    Color? Color = null,
    bool IsOutline = false,
    int Thickness = 1)
{
}