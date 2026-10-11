using UmbrashiftECS.Components;

namespace UmbrashiftECS.Serialization;

public record Prefab()
{
    public string Name = "Untitled";
    public string BasePrefab = "";
    public ComponentStatement Overrides = new NullStatement();
}