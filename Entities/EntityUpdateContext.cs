using Microsoft.Xna.Framework;
using NewProject.World;

namespace NewProject.Entities;

public readonly record struct EntityUpdateContext(InfiniteWorld World, Vector3 PlayerPosition);
