using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NewProject.Entities;

namespace NewProject.Rendering;

public sealed class EntityRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private readonly RasterizerState _rasterizerState;

    public EntityRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };
        _rasterizerState = new RasterizerState
        {
            CullMode = CullMode.None
        };
    }

    public void Draw(IReadOnlyList<EntityBase> entities, Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        if (entities.Count == 0)
        {
            return;
        }

        List<VoxelVertex> vertices = new();
        List<int> indices = new();

        foreach (EntityBase entity in entities)
        {
            if (Vector3.DistanceSquared(entity.Position, cameraPosition) > 96f * 96f)
            {
                continue;
            }

            switch (entity.Kind)
            {
                case EntityKind.Pig:
                    AddPig(vertices, indices, entity);
                    break;
                case EntityKind.Cow:
                    AddCow(vertices, indices, entity);
                    break;
                case EntityKind.Chicken:
                    AddChicken(vertices, indices, entity);
                    break;
            }
        }

        if (vertices.Count == 0)
        {
            return;
        }

        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.BlendState = BlendState.Opaque;
        _effect.World = Matrix.Identity;
        _effect.View = view;
        _effect.Projection = projection;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices.ToArray(),
                0,
                vertices.Count,
                indices.ToArray(),
                0,
                indices.Count / 3,
                VoxelVertex.VertexDeclaration);
        }
    }

    public void Dispose()
    {
        _effect.Dispose();
        _rasterizerState.Dispose();
    }

    private static void AddPig(List<VoxelVertex> vertices, List<int> indices, EntityBase entity)
    {
        EntityDefinition def = entity.Definition;
        BuildQuadruped(
            vertices,
            indices,
            entity,
            def.PrimaryColor,
            def.SecondaryColor,
            def.AccentColor,
            new Vector3(def.BodyWidth, def.BodyHeight, def.BodyLength),
            new Vector3(0.66f, 0.56f, 0.52f),
            0.32f,
            0.36f,
            0.16f,
            0.20f);
    }

    private static void AddCow(List<VoxelVertex> vertices, List<int> indices, EntityBase entity)
    {
        EntityDefinition def = entity.Definition;
        BuildQuadruped(
            vertices,
            indices,
            entity,
            def.PrimaryColor,
            def.SecondaryColor,
            def.AccentColor,
            new Vector3(def.BodyWidth, def.BodyHeight, def.BodyLength),
            new Vector3(0.72f, 0.66f, 0.56f),
            0.44f,
            0.46f,
            0.18f,
            0.24f);
    }

    private static void AddChicken(List<VoxelVertex> vertices, List<int> indices, EntityBase entity)
    {
        EntityDefinition def = entity.Definition;
        GetBasis(entity.Yaw, out Vector3 right, out Vector3 forward);
        Vector3 up = Vector3.Up;
        Vector3 bodyCenter = entity.Position + up * 0.44f;
        float flap = MathF.Sin(entity.WalkAnimation * 1.2f) * 0.04f;

        AddOrientedBox(vertices, indices, bodyCenter, right, up, forward, new Vector3(0.42f, 0.46f, 0.48f), def.PrimaryColor, def.SecondaryColor);
        AddOrientedBox(vertices, indices, bodyCenter + forward * 0.33f + up * 0.16f, right, up, forward, new Vector3(0.34f, 0.34f, 0.32f), def.PrimaryColor, def.SecondaryColor);
        AddOrientedBox(vertices, indices, bodyCenter + forward * 0.53f + up * 0.08f, right, up, forward, new Vector3(0.16f, 0.10f, 0.16f), def.AccentColor, ScaleColor(def.AccentColor, 0.92f));
        AddOrientedBox(vertices, indices, bodyCenter + up * 0.42f, right, up, forward, new Vector3(0.12f, 0.20f, 0.12f), new Color(196, 32, 28), new Color(170, 24, 22));
        AddOrientedBox(vertices, indices, bodyCenter + right * 0.28f + up * 0.04f, right, up, forward, new Vector3(0.08f, 0.24f + flap, 0.30f), def.SecondaryColor, ScaleColor(def.SecondaryColor, 0.94f));
        AddOrientedBox(vertices, indices, bodyCenter - right * 0.28f + up * 0.04f, right, up, forward, new Vector3(0.08f, 0.24f + flap, 0.30f), def.SecondaryColor, ScaleColor(def.SecondaryColor, 0.94f));

        AddLegPair(vertices, indices, entity.Position, right, up, forward, 0.12f, 0.10f, 0.16f, 0.30f, def.AccentColor);
    }

    private static void BuildQuadruped(
        List<VoxelVertex> vertices,
        List<int> indices,
        EntityBase entity,
        Color bodyColor,
        Color sideColor,
        Color accentColor,
        Vector3 bodySize,
        Vector3 headSize,
        float bodyHeightOffset,
        float headHeightOffset,
        float legWidth,
        float legHeight)
    {
        GetBasis(entity.Yaw, out Vector3 right, out Vector3 forward);
        Vector3 up = Vector3.Up;
        Vector3 basePos = entity.Position;
        Vector3 bodyCenter = basePos + up * (bodyHeightOffset + bodySize.Y * 0.5f);
        Vector3 headCenter = bodyCenter + forward * (bodySize.Z * 0.52f + headSize.Z * 0.33f) + up * (headHeightOffset - bodyHeightOffset);
        float stride = MathF.Sin(entity.WalkAnimation) * 0.08f;

        AddOrientedBox(vertices, indices, bodyCenter, right, up, forward, bodySize, bodyColor, sideColor);
        AddOrientedBox(vertices, indices, headCenter, right, up, forward, headSize, bodyColor, sideColor);
        AddOrientedBox(vertices, indices, headCenter + forward * (headSize.Z * 0.45f), right, up, forward, new Vector3(headSize.X * 0.52f, headSize.Y * 0.34f, headSize.Z * 0.26f), accentColor, ScaleColor(accentColor, 0.9f));

        AddLeg(vertices, indices, basePos + right * (bodySize.X * 0.33f) + forward * (bodySize.Z * 0.28f) + forward * stride, right, up, forward, legWidth, legHeight, sideColor);
        AddLeg(vertices, indices, basePos - right * (bodySize.X * 0.33f) + forward * (bodySize.Z * 0.28f) - forward * stride, right, up, forward, legWidth, legHeight, sideColor);
        AddLeg(vertices, indices, basePos + right * (bodySize.X * 0.33f) - forward * (bodySize.Z * 0.28f) - forward * stride, right, up, forward, legWidth, legHeight, sideColor);
        AddLeg(vertices, indices, basePos - right * (bodySize.X * 0.33f) - forward * (bodySize.Z * 0.28f) + forward * stride, right, up, forward, legWidth, legHeight, sideColor);
    }

    private static void AddLegPair(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 basePosition,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        float width,
        float depth,
        float sideOffset,
        float height,
        Color color)
    {
        AddOrientedBox(vertices, indices, basePosition + right * sideOffset + up * (height * 0.5f), right, up, forward, new Vector3(width, height, depth), color, ScaleColor(color, 0.9f));
        AddOrientedBox(vertices, indices, basePosition - right * sideOffset + up * (height * 0.5f), right, up, forward, new Vector3(width, height, depth), color, ScaleColor(color, 0.9f));
    }

    private static void AddLeg(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 anchor,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        float width,
        float height,
        Color color)
    {
        Vector3 center = anchor + up * (height * 0.5f);
        AddOrientedBox(vertices, indices, center, right, up, forward, new Vector3(width, height, width), color, ScaleColor(color, 0.88f));
    }

    private static void GetBasis(float yaw, out Vector3 right, out Vector3 forward)
    {
        Matrix rotation = Matrix.CreateRotationY(yaw);
        right = Vector3.Normalize(Vector3.Transform(Vector3.Right, rotation));
        forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, rotation));
    }

    private static void AddOrientedBox(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        Vector3 size,
        Color topColor,
        Color sideColor)
    {
        Vector3 halfRight = right * (size.X * 0.5f);
        Vector3 halfUp = up * (size.Y * 0.5f);
        Vector3 halfForward = forward * (size.Z * 0.5f);

        Vector3 lbf = center - halfRight - halfUp - halfForward;
        Vector3 rbf = center + halfRight - halfUp - halfForward;
        Vector3 rtf = center + halfRight + halfUp - halfForward;
        Vector3 ltf = center - halfRight + halfUp - halfForward;
        Vector3 lbb = center - halfRight - halfUp + halfForward;
        Vector3 rbb = center + halfRight - halfUp + halfForward;
        Vector3 rtb = center + halfRight + halfUp + halfForward;
        Vector3 ltb = center - halfRight + halfUp + halfForward;

        Color top = ScaleColor(topColor, 1.06f);
        Color side = sideColor;
        Color shadow = ScaleColor(sideColor, 0.86f);

        AddFace(vertices, indices, -forward, side, ltf, rtf, rbf, lbf);
        AddFace(vertices, indices, forward, shadow, lbb, rbb, rtb, ltb);
        AddFace(vertices, indices, -right, side, lbf, lbb, ltb, ltf);
        AddFace(vertices, indices, right, side, rbb, rbf, rtf, rtb);
        AddFace(vertices, indices, up, top, ltb, rtb, rtf, ltf);
        AddFace(vertices, indices, -up, shadow, lbf, rbf, rbb, lbb);
    }

    private static void AddFace(
        List<VoxelVertex> vertices,
        List<int> indices,
        Vector3 normal,
        Color color,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(new VoxelVertex(a, normal, color, Vector2.Zero));
        vertices.Add(new VoxelVertex(b, normal, color, Vector2.Zero));
        vertices.Add(new VoxelVertex(c, normal, color, Vector2.Zero));
        vertices.Add(new VoxelVertex(d, normal, color, Vector2.Zero));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)Math.Clamp((int)(color.R * scale), 0, 255),
            (byte)Math.Clamp((int)(color.G * scale), 0, 255),
            (byte)Math.Clamp((int)(color.B * scale), 0, 255),
            color.A);
    }
}
