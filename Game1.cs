using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NewProject.Player;
using NewProject.Rendering;
using NewProject.World;

namespace NewProject;

public class Game1 : Game
{
    private const int ChunkLoadRadius = 2;
    private const int ChunkKeepRadius = 3;
    private const int ChunkBuildsPerFrame = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _debugFont = null!;

    private PlayerController _player = null!;
    private InfiniteWorld _world = null!;
    private VoxelWorldRenderer _renderer = null!;
    private Point _lastChunk;
    private int _fps;
    private int _frameCounter;
    private double _fpsTimer;

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
        Window.Title = "Mini Block World  |  WASD move, mouse look, Space jump, Shift sprint, Esc exit";
    }

    protected override void Initialize()
    {
        _world = new InfiniteWorld(height: 32, seed: 1337);
        _world.EnsureChunksAround(Vector3.Zero, chunkRadius: ChunkLoadRadius);

        Vector3 spawn = new(
            8f,
            _world.GetSurfaceHeight(8, 8) + 1.2f,
            8f);

        _player = new PlayerController(spawn);
        _lastChunk = _world.GetChunkCoordinate(_player.CameraPosition);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _debugFont = Content.Load<SpriteFont>("Fonts/DebugFont");
        _renderer = new VoxelWorldRenderer(GraphicsDevice, Content);
        _renderer.SyncWorld(_world);
        _renderer.BuildPendingChunks(_world, int.MaxValue);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        _player.Update(gameTime, Window, IsActive, _world);
        UpdateWorldStreaming();
        _renderer.BuildPendingChunks(_world, ChunkBuildsPerFrame);
        UpdateFps(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(VoxelWorldRenderer.SkyColor);
        _renderer.Draw(
            _player.CameraPosition,
            _player.ViewMatrix,
            _player.GetProjectionMatrix(GraphicsDevice.Viewport.AspectRatio),
            (float)gameTime.TotalGameTime.TotalSeconds);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.DrawString(_debugFont, $"FPS: {_fps}", new Vector2(12f, 10f), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _spriteBatch?.Dispose();
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

        _world.EnsureChunksAround(_player.CameraPosition, chunkRadius: ChunkLoadRadius);
        _world.TrimChunksOutside(currentChunk.X, currentChunk.Y, ChunkKeepRadius);
        _renderer.SyncWorld(_world);
        _lastChunk = currentChunk;
    }

    private void UpdateFps(GameTime gameTime)
    {
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        _frameCounter++;

        if (_fpsTimer >= 1.0)
        {
            _fps = _frameCounter;
            _frameCounter = 0;
            _fpsTimer -= 1.0;
        }
    }
}
