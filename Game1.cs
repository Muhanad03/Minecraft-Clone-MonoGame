using System;
using System.Collections.Generic;
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
    private const int ChunkLoadRadius = 10;
    private const int ChunkKeepRadius = 10;
    private const int ChunkBuildsPerFrame = 4;
    private const float ReachDistance = 7f;
    private const float UseAnimationDuration = 0.18f;

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
    private readonly Dictionary<BlockType, int> _inventory = new();
    private int _selectedSlot;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private Texture2D _pixelTexture = null!;
    private readonly Dictionary<BlockType, Texture2D> _blockIcons = new();
    private readonly Dictionary<ToolType, Texture2D> _toolIcons = new();
    private float _useAnimationTimer;

    private readonly record struct BlockRayHit(Point3 Hit, Point3 Place);

    private readonly record struct Point3(int X, int Y, int Z);
    private enum ToolType
    {
        Pickaxe,
        Axe,
        Sword
    }

    private enum HotbarItemKind
    {
        Tool,
        Block
    }

    private readonly record struct HotbarEntry(HotbarItemKind Kind, ToolType Tool, BlockType Block, string Label);

    private static readonly HotbarEntry[] HotbarEntries =
    [
        new(HotbarItemKind.Tool, ToolType.Pickaxe, BlockType.Air, "Pick"),
        new(HotbarItemKind.Tool, ToolType.Axe, BlockType.Air, "Axe"),
        new(HotbarItemKind.Tool, ToolType.Sword, BlockType.Air, "Sword"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Grass, "Grass"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Dirt, "Dirt"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Stone, "Stone"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Trunk, "Log"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Leaves, "Leaf")
    ];

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        //_graphics.IsFullScreen = true;

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

        foreach (HotbarEntry entry in HotbarEntries)
        {
            if (entry.Kind == HotbarItemKind.Block)
            {
                _inventory[entry.Block] = 48;
            }
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _debugFont = Content.Load<SpriteFont>("Fonts/DebugFont");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
        _blockIcons[BlockType.Grass] = Content.Load<Texture2D>("Textures/blocks/grass_top");
        _blockIcons[BlockType.Dirt] = Content.Load<Texture2D>("Textures/blocks/dirt");
        _blockIcons[BlockType.Stone] = Content.Load<Texture2D>("Textures/blocks/dirt");
        _blockIcons[BlockType.Trunk] = Content.Load<Texture2D>("Textures/blocks/log_oak_top");
        _blockIcons[BlockType.Leaves] = Content.Load<Texture2D>("Textures/blocks/leaves_oak_opaque");
        _toolIcons[ToolType.Pickaxe] = Content.Load<Texture2D>("Textures/items/iron_pickaxe");
        _toolIcons[ToolType.Axe] = Content.Load<Texture2D>("Textures/items/iron_axe");
        _toolIcons[ToolType.Sword] = Content.Load<Texture2D>("Textures/items/iron_sword");
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
        HandleHotbarInput();
        HandleBlockInteraction();
        UpdateWorldStreaming();
        _renderer.BuildPendingChunks(_world, ChunkBuildsPerFrame);
        UpdateFps(gameTime);
        UpdateUseAnimation(gameTime);
        _previousMouse = Mouse.GetState();
        _previousKeyboard = Keyboard.GetState();
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
        DrawHotbar();
        DrawHeldItem();
        _spriteBatch.DrawString(_debugFont, $"FPS: {_fps}", new Vector2(12f, 10f), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _pixelTexture?.Dispose();
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

    private void HandleHotbarInput()
    {
        KeyboardState keyboard = Keyboard.GetState();

        Keys[] numberKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8];
        for (int i = 0; i < numberKeys.Length; i++)
        {
            if (keyboard.IsKeyDown(numberKeys[i]) && !_previousKeyboard.IsKeyDown(numberKeys[i]))
            {
                _selectedSlot = i;
            }
        }

        MouseState mouse = Mouse.GetState();
        int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (wheelDelta != 0)
        {
            int direction = wheelDelta > 0 ? -1 : 1;
            _selectedSlot = (_selectedSlot + direction + HotbarEntries.Length) % HotbarEntries.Length;
        }
    }

    private void HandleBlockInteraction()
    {
        MouseState mouse = Mouse.GetState();
        HotbarEntry selected = HotbarEntries[_selectedSlot];

        if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
        {
            TriggerUseAnimation();
        }

        if (mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released)
        {
            TriggerUseAnimation();
        }

        if (TryRaycastBlock(out BlockRayHit rayHit))
        {
            if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            {
                if (selected.Kind == HotbarItemKind.Tool)
                {
                    UseTool(selected.Tool, rayHit.Hit);
                }
                else
                {
                    BreakBlock(rayHit.Hit);
                }
            }

            if (mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released && selected.Kind == HotbarItemKind.Block)
            {
                PlaceBlock(rayHit.Place, selected.Block);
            }
        }
    }

    private void UseTool(ToolType tool, Point3 hitBlock)
    {
        switch (tool)
        {
            case ToolType.Pickaxe:
                if (_world.GetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z) == BlockType.Stone)
                {
                    BreakBlock(hitBlock);
                }
                break;
            case ToolType.Axe:
                if (_world.GetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z) == BlockType.Trunk)
                {
                    BreakBlock(hitBlock);
                }
                break;
            case ToolType.Sword:
                break;
        }
    }

    private bool TryRaycastBlock(out BlockRayHit rayHit)
    {
        Vector3 origin = _player.CameraPosition;
        Vector3 direction = Vector3.Normalize(_player.GetLookDirection());
        Point3 previous = default;
        bool hasPrevious = false;

        for (float distance = 0f; distance <= ReachDistance; distance += 0.1f)
        {
            Vector3 sample = origin + direction * distance;
            Point3 current = new(
                (int)MathF.Floor(sample.X),
                (int)MathF.Floor(sample.Y),
                (int)MathF.Floor(sample.Z));

            if (hasPrevious && current.Equals(previous))
            {
                continue;
            }

            if (_world.IsSolid(current.X, current.Y, current.Z))
            {
                rayHit = new BlockRayHit(current, previous);
                return hasPrevious;
            }

            previous = current;
            hasPrevious = true;
        }

        rayHit = default;
        return false;
    }

    private void BreakBlock(Point3 hitBlock)
    {
        BlockType block = _world.GetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z);
        if (block == BlockType.Air)
        {
            return;
        }

        if (_world.SetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z, BlockType.Air))
        {
            if (_inventory.ContainsKey(block))
            {
                _inventory[block]++;
            }

            MarkAffectedChunksDirty(hitBlock.X, hitBlock.Z);
        }
    }

    private void PlaceBlock(Point3 placeBlock, BlockType block)
    {
        if (!_inventory.TryGetValue(block, out int count) || count <= 0)
        {
            return;
        }

        if (_world.IsSolid(placeBlock.X, placeBlock.Y, placeBlock.Z))
        {
            return;
        }

        BoundingBox blockBounds = new(
            new Vector3(placeBlock.X, placeBlock.Y, placeBlock.Z),
            new Vector3(placeBlock.X + 1, placeBlock.Y + 1, placeBlock.Z + 1));

        if (blockBounds.Intersects(_player.Bounds))
        {
            return;
        }

        if (_world.SetBlock(placeBlock.X, placeBlock.Y, placeBlock.Z, block))
        {
            _inventory[block]--;
            MarkAffectedChunksDirty(placeBlock.X, placeBlock.Z);
        }
    }

    private void MarkAffectedChunksDirty(int worldX, int worldZ)
    {
        Point chunk = _world.GetChunkCoordinate(worldX, worldZ);
        _renderer.MarkChunkDirty(chunk);

        int localX = ((worldX % InfiniteWorld.ChunkSize) + InfiniteWorld.ChunkSize) % InfiniteWorld.ChunkSize;
        int localZ = ((worldZ % InfiniteWorld.ChunkSize) + InfiniteWorld.ChunkSize) % InfiniteWorld.ChunkSize;

        if (localX == 0)
        {
            _renderer.MarkChunkDirty(new Point(chunk.X - 1, chunk.Y));
        }
        else if (localX == InfiniteWorld.ChunkSize - 1)
        {
            _renderer.MarkChunkDirty(new Point(chunk.X + 1, chunk.Y));
        }

        if (localZ == 0)
        {
            _renderer.MarkChunkDirty(new Point(chunk.X, chunk.Y - 1));
        }
        else if (localZ == InfiniteWorld.ChunkSize - 1)
        {
            _renderer.MarkChunkDirty(new Point(chunk.X, chunk.Y + 1));
        }
    }

    private void DrawHotbar()
    {
        Viewport viewport = GraphicsDevice.Viewport;
        int slotSize = 64;
        int gap = 8;
        int totalWidth = HotbarEntries.Length * slotSize + (HotbarEntries.Length - 1) * gap;
        int startX = (viewport.Width - totalWidth) / 2;
        int y = viewport.Height - slotSize - 24;

        for (int i = 0; i < HotbarEntries.Length; i++)
        {
            Rectangle slotRect = new(startX + i * (slotSize + gap), y, slotSize, slotSize);
            bool selected = i == _selectedSlot;
            Color bg = selected ? new Color(42, 42, 42, 220) : new Color(20, 20, 20, 180);
            Color border = selected ? new Color(255, 226, 128) : new Color(180, 180, 180);

            _spriteBatch.Draw(_pixelTexture, slotRect, bg);
            DrawBorder(slotRect, border);

            HotbarEntry entry = HotbarEntries[i];
            Texture2D icon = entry.Kind == HotbarItemKind.Tool ? _toolIcons[entry.Tool] : _blockIcons[entry.Block];
            Rectangle iconRect = new(slotRect.X + 10, slotRect.Y + 8, slotSize - 20, slotSize - 20);
            _spriteBatch.Draw(icon, iconRect, Color.White);

            if (entry.Kind == HotbarItemKind.Block)
            {
                string count = _inventory.TryGetValue(entry.Block, out int amount) ? amount.ToString() : "0";
                _spriteBatch.DrawString(_debugFont, count, new Vector2(slotRect.X + 8, slotRect.Bottom - 24), Color.White);
            }
            else
            {
                _spriteBatch.DrawString(_debugFont, (i + 1).ToString(), new Vector2(slotRect.X + 8, slotRect.Bottom - 24), Color.White);
            }
        }
    }

    private void DrawHeldItem()
    {
        HotbarEntry entry = HotbarEntries[_selectedSlot];
        Texture2D texture = entry.Kind == HotbarItemKind.Tool ? _toolIcons[entry.Tool] : _blockIcons[entry.Block];

        Viewport viewport = GraphicsDevice.Viewport;
        float swingProgress = 1f - (_useAnimationTimer / UseAnimationDuration);
        swingProgress = Math.Clamp(swingProgress, 0f, 1f);
        float swing = MathF.Sin(swingProgress * MathHelper.Pi);

        Vector2 origin = new(texture.Width * 0.15f, texture.Height * 0.85f);
        Vector2 position = new(
            viewport.Width - 140f + swing * 26f,
            viewport.Height - 10f + swing * 20f);

        float rotation = 0.65f + swing * 0.85f;
        float scale = entry.Kind == HotbarItemKind.Tool ? 5f : 4f;

        _spriteBatch.Draw(
            texture,
            position,
            null,
            Color.White,
            rotation,
            origin,
            scale,
            SpriteEffects.None,
            0f);
    }

    private void DrawBorder(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }

    private void UpdateUseAnimation(GameTime gameTime)
    {
        if (_useAnimationTimer > 0f)
        {
            _useAnimationTimer = Math.Max(0f, _useAnimationTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private void TriggerUseAnimation()
    {
        _useAnimationTimer = UseAnimationDuration;
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
