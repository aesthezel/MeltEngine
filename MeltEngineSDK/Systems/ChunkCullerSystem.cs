using System;
using System.Collections.Generic;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Utils;
using Raylib_cs;
using RayBoundingBox = Raylib_cs.BoundingBox;

namespace MeltEngine.Systems;

public class ChunkCullerSystem : ISystem
{
    private readonly Dictionary<(int cx, int cz), Utils.BoundingBox> _chunkBounds = new();
    private readonly HashSet<(int cx, int cz)> _visibleChunks = new();
    private Vector3 _lastCameraPos;
    private Vector3 _lastCameraTarget;
    private bool _needsUpdate = true;
    private const float ChunkWorldWidth = TerrainData.ChunkSizeX;
    private const float ChunkWorldDepth = TerrainData.ChunkSizeZ;
    private const float ChunkWorldHeight = TerrainData.ChunkSizeY;

    public IReadOnlySet<(int cx, int cz)> GetVisibleChunks() => _visibleChunks;

    public bool IsChunkVisible(int cx, int cz)
    {
        return _visibleChunks.Contains((cx, cz));
    }

    public void Update(ECSOperator entityOperator, float frameTime)
    {
        var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();
        if (cameraComponents.Count == 0) return;

        var cameraEntity = cameraComponents.DenseEntities[0];
        var camera = cameraComponents.Components[cameraEntity].Camera;

        var camPos = camera.Position;
        var camTarget = camera.Target;

        if (Vector3.DistanceSquared(camPos, _lastCameraPos) < 4.0f &&
            Vector3.DistanceSquared(camTarget, _lastCameraTarget) < 4.0f &&
            !_needsUpdate)
            return;

        _lastCameraPos = camPos;
        _lastCameraTarget = camTarget;
        _needsUpdate = false;

        UpdateVisibleChunks(camera);
    }

    private void UpdateVisibleChunks(Camera3D camera)
    {
        _visibleChunks.Clear();

        var frustum = ExtractFrustumPlanes(camera);

        int camChunkX = (int)(camera.Target.X >= 0 
            ? camera.Target.X / TerrainData.ChunkSizeX 
            : (camera.Target.X - TerrainData.ChunkSizeX + 1) / TerrainData.ChunkSizeX);
        int camChunkZ = (int)(camera.Target.Z >= 0 
            ? camera.Target.Z / TerrainData.ChunkSizeZ 
            : (camera.Target.Z - TerrainData.ChunkSizeZ + 1) / TerrainData.ChunkSizeZ);
        
        int radius = 16;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int cx = camChunkX + dx;
                int cz = camChunkZ + dz;

                var bounds = GetOrCreateChunkBounds(cx, cz);

                if (BoundsInFrustum(bounds, frustum))
                {
                    _visibleChunks.Add((cx, cz));
                }
            }
        }
    }

    private FrustumPlanes ExtractFrustumPlanes(Camera3D camera)
    {
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        var up = Vector3.Cross(right, forward);

        float fovY = camera.FovY * MathF.PI / 180f;
        float aspect = 1920f / 1080f;
        float halfFovY = fovY / 2f;
        float halfFovX = MathF.Atan(MathF.Tan(halfFovY) * aspect);

        float nearDist = 0.1f;
        float farDist = 500f;

        float tanX = MathF.Tan(halfFovX);
        float tanY = MathF.Tan(halfFovY);

        var frustum = new FrustumPlanes();

        frustum.Near = new Vector4(forward, -Vector3.Dot(forward, camera.Position) - nearDist);

        frustum.Far = new Vector4(-forward, Vector3.Dot(forward, camera.Position) + farDist);

        var leftNormal = Vector3.Normalize(forward + right * tanX);
        frustum.Left = new Vector4(leftNormal, -(Vector3.Dot(leftNormal, camera.Position) - nearDist));

        var rightNormal = Vector3.Normalize(forward - right * tanX);
        frustum.Right = new Vector4(rightNormal, -(Vector3.Dot(rightNormal, camera.Position) - nearDist));

        var topNormal = Vector3.Normalize(forward - up * tanY);
        frustum.Top = new Vector4(topNormal, -(Vector3.Dot(topNormal, camera.Position) - nearDist));

        var bottomNormal = Vector3.Normalize(forward + up * tanY);
        frustum.Bottom = new Vector4(bottomNormal, -(Vector3.Dot(bottomNormal, camera.Position) - nearDist));

        return frustum;
    }

    private bool BoundsInFrustum(Utils.BoundingBox bounds, FrustumPlanes frustum)
    {
        Vector3 center = (bounds.Min + bounds.Max) / 2;
        Vector3 extent = (bounds.Max - bounds.Min) / 2;

        return PlaneIntersectsBox(frustum.Left, center, extent) &&
               PlaneIntersectsBox(frustum.Right, center, extent) &&
               PlaneIntersectsBox(frustum.Top, center, extent) &&
               PlaneIntersectsBox(frustum.Bottom, center, extent) &&
               PlaneIntersectsBox(frustum.Near, center, extent) &&
               PlaneIntersectsBox(frustum.Far, center, extent);
    }

    private static bool PlaneIntersectsBox(Vector4 plane, Vector3 center, Vector3 extent)
    {
        float dot = plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z;
        float dist = MathF.Abs(extent.X * plane.X) + MathF.Abs(extent.Y * plane.Y) + MathF.Abs(extent.Z * plane.Z);
        return dot + dist + plane.W > 0;
    }

    private Utils.BoundingBox GetOrCreateChunkBounds(int cx, int cz)
    {
        if (_chunkBounds.TryGetValue((cx, cz), out var bounds)) return bounds;

        Vector3 center = new Vector3(
            cx * ChunkWorldWidth + ChunkWorldWidth / 2f,
            ChunkWorldHeight / 2f,
            cz * ChunkWorldDepth + ChunkWorldDepth / 2f
        );

        Vector3 size = new Vector3(ChunkWorldWidth, ChunkWorldHeight, ChunkWorldDepth);
        bounds = Utils.BoundingBox.FromCenterSize(center, size);
        _chunkBounds[(cx, cz)] = bounds;
        return bounds;
    }

    public void MarkDirty() => _needsUpdate = true;

    public void RemoveChunk(int cx, int cz)
    {
        _chunkBounds.Remove((cx, cz));
        _visibleChunks.Remove((cx, cz));
    }

    public void Clear()
    {
        _chunkBounds.Clear();
        _visibleChunks.Clear();
        _needsUpdate = true;
    }

    public int GetVisibleCount() => _visibleChunks.Count;
    public int GetCachedCount() => _chunkBounds.Count;
}
