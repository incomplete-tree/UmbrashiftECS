namespace UmbrashiftECS.Components.EntityComponents.Rendering;

public record struct BasicSpriteRenderer
{
    public BasicSpriteRenderer(string PngPath, int OffsetX=0, int OffsetY=0)
    {
        this.PngPath = PngPath;
        this.OffsetX = OffsetX;
        this.OffsetY = OffsetY;
    }

    public string PngPath
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            TextureId = -1;
        }
    }

    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    /// <summary>
    /// To be assigned only at runtime.
    /// </summary>
    [NonSerialized]
    public int TextureId = -1;
    public readonly void Deconstruct(out string PngPath, out int OffsetX, out int OffsetY)
    {
        PngPath = this.PngPath;
        OffsetX = this.OffsetX;
        OffsetY = this.OffsetY;
    }
}