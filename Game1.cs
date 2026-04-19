using System;
using NewProject.Entities;
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
    private const int ChunkBuildsPerFrame = 1;

    private readonly GraphicsDeviceManager _graphics;

    private PlayerController _player = null!;
    private InfiniteWorld _world = null!;
    private EntitySystem _entities = null!;
    private VoxelWorldRenderer _renderer = null!;
    private EntityRenderer _entityRenderer = null!;
    private HotbarState _hotbar = null!;
    private WorldInteractionController _interaction = null!;
    private GameHudRenderer _hud = null!;
    private GameConsole _console = null!;
    private GameConsoleRenderer _consoleRenderer = null!;
    private GameMenu _menu = null!;
    private GameMenuRenderer _menuRenderer = null!;

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
        _world = new InfiniteWorld(height: 64, seed: 1337);
        _world.EnsureChunksAround(Vector3.Zero, ChunkLoadRadius);

        Vector3 spawn = new(8f, _world.GetSurfaceHeight(8, 8) + 1.2f, 8f);
        _player = new PlayerController(spawn);
        _entities = new EntitySystem();
        _hotbar = new HotbarState();
        _menu = new GameMenu(_player.FieldOfViewDegrees, _graphics.IsFullScreen);
        _worldTime = VoxelWorldRenderer.GetDayTimeValue();
        _console = new GameConsole(_hotbar, _player, _world, () => _worldTime, SetTimePreset);
        _lastChunk = _world.GetChunkCoordinate(_player.CameraPosition);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderer = new VoxelWorldRenderer(GraphicsDevice, Content);
        _renderer.SyncWorld(_world);
        _renderer.BuildPendingChunks(_world, 36);
        _entityRenderer = new EntityRenderer(GraphicsDevice);

        _interaction = new WorldInteractionController(_world, _player, _renderer, _hotbar);
        _hud = new GameHudRenderer(GraphicsDevice, Content);
        _consoleRenderer = new GameConsoleRenderer(GraphicsDevice, Content);
        _menuRenderer = new GameMenuRenderer(GraphicsDevice, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();
        _worldTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        IsMouseVisible = _menu.IsOpen || _console.IsOpen;

        if (_menu.IsOpen)
        {
            MenuAction menuAction = _menu.HandleInput(GraphicsDevice.Viewport, mouse, _previousMouse);
            ApplyMenuSettings();

            if (menuAction == MenuAction.Play)
            {
                IsMouseVisible = false;
            }
            else if (menuAction == MenuAction.Quit)
            {
                Exit();
                return;
            }

            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

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

        if (keyboard.IsKeyDown(Keys.Escape) && !_previousKeyboard.IsKeyDown(Keys.Escape))
        {
            _menu.OpenMain();
            IsMouseVisible = true;
            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        if (keyboard.IsKeyDown(Keys.F) && !_previousKeyboard.IsKeyDown(Keys.F))
        {
            _player.ToggleFly();
        }

        _player.Update(gameTime, Window, IsActive, _world);
        _hotbar.HandleInput(keyboard, _previousKeyboard, mouse, _previousMouse);
        _interaction.HandleInput(mouse, _previousMouse);
        _interaction.Update(gameTime);
        UpdateWorldStreaming();
        _entities.Update(gameTime, _world, _player.Position);
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
        _entityRenderer.Draw(_entities.Entities, _player.ViewMatrix, projection, _player.CameraPosition);
        _renderer.DrawViewModel(
            _player.CameraPosition,
            _player.ViewMatrix,
            projection,
            heldTexture,
            selected.Kind == HotbarItemKind.Tool,
            selected.Kind == HotbarItemKind.Tool ? selected.Tool : null,
            selected.Kind == HotbarItemKind.Block ? selected.Block : BlockType.Air,
            _interaction.UseAnimationTimer,
            WorldInteractionController.UseAnimationDuration);
        _hud.Draw(GraphicsDevice, _hotbar, _fps);
        _consoleRenderer.Draw(GraphicsDevice, _console);
        _menuRenderer.Draw(GraphicsDevice, _menu, Mouse.GetState());

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _hud?.Dispose();
        _menuRenderer?.Dispose();
        _entityRenderer?.Dispose();
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

    private void ApplyMenuSettings()
    {
        _player.FieldOfViewDegrees = _menu.FovDegrees;

        if (_graphics.IsFullScreen != _menu.Fullscreen)
        {
            _graphics.IsFullScreen = _menu.Fullscreen;
            _graphics.ApplyChanges();
        }
    }
}
