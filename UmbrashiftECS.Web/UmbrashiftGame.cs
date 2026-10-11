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
        _spawner.SpawnAll();

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

        var (commands, cameraMatrix) = _renderer.Update(_engine.CurrentFrame, _showDebugHitboxes);
        var xnaMatrix = new Matrix(
            cameraMatrix.M11, cameraMatrix.M12, cameraMatrix.M13, cameraMatrix.M14,
            cameraMatrix.M21, cameraMatrix.M22, cameraMatrix.M23, cameraMatrix.M24,
            cameraMatrix.M31, cameraMatrix.M32, cameraMatrix.M33, cameraMatrix.M34,
            cameraMatrix.M41, cameraMatrix.M42, cameraMatrix.M43, cameraMatrix.M44);
        _spriteBatch.Begin(transformMatrix: xnaMatrix);
        // _spriteBatch.Begin();
        foreach (var command in commands)
        {
            RenderCommandExecutor.ExecuteRenderCommand(GraphicsDevice, _spriteBatch, command);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}