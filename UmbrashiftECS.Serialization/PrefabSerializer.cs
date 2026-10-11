using System.Diagnostics;
using System.Text.Json;
using Arch.Core;
using UmbrashiftECS.Components;
using UmbrashiftECS.Operations;

namespace UmbrashiftECS.Serialization;

public class PrefabSerializer
{
    public static void TestSerialization()
    {
        var prefab = new Prefab()
        {
            Name = "TestPrefab",
            Overrides = new SequenceStatement(
                new SetFieldStatement("DashConfig", "DashSpeed", new ConstantExpression(new IntValue(200))),
                new SetFieldStatement("Test", "fijreojfoirejfoire", new GetFieldExpression("Test", "jfie"))
            )
        };
        Console.WriteLine(prefab);
        var options = new JsonSerializerOptions()
        {
            IncludeFields = true,
        };
        var text = JsonSerializer.Serialize(prefab, options);
        Console.WriteLine(text);
        var roundPrefab = JsonSerializer.Deserialize<Prefab>(text, options);
        Debug.Assert(prefab == roundPrefab);
    }

    public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions()
    {
        IncludeFields = true,
    };
    
    public static async Task<Entity> GetEntity(World world, UmbrFile file, string prefabName, ComponentStatement overrides)
    {
        var statementsToApply = await ResolvePrefab(file, prefabName);
        statementsToApply.Add(overrides);

        var entity = world.Create();
        foreach (var statement in statementsToApply)
        {
            statement.Execute(entity);
        }

        return entity;
    }

    private static async Task<List<ComponentStatement>> ResolvePrefab(UmbrFile file, string name)
    {
        var result = new List<ComponentStatement>();
        var visited = new HashSet<string>();

        while (!string.IsNullOrEmpty(name))
        {
            if (!visited.Add(name))
            {
                throw new InvalidDataException("prefab derivation cycle!");
            }
            
            using var stream = file.OpenEntryRead(GetPrefabPath(name));

            var prefab = await JsonSerializer.DeserializeAsync<Prefab>(stream) ?? throw new NullReferenceException();

            result.Add(prefab.Overrides);
            name = prefab.BasePrefab;
        }

        result.Reverse();
        return result;
    }

    private static string GetPrefabPath(string name)
    {
        return $"Prefabs/{name}";
    }
}