using System;
using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.GameLogic;

public class Engine : IDisposable
{
    public World MainWorld;
    public uint CurrentFrame;
    private Group<uint> _systems;
    private PlayerInputSystem _playerInputSystem;
    
    public void Initialize()
    {
        MainWorld = World.Create();
        _playerInputSystem = new PlayerInputSystem(MainWorld);
        _systems = new Group<uint>("Umbrashift",
            _playerInputSystem, // First gather input
            new LayerToggleSystem(MainWorld),
            new GroundedCheckSystem(MainWorld),
            new JumpSystem(MainWorld),
            new CrouchSystem(MainWorld),
            new ApplyVelocitySystem(MainWorld),
            new DashSystem(MainWorld),
            new ActorCollisionSystem(MainWorld),
            new DashCollisionSystem(MainWorld),
            new MovementSystem(MainWorld));
        _systems.Initialize();
    }

    public void Update(params PlayerInputStateDTO[] inputs)
    {
        CurrentFrame++;
        foreach (var input in inputs)
        {
            _playerInputSystem.SetInput(input, CurrentFrame);
        }
        _playerInputSystem.ClearUnprovidedInputs(inputs, CurrentFrame);

        _systems.BeforeUpdate(CurrentFrame);
        _systems.Update(CurrentFrame);
        _systems.AfterUpdate(CurrentFrame);
    }

    public void Dispose()
    {
        _systems.Dispose();
        GC.SuppressFinalize(this);
    }
}
