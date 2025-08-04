using System;

namespace MeltEngine.Entities;

public readonly struct Entity(uint id) : IEquatable<Entity>
{
    public uint Id { get; init; } = id;
    public bool Equals(Entity other) => Id == other.Id;
    public override bool Equals(object obj) => obj is Entity other && Equals(other);
    public override int GetHashCode() => (int)Id;

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }
}