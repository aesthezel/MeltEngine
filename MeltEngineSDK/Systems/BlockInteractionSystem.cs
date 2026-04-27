using System;
using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Utils;
using Raylib_cs;

using RRay = Raylib_cs.Ray;
using RBoundingBox = Raylib_cs.BoundingBox;

namespace MeltEngine.Systems
{
    public class BlockInteractionSystem : ISystem
    {
        public static BlockInteractionSystem? Instance { get; private set; }
        
        private const float MaxReachDistance = 20.0f;

        private float _cooldownTimer;
        private const float InteractionCooldown = 0.2f;

        // Track consumed clicks to prevent re-firing in the same frame
        private bool _leftClickConsumed;
        private bool _rightClickConsumed;

        public int SelectedSlot = 0;

        /// <summary>
        /// All selectable block types for the hotbar. Order matters for slot indexing.
        /// </summary>
        public static readonly (BlockType type, string name)[] SelectableBlocks = 
        {
            (BlockType.Grass, "Grass"),
            (BlockType.Dirt, "Dirt"),
            (BlockType.Stone, "Stone"),
            (BlockType.Sand, "Sand"),
            (BlockType.Wood, "Wood"),
            (BlockType.Leaves, "Leaves"),
            (BlockType.Brick, "Brick"),
            (BlockType.Water, "Water"),
            (BlockType.Bedrock, "Bedrock"),
        };

        private WorldGeneratorSystem _worldGenerator;

        public static bool HasHit { get; private set; }
        public static Vector3? HitPosition { get; private set; }
        public static Vector3? PlacePosition { get; private set; }
        public static Vector3? HitNormal { get; private set; }

        public BlockInteractionSystem(WorldGeneratorSystem worldGenerator)
        {
            Instance = this;
            _worldGenerator = worldGenerator;
        }

        public BlockInteractionSystem()
        {
            Instance = this;
            if (_worldGenerator == null)
            {
                _worldGenerator = WorldGeneratorSystem.Instance ?? throw new InvalidOperationException("WorldGeneratorSystem not initialized");
            }
        }

        public BlockType SelectedBlockType => SelectableBlocks[SelectedSlot].type;
        public string SelectedBlockName => SelectableBlocks[SelectedSlot].name;

        public void Update(ECSOperator entityOperator, float deltaTime)
        {
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= deltaTime;
            }

            HandleBlockTypeSelection();

            // Reset consumed flags when buttons are released
            if (!Raylib.IsMouseButtonDown(MouseButton.Left)) _leftClickConsumed = false;
            if (!Raylib.IsMouseButtonDown(MouseButton.Right)) _rightClickConsumed = false;

            bool leftClick = Raylib.IsMouseButtonPressed(MouseButton.Left) && !_leftClickConsumed;
            bool rightClick = Raylib.IsMouseButtonPressed(MouseButton.Right) && !_rightClickConsumed;

            UpdateRaycast(entityOperator);

            if (!leftClick && !rightClick) return;
            if (_cooldownTimer > 0) return;

            if (leftClick && HasHit && HitPosition.HasValue)
            {
                var pos = HitPosition.Value;
                _worldGenerator.ModifyBlock(entityOperator, (int)pos.X, (int)pos.Y, (int)pos.Z, BlockType.Air);
                _cooldownTimer = InteractionCooldown;
                _leftClickConsumed = true;
            }
            else if (rightClick && HasHit && PlacePosition.HasValue)
            {
                var pos = PlacePosition.Value;
                _worldGenerator.ModifyBlock(entityOperator, (int)pos.X, (int)pos.Y, (int)pos.Z, SelectedBlockType);
                _cooldownTimer = InteractionCooldown;
                _rightClickConsumed = true;
            }
        }

