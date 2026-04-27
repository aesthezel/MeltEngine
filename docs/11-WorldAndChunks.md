# World Generator & Chunk System

Procedural voxel world generation with chunk-based loading and frustum culling.

## WorldGeneratorSystem

Procedurally generates the terrain world using a chunk system. Runs as an `ISystem` on the main loop.

### Constructor

```csharp
new WorldGeneratorSystem(seed, worldSizeX, worldSizeZ, renderRadius)
```

| Param | Default | Description |
|-------|---------|-------------|
| `seed` | - | Terrain generation seed |
| `worldSizeX` | 256 | World width in blocks |
| `worldSizeZ` | 256 | World depth in blocks |
| `renderRadius` | 6 | Chunk load radius around player |

### Initialization Flow

1. Calculate spawn position at world center + height lookup + 3 blocks up
2. Queue chunks within render radius for async generation
3. Generate initial 16 chunks immediately via `Task.Run()`
4. Chunks are generated asynchronously and queued via `ConcurrentQueue`

### Update Loop

1. **ApplyPendingChunks**: Dequeue pending chunks (max 4/frame, max 2000 blocks/frame). For each chunk, iterate local blocks, skip air/bedrock, skip non-visible (buried) blocks, create ECS entities for surface blocks only.
2. **DestroyQueuedEntities**: Dequeue entities marked for destruction (max 100/frame).
3. **UpdateChunkLoading**: Keep pending generation queue filled (trigger new async tasks when < 20 pending, max 8 new tasks at a time).

### Block Visibility Culling

Only blocks exposed to air or water are created. Visibility check: 6-direction neighbor test within the chunk. If a neighbor is air/water, the block is visible and gets a render entity.

### Created Entity Structure

Each surface block gets:
```
Entity
├── CoordComponent        (world position, scale=1)
├── CubeRendererComponent (renderable)
├── EnabledComponent      (active)
├── BlockTypeComponent    (block type enum)
├── SurfaceBlockComponent (marker for surface)
└── SceneMemberComponent  ("Terrain")
```

---

## ChunkCullerSystem

Frustum-based chunk visibility culling. Determines which chunks are visible to the camera using view frustum plane tests.

### Key Features

- **Frustum plane extraction** from Camera3D (near, far, left, right, top, bottom planes)
- **Bounding box culling** for each chunk (16x16 block radius default)
- **Dirty tracking**: only recalculates when camera moves > 2 units or target changes > 2 units
- Chunk bounds are cached in dictionary keyed by `(cx, cz)`

### Frustum Calculation

- FOV from `Camera3D.FovY`
- Aspect ratio: 1920/1080
- Near: 0.1, Far: 500
- Plane test: AABB vs 6 planes using center-extent intersection

### API

```csharp
class ChunkCullerSystem : ISystem
{
    IReadOnlySet<(int cx, int cz)> GetVisibleChunks();
    bool IsChunkVisible(int cx, int cz);
    void MarkDirty();                    // Force recalculation next frame
    void RemoveChunk(int cx, int cz);    // Remove cached chunk
    void Clear();                        // Clear all data
    int GetVisibleCount();               // Currently visible chunks
    int GetCachedCount();                // Total cached chunk bounds
}
```

---

## TerrainData

Inner data provider used by `WorldGeneratorSystem`:

- `ChunkSizeX = 16`, `ChunkSizeY = 64`, `ChunkSizeZ = 16`
- `GetHeight(worldX, worldZ)` - heightmap lookup from seed-based noise
- `GetBlock(worldX, worldY, worldZ)` - block type at world coords
- `GenerateChunk(chunkX, chunkZ)` - async batch generation returning `BlockType[,,]`

### Block Types

```csharp
enum BlockType
{
    Air, Grass, Dirt, Stone, Sand, Water, Bedrock, Snow, Wood, Leaves
}
```

---

## Related Components

### SurfaceBlockComponent
Marker struct. No data. Used to identify surface-rendered blocks.

### BlockTypeComponent
```csharp
struct BlockTypeComponent
{
    BlockType Type;
    BlockTypeComponent(BlockType type) => Type = type;
}
```

---

## Integration in Workflow

```csharp
var worldGenerator = new WorldGeneratorSystem(seed: 12345, worldSizeX: 32, worldSizeZ: 32, renderRadius: 8);
var chunkCuller = new ChunkCullerSystem();

var updateSystems = { ..., chunkCuller, worldGenerator };
```

Both run every frame on the variable timestep. ChunkCuller feeds frustum data to render system for chunk-level visibility decisions.

---

## Performance Limits

| Constraint | Value |
|------------|-------|
| Max blocks per frame | 2000 |
| Max chunks per frame | 4 |
| Max async chunk queue | 20 (below triggers new generation) |
| Max concurrent generation tasks | 8 per batch |
| Max entity destruction per frame | 100 |
