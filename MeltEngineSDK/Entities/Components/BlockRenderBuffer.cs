using System;
using System.Numerics;
using MeltEngine.Utils;

namespace MeltEngine.Entities.Components;

public struct BlockRenderData
{
    public Matrix4x4 Transform;
    public byte BlockType;
    public byte Padding1;
    public byte Padding2;
    public byte Padding3;
    
    public BlockRenderData(Matrix4x4 transform, BlockType type)
    {
        Transform = transform;
        BlockType = (byte)type;
        Padding1 = 0;
        Padding2 = 0;
        Padding3 = 0;
    }
}

public class TypedRenderBuffer<T> where T : struct
{
    private T[] _data;
    private int _count;
    private readonly int _initialCapacity;
    private readonly int _maxCapacity;
    private bool _overflow;

    public TypedRenderBuffer(int initialCapacity, int maxCapacity)
    {
        _initialCapacity = initialCapacity;
        _maxCapacity = maxCapacity;
        _data = new T[initialCapacity];
    }

    public int Count => _count;
    public bool HasOverflow => _overflow;
    public Span<T> Data => new(_data, 0, _count);
    
    public T[] GetArray()
    {
        return _data;
    }

    public void Reset()
    {
        _count = 0;
        _overflow = false;
    }

    public bool Add(T item)
    {
        if (_count >= _maxCapacity)
        {
            _overflow = true;
            return false;
        }

        if (_count >= _data.Length)
        {
            int newSize = Math.Min(_data.Length * 2, _maxCapacity);
            if (newSize <= _data.Length) return false;
            Array.Resize(ref _data, newSize);
        }

        _data[_count++] = item;
        return true;
    }

    public T[] ToArray()
    {
        if (_count == _data.Length) return _data;
        var result = new T[_count];
        Array.Copy(_data, result, _count);
        return result;
    }
}

public class BlockRenderBuffers
{
    public TypedRenderBuffer<Matrix4x4> Grass { get; }
    public TypedRenderBuffer<Matrix4x4> Dirt { get; }
    public TypedRenderBuffer<Matrix4x4> Stone { get; }
    public TypedRenderBuffer<Matrix4x4> Sand { get; }
    public TypedRenderBuffer<Matrix4x4> Water { get; }
    public TypedRenderBuffer<Matrix4x4> Wood { get; }
    public TypedRenderBuffer<Matrix4x4> Leaves { get; }
    public TypedRenderBuffer<Matrix4x4> Brick { get; }
    public TypedRenderBuffer<Matrix4x4> Bedrock { get; }
    public TypedRenderBuffer<Matrix4x4> PhysicsAwake { get; }
    public TypedRenderBuffer<Matrix4x4> PhysicsSleep { get; }
    public TypedRenderBuffer<Matrix4x4> Bullets { get; }

    public BlockRenderBuffers()
    {
        Grass = new TypedRenderBuffer<Matrix4x4>(8192, 32768);
        Dirt = new TypedRenderBuffer<Matrix4x4>(8192, 32768);
        Stone = new TypedRenderBuffer<Matrix4x4>(8192, 32768);
        Sand = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        Water = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        Wood = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        Leaves = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        Brick = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        Bedrock = new TypedRenderBuffer<Matrix4x4>(2048, 8192);
        PhysicsAwake = new TypedRenderBuffer<Matrix4x4>(1024, 4096);
        PhysicsSleep = new TypedRenderBuffer<Matrix4x4>(1024, 4096);
        Bullets = new TypedRenderBuffer<Matrix4x4>(256, 512);
    }

    public void ResetAll()
    {
        Grass.Reset();
        Dirt.Reset();
        Stone.Reset();
        Sand.Reset();
        Water.Reset();
        Wood.Reset();
        Leaves.Reset();
        Brick.Reset();
        Bedrock.Reset();
        PhysicsAwake.Reset();
        PhysicsSleep.Reset();
        Bullets.Reset();
    }

    public (int grass, int dirt, int stone, int sand, int water, int wood, int leaves, int brick, int bedrock, int physics, int bullets) GetCounts()
    {
        int physics = PhysicsAwake.Count + PhysicsSleep.Count;
        int bullets = Bullets.Count;
        return (Grass.Count, Dirt.Count, Stone.Count, Sand.Count, Water.Count, Wood.Count, Leaves.Count, Brick.Count, Bedrock.Count, physics, bullets);
    }
}
