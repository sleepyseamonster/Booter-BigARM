using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public sealed class PrototypeWorldGenerator : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Sprite[] tileSprites;
        [SerializeField] private GameObject propPrefab;
        [SerializeField] private GameObject[] propPrefabs;
        [SerializeField] private Transform propParent;
        [SerializeField, Range(0f, 1f)] private float propSpawnChance = 0.12f;
        [SerializeField] private PrototypeWorldSettings worldSettings;
        [SerializeField] private Tilemap pebbleTilemap;
        [SerializeField] private Sprite[] pebbleTileSprites;
        [SerializeField] private Tilemap rockTilemap;
        [SerializeField] private Sprite[] rockTileSprites;
        [SerializeField] private Tilemap smoothTilemap;
        [SerializeField] private Sprite[] smoothTileSprites;
        [SerializeField] private Tilemap sandOverlayTilemap;
        [SerializeField] private Sprite[] sandOverlayTileSprites;
        [SerializeField] private int seed = 12345;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int chunkRadius = 4;
        [SerializeField] private int chunkOperationsPerFrame = 4;

        private readonly HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> chunkOperationQueue = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedChunks = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, List<GameObject>> spawnedPropsByChunk = new Dictionary<Vector2Int, List<GameObject>>();
        private Tilemap tilemap;
        private TileBase[] runtimeTiles;
        private TileBase[] runtimePebbleTiles;
        private TileBase[] runtimeRockTiles;
        private TileBase[] runtimeSmoothTiles;
        private TileBase[] runtimeSandOverlayTiles;
        private TileBase[] emptyChunkTiles;
        private TileBase[] chunkTileBuffer;
        private TileBase[] sandOverlayChunkTileBuffer;
        private TileBase[] pebbleChunkTileBuffer;
        private TileBase[] rockChunkTileBuffer;
        private TileBase[] smoothChunkTileBuffer;
        private TileBase[] emptySandOverlayChunkTiles;
        private TileBase[] emptyPebbleChunkTiles;
        private TileBase[] emptyRockChunkTiles;
        private TileBase[] emptySmoothChunkTiles;
        private Matrix4x4[] chunkTransformBuffer;
        private Matrix4x4[] sandOverlayChunkTransformBuffer;
        private Matrix4x4[] pebbleChunkTransformBuffer;
        private Matrix4x4[] rockChunkTransformBuffer;
        private Matrix4x4[] smoothChunkTransformBuffer;
        private Vector2Int currentCenterChunk;

        public int Seed => seed;
        public Vector2Int CurrentCenterChunk => currentCenterChunk;
        public int VisibleChunkCount => visibleChunks.Count;

        public void Configure(Transform worldTarget, Sprite squareSprite, int worldSeed, int worldChunkSize, int worldChunkRadius)
        {
            target = worldTarget;
            tileSprites = new[] { squareSprite };
            seed = worldSeed;
            chunkSize = Mathf.Max(1, worldChunkSize);
            chunkRadius = Mathf.Max(0, worldChunkRadius);
            RefreshChunkTargets(true);
        }

        public void ResetWorld(int newSeed)
        {
            seed = newSeed;
            visibleChunks.Clear();
            requiredChunks.Clear();
            chunkOperationQueue.Clear();
            queuedChunks.Clear();
            ClearSpawnedProps();
            tilemap.ClearAllTiles();
            if (pebbleTilemap != null)
            {
                pebbleTilemap.ClearAllTiles();
            }
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.ClearAllTiles();
            }
            if (rockTilemap != null)
            {
                rockTilemap.ClearAllTiles();
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.ClearAllTiles();
            }
            RefreshChunkTargets(true);
        }

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            if (worldSettings == null)
            {
                worldSettings = GetComponentInParent<PrototypeWorldSettings>();
            }
            chunkSize = Mathf.Max(1, chunkSize);
            chunkRadius = Mathf.Max(0, chunkRadius);
            chunkOperationsPerFrame = Mathf.Max(1, chunkOperationsPerFrame);
        }

        private void Start()
        {
            RefreshChunkTargets(true);
        }

        private void Update()
        {
            RefreshChunkTargets(false);
            ProcessChunkQueue();
        }

        private void RefreshChunkTargets(bool force)
        {
            if (tilemap == null)
            {
                tilemap = GetComponent<Tilemap>();
            }

            if (tilemap == null)
            {
                return;
            }

            EnsureRuntimeTiles();

            var worldPosition = target != null ? target.position : Vector3.zero;
            var newCenterChunk = WorldToChunk(worldPosition);
            if (!force && newCenterChunk == currentCenterChunk)
            {
                return;
            }

            currentCenterChunk = newCenterChunk;
            requiredChunks.Clear();
            for (var y = -chunkRadius; y <= chunkRadius; y++)
            {
                for (var x = -chunkRadius; x <= chunkRadius; x++)
                {
                    var chunk = new Vector2Int(currentCenterChunk.x + x, currentCenterChunk.y + y);
                    requiredChunks.Add(chunk);
                }
            }

            QueueChunksToUnload();
            QueueChunksToLoadByDistance();
        }

        private void QueueChunksToUnload()
        {
            foreach (var chunk in visibleChunks)
            {
                if (!requiredChunks.Contains(chunk))
                {
                    QueueChunkOperation(chunk);
                }
            }
        }

        private void QueueChunksToLoadByDistance()
        {
            for (var distance = 0; distance <= chunkRadius; distance++)
            {
                for (var y = -distance; y <= distance; y++)
                {
                    for (var x = -distance; x <= distance; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != distance)
                        {
                            continue;
                        }

                        var chunk = new Vector2Int(currentCenterChunk.x + x, currentCenterChunk.y + y);
                        if (!visibleChunks.Contains(chunk))
                        {
                            QueueChunkOperation(chunk);
                        }
                    }
                }
            }
        }

        private void EnsureRuntimeTiles()
        {
            if (runtimeTiles != null &&
                runtimePebbleTiles != null &&
                runtimeRockTiles != null &&
                runtimeSmoothTiles != null &&
                tileSprites != null &&
                pebbleTileSprites != null &&
                rockTileSprites != null &&
                smoothTileSprites != null &&
                runtimeTiles.Length == tileSprites.Length &&
                runtimePebbleTiles.Length == pebbleTileSprites.Length &&
                runtimeRockTiles.Length == rockTileSprites.Length &&
                runtimeSmoothTiles.Length == smoothTileSprites.Length)
            {
                return;
            }

            if (tileSprites == null || tileSprites.Length == 0)
            {
                runtimeTiles = CreateFallbackBaseTiles();
            }
            else
            {
                runtimeTiles = new TileBase[tileSprites.Length];
                for (var i = 0; i < tileSprites.Length; i++)
                {
                    var sprite = tileSprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;
                    tile.flags = TileFlags.None;
                    tile.name = $"PrototypeRuntimeTile_{i}";
                    tile.hideFlags = HideFlags.HideAndDontSave;
                    runtimeTiles[i] = tile;
                }
            }

            runtimePebbleTiles = BuildRuntimeTiles(pebbleTileSprites, CreateFallbackPebbleTiles, "PrototypeRuntimePebbleTile");
            runtimeRockTiles = BuildRuntimeTiles(rockTileSprites, CreateFallbackRockTiles, "PrototypeRuntimeRockTile");
            runtimeSmoothTiles = BuildRuntimeTiles(smoothTileSprites, CreateFallbackSmoothTiles, "PrototypeRuntimeSmoothTile");
            runtimeSandOverlayTiles = BuildRuntimeTiles(sandOverlayTileSprites, CreateFallbackSandOverlayTiles, "PrototypeRuntimeSandOverlayTile");

            EnsureChunkBuffers();
        }

        private static TileBase[] BuildRuntimeTiles(Sprite[] sprites, System.Func<TileBase[]> fallbackFactory, string tileNamePrefix)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return fallbackFactory();
            }

            var runtimeTiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null)
                {
                    continue;
                }

                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"{tileNamePrefix}_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                runtimeTiles[i] = tile;
            }

            return runtimeTiles;
        }

        private TileBase[] CreateFallbackBaseTiles()
        {
            var sprites = new[]
            {
                CreateSolidSprite(new Color32(173, 136, 86, 255)),
                CreateSolidSprite(new Color32(179, 141, 88, 255)),
                CreateSolidSprite(new Color32(169, 132, 82, 255)),
                CreateSolidSprite(new Color32(181, 145, 92, 255))
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackTile_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                tiles[i] = tile;
            }

            return tiles;
        }

        private TileBase[] CreateFallbackPebbleTiles()
        {
            var sprites = new[]
            {
                CreateDetailSprite(new Color32(92, 78, 60, 230), DetailPattern.PebblesSmall),
                CreateDetailSprite(new Color32(103, 87, 69, 230), DetailPattern.PebblesPatch),
                CreateDetailSprite(new Color32(117, 101, 78, 220), DetailPattern.PebblesMixed),
                CreateDetailSprite(new Color32(86, 74, 57, 230), DetailPattern.PebblesSparse)
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackPebbleTile_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                tiles[i] = tile;
            }

            return tiles;
        }

        private TileBase[] CreateFallbackRockTiles()
        {
            var sprites = new[]
            {
                CreateDetailSprite(new Color32(102, 99, 94, 235), DetailPattern.RocksSmall),
                CreateDetailSprite(new Color32(116, 113, 107, 235), DetailPattern.RocksPatch),
                CreateDetailSprite(new Color32(128, 124, 117, 225), DetailPattern.RocksMixed),
                CreateDetailSprite(new Color32(90, 87, 83, 235), DetailPattern.RocksSparse)
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackRockTile_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                tiles[i] = tile;
            }

            return tiles;
        }

        private TileBase[] CreateFallbackSmoothTiles()
        {
            var sprites = new[]
            {
                CreateDetailSprite(new Color32(221, 194, 138, 145), DetailPattern.SmoothPatchA),
                CreateDetailSprite(new Color32(232, 205, 147, 140), DetailPattern.SmoothPatchB),
                CreateDetailSprite(new Color32(214, 188, 130, 145), DetailPattern.SmoothPatchC),
                CreateDetailSprite(new Color32(225, 199, 141, 140), DetailPattern.SmoothPatchD)
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackSmoothTile_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                tiles[i] = tile;
            }

            return tiles;
        }

        private static Sprite CreateSolidSprite(Color32 color)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateDetailSprite(Color32 tint, DetailPattern pattern)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, new Color32(0, 0, 0, 0));
                }
            }

            switch (pattern)
            {
                case DetailPattern.PebblesSmall:
                    DrawPebbleTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 2, 0);
                    break;
                case DetailPattern.PebblesPatch:
                    DrawPebbleTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 2, 1);
                    break;
                case DetailPattern.PebblesMixed:
                    DrawPebbleTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 3, 0);
                    break;
                case DetailPattern.PebblesSparse:
                    DrawPebbleTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 1, 1);
                    break;
                case DetailPattern.RocksSmall:
                    DrawRockTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 3, 0);
                    break;
                case DetailPattern.RocksPatch:
                    DrawRockTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 3, 1);
                    break;
                case DetailPattern.RocksMixed:
                    DrawRockTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 4, 0);
                    break;
                case DetailPattern.RocksSparse:
                    DrawRockTile(texture, tint, new System.Random(GetSeed(tint, pattern)), 2, 1);
                    break;
                case DetailPattern.SmoothPatchA:
                    DrawSmoothPatch(texture, tint, 5, 6, 25, 24, 4);
                    break;
                case DetailPattern.SmoothPatchB:
                    DrawSmoothPatch(texture, tint, 6, 5, 26, 25, 3);
                    break;
                case DetailPattern.SmoothPatchC:
                    DrawSmoothPatch(texture, tint, 4, 7, 24, 24, 5);
                    break;
                case DetailPattern.SmoothPatchD:
                    DrawSmoothPatch(texture, tint, 7, 6, 25, 23, 4);
                    break;
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        }

        private static void DrawPebbleTile(Texture2D texture, Color32 tint, System.Random rng, int radius, int variantBias)
        {
            var centerX = 16 + rng.Next(-1 - variantBias, 2 + variantBias);
            var centerY = 16 + rng.Next(-1, 2);
            var highlight = Blend(tint, new Color32(223, 216, 200, tint.a), 0.25f);
            var shadow = Blend(tint, new Color32(74, 63, 52, tint.a), 0.35f);

            for (var y = -radius - 1; y <= radius + 1; y++)
            {
                for (var x = -radius - 1; x <= radius + 1; x++)
                {
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (distance > radius + 0.35f)
                    {
                        continue;
                    }

                    var baseColor = Blend(shadow, tint, Mathf.InverseLerp(radius + 0.35f, 0f, distance));
                    Put(texture, centerX + x, centerY + y, baseColor);
                }
            }

            Put(texture, centerX, centerY, highlight);
            Put(texture, centerX + 1, centerY, highlight);
        }

        private static void DrawRockTile(Texture2D texture, Color32 tint, System.Random rng, int radius, int variantBias)
        {
            var centerX = 16 + rng.Next(-1 - variantBias, 2 + variantBias);
            var centerY = 16 + rng.Next(-1, 2);
            var highlight = Blend(tint, new Color32(232, 230, 226, tint.a), 0.2f);
            var shadow = Blend(tint, new Color32(54, 52, 50, tint.a), 0.45f);

            for (var y = -radius - 1; y <= radius + 1; y++)
            {
                for (var x = -radius - 1; x <= radius + 1; x++)
                {
                    var chebyshev = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (chebyshev > radius)
                    {
                        continue;
                    }

                    if (chebyshev == radius && ((x + y + variantBias) & 1) == 1 && NextSignedNoise(rng) < 0.4f)
                    {
                        continue;
                    }

                    var falloff = Mathf.InverseLerp(radius + 0.5f, 0f, chebyshev);
                    var baseColor = Blend(shadow, tint, falloff);
                    Put(texture, centerX + x, centerY + y, baseColor);
                }
            }

            Put(texture, centerX, centerY, highlight);
        }

        private static void DrawSmoothPatch(Texture2D texture, Color32 tint, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    Put(texture, x, y, tint);
                    Put(texture, x + 1, y, tint);
                    Put(texture, x, y + 1, tint);
                }
            }
        }

        private static void Put(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || x >= 32 || y < 0 || y >= 32)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static int GetSeed(Color32 a, Color32 b, Color32 c, DetailPattern pattern)
        {
            unchecked
            {
                return a.r
                    | (a.g << 8)
                    | (a.b << 16)
                    | (b.r << 1)
                    | (b.g << 9)
                    | (b.b << 17)
                    | (c.r << 2)
                    | (c.g << 10)
                    | (c.b << 18)
                    | (((int)pattern) << 24);
            }
        }

        private static int GetSeed(Color32 tint, DetailPattern pattern)
        {
            unchecked
            {
                return tint.r
                    | (tint.g << 8)
                    | (tint.b << 16)
                    | (((int)pattern) << 24);
            }
        }

        private static float NextSignedNoise(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private static Color32 Blend(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private void EnsureChunkBuffers()
        {
            var tileCount = chunkSize * chunkSize;
            if (chunkTileBuffer != null && chunkTileBuffer.Length == tileCount)
            {
                return;
            }

            chunkTileBuffer = new TileBase[tileCount];
            sandOverlayChunkTileBuffer = new TileBase[tileCount];
            pebbleChunkTileBuffer = new TileBase[tileCount];
            rockChunkTileBuffer = new TileBase[tileCount];
            smoothChunkTileBuffer = new TileBase[tileCount];
            emptyChunkTiles = new TileBase[tileCount];
            emptySandOverlayChunkTiles = new TileBase[tileCount];
            emptyPebbleChunkTiles = new TileBase[tileCount];
            emptyRockChunkTiles = new TileBase[tileCount];
            emptySmoothChunkTiles = new TileBase[tileCount];
            chunkTransformBuffer = new Matrix4x4[tileCount];
            sandOverlayChunkTransformBuffer = new Matrix4x4[tileCount];
            pebbleChunkTransformBuffer = new Matrix4x4[tileCount];
            rockChunkTransformBuffer = new Matrix4x4[tileCount];
            smoothChunkTransformBuffer = new Matrix4x4[tileCount];
        }

        private void QueueChunkOperation(Vector2Int chunkCoord)
        {
            if (queuedChunks.Add(chunkCoord))
            {
                chunkOperationQueue.Enqueue(chunkCoord);
            }
        }

        private void ProcessChunkQueue()
        {
            var operationsRemaining = chunkOperationsPerFrame;
            while (operationsRemaining > 0 && chunkOperationQueue.Count > 0)
            {
                var chunk = chunkOperationQueue.Dequeue();
                queuedChunks.Remove(chunk);

                var shouldBeVisible = requiredChunks.Contains(chunk);
                var isVisible = visibleChunks.Contains(chunk);
                if (shouldBeVisible == isVisible)
                {
                    operationsRemaining--;
                    continue;
                }

                if (shouldBeVisible)
                {
                    LoadChunk(chunk);
                }
                else
                {
                    UnloadChunk(chunk);
                }

                operationsRemaining--;
            }
        }

        private void LoadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var bounds = GetChunkBounds(chunkCoord);
            FillChunkBuffer(chunkCoord);
            FillChunkTransforms(chunkCoord);
            tilemap.SetTilesBlock(bounds, chunkTileBuffer);
            ApplyChunkTransforms(tilemap, bounds, chunkTransformBuffer);
            FillSandOverlayChunkBuffer(chunkCoord);
            FillSandOverlayChunkTransforms(chunkCoord);
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.SetTilesBlock(bounds, sandOverlayChunkTileBuffer);
                ApplyChunkTransforms(sandOverlayTilemap, bounds, sandOverlayChunkTransformBuffer);
            }
            FillPebbleChunkBuffer(chunkCoord);
            FillPebbleChunkTransforms(chunkCoord);
            FillRockChunkBuffer(chunkCoord);
            FillRockChunkTransforms(chunkCoord);
            FillSmoothChunkBuffer(chunkCoord);
            FillSmoothChunkTransforms(chunkCoord);
            if (pebbleTilemap != null)
            {
                pebbleTilemap.SetTilesBlock(bounds, pebbleChunkTileBuffer);
                ApplyChunkTransforms(pebbleTilemap, bounds, pebbleChunkTransformBuffer);
            }
            if (rockTilemap != null)
            {
                rockTilemap.SetTilesBlock(bounds, rockChunkTileBuffer);
                ApplyChunkTransforms(rockTilemap, bounds, rockChunkTransformBuffer);
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.SetTilesBlock(bounds, smoothChunkTileBuffer);
                ApplyChunkTransforms(smoothTilemap, bounds, smoothChunkTransformBuffer);
            }
            SpawnChunkProps(chunkCoord);
            visibleChunks.Add(chunkCoord);
        }

        private void UnloadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var bounds = GetChunkBounds(chunkCoord);
            tilemap.SetTilesBlock(bounds, emptyChunkTiles);
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.SetTilesBlock(bounds, emptySandOverlayChunkTiles);
            }
            if (pebbleTilemap != null)
            {
                pebbleTilemap.SetTilesBlock(bounds, emptyPebbleChunkTiles);
            }
            if (rockTilemap != null)
            {
                rockTilemap.SetTilesBlock(bounds, emptyRockChunkTiles);
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.SetTilesBlock(bounds, emptySmoothChunkTiles);
            }
            DespawnChunkProps(chunkCoord);
            visibleChunks.Remove(chunkCoord);
        }

        private BoundsInt GetChunkBounds(Vector2Int chunkCoord)
        {
            return new BoundsInt(
                chunkCoord.x * chunkSize,
                chunkCoord.y * chunkSize,
                0,
                chunkSize,
                chunkSize,
                1);
        }

        private void FillChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    chunkTileBuffer[index++] = SelectTile(worldX, worldY);
                }
            }
        }

        private void FillChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(chunkCoord, seed, chunkTransformBuffer);
        }

        private void FillSandOverlayChunkBuffer(Vector2Int chunkCoord)
        {
            System.Array.Clear(sandOverlayChunkTileBuffer, 0, sandOverlayChunkTileBuffer.Length);

            var chunkNoise = Hash01(seed + 191, chunkCoord.x, chunkCoord.y);
            if (chunkNoise > 0.88f || runtimeSandOverlayTiles == null || runtimeSandOverlayTiles.Length == 0)
            {
                return;
            }

            var localX = GetSparsePropLocalCell(seed + 193, chunkCoord.x, chunkCoord.y);
            var localY = GetSparsePropLocalCell(seed + 197, chunkCoord.x, chunkCoord.y);
            var index = localY * chunkSize + localX;
            sandOverlayChunkTileBuffer[index] = SelectDominantTile(runtimeSandOverlayTiles, chunkCoord.x * chunkSize + localX, chunkCoord.y * chunkSize + localY, seed + 191, 0.75f);
        }

        private void FillSandOverlayChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(chunkCoord, seed + 191, sandOverlayChunkTransformBuffer);
        }

        private void FillPebbleChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    pebbleChunkTileBuffer[index++] = SelectSparseLayerTile(runtimePebbleTiles, worldX, worldY, seed + 17, 0.991f);
                }
            }
        }

        private void FillPebbleChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(chunkCoord, seed + 17, pebbleChunkTransformBuffer);
        }

        private void FillRockChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    rockChunkTileBuffer[index++] = SelectSparseLayerTile(runtimeRockTiles, worldX, worldY, seed + 43, 0.996f);
                }
            }
        }

        private void FillRockChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(chunkCoord, seed + 43, rockChunkTransformBuffer);
        }

        private void FillSmoothChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    smoothChunkTileBuffer[index++] = SelectLayerTile(runtimeSmoothTiles, worldX, worldY, seed + 71, 0.84f, 10, 0.18f);
                }
            }
        }

        private void FillSmoothChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(chunkCoord, seed + 71, smoothChunkTransformBuffer);
        }

        private void SpawnChunkProps(Vector2Int chunkCoord)
        {
            var prefab = SelectSparsePropPrefab(chunkCoord);
            if (prefab == null || propParent == null)
            {
                return;
            }

            if (spawnedPropsByChunk.ContainsKey(chunkCoord))
            {
                return;
            }

            var chunkNoise = Hash01(seed + 109, chunkCoord.x, chunkCoord.y);
            if (chunkNoise > GetPropSpawnChance())
            {
                return;
            }

            var localX = GetSparsePropLocalCell(seed + 113, chunkCoord.x, chunkCoord.y);
            var localY = GetSparsePropLocalCell(seed + 127, chunkCoord.x, chunkCoord.y);
            var worldX = chunkCoord.x * chunkSize + localX + 0.5f;
            var worldY = chunkCoord.y * chunkSize + localY + 0.5f;
            var instance = Instantiate(prefab, new Vector3(worldX, worldY, 0f), Quaternion.identity, propParent);
            instance.name = $"{prefab.name} ({chunkCoord.x}, {chunkCoord.y})";

            spawnedPropsByChunk[chunkCoord] = new List<GameObject> { instance };
        }

        private GameObject SelectSparsePropPrefab(Vector2Int chunkCoord)
        {
            if (propPrefabs != null && propPrefabs.Length > 0)
            {
                var variantIndex = GetVariantIndex(seed + 167, chunkCoord.x, chunkCoord.y, propPrefabs.Length);
                var selected = propPrefabs[variantIndex];
                if (selected != null)
                {
                    return selected;
                }

                for (var i = 0; i < propPrefabs.Length; i++)
                {
                    if (propPrefabs[i] != null)
                    {
                        return propPrefabs[i];
                    }
                }
            }

            return propPrefab;
        }

        public float GetPropSpawnChance()
        {
            if (worldSettings != null)
            {
                return Mathf.Clamp01(worldSettings.PropSpawnChance);
            }

            return propSpawnChance;
        }

        private void DespawnChunkProps(Vector2Int chunkCoord)
        {
            if (!spawnedPropsByChunk.TryGetValue(chunkCoord, out var instances))
            {
                return;
            }

            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                {
                    Destroy(instances[i]);
                }
            }

            spawnedPropsByChunk.Remove(chunkCoord);
        }

        private void ClearSpawnedProps()
        {
            foreach (var instances in spawnedPropsByChunk.Values)
            {
                for (var i = 0; i < instances.Count; i++)
                {
                    if (instances[i] != null)
                    {
                        Destroy(instances[i]);
                    }
                }
            }

            spawnedPropsByChunk.Clear();
        }

        private void FillRotationBuffer(Vector2Int chunkCoord, int noiseSeed, Matrix4x4[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    buffer[index++] = CreateRotationMatrix(noiseSeed, worldX, worldY);
                }
            }
        }

        private TileBase SelectDominantTile(TileBase[] tiles, int worldX, int worldY, int noiseSeed, float accentChance)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return null;
            }

            if (tiles.Length == 1)
            {
                return tiles[0];
            }

            var baseTile = tiles[0];
            if (baseTile == null)
            {
                for (var i = 1; i < tiles.Length; i++)
                {
                    if (tiles[i] != null)
                    {
                        return tiles[i];
                    }
                }

                return null;
            }

            if (Hash01(noiseSeed + 101, worldX, worldY) >= accentChance)
            {
                return baseTile;
            }

            var accentIndex = 1 + GetVariantIndex(noiseSeed + 103, worldX, worldY, tiles.Length - 1);
            if (accentIndex < 0 || accentIndex >= tiles.Length || tiles[accentIndex] == null)
            {
                return baseTile;
            }

            return tiles[accentIndex];
        }

        private TileBase SelectTile(int worldX, int worldY)
        {
            return SelectDominantTile(runtimeTiles, worldX, worldY, seed, 0.24f);
        }

        private TileBase SelectLayerTile(TileBase[] tiles, int worldX, int worldY, int noiseSeed, float presenceThreshold, int regionScale, float detailChance)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return null;
            }

            var noise = Hash01(noiseSeed, worldX / Mathf.Max(1, regionScale), worldY / Mathf.Max(1, regionScale));
            if (noise < presenceThreshold)
            {
                return null;
            }

            if (tiles.Length == 1)
            {
                return tiles[0];
            }

            var variantNoise = Hash01(noiseSeed + 5, worldX, worldY);
            if (variantNoise < detailChance)
            {
                variantNoise = Mathf.Repeat(variantNoise * 1.73f + 0.19f, 1f);
            }

            var variant = Mathf.FloorToInt(variantNoise * tiles.Length);
            variant = Mathf.Clamp(variant, 0, tiles.Length - 1);
            return tiles[variant];
        }

        private TileBase SelectSparseLayerTile(TileBase[] tiles, int worldX, int worldY, int noiseSeed, float presenceThreshold)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return null;
            }

            var noise = Hash01(noiseSeed, worldX, worldY);
            if (noise < presenceThreshold)
            {
                return null;
            }

            if (tiles.Length == 1)
            {
                return tiles[0];
            }

            var variant = GetVariantIndex(noiseSeed + 11, worldX, worldY, tiles.Length);
            return tiles[variant];
        }

        private Vector2Int WorldToChunk(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / chunkSize),
                Mathf.FloorToInt(worldPosition.y / chunkSize));
        }

        private static float Hash01(int worldSeed, int x, int y)
        {
            unchecked
            {
                var hash = (uint)worldSeed;
                hash ^= (uint)(x * 374761393);
                hash = (hash << 13) ^ hash;
                hash ^= (uint)(y * 668265263);
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static int GetVariantIndex(int noiseSeed, int worldX, int worldY, int variantCount)
        {
            if (variantCount <= 1)
            {
                return 0;
            }

            var value = Hash01(noiseSeed, worldX, worldY) * variantCount;
            var variant = Mathf.FloorToInt(value);
            return Mathf.Clamp(variant, 0, variantCount - 1);
        }

        private int GetSparsePropLocalCell(int noiseSeed, int chunkX, int chunkY)
        {
            if (chunkSize <= 2)
            {
                return 0;
            }

            var value = Hash01(noiseSeed, chunkX, chunkY);
            var minCell = 1;
            var maxCell = chunkSize - 2;
            var span = Mathf.Max(1, maxCell - minCell + 1);
            var cell = minCell + Mathf.FloorToInt(value * span);
            return Mathf.Clamp(cell, minCell, maxCell);
        }

        private static Matrix4x4 CreateRotationMatrix(int noiseSeed, int worldX, int worldY)
        {
            var rotationIndex = Mathf.FloorToInt(Hash01(noiseSeed + 97, worldX, worldY) * 4f);
            rotationIndex = Mathf.Clamp(rotationIndex, 0, 3);
            if (rotationIndex == 0)
            {
                return Matrix4x4.identity;
            }

            return Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotationIndex * 90f));
        }

        private TileBase[] CreateFallbackSandOverlayTiles()
        {
            var sprites = new[]
            {
                CreateDetailSprite(new Color32(217, 190, 132, 145), DetailPattern.SmoothPatchA),
                CreateDetailSprite(new Color32(227, 200, 142, 140), DetailPattern.SmoothPatchB),
                CreateDetailSprite(new Color32(209, 183, 126, 145), DetailPattern.SmoothPatchC),
                CreateDetailSprite(new Color32(221, 194, 136, 140), DetailPattern.SmoothPatchD)
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackSandOverlayTile_{i}";
                tile.hideFlags = HideFlags.HideAndDontSave;
                tiles[i] = tile;
            }

            return tiles;
        }

        private static void ApplyChunkTransforms(Tilemap targetTilemap, BoundsInt bounds, Matrix4x4[] transforms)
        {
            if (targetTilemap == null || transforms == null)
            {
                return;
            }

            var index = 0;
            for (var y = 0; y < bounds.size.y; y++)
            {
                for (var x = 0; x < bounds.size.x; x++)
                {
                    targetTilemap.SetTransformMatrix(
                        new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0),
                        transforms[index++]);
                }
            }
        }

        private enum DetailPattern
        {
            PebblesSmall,
            PebblesPatch,
            PebblesMixed,
            PebblesSparse,
            RocksSmall,
            RocksPatch,
            RocksMixed,
            RocksSparse,
            SmoothPatchA,
            SmoothPatchB,
            SmoothPatchC,
            SmoothPatchD
        }
    }
}
