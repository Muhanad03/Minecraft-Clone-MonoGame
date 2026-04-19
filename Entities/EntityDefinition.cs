using System;
using Microsoft.Xna.Framework;

namespace NewProject.Entities;

public sealed record EntityDefinition(
    EntityKind Kind,
    string Name,
    float MoveSpeed,
    float BodyWidth,
    float BodyHeight,
    float BodyLength,
    Color PrimaryColor,
    Color SecondaryColor,
    Color AccentColor)
{
    public static EntityDefinition Get(EntityKind kind)
    {
        return kind switch
        {
            EntityKind.Pig => new EntityDefinition(
                EntityKind.Pig,
                "Pig",
                1.6f,
                0.90f,
                0.72f,
                1.18f,
                new Color(232, 164, 176),
                new Color(214, 138, 154),
                new Color(248, 196, 206)),
            EntityKind.Cow => new EntityDefinition(
                EntityKind.Cow,
                "Cow",
                1.45f,
                1.10f,
                0.95f,
                1.35f,
                new Color(122, 90, 66),
                new Color(58, 42, 34),
                new Color(232, 214, 196)),
            EntityKind.Chicken => new EntityDefinition(
                EntityKind.Chicken,
                "Chicken",
                1.9f,
                0.56f,
                0.62f,
                0.66f,
                new Color(242, 242, 236),
                new Color(214, 214, 208),
                new Color(242, 188, 68)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
