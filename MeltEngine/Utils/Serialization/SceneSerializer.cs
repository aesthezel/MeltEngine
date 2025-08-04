#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeltEngine.Scenes;

namespace MeltEngine.Utils.Serialization;

public static class SceneSerializer
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void SaveScene(Scene scene, string path)
    {
        var json = JsonSerializer.Serialize(scene, Options);
        File.WriteAllText(path, json);
    }

    public static Scene? LoadScene(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Scene>(json, Options);
    }
}