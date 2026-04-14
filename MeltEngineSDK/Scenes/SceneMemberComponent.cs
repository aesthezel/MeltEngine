namespace MeltEngine.Entities.Components
{
    /// <summary>
    /// Marca las entidades que pertenecen a una escena cargada
    /// </summary>
    public struct SceneMemberComponent(string sceneName = "")
    {
        public string SceneName { get; set; } = sceneName;
    }
}