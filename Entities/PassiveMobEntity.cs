using System;
using Microsoft.Xna.Framework;
using NewProject.World;

namespace NewProject.Entities;

public sealed class PassiveMobEntity : EntityBase
{
    private readonly Random _random;
    private Vector2? _wanderTarget;
    private float _decisionTimer;
    private float _idleYawVelocity;

    public PassiveMobEntity(EntityDefinition definition, Vector3 position)
        : base(definition, position)
    {
        _random = new Random(Hash(definition.Kind, position));
        _decisionTimer = NextRange(0.4f, 1.6f);
        Yaw = NextRange(-MathHelper.Pi, MathHelper.Pi);
    }

    public override void Update(GameTime gameTime, EntityUpdateContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _decisionTimer -= dt;

        Vector2 current = new(Position.X, Position.Z);
        Vector3 snappedPosition = Position;
        SnapToGround(context.World, current, ref snappedPosition);
        Position = snappedPosition;

        if (_wanderTarget is null)
        {
            WalkAnimation = MathF.Max(0f, WalkAnimation - dt * 3f);
            Yaw += _idleYawVelocity * dt;

            if (_decisionTimer <= 0f)
            {
                if (_random.NextDouble() < 0.58)
                {
                    _decisionTimer = NextRange(0.8f, 2.4f);
                    _idleYawVelocity = NextRange(-0.85f, 0.85f);
                }
                else if (TryChooseTarget(context.World, current, out Vector2 target))
                {
                    _wanderTarget = target;
                    _decisionTimer = NextRange(2.8f, 5.8f);
                    _idleYawVelocity = 0f;
                }
                else
                {
                    _decisionTimer = NextRange(1.0f, 2.0f);
                }
            }

            return;
        }

        Vector2 targetDelta = _wanderTarget.Value - current;
        float distance = targetDelta.Length();
        if (distance < 0.16f || _decisionTimer <= 0f)
        {
            _wanderTarget = null;
            _decisionTimer = NextRange(0.35f, 1.2f);
            return;
        }

        Vector2 direction = targetDelta / MathF.Max(distance, 0.0001f);
        Vector2 next = current + direction * Definition.MoveSpeed * dt;
        if (!TryGetGroundPosition(context.World, next, out Vector3 groundedPosition))
        {
            _wanderTarget = null;
            _decisionTimer = NextRange(0.5f, 1.4f);
            return;
        }

        float heightDelta = groundedPosition.Y - Position.Y;
        if (MathF.Abs(heightDelta) > 1.1f)
        {
            _wanderTarget = null;
            _decisionTimer = NextRange(0.5f, 1.4f);
            return;
        }

        Position = new Vector3(next.X, MathHelper.Lerp(Position.Y, groundedPosition.Y, MathHelper.Clamp(dt * 10f, 0f, 1f)), next.Y);
        Yaw = MathF.Atan2(direction.X, direction.Y);
        WalkAnimation += Definition.MoveSpeed * dt * 5.5f;
    }

    private bool TryChooseTarget(InfiniteWorld world, Vector2 current, out Vector2 target)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = NextRange(0f, MathHelper.TwoPi);
            float distance = NextRange(2.4f, 6.8f);
            Vector2 candidate = current + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

            if (!TryGetGroundPosition(world, candidate, out _))
            {
                continue;
            }

            target = candidate;
            return true;
        }

        target = default;
        return false;
    }

    private static void SnapToGround(InfiniteWorld world, Vector2 xz, ref Vector3 position)
    {
        if (!TryGetGroundPosition(world, xz, out Vector3 grounded))
        {
            return;
        }

        position = new Vector3(position.X, MathHelper.Lerp(position.Y, grounded.Y, 0.4f), position.Z);
    }

    private static bool TryGetGroundPosition(InfiniteWorld world, Vector2 xz, out Vector3 position)
    {
        int blockX = (int)MathF.Floor(xz.X);
        int blockZ = (int)MathF.Floor(xz.Y);
        int surfaceY = world.GetSurfaceHeight(blockX, blockZ);
        int groundY = surfaceY - 1;
        BlockType top = world.GetBlock(blockX, groundY, blockZ);

        if (top == BlockType.Water || top == BlockType.Air || top == BlockType.Torch)
        {
            position = default;
            return false;
        }

        position = new Vector3(xz.X, surfaceY + 0.001f, xz.Y);
        return true;
    }

    private float NextRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }

    private static int Hash(EntityKind kind, Vector3 position)
    {
        unchecked
        {
            int value = (int)kind * 397;
            value = (value * 397) ^ (int)MathF.Round(position.X * 10f);
            value = (value * 397) ^ (int)MathF.Round(position.Z * 10f);
            return value;
        }
    }
}
