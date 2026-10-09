using Arch.Core;

namespace UmbrashiftECS.Operations;

public static class EntityExtensions
{
    extension(Entity e)
    {
        public World World => World.Worlds[e.WorldId];
    }
}