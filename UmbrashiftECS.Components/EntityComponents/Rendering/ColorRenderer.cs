using System.Drawing;

namespace UmbrashiftECS.Components.EntityComponents.Rendering;

public record struct ColorRenderer(Color Color, Rectangle? Rectangle = null)
{
    public ColorRenderer(Color color) : this(color, null) { }
}