using System;
using System.Collections.Generic;
using Raylib_cs;

namespace MeltEngine.Utils;

/// <summary>
/// Generates cube meshes with UV coordinates mapped to a texture atlas.
/// Each block type gets a unique mesh whose UVs point to the correct atlas tiles.
/// Ported from the examples/Block.cs SetCubeMeshUVs approach.
/// </summary>
public static class BlockMeshGenerator
{
    /// <summary>Number of columns in the texture atlas.</summary>
    public const int AtlasCols = 24;

    /// <summary>Number of rows in the texture atlas.</summary>
    public const int AtlasRows = 26;

    /// <summary>Cache of generated meshes per block type to avoid regenerating.</summary>
    private static readonly Dictionary<BlockType, Mesh> _meshCache = new();

    /// <summary>
    /// Generates (or retrieves from cache) a cube mesh with UVs mapped to the
    /// texture atlas for the specified block type.
    /// </summary>
    /// <param name="type">The block type to generate a mesh for.</param>
    /// <returns>A Mesh with UVs pointing to the correct atlas tiles.</returns>
    public static Mesh GenerateBlockMesh(BlockType type)
    {
        if (_meshCache.TryGetValue(type, out var cached))
            return cached;

        int[] faceIndices = BlockRegistry.GetFaceIndices(type);

        Mesh cubeMesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
        SetCubeMeshUVs(ref cubeMesh, faceIndices, AtlasCols, AtlasRows);

        _meshCache[type] = cubeMesh;
        return cubeMesh;
    }

    /// <summary>
    /// Modifies the UV coordinates of a cube mesh to point to specific tiles
    /// in a texture atlas. Each face of the cube can reference a different tile.
    /// 
    /// Face order: [0]=Front, [1]=Back, [2]=Top, [3]=Bottom, [4]=Right, [5]=Left
    /// </summary>
    /// <param name="mesh">The cube mesh to modify (from GenMeshCube).</param>
    /// <param name="faceIndices">Array of 6 atlas tile indices, one per face.</param>
    /// <param name="cols">Number of columns in the texture atlas.</param>
    /// <param name="rows">Number of rows in the texture atlas.</param>
    public static unsafe void SetCubeMeshUVs(ref Mesh mesh, int[] faceIndices, int cols, int rows)
    {
        float tileWidth = 1.0f / cols;
        float tileHeight = 1.0f / rows;
        float* texcoords = (float*)mesh.TexCoords;

        // A Raylib GenMeshCube has 6 faces, each with 4 vertices (24 vertices total)
        for (int face = 0; face < 6; face++)
        {
            // Get the atlas index for this face
            int atlasIndex = faceIndices[face];

            // Convert 1D index to UV offset coordinates (0.0 to 1.0)
            float uOffset = (float)(atlasIndex % cols) * tileWidth;  // Column (X)
            float vOffset = (float)(atlasIndex / cols) * tileHeight;  // Row (Y)

            for (int v = 0; v < 4; v++)
            {
                int i = (face * 4) + v;
                float u = texcoords[i * 2 + 0];
                float v_coord = texcoords[i * 2 + 1];

                // Side faces (0, 1, 4, 5) come inverted in GenMeshCube.
                // Flip V so that "up" in the texture matches "up" on the cube.
                if (face == 0 || face == 1 || face == 4 || face == 5)
                {
                    v_coord = 1.0f - v_coord;
                }

                texcoords[i * 2 + 0] = (u * tileWidth) + uOffset;
                texcoords[i * 2 + 1] = (v_coord * tileHeight) + vOffset;
            }
        }

        // Update the GPU buffer (index 1 = Texcoords)
        Raylib.UpdateMeshBuffer(mesh, 1, texcoords, mesh.VertexCount * 2 * sizeof(float), 0);
    }

    /// <summary>
    /// Unloads all cached meshes from GPU memory. Call on shutdown.
    /// </summary>
    public static void UnloadCache()
    {
        foreach (var mesh in _meshCache.Values)
        {
            Raylib.UnloadMesh(mesh);
        }
        _meshCache.Clear();
    }

    /// <summary>
    /// Checks if a mesh has been generated for the specified block type.
    /// </summary>
    public static bool HasMesh(BlockType type) => _meshCache.ContainsKey(type);
}
