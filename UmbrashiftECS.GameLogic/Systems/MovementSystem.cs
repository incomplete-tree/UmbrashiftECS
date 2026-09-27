using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class MovementSystem(World world) : BaseSystem<World, uint>(world)
{
    private const double MaximumElapsedSeconds = 0.1;

    [Query]
    public void MoveEntity(in Entity entity, ref Position position, in MovementDelta movementDelta, ref FractionalPositionRemainder fractionalPositionRemainder)
    {
        var movementX = movementDelta.X;
        var movementY = movementDelta.Y;
        
        var newPositionX = movementX + position.X + fractionalPositionRemainder.X;
        var newPositionY = movementY + position.Y + fractionalPositionRemainder.Y;
        fractionalPositionRemainder = new FractionalPositionRemainder()
        {
            X = newPositionX - (int)newPositionX,
            Y = newPositionY - (int)newPositionY
        };
        entity.Set(fractionalPositionRemainder);
        position = new Position((int)newPositionX, (int)newPositionY);
    }
}
