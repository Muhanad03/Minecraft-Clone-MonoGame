using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NewProject.World;

namespace NewProject.Entities;

public sealed class EntitySystem
{
    private const int SpawnChunkRadius = 6;
    private const float DespawnDistance = 108f;
    private const int MaxEntities = 64;
    private const int TargetNearbyEntities = 18;

    private readonly List<EntityBase> _entities = new();
    private readonly HashSet<Point> _spawnedChunks = new();

    public IReadOnlyList<EntityBase> Entities => _entities;

    public void Update(GameTime gameTime, InfiniteWorld world, Vector3 playerPosition)
    {
        SyncChunkSpawns(world, playerPosition);

        EntityUpdateContext context = new(world, playerPosition);
        foreach (EntityBase entity in _entities)
        {
            entity.Update(gameTime, context);
        }

        float despawnDistanceSquared = DespawnDistance * DespawnDistance;
        _entities.RemoveAll(entity => Vector2.DistanceSquared(
            new Vector2(entity.Position.X, entity.Position.Z),
            new Vector2(playerPosition.X, playerPosition.Z)) > despawnDistanceSquared);
    }

    private void SyncChunkSpawns(InfiniteWorld world, Vector3 playerPosition)
    {
        Point centerChunk = world.GetChunkCoordinate(playerPosition);
        HashSet<Point> activeChunks = new();
        List<WorldChunk> nearbyChunks = new();

        foreach (WorldChunk chunk in world.GetLoadedChunksSnapshot())
        {
            if (Math.Abs(chunk.ChunkX - centerChunk.X) > SpawnChunkRadius || Math.Abs(chunk.ChunkZ - centerChunk.Y) > SpawnChunkRadius)
            {
                continue;
            }

            activeChunks.Add(chunk.Key);
            nearbyChunks.Add(chunk);
            if (_spawnedChunks.Contains(chunk.Key) || _entities.Count >= MaxEntities)
            {
                continue;
            }

            SpawnChunkEntities(world, chunk, playerPosition, guaranteed: false);
            _spawnedChunks.Add(chunk.Key);
        }

        _spawnedChunks.RemoveWhere(key => !activeChunks.Contains(key));

        if (_entities.Count < TargetNearbyEntities)
        {
            nearbyChunks.Sort((a, b) =>
            {
                int da = Math.Abs(a.ChunkX - centerChunk.X) + Math.Abs(a.ChunkZ - centerChunk.Y);
                int db = Math.Abs(b.ChunkX - centerChunk.X) + Math.Abs(b.ChunkZ - centerChunk.Y);
                return da.CompareTo(db);
            });

            foreach (WorldChunk chunk in nearbyChunks)
            {
                if (_entities.Count >= TargetNearbyEntities)
                {
                    break;
                }

                SpawnChunkEntities(world, chunk, playerPosition, guaranteed: true);
            }
        }
    }

    private void SpawnChunkEntities(InfiniteWorld world, WorldChunk chunk, Vector3 playerPosition, bool guaranteed)
    {
        int hash = Hash(chunk.ChunkX, chunk.ChunkZ, 1847);
        if (!guaranteed && hash % 100 > 55)
        {
            return;
        }

        int spawnsWanted = guaranteed ? 1 + ((hash >> 3) % 2) : 1 + (hash >> 3) % 2;
        int positionAttempts = guaranteed ? 12 : 4;
        int spawned = 0;

        for (int i = 0; i < positionAttempts && _entities.Count < MaxEntities && spawned < spawnsWanted; i++)
        {
            int sampleHash = Hash(chunk.ChunkX * 31 + i, chunk.ChunkZ * 31 + i * 7, hash + 97);
            int localX = 1 + (sampleHash % (InfiniteWorld.ChunkSize - 2));
            int localZ = 1 + ((sampleHash >> 5) % (InfiniteWorld.ChunkSize - 2));
            int worldX = chunk.ChunkX * InfiniteWorld.ChunkSize + localX;
            int worldZ = chunk.ChunkZ * InfiniteWorld.ChunkSize + localZ;
            int surfaceY = world.GetSurfaceHeight(worldX, worldZ);
            int groundY = surfaceY - 1;
            BlockType block = world.GetBlock(worldX, groundY, worldZ);

            if (block != BlockType.Grass && block != BlockType.Dirt)
            {
                continue;
            }

            Vector3 spawnPosition = new(worldX + 0.5f, surfaceY + 0.001f, worldZ + 0.5f);
            if (Vector3.DistanceSquared(spawnPosition, playerPosition) < 7f * 7f)
            {
                continue;
            }

            if (HasNearbyEntity(spawnPosition, 4.5f))
            {
                continue;
            }

            EntityKind kind = (EntityKind)((sampleHash + i) % 3);
            EntityDefinition definition = EntityDefinition.Get(kind);
            _entities.Add(new PassiveMobEntity(definition, spawnPosition));
            spawned++;
        }
    }

    private bool HasNearbyEntity(Vector3 position, float radius)
    {
        float radiusSquared = radius * radius;
        foreach (EntityBase entity in _entities)
        {
            if (Vector3.DistanceSquared(entity.Position, position) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static int Hash(int x, int z, int seed)
    {
        unchecked
        {
            int value = seed;
            value = (value * 397) ^ x;
            value = (value * 397) ^ z;
            value ^= value >> 13;
            value *= 1274126177;
            value ^= value >> 16;
            return value & int.MaxValue;
        }
    }
}
