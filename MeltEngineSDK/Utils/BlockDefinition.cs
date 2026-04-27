using System.Text.Json.Serialization;

namespace MeltEngine.Utils;

/// <summary>
/// Data-driven block definition loaded from blocks.json.
/// Each definition maps a block ID to its texture atlas face indices.
/// </summary>
public class BlockDefinition
{
    /// <summary>Block type ID (matches BlockType enum value).</summary>
    [JsonPropertyName("Id")]
    public byte Id { get; set; }

    /// <summary>Human-readable block name.</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Texture atlas indices for each face of the cube.
    /// [0]=Front, [1]=Back, [2]=Top, [3]=Bottom, [4]=Right, [5]=Left
    /// Each index is a 1D position in the atlas grid (row-major order).
    /// </summary>
    [JsonPropertyName("FaceIndices")]
    public int[] FaceIndices { get; set; } = new int[6];
}