        /// <summary>
        /// Uses Raylib.GetRayCollisionBox per block (like the example) for reliable hit detection.
        /// Steps along the ray through the terrain grid, testing each solid block's bounding box.
        /// </summary>
        private void UpdateRaycast(ECSOperator entityOperator)
        {
            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();
            if (cameraComponents.Components.Count == 0) 
            {
                ClearHit();
                return;
            }

            var cameraEntity = cameraComponents.Components.FirstOrDefault();
            var camera = cameraEntity.Value.Camera;

            Vector2 screenCenter = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            RRay ray = Raylib.GetScreenToWorldRay(screenCenter, camera);

            var terrain = _worldGenerator.GetTerrainData();
            
            Vector3 origin = ray.Position;
            Vector3 dir = Vector3.Normalize(ray.Direction);

            // DDA-style ray marching: step through grid cells along the ray
            // and test each solid block with bounding box collision
            float closestDist = float.MaxValue;
            Vector3? bestHitPos = null;
            Vector3? bestPlacePos = null;
            Vector3? bestHitNormal = null;

            var tested = new System.Collections.Generic.HashSet<(int, int, int)>();

            // Use a finer step for accurate detection
            for (float d = 0; d < MaxReachDistance; d += 0.05f)
            {
                Vector3 samplePos = origin + dir * d;

                int bx = (int)MathF.Floor(samplePos.X + 0.5f);
                int by = (int)MathF.Floor(samplePos.Y + 0.5f);
                int bz = (int)MathF.Floor(samplePos.Z + 0.5f);

                if (!tested.Add((bx, by, bz))) continue;

                BlockType block = terrain.GetBlock(bx, by, bz);
                if (block == BlockType.Air || block == BlockType.Water) continue;

                // Solid block found — precise BoundingBox test
                Vector3 blockCenter = new Vector3(bx, by, bz);
                RBoundingBox box = new RBoundingBox(
                    blockCenter - new Vector3(0.5f, 0.5f, 0.5f),
                    blockCenter + new Vector3(0.5f, 0.5f, 0.5f)
                );

                RayCollision collision = Raylib.GetRayCollisionBox(ray, box);
                if (collision.Hit && collision.Distance < closestDist && collision.Distance > 0)
                {
                    closestDist = collision.Distance;
                    bestHitPos = blockCenter;
                    bestHitNormal = collision.Normal;

                    // Place position = hit block + face normal (like the example)
                    Vector3 rawPlace = blockCenter + collision.Normal;
                    bestPlacePos = new Vector3(
                        MathF.Round(rawPlace.X),
                        MathF.Round(rawPlace.Y),
                        MathF.Round(rawPlace.Z)
                    );

                    // We found the closest hit — no need to keep searching further along the ray
                    break;
                }
            }

            if (bestHitPos.HasValue)
            {
                HasHit = true;
                HitPosition = bestHitPos;
                PlacePosition = bestPlacePos;
                HitNormal = bestHitNormal;
            }
            else
            {
                ClearHit();
            }
        }

        private static void ClearHit()
        {
            HasHit = false;
            HitPosition = null;
            PlacePosition = null;
            HitNormal = null;
        }

        private void HandleBlockTypeSelection()
        {
            // Number keys 1-9
            if (Raylib.IsKeyPressed(KeyboardKey.One)) SelectedSlot = 0;
            else if (Raylib.IsKeyPressed(KeyboardKey.Two)) SelectedSlot = 1;
            else if (Raylib.IsKeyPressed(KeyboardKey.Three)) SelectedSlot = 2;
            else if (Raylib.IsKeyPressed(KeyboardKey.Four)) SelectedSlot = 3;
            else if (Raylib.IsKeyPressed(KeyboardKey.Five)) SelectedSlot = 4;
            else if (Raylib.IsKeyPressed(KeyboardKey.Six)) SelectedSlot = 5;
            else if (Raylib.IsKeyPressed(KeyboardKey.Seven)) SelectedSlot = 6;
            else if (Raylib.IsKeyPressed(KeyboardKey.Eight)) SelectedSlot = 7;
            else if (Raylib.IsKeyPressed(KeyboardKey.Nine)) SelectedSlot = 8;

            // Scroll wheel to cycle
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0)
            {
                SelectedSlot -= (int)wheel;
                if (SelectedSlot < 0) SelectedSlot = SelectableBlocks.Length - 1;
                if (SelectedSlot >= SelectableBlocks.Length) SelectedSlot = 0;
            }

            // Clamp just in case
            SelectedSlot = Math.Clamp(SelectedSlot, 0, SelectableBlocks.Length - 1);
        }
        
        public static BlockType CurrentBlockType => Instance?.SelectedBlockType ?? BlockType.Wood;
        public static string CurrentBlockName => Instance?.SelectedBlockName ?? "Unknown";
    }
}