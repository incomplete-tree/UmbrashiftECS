#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.Rendering;

namespace UmbrashiftECS.Web;

public class UmbrashiftGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private UmbrashiftRenderer _renderer = null!;
    private Engine _engine = null!;
    private InputCapturer _inputCapturer = null!;
    private DemoEntitySpawner _spawner = null!;
    private bool _spawnWasPressed;
    private bool _showDebugHitboxes;
    private bool _f1WasPressed;

    public UmbrashiftGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 640,
            PreferredBackBufferHeight = 360
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _engine = new Engine();
        _engine.Initialize();
        
        _renderer = new UmbrashiftRenderer(_engine.MainWorld);
        _renderer.Initialize();
        
        _inputCapturer = new InputCapturer();

        _spawner = new DemoEntitySpawner(_engine.MainWorld);
        _spawner.SpawnAll(640, 360);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            keyboard.IsKeyDown(Keys.Escape))
            Exit();

        if (keyboard.IsKeyDown(Keys.F1) && !_f1WasPressed)
        {
            _showDebugHitboxes = !_showDebugHitboxes;
        }
        _f1WasPressed = keyboard.IsKeyDown(Keys.F1);

        if (keyboard.IsKeyDown(Keys.E) && !_spawnWasPressed)
        {
            _spawner.SpawnCrate(new Position(176, 24));
        }
        _spawnWasPressed = keyboard.IsKeyDown(Keys.E);

        var input = _inputCapturer.GetKeyboardInput();
        _engine.Update(input);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 16, 27));

        var commands = _renderer.Update(_engine.CurrentFrame, _showDebugHitboxes);
        _spriteBatch.Begin();
        foreach (var command in commands)
        {
            RenderCommandExecutor.ExecuteRenderCommand(GraphicsDevice, _spriteBatch, command);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}