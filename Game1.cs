using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NewProject.Gameplay;
using NewProject.Player;
using NewProject.Rendering;
using NewProject.UI;
using NewProject.World;

namespace NewProject;

public class Game1 : Game
{
    private const int ChunkLoadRadius = 10;
    private const int ChunkKeepRadius = 10;
    private const int ChunkBuildsPerFrame = 4;

    private readonly GraphicsDeviceManager _graphics;

    private PlayerController _player = null!;
    private InfiniteWorld _world = null!;
    private VoxelWorldRenderer _renderer = null!;
    private HotbarState _hotbar = null!;
    private WorldInteractionController _interaction = null!;
    private GameHudRenderer _hud = null!;
    private GameConsole _console = null!;
    private GameConsoleRenderer _consoleRenderer = null!;

    private Point _lastChunk;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private int _fps;
    private int _frameCounter;
    private double _fpsTimer;
    private float _worldTime;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.SynchronizeWithVerticalRetrace = false;
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;

        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        IsFixedTimeStep = false;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 165d);
        Window.AllowUserResizing = true;
        Window.Title = "Bad clone";
    }

    protected override void Initialize()
    {
        _world = new InfiniteWorld(height: 32, seed: 1337);
        _world.EnsureChunksAround(Vector3.Zero, ChunkLoadRadius);

        Vector3 spawn = new(8f, _world.GetSurfaceHeight(8, 8) + 1.2f, 8f);
        _player = new PlayerController(spawn);
        _hotbar = new HotbarState();
        _worldTime = VoxelWorldRenderer.GetDayTimeValue();
        _console = new GameConsole(_hotbar, _player, _world, () => _worldTime, SetTimePreset);
        _lastChunk = _world.GetChunkCoordinate(_player.CameraPosition);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderer = new VoxelWorldRenderer(GraphicsDevice, Content);
        _renderer.SyncWorld(_world);
        _renderer.BuildPendingChunks(_world, int.MaxValue);

        _interaction = new WorldInteractionController(_world, _player, _renderer, _hotbar);
        _hud = new GameHudRenderer(GraphicsDevice, Content);
        _consoleRenderer = new GameConsoleRenderer(GraphicsDevice, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();
        _worldTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool toggledConsole = _console.HandleToggle(keyboard, _previousKeyboard);
        if (toggledConsole)
        {
            IsMouseVisible = _console.IsOpen;
        }

        if (_console.IsOpen)
        {
            _console.HandleInput(keyboard, _previousKeyboard, mouse, _previousMouse);
            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        
        _player.Update(gameTime, Window, IsActive, _world);
        _hotbar.HandleInput(keyboard, _previousKeyboard, mouse, _previousMouse);
        _interaction.HandleInput(mouse, _previousMouse);
        _interaction.Update(gameTime);
        UpdateWorldStreaming();
        _renderer.BuildPendingChunks(_world, ChunkBuildsPerFrame);
        UpdateFps(gameTime);

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        HotbarEntry selected = _hotbar.SelectedEntry;
        Texture2D heldTexture = _hud.GetHeldTexture(selected);
        Matrix projection = _player.GetProjectionMatrix(GraphicsDevice.Viewport.AspectRatio);
        int cameraBlockX = (int)MathF.Floor(_player.CameraPosition.X);
        int cameraBlockY = (int)MathF.Floor(_player.CameraPosition.Y);
        int cameraBlockZ = (int)MathF.Floor(_player.CameraPosition.Z);
        float underwaterFactor = _world.GetBlock(cameraBlockX, cameraBlockY, cameraBlockZ) == BlockType.Water ? 1f : 0f;

        float time = _worldTime;
        GraphicsDevice.Clear(VoxelWorldRenderer.GetSkyColor(time));
        _renderer.Draw(_player.CameraPosition, _player.ViewMatrix, projection, time, underwaterFactor);
        _renderer.DrawViewModel(
            _player.CameraPosition,
            _player.ViewMatrix,
            projection,
            heldTexture,
            selected.Kind == HotbarItemKind.Tool,
            selected.Kind == HotbarItemKind.Tool ? selected.Tool : null,
            _interaction.UseAnimationTimer,
            WorldInteractionController.UseAnimationDuration);
        _hud.Draw(GraphicsDevice, _hotbar, _fps);
        _consoleRenderer.Draw(GraphicsDevice, _console);

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _hud?.Dispose();
        _renderer?.Dispose();
        base.UnloadContent();
    }

    private void UpdateWorldStreaming()
    {
        Point currentChunk = _world.GetChunkCoordinate(_player.CameraPosition);
        if (currentChunk == _lastChunk)
        {
            return;
        }

        _world.EnsureChunksAround(_player.CameraPosition, ChunkLoadRadius);
        _world.TrimChunksOutside(currentChunk.X, currentChunk.Y, ChunkKeepRadius);
        _renderer.SyncWorld(_world);
        _lastChunk = currentChunk;
    }

    private void UpdateFps(GameTime gameTime)
    {
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        _frameCounter++;

        if (_fpsTimer < 1.0)
        {
            return;
        }

        _fps = _frameCounter;
        _frameCounter = 0;
        _fpsTimer -= 1.0;
    }

    private void SetTimePreset(string preset)
    {
        _worldTime = preset == "night"
            ? VoxelWorldRenderer.GetNightTimeValue()
            : VoxelWorldRenderer.GetDayTimeValue();
    }
}
