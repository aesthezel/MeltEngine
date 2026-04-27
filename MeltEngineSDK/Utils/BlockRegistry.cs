using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MeltEngine.Utils;

/// <summary>
/// Central registry for block type definitions.
/// Loads block data from a JSON file, providing data-driven block configuration.
/// New block types can be added by editing blocks.json without recompilation.
/// </summary>
public static class BlockRegistry
{
    /// <summary>Quick access by block ID.</summary>
    public static Dictionary<byte, BlockDefinition> Definitions { get; } = new();

    /// <summary>Quick access by block name (case-insensitive).</summary>
    private static readonly Dictionary<string, BlockDefinition> _definitionsByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the registry has been loaded.</summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>
    /// Loads block definitions from a JSON file.
    /// </summary>
    /// <param name="path">Path to the blocks.json file.</param>
    public static void LoadDatabase(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[BlockRegistry] Error: File not found: {path}");
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<BlockDefinition>>(jsonString);

            if (list == null)
            {
                Console.WriteLine("[BlockRegistry] Error: Failed to deserialize blocks.json");
                return;
            }

            Definitions.Clear();
            _definitionsByName.Clear();

            foreach (var def in list)
            {
                if (def.FaceIndices == null || def.FaceIndices.Length != 6)
                {
                    Console.WriteLine($"[BlockRegistry] Warning: Block '{def.Name}' (ID={def.Id}) has invalid FaceIndices, skipping.");
                    continue;
                }

                Definitions[def.Id] = def;
                _definitionsByName[def.Name] = def;
            }

            IsLoaded = true;
            Console.WriteLine($"[BlockRegistry] Database loaded: {Definitions.Count} block types ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlockRegistry] Error loading database: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the face indices for a given BlockType.
    /// Returns a default array of zeros if the type is not registered.
    /// </summary>
    public static int[] GetFaceIndices(BlockType type)
    {
        if (Definitions.TryGetValue((byte)type, out var def))
        {
            return def.FaceIndices;
        }

        return new int[6]; // Default: top-left tile of atlas
    }

    /// <summary>
    /// Gets a block definition by name.
    /// </summary>
    public static BlockDefinition? GetByName(string name)
    {
        return _definitionsByName.TryGetValue(name, out var def) ? def : null;
    }

    /// <summary>
    /// Gets all registered block type IDs.
    /// </summary>
    public static IEnumerable<byte> GetRegisteredIds() => Definitions.Keys;

    /// <summary>
    /// Checks if a block type has a registered definition.
    /// </summary>
    public static bool HasDefinition(BlockType type) => Definitions.ContainsKey((byte)type);
}
