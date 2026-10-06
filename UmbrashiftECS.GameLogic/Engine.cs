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
    private bool _initialized;
    
    public void Initialize()
    {
        if (_initialized)
        {
            throw new InvalidOperationException("The engine is already initialized.");
        }

        CurrentFrame = 200; // not 0 because that bugs input buffers.
        MainWorld = World.Create();
        _playerInputSystem = new PlayerInputSystem(MainWorld);

        var inputSystems = new Group<uint>("Input",
            _playerInputSystem);
        var gameplaySystems = new Group<uint>("Gameplay",
            new LayerToggleSystem(MainWorld),
            new GroundedCheckSystem(MainWorld),
            new JumpSystem(MainWorld),
            new CrouchSystem(MainWorld),
            new WalkingSystem(MainWorld),
            new FrictionSystem(MainWorld),
            new GravitySystem(MainWorld));
        var movementSystems = new Group<uint>("Movement",
            new ApplyVelocitySystem(MainWorld),
            new DashSystem(MainWorld),
            new ActorCollisionSystem(MainWorld),
            new MovementSystem(MainWorld));

        _systems = new Group<uint>("Umbrashift",
            inputSystems,
            gameplaySystems,
            movementSystems);
        _systems.Initialize();
        _initialized = true;
    }

    public void Update(params PlayerInputStateDTO[] inputs)
    {
        if (!_initialized || _systems is null || _playerInputSystem is null)
        {
            throw new InvalidOperationException("The engine must be initialized before updating.");
        }

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
        if (!_initialized)
        {
            return;
        }

        _systems?.Dispose();
        MainWorld.Dispose();
        _systems = null;
        _playerInputSystem = null;
        _initialized = false;
        GC.SuppressFinalize(this);
    }
}
