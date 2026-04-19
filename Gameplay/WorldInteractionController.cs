using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NewProject.Player;
using NewProject.Rendering;
using NewProject.World;

namespace NewProject.Gameplay;

public sealed class WorldInteractionController
{
    public const float ReachDistance = 7f;
    public const float UseAnimationDuration = 0.18f;

    private readonly InfiniteWorld _world;
    private readonly PlayerController _player;
    private readonly VoxelWorldRenderer _renderer;
    private readonly HotbarState _hotbar;

    private readonly record struct Point3(int X, int Y, int Z);
    private readonly record struct BlockRayHit(Point3 Hit, Point3 Place);

    public WorldInteractionController(InfiniteWorld world, PlayerController player, VoxelWorldRenderer renderer, HotbarState hotbar)
    {
        _world = world;
        _player = player;
        _renderer = renderer;
        _hotbar = hotbar;
    }

    public float UseAnimationTimer { get; private set; }
    public ToolType? LastAnimatedTool { get; private set; }

    public void Update(GameTime gameTime)
    {
        if (UseAnimationTimer > 0f)
        {
            UseAnimationTimer = Math.Max(0f, UseAnimationTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    public void HandleInput(MouseState mouse, MouseState previousMouse)
    {
        HotbarEntry selected = _hotbar.SelectedEntry;

        if (IsPressed(mouse.LeftButton, previousMouse.LeftButton) || IsPressed(mouse.RightButton, previousMouse.RightButton))
        {
            TriggerUseAnimation(selected.Kind == HotbarItemKind.Tool ? selected.Tool : null);
        }

        if (!TryRaycastBlock(out BlockRayHit rayHit))
        {
            return;
        }

        if (IsPressed(mouse.LeftButton, previousMouse.LeftButton))
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

        if (IsPressed(mouse.RightButton, previousMouse.RightButton) && selected.Kind == HotbarItemKind.Block)
        {
            PlaceBlock(rayHit.Place, selected.Block);
        }
    }

    private static bool IsPressed(ButtonState current, ButtonState previous) =>
        current == ButtonState.Pressed && previous == ButtonState.Released;

    private void UseTool(ToolType tool, Point3 hitBlock)
    {
        BlockType target = _world.GetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z);
        switch (tool)
        {
            case ToolType.Pickaxe when target == BlockType.Stone:
            case ToolType.Axe when target == BlockType.Trunk:
                BreakBlock(hitBlock);
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

            BlockType target = _world.GetBlock(current.X, current.Y, current.Z);
            if (target != BlockType.Air && target != BlockType.Water)
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

        BlockType replacement = CanFloodFromWater(hitBlock) ? BlockType.Water : BlockType.Air;
        if (_world.SetBlock(hitBlock.X, hitBlock.Y, hitBlock.Z, replacement))
        {
            _hotbar.AddBlock(block);
            MarkAffectedChunksDirty(hitBlock.X, hitBlock.Z);
        }
    }

    private void PlaceBlock(Point3 placeBlock, BlockType block)
    {
        if (!_hotbar.HasBlock(block))
        {
            return;
        }

        if (_world.GetBlock(placeBlock.X, placeBlock.Y, placeBlock.Z) != BlockType.Air &&
            _world.GetBlock(placeBlock.X, placeBlock.Y, placeBlock.Z) != BlockType.Water)
        {
            return;
        }

        if (block == BlockType.Torch && !_world.IsSolid(placeBlock.X, placeBlock.Y - 1, placeBlock.Z))
        {
            return;
        }

        if (block == BlockType.Torch && _world.GetBlock(placeBlock.X, placeBlock.Y, placeBlock.Z) == BlockType.Water)
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

        if (_world.SetBlock(placeBlock.X, placeBlock.Y, placeBlock.Z, block) && _hotbar.TryConsumeBlock(block))
        {
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

    private void TriggerUseAnimation(ToolType? tool)
    {
        LastAnimatedTool = tool;
        UseAnimationTimer = UseAnimationDuration;
    }

    private bool CanFloodFromWater(Point3 start)
    {
        Queue<Point3> open = new();
        HashSet<Point3> visited = new();
        open.Enqueue(start);
        visited.Add(start);

        const int maxVisited = 160;
        const int maxHorizontalDistance = 6;
        const int maxVerticalDistance = 8;

        while (open.Count > 0 && visited.Count <= maxVisited)
        {
            Point3 current = open.Dequeue();

            foreach (Point3 next in EnumerateNeighbors(current))
            {
                if (visited.Contains(next))
                {
                    continue;
                }

                if (Math.Abs(next.X - start.X) > maxHorizontalDistance ||
                    Math.Abs(next.Z - start.Z) > maxHorizontalDistance ||
                    Math.Abs(next.Y - start.Y) > maxVerticalDistance)
                {
                    continue;
                }

                BlockType block = _world.GetBlock(next.X, next.Y, next.Z);
                if (block == BlockType.Water)
                {
                    return true;
                }

                if (block != BlockType.Air)
                {
                    continue;
                }

                visited.Add(next);
                open.Enqueue(next);
            }
        }

        return false;
    }

    private static IEnumerable<Point3> EnumerateNeighbors(Point3 point)
    {
        yield return new Point3(point.X + 1, point.Y, point.Z);
        yield return new Point3(point.X - 1, point.Y, point.Z);
        yield return new Point3(point.X, point.Y + 1, point.Z);
        yield return new Point3(point.X, point.Y - 1, point.Z);
        yield return new Point3(point.X, point.Y, point.Z + 1);
        yield return new Point3(point.X, point.Y, point.Z - 1);
    }
}
