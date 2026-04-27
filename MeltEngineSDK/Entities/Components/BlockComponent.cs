using System;
using System.Collections.Generic;
using System.Numerics;
using MeltEngine.Entities.Components;

namespace MeltEngine.Utils;

public enum BlockType : byte
{
    Air = 0,
    Grass = 1,
    Dirt = 2,
    Stone = 3,
    Water = 4,
    Sand = 5,
    Wood = 6,
    Leaves = 7,
    Bedrock = 8,
    Brick = 9
}

public struct BlockTypeComponent
{
    public BlockType Type;
    public Vector3 Color;
    public float TextureScale;

    public BlockTypeComponent(BlockType type)
    {
        Type = type;
        (Color, TextureScale) = type switch
        {
            BlockType.Grass => (new Vector3(0.3f, 0.7f, 0.2f), 1f),
            BlockType.Dirt => (new Vector3(0.55f, 0.35f, 0.2f), 1f),
            BlockType.Stone => (new Vector3(0.5f, 0.5f, 0.5f), 1f),
            BlockType.Water => (new Vector3(0.2f, 0.4f, 0.8f), 1f),
            BlockType.Sand => (new Vector3(0.85f, 0.75f, 0.5f), 1f),
            BlockType.Wood => (new Vector3(0.4f, 0.3f, 0.15f), 1f),
            BlockType.Leaves => (new Vector3(0.2f, 0.6f, 0.15f), 1f),
            BlockType.Bedrock => (new Vector3(0.2f, 0.2f, 0.2f), 1f),
            BlockType.Brick => (new Vector3(0.7f, 0.3f, 0.25f), 1f),
            _ => (Vector3.One, 1f)
        };
    }
}
