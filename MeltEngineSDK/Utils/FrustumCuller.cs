using System;
using System.Numerics;

namespace MeltEngine.Utils;

public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public bool Intersects(BoundingBox other)
    {
        if (Max.X < other.Min.X || Min.X > other.Max.X) return false;
        if (Max.Y < other.Min.Y || Min.Y > other.Max.Y) return false;
        if (Max.Z < other.Min.Z || Min.Z > other.Max.Z) return false;
        return true;
    }

    public static BoundingBox FromCenterSize(Vector3 center, Vector3 size)
    {
        Vector3 half = size / 2;
        return new BoundingBox(center - half, center + half);
    }
}

public struct FrustumPlanes
{
    public Vector4 Left;
    public Vector4 Right;
    public Vector4 Bottom;
    public Vector4 Top;
    public Vector4 Near;
    public Vector4 Far;

    public static FrustumPlanes FromCamera(Matrix4x4 view, Matrix4x4 projection)
    {
        Matrix4x4.Invert(view * projection, out var invVP);
        
        var planes = new FrustumPlanes();

        planes.Left = new Vector4(
            invVP.M14 + invVP.M11,
            invVP.M24 + invVP.M21,
            invVP.M34 + invVP.M31,
            invVP.M44 + invVP.M41
        );
        planes.Right = new Vector4(
            invVP.M14 - invVP.M11,
            invVP.M24 - invVP.M21,
            invVP.M34 - invVP.M31,
            invVP.M44 - invVP.M41
        );
        planes.Bottom = new Vector4(
            invVP.M14 + invVP.M12,
            invVP.M24 + invVP.M22,
            invVP.M34 + invVP.M32,
            invVP.M44 + invVP.M42
        );
        planes.Top = new Vector4(
            invVP.M14 - invVP.M12,
            invVP.M24 - invVP.M22,
            invVP.M34 - invVP.M32,
            invVP.M44 - invVP.M42
        );
        planes.Near = new Vector4(
            invVP.M14 + invVP.M13,
            invVP.M24 + invVP.M23,
            invVP.M34 + invVP.M33,
            invVP.M44 + invVP.M43
        );
        planes.Far = new Vector4(
            invVP.M14 - invVP.M13,
            invVP.M24 - invVP.M23,
            invVP.M34 - invVP.M33,
            invVP.M44 - invVP.M43
        );

        NormalizePlane(ref planes.Left);
        NormalizePlane(ref planes.Right);
        NormalizePlane(ref planes.Bottom);
        NormalizePlane(ref planes.Top);
        NormalizePlane(ref planes.Near);
        NormalizePlane(ref planes.Far);

        return planes;
    }

    private static void NormalizePlane(ref Vector4 plane)
    {
        float length = MathF.Sqrt(plane.X * plane.X + plane.Y * plane.Y + plane.Z * plane.Z);
        plane.X /= length;
        plane.Y /= length;
        plane.Z /= length;
        plane.W /= length;
    }

    public bool Intersects(BoundingBox box)
    {
        Vector3 center = (box.Min + box.Max) / 2;
        Vector3 extent = (box.Max - box.Min) / 2;

        if (!IntersectsPlane(Left, center, extent)) return false;
        if (!IntersectsPlane(Right, center, extent)) return false;
        if (!IntersectsPlane(Bottom, center, extent)) return false;
        if (!IntersectsPlane(Top, center, extent)) return false;
        if (!IntersectsPlane(Near, center, extent)) return false;
        if (!IntersectsPlane(Far, center, extent)) return false;

        return true;
    }

    private static bool IntersectsPlane(Vector4 plane, Vector3 center, Vector3 extent)
    {
        float dot = plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z;
        float dist = MathF.Abs(extent.X * plane.X) + MathF.Abs(extent.Y * plane.Y) + MathF.Abs(extent.Z * plane.Z);
        return dot + dist + plane.W > 0;
    }
}
