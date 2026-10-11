namespace UmbrashiftECS.Components.EntityComponents.Tilemap;

public record struct TilemapData(Dictionary<(int X, int Y), int> Values)
{
    public TilemapData() : this(new Dictionary<(int X, int Y), int>()) { }

    public readonly bool IsSolidAt(int x, int y) =>
        Values != null && Values.TryGetValue((x, y), out var val) && val > 0;

    public readonly int GetTile(int x, int y) =>
        Values != null && Values.TryGetValue((x, y), out var val) ? val : 0;

    public readonly void SetTile(int x, int y, int value)
    {
        if (Values != null)
        {
            Values[(x, y)] = value;
        }
    }
}
