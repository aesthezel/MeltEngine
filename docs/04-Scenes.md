# Scenes & Serialization

Scene = JSON map of Entities and Components. Loaded via `SceneService`.

## SceneService Flow

1. Parse JSON -> `Scene` object.
2. `ClearScene()`: delete entities with `SceneMemberComponent`.
3. Create pass: map JSON entity definitions to real `Entity`.
4. Parse JSON components dynamically. Inject into ECS.
5. Reference pass: link entity names (e.g. Camera target Name -> actual `Entity` ID).
6. Auto-fallback to DefaultScene logic if JSON fail.

## Scene JSON Format

```json
{
  "Name": "MapName",
  "Entities": [
    {
      "Name": "Player",
      "Components": {
        "CoordComponent": { "Position": {"X":0, "Y":2, "Z":0}, "Scale": {"X":1, "Y":1, "Z":1} },
        "CubeRendererComponent": {},
        "PhysicsBodyComponent": { "Mass": 1.0 },
        "EnabledComponent": {}
      }
    }
  ]
}
```

## Hot Reload
- `FileWatcher` / `HotReload` track JSON. F5 trigger manual `ReloadScene()`.
