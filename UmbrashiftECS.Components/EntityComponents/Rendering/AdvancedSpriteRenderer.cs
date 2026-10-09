using System.Drawing;

namespace UmbrashiftECS.Components.EntityComponents.Rendering;

public record struct AdvancedSpriteRenderer(string PngPath, Rectangle SourceRectangle, Rectangle DestRectangle);