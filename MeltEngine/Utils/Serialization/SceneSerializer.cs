using System.IO;
using MeltEngine.Core.Scenes;
using System.Text.Json;

namespace MeltEngine.Utils.Serialization;

public static class SceneSerializer
{
    public static void SaveScene(Scene scene, string path)
    {
        var json = JsonSerializer.Serialize(scene);
        File.WriteAllText(path, json);
    }

    public static Scene LoadScene(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Scene>(json);
    }
}