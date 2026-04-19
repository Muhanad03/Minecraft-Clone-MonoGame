using Microsoft.Xna.Framework;

namespace NewProject.Entities;

public abstract class EntityBase
{
    private static int _nextId = 1;

    protected EntityBase(EntityDefinition definition, Vector3 position)
    {
        Id = _nextId++;
        Definition = definition;
        Position = position;
    }

    public int Id { get; }

    public EntityDefinition Definition { get; }

    public EntityKind Kind => Definition.Kind;

    public Vector3 Position { get; protected set; }

    public float Yaw { get; protected set; }

    public float WalkAnimation { get; protected set; }

    public virtual float EyeHeight => Definition.BodyHeight * 0.7f;

    public abstract void Update(GameTime gameTime, EntityUpdateContext context);
}
