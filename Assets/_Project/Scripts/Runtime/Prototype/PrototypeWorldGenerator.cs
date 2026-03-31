using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        [SerializeField] private Tilemap ruleGroundTilemap;
        [SerializeField] private RuleTile ruleGroundTile;
        [SerializeField] private Tilemap pebbleTilemap;
        [SerializeField] private Sprite[] pebbleTileSprites;
        [SerializeField] private Tilemap rockTilemap;
        [SerializeField] private Sprite[] rockTileSprites;
        [SerializeField] private Tilemap smoothTilemap;
        [SerializeField] private Sprite[] smoothTileSprites;
        [SerializeField] private Tilemap sandOverlayTilemap;
        [SerializeField] private Sprite[] sandOverlayTileSprites;
        [SerializeField] private Tilemap sandOverlayOffsetTilemap;
        [SerializeField] private Sprite[] sandOverlayOffsetTileSprites;
        [SerializeField, Min(4f)] private float sandPatchRegionSizeWorld = 18f;
        [SerializeField, Min(1f)] private float sandPatchMinRadiusWorld = 4f;
        [SerializeField, Min(1f)] private float sandPatchMaxRadiusWorld = 9f;
        [SerializeField, Range(0f, 1f)] private float sandPatchRegionChance = 0.52f;
        [SerializeField, Range(0f, 1.5f)] private float sandPatchEdgeNoise = 0.42f;
        [SerializeField, Range(0f, 1f)] private float sandPatchWobbleStrength = 0.34f;
        [SerializeField, Min(1f)] private float sandPatchWobbleScaleWorld = 2.5f;
        [SerializeField, Range(0f, 1f)] private float sandPatchErosion = 0.12f;
        [SerializeField, Range(0f, 1f)] private float sandPatchInteriorCutout = 0.3f;
        [SerializeField, Range(0f, 1f)] private float sandPatchRibbonBias = 0.38f;
        [SerializeField, Min(1f)] private float sandPatchRibbonScaleWorld = 4f;
        [SerializeField] private int seed = 12345;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int chunkRadius = 4;
        [SerializeField] private int chunkOperationsPerFrame = 4;

        private readonly HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> chunkLoadQueue = new Queue<Vector2Int>();
        private readonly Queue<Vector2Int> chunkUnloadQueue = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedLoadChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedUnloadChunks = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, List<GameObject>> spawnedPropsByChunk = new Dictionary<Vector2Int, List<GameObject>>();
        private readonly Dictionary<Vector2Int, TileBase[]> ruleGroundChunkCache = new Dictionary<Vector2Int, TileBase[]>();
        private Tilemap tilemap;
        private TileBase[] runtimeTiles;
        private TileBase[] runtimePebbleTiles;
        private TileBase[] runtimeRockTiles;
        private TileBase[] runtimeSmoothTiles;
        private TileBase[] runtimeSandOverlayTiles;
        private TileBase[] runtimeSandOverlayOffsetTiles;
        private RuleTile runtimeRuleGroundTile;
        private TileBase[] ruleGroundChunkTiles;
        private TileBase[] emptyChunkTiles;
        private TileBase[] chunkTileBuffer;
        private TileBase[] sandOverlayChunkTileBuffer;
        private TileBase[] sandOverlayOffsetChunkTileBuffer;
        private TileBase[] pebbleChunkTileBuffer;
        private TileBase[] rockChunkTileBuffer;
        private TileBase[] smoothChunkTileBuffer;
        private TileBase[] emptyRuleGroundChunkTiles;
        private TileBase[] emptySandOverlayChunkTiles;
        private TileBase[] emptySandOverlayOffsetChunkTiles;
        private TileBase[] emptyPebbleChunkTiles;
        private TileBase[] emptyRockChunkTiles;
        private TileBase[] emptySmoothChunkTiles;
        private Matrix4x4[] chunkTransformBuffer;
        private Matrix4x4[] sandOverlayChunkTransformBuffer;
        private Matrix4x4[] sandOverlayOffsetChunkTransformBuffer;
        private Matrix4x4[] pebbleChunkTransformBuffer;
        private Matrix4x4[] rockChunkTransformBuffer;
        private Matrix4x4[] smoothChunkTransformBuffer;
        private Vector2Int currentCenterChunk;

        public int Seed => seed;
        public Vector2Int CurrentCenterChunk => currentCenterChunk;
        public int VisibleChunkCount => visibleChunks.Count;
        public int PendingLoadChunkCount => chunkLoadQueue.Count;
        public int PendingUnloadChunkCount => chunkUnloadQueue.Count;

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
            chunkLoadQueue.Clear();
            chunkUnloadQueue.Clear();
            queuedLoadChunks.Clear();
            queuedUnloadChunks.Clear();
            ruleGroundChunkCache.Clear();
            ClearSpawnedProps();
            tilemap.ClearAllTiles();
            if (ruleGroundTilemap != null)
            {
                ruleGroundTilemap.ClearAllTiles();
            }
            if (pebbleTilemap != null)
            {
                pebbleTilemap.ClearAllTiles();
            }
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.ClearAllTiles();
            }
            if (sandOverlayOffsetTilemap != null)
            {
                sandOverlayOffsetTilemap.ClearAllTiles();
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
            ProcessChunkQueue(requiredChunks.Count);
        }

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            if (worldSettings == null)
            {
                worldSettings = GetComponentInParent<PrototypeWorldSettings>();
            }

            ConfigureTilemapRenderers();
            chunkSize = Mathf.Max(1, chunkSize);
            chunkRadius = Mathf.Max(0, chunkRadius);
            chunkOperationsPerFrame = Mathf.Max(1, chunkOperationsPerFrame);
        }

        private void Start()
        {
            RefreshChunkTargets(true);
            ProcessChunkQueue(requiredChunks.Count);
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

            ConfigureTilemapRenderers();
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
                    QueueChunkUnloadOperation(chunk);
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
                            QueueChunkLoadOperation(chunk);
                        }
                    }
                }
            }
        }

        private void EnsureRuntimeTiles()
        {
            var sandOverlayOffsetSpriteCount =
                sandOverlayOffsetTileSprites != null && sandOverlayOffsetTileSprites.Length > 0
                    ? sandOverlayOffsetTileSprites.Length
                    : sandOverlayTileSprites != null
                        ? sandOverlayTileSprites.Length
                        : 0;

            if (runtimeTiles != null &&
                runtimePebbleTiles != null &&
                runtimeRockTiles != null &&
                runtimeSmoothTiles != null &&
                runtimeSandOverlayTiles != null &&
                runtimeSandOverlayOffsetTiles != null &&
                runtimeRuleGroundTile != null &&
                ruleGroundTile != null &&
                tileSprites != null &&
                pebbleTileSprites != null &&
                rockTileSprites != null &&
                smoothTileSprites != null &&
                sandOverlayTileSprites != null &&
                runtimeTiles.Length == tileSprites.Length &&
                runtimePebbleTiles.Length == pebbleTileSprites.Length &&
                runtimeRockTiles.Length == rockTileSprites.Length &&
                runtimeSmoothTiles.Length == smoothTileSprites.Length &&
                runtimeSandOverlayTiles.Length == sandOverlayTileSprites.Length &&
                runtimeSandOverlayOffsetTiles.Length == sandOverlayOffsetSpriteCount)
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
            runtimeSandOverlayOffsetTiles = BuildRuntimeTiles(
                sandOverlayOffsetTileSprites != null && sandOverlayOffsetTileSprites.Length > 0
                    ? sandOverlayOffsetTileSprites
                    : sandOverlayTileSprites,
                CreateFallbackSandOverlayTiles,
                "PrototypeRuntimeSandOverlayOffsetTile");
            runtimeRuleGroundTile = ruleGroundTile;

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
            EnsureTileBuffer(ref chunkTileBuffer, GetChunkTileCount(tilemap));
            EnsureTileBuffer(ref ruleGroundChunkTiles, GetChunkTileCount(ruleGroundTilemap));
            EnsureTileBuffer(ref sandOverlayChunkTileBuffer, GetChunkTileCount(sandOverlayTilemap));
            EnsureTileBuffer(ref sandOverlayOffsetChunkTileBuffer, GetChunkTileCount(sandOverlayOffsetTilemap));
            EnsureTileBuffer(ref pebbleChunkTileBuffer, GetChunkTileCount(pebbleTilemap));
            EnsureTileBuffer(ref rockChunkTileBuffer, GetChunkTileCount(rockTilemap));
            EnsureTileBuffer(ref smoothChunkTileBuffer, GetChunkTileCount(smoothTilemap));

            EnsureTileBuffer(ref emptyChunkTiles, chunkTileBuffer != null ? chunkTileBuffer.Length : 0);
            EnsureTileBuffer(ref emptyRuleGroundChunkTiles, ruleGroundChunkTiles != null ? ruleGroundChunkTiles.Length : 0);
            EnsureTileBuffer(ref emptySandOverlayChunkTiles, sandOverlayChunkTileBuffer != null ? sandOverlayChunkTileBuffer.Length : 0);
            EnsureTileBuffer(ref emptySandOverlayOffsetChunkTiles, sandOverlayOffsetChunkTileBuffer != null ? sandOverlayOffsetChunkTileBuffer.Length : 0);
            EnsureTileBuffer(ref emptyPebbleChunkTiles, pebbleChunkTileBuffer != null ? pebbleChunkTileBuffer.Length : 0);
            EnsureTileBuffer(ref emptyRockChunkTiles, rockChunkTileBuffer != null ? rockChunkTileBuffer.Length : 0);
            EnsureTileBuffer(ref emptySmoothChunkTiles, smoothChunkTileBuffer != null ? smoothChunkTileBuffer.Length : 0);

            EnsureTransformBuffer(ref chunkTransformBuffer, chunkTileBuffer != null ? chunkTileBuffer.Length : 0);
            EnsureTransformBuffer(ref sandOverlayChunkTransformBuffer, sandOverlayChunkTileBuffer != null ? sandOverlayChunkTileBuffer.Length : 0);
            EnsureTransformBuffer(ref sandOverlayOffsetChunkTransformBuffer, sandOverlayOffsetChunkTileBuffer != null ? sandOverlayOffsetChunkTileBuffer.Length : 0);
            EnsureTransformBuffer(ref pebbleChunkTransformBuffer, pebbleChunkTileBuffer != null ? pebbleChunkTileBuffer.Length : 0);
            EnsureTransformBuffer(ref rockChunkTransformBuffer, rockChunkTileBuffer != null ? rockChunkTileBuffer.Length : 0);
            EnsureTransformBuffer(ref smoothChunkTransformBuffer, smoothChunkTileBuffer != null ? smoothChunkTileBuffer.Length : 0);
        }

        private static void EnsureTileBuffer(ref TileBase[] buffer, int tileCount)
        {
            tileCount = Mathf.Max(0, tileCount);
            if (buffer != null && buffer.Length == tileCount)
            {
                return;
            }

            buffer = tileCount > 0 ? new TileBase[tileCount] : System.Array.Empty<TileBase>();
        }

        private static void EnsureTransformBuffer(ref Matrix4x4[] buffer, int tileCount)
        {
            tileCount = Mathf.Max(0, tileCount);
            if (buffer != null && buffer.Length == tileCount)
            {
                return;
            }

            buffer = tileCount > 0 ? new Matrix4x4[tileCount] : System.Array.Empty<Matrix4x4>();
        }

        private void QueueChunkLoadOperation(Vector2Int chunkCoord)
        {
            if (queuedLoadChunks.Add(chunkCoord))
            {
                chunkLoadQueue.Enqueue(chunkCoord);
            }
        }

        private void QueueChunkUnloadOperation(Vector2Int chunkCoord)
        {
            if (queuedUnloadChunks.Add(chunkCoord))
            {
                chunkUnloadQueue.Enqueue(chunkCoord);
            }
        }

        private void ProcessChunkQueue()
        {
            ProcessChunkQueue(chunkOperationsPerFrame);
        }

        private void ProcessChunkQueue(int operationsBudget)
        {
            var operationsRemaining = Mathf.Max(0, operationsBudget);
            operationsRemaining = ProcessChunkQueue(chunkLoadQueue, queuedLoadChunks, operationsRemaining);
            ProcessChunkQueue(chunkUnloadQueue, queuedUnloadChunks, operationsRemaining);
        }

        private int ProcessChunkQueue(
            Queue<Vector2Int> queue,
            HashSet<Vector2Int> queuedChunks,
            int operationsRemaining)
        {
            while (operationsRemaining > 0 && queue.Count > 0)
            {
                var chunk = queue.Dequeue();
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

            return operationsRemaining;
        }

        private void LoadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var sandBounds = GetChunkBounds(tilemap, chunkCoord);
            var ruleGroundBounds = GetChunkBounds(ruleGroundTilemap, chunkCoord);
            var sandOverlayBounds = GetChunkBounds(sandOverlayTilemap, chunkCoord);
            var sandOverlayOffsetBounds = GetChunkBounds(sandOverlayOffsetTilemap, chunkCoord);
            var pebbleBounds = GetChunkBounds(pebbleTilemap, chunkCoord);
            var rockBounds = GetChunkBounds(rockTilemap, chunkCoord);
            var smoothBounds = GetChunkBounds(smoothTilemap, chunkCoord);
            FillRuleGroundChunkBuffer(chunkCoord);
            if (ruleGroundTilemap != null)
            {
                ruleGroundTilemap.SetTilesBlock(ruleGroundBounds, ruleGroundChunkTiles);
                RefreshRuleGroundBounds(ruleGroundBounds);
            }
            FillChunkBuffer(chunkCoord);
            FillChunkTransforms(chunkCoord);
            tilemap.SetTilesBlock(sandBounds, chunkTileBuffer);
            ApplyChunkTransforms(tilemap, sandBounds, chunkTransformBuffer);
            FillSandOverlayChunkBuffer(chunkCoord);
            FillSandOverlayChunkTransforms(chunkCoord);
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.SetTilesBlock(sandOverlayBounds, sandOverlayChunkTileBuffer);
                ApplyChunkTransforms(sandOverlayTilemap, sandOverlayBounds, sandOverlayChunkTransformBuffer);
            }
            FillSandOverlayOffsetChunkBuffer(chunkCoord);
            FillSandOverlayOffsetChunkTransforms(chunkCoord);
            if (sandOverlayOffsetTilemap != null)
            {
                sandOverlayOffsetTilemap.SetTilesBlock(sandOverlayOffsetBounds, sandOverlayOffsetChunkTileBuffer);
                ApplyChunkTransforms(sandOverlayOffsetTilemap, sandOverlayOffsetBounds, sandOverlayOffsetChunkTransformBuffer);
            }
            FillPebbleChunkBuffer(chunkCoord);
            FillPebbleChunkTransforms(chunkCoord);
            FillRockChunkBuffer(chunkCoord);
            FillRockChunkTransforms(chunkCoord);
            FillSmoothChunkBuffer(chunkCoord);
            FillSmoothChunkTransforms(chunkCoord);
            if (pebbleTilemap != null)
            {
                pebbleTilemap.SetTilesBlock(pebbleBounds, pebbleChunkTileBuffer);
                ApplyChunkTransforms(pebbleTilemap, pebbleBounds, pebbleChunkTransformBuffer);
            }
            if (rockTilemap != null)
            {
                rockTilemap.SetTilesBlock(rockBounds, rockChunkTileBuffer);
                ApplyChunkTransforms(rockTilemap, rockBounds, rockChunkTransformBuffer);
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.SetTilesBlock(smoothBounds, smoothChunkTileBuffer);
                ApplyChunkTransforms(smoothTilemap, smoothBounds, smoothChunkTransformBuffer);
            }
            SpawnChunkProps(chunkCoord);
            visibleChunks.Add(chunkCoord);
        }

        private void UnloadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var sandBounds = GetChunkBounds(tilemap, chunkCoord);
            var ruleGroundBounds = GetChunkBounds(ruleGroundTilemap, chunkCoord);
            var sandOverlayBounds = GetChunkBounds(sandOverlayTilemap, chunkCoord);
            var sandOverlayOffsetBounds = GetChunkBounds(sandOverlayOffsetTilemap, chunkCoord);
            var pebbleBounds = GetChunkBounds(pebbleTilemap, chunkCoord);
            var rockBounds = GetChunkBounds(rockTilemap, chunkCoord);
            var smoothBounds = GetChunkBounds(smoothTilemap, chunkCoord);
            if (ruleGroundTilemap != null)
            {
                ruleGroundTilemap.SetTilesBlock(ruleGroundBounds, emptyRuleGroundChunkTiles);
                RefreshRuleGroundBounds(ruleGroundBounds);
            }
            tilemap.SetTilesBlock(sandBounds, emptyChunkTiles);
            if (sandOverlayTilemap != null)
            {
                sandOverlayTilemap.SetTilesBlock(sandOverlayBounds, emptySandOverlayChunkTiles);
            }
            if (sandOverlayOffsetTilemap != null)
            {
                sandOverlayOffsetTilemap.SetTilesBlock(sandOverlayOffsetBounds, emptySandOverlayOffsetChunkTiles);
            }
            if (pebbleTilemap != null)
            {
                pebbleTilemap.SetTilesBlock(pebbleBounds, emptyPebbleChunkTiles);
            }
            if (rockTilemap != null)
            {
                rockTilemap.SetTilesBlock(rockBounds, emptyRockChunkTiles);
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.SetTilesBlock(smoothBounds, emptySmoothChunkTiles);
            }
            DespawnChunkProps(chunkCoord);
            visibleChunks.Remove(chunkCoord);
        }

        private BoundsInt GetChunkBounds(Tilemap targetTilemap, Vector2Int chunkCoord)
        {
            var cellsPerChunk = GetCellsPerChunk(targetTilemap);
            return new BoundsInt(
                chunkCoord.x * cellsPerChunk,
                chunkCoord.y * cellsPerChunk,
                0,
                cellsPerChunk,
                cellsPerChunk,
                1);
        }

        private void FillChunkBuffer(Vector2Int chunkCoord)
        {
            FillTileBuffer(tilemap, chunkCoord, chunkTileBuffer, SelectTile);
        }

        private void FillRuleGroundChunkBuffer(Vector2Int chunkCoord)
        {
            if (ruleGroundChunkTiles == null)
            {
                return;
            }

            if (!ruleGroundChunkCache.TryGetValue(chunkCoord, out var cachedChunkTiles) || cachedChunkTiles.Length != ruleGroundChunkTiles.Length)
            {
                FillTileBuffer(ruleGroundTilemap, chunkCoord, ruleGroundChunkTiles, SelectRuleGroundTile);
                cachedChunkTiles = new TileBase[ruleGroundChunkTiles.Length];
                System.Array.Copy(ruleGroundChunkTiles, cachedChunkTiles, ruleGroundChunkTiles.Length);
                ruleGroundChunkCache[chunkCoord] = cachedChunkTiles;
                return;
            }

            System.Array.Copy(cachedChunkTiles, ruleGroundChunkTiles, ruleGroundChunkTiles.Length);
        }

        private void RefreshRuleGroundBounds(BoundsInt bounds)
        {
            if (ruleGroundTilemap == null)
            {
                return;
            }

            for (var y = bounds.yMin - 1; y <= bounds.yMax; y++)
            {
                for (var x = bounds.xMin - 1; x <= bounds.xMax; x++)
                {
                    ruleGroundTilemap.RefreshTile(new Vector3Int(x, y, 0));
                }
            }
        }

        private void FillChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(tilemap, chunkCoord, seed, chunkTransformBuffer);
        }

        private void FillSandOverlayChunkBuffer(Vector2Int chunkCoord)
        {
            System.Array.Clear(sandOverlayChunkTileBuffer, 0, sandOverlayChunkTileBuffer.Length);

            if (runtimeSandOverlayTiles == null || runtimeSandOverlayTiles.Length == 0)
            {
                return;
            }

            FillTileBuffer(
                sandOverlayTilemap,
                chunkCoord,
                sandOverlayChunkTileBuffer,
                (worldX, worldY) => SelectSparseLayerTile(runtimeSandOverlayTiles, worldX, worldY, seed + 191, 0.84f));
        }

        private void FillSandOverlayChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(sandOverlayTilemap, chunkCoord, seed + 191, sandOverlayChunkTransformBuffer);
        }

        private void FillSandOverlayOffsetChunkBuffer(Vector2Int chunkCoord)
        {
            System.Array.Clear(sandOverlayOffsetChunkTileBuffer, 0, sandOverlayOffsetChunkTileBuffer.Length);

            if (runtimeSandOverlayOffsetTiles == null || runtimeSandOverlayOffsetTiles.Length == 0)
            {
                return;
            }

            FillTileBuffer(
                sandOverlayOffsetTilemap,
                chunkCoord,
                sandOverlayOffsetChunkTileBuffer,
                (worldX, worldY) => SelectSparseLayerTile(runtimeSandOverlayOffsetTiles, worldX, worldY, seed + 223, 0.84f));
        }

        private void FillSandOverlayOffsetChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(sandOverlayOffsetTilemap, chunkCoord, seed + 223, sandOverlayOffsetChunkTransformBuffer);
        }

        private void FillPebbleChunkBuffer(Vector2Int chunkCoord)
        {
            FillTileBuffer(
                pebbleTilemap,
                chunkCoord,
                pebbleChunkTileBuffer,
                (worldX, worldY) => SelectSparseLayerTile(runtimePebbleTiles, worldX, worldY, seed + 17, 0.991f));
        }

        private void FillPebbleChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(pebbleTilemap, chunkCoord, seed + 17, pebbleChunkTransformBuffer);
        }

        private void FillRockChunkBuffer(Vector2Int chunkCoord)
        {
            FillTileBuffer(
                rockTilemap,
                chunkCoord,
                rockChunkTileBuffer,
                (worldX, worldY) => SelectSparseLayerTile(runtimeRockTiles, worldX, worldY, seed + 43, 0.996f));
        }

        private void FillRockChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(rockTilemap, chunkCoord, seed + 43, rockChunkTransformBuffer);
        }

        private void FillSmoothChunkBuffer(Vector2Int chunkCoord)
        {
            var regionScale = ScaleWorldUnitsToSample(10f);
            FillTileBuffer(
                smoothTilemap,
                chunkCoord,
                smoothChunkTileBuffer,
                (worldX, worldY) => SelectLayerTile(runtimeSmoothTiles, worldX, worldY, seed + 71, 0.84f, regionScale, 0.18f));
        }

        private void FillSmoothChunkTransforms(Vector2Int chunkCoord)
        {
            FillRotationBuffer(smoothTilemap, chunkCoord, seed + 71, smoothChunkTransformBuffer);
        }

        private void SpawnChunkProps(Vector2Int chunkCoord)
        {
            var definition = SelectSparsePropDefinition(chunkCoord);
            var prefab = definition != null ? definition.Prefab : SelectSparsePropPrefab(chunkCoord);
            if (prefab == null || propParent == null)
            {
                return;
            }

            if (spawnedPropsByChunk.ContainsKey(chunkCoord))
            {
                return;
            }

            var chunkNoise = Hash01(seed + 109, chunkCoord.x, chunkCoord.y);
            var spawnChance = definition != null
                ? GetPropSpawnChance() * definition.SpawnChanceMultiplier
                : GetPropSpawnChance();
            if (chunkNoise > spawnChance)
            {
                return;
            }

            var localX = GetSparsePropLocalCell(seed + 113, chunkCoord.x, chunkCoord.y);
            var localY = GetSparsePropLocalCell(seed + 127, chunkCoord.x, chunkCoord.y);
            var worldX = chunkCoord.x * chunkSize + localX + 0.5f;
            var worldY = chunkCoord.y * chunkSize + localY + 0.5f;
            var instance = Instantiate(prefab, new Vector3(worldX, worldY, 0f), Quaternion.identity, propParent);
            instance.name = $"{prefab.name} ({chunkCoord.x}, {chunkCoord.y})";
            EnsureSortingGroup(instance);

            spawnedPropsByChunk[chunkCoord] = new List<GameObject> { instance };
        }

        private PrototypeWorldPropDefinition SelectSparsePropDefinition(Vector2Int chunkCoord)
        {
            var catalog = worldSettings != null ? worldSettings.PropCatalog : null;
            if (catalog == null)
            {
                return null;
            }

            var totalWeight = 0f;
            AccumulateCatalogWeights(catalog.Entries, ref totalWeight);
            if (catalog.BiomeGroups != null)
            {
                for (var i = 0; i < catalog.BiomeGroups.Count; i++)
                {
                    var group = catalog.BiomeGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    AccumulateCatalogWeights(group.Entries, ref totalWeight);
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            var pick = Hash01(seed + 167, chunkCoord.x, chunkCoord.y) * totalWeight;
            if (TryPickCatalogEntry(catalog.Entries, ref pick, out var selectedEntry))
            {
                return selectedEntry;
            }

            if (catalog.BiomeGroups != null)
            {
                for (var i = 0; i < catalog.BiomeGroups.Count; i++)
                {
                    var group = catalog.BiomeGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    if (TryPickCatalogEntry(group.Entries, ref pick, out selectedEntry))
                    {
                        return selectedEntry;
                    }
                }
            }

            if (TryGetLastValidCatalogEntry(catalog.Entries, out selectedEntry))
            {
                return selectedEntry;
            }

            if (catalog.BiomeGroups != null)
            {
                for (var i = catalog.BiomeGroups.Count - 1; i >= 0; i--)
                {
                    var group = catalog.BiomeGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    if (TryGetLastValidCatalogEntry(group.Entries, out selectedEntry))
                    {
                        return selectedEntry;
                    }
                }
            }

            return null;
        }

        private static void AccumulateCatalogWeights(IReadOnlyList<PrototypeWorldPropDefinition> entries, ref float totalWeight)
        {
            if (entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                totalWeight += entry.Weight;
            }
        }

        private static bool TryPickCatalogEntry(
            IReadOnlyList<PrototypeWorldPropDefinition> entries,
            ref float pick,
            out PrototypeWorldPropDefinition selectedEntry)
        {
            if (entries != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry == null || !entry.IsValid)
                    {
                        continue;
                    }

                    if (pick <= entry.Weight)
                    {
                        selectedEntry = entry;
                        return true;
                    }

                    pick -= entry.Weight;
                }
            }

            selectedEntry = null;
            return false;
        }

        private static bool TryGetLastValidCatalogEntry(
            IReadOnlyList<PrototypeWorldPropDefinition> entries,
            out PrototypeWorldPropDefinition selectedEntry)
        {
            if (entries != null)
            {
                for (var i = entries.Count - 1; i >= 0; i--)
                {
                    var entry = entries[i];
                    if (entry != null && entry.IsValid)
                    {
                        selectedEntry = entry;
                        return true;
                    }
                }
            }

            selectedEntry = null;
            return false;
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

        private static void EnsureSortingGroup(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var renderers = instance.GetComponentsInChildren<SpriteRenderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var legacySorter = instance.GetComponent<PrototypeSpriteDepthSorter>();
            if (legacySorter != null)
            {
                Destroy(legacySorter);
            }

            var sortingGroup = instance.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = instance.AddComponent<SortingGroup>();
            }

            sortingGroup.sortingLayerID = 0;
            sortingGroup.sortingOrder = 100;
        }

        private void FillRotationBuffer(Tilemap targetTilemap, Vector2Int chunkCoord, int noiseSeed, Matrix4x4[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            var cellsPerChunk = GetCellsPerChunk(targetTilemap);
            var index = 0;
            for (var localY = 0; localY < cellsPerChunk; localY++)
            {
                for (var localX = 0; localX < cellsPerChunk; localX++)
                {
                    GetLayerSamplePosition(targetTilemap, chunkCoord, localX, localY, out var worldX, out var worldY);
                    buffer[index++] = CreateRotationMatrix(noiseSeed, worldX, worldY);
                }
            }
        }

        private void FillTileBuffer(Tilemap targetTilemap, Vector2Int chunkCoord, TileBase[] buffer, System.Func<int, int, TileBase> selector)
        {
            if (buffer == null || selector == null)
            {
                return;
            }

            var cellsPerChunk = GetCellsPerChunk(targetTilemap);
            var index = 0;
            for (var localY = 0; localY < cellsPerChunk; localY++)
            {
                for (var localX = 0; localX < cellsPerChunk; localX++)
                {
                    GetLayerSamplePosition(targetTilemap, chunkCoord, localX, localY, out var worldX, out var worldY);
                    buffer[index++] = selector(worldX, worldY);
                }
            }
        }

        private TileBase SelectRuleGroundTile(int worldX, int worldY)
        {
            if (runtimeRuleGroundTile == null)
            {
                return null;
            }

            return IsInsideSandPatch(worldX, worldY) ? runtimeRuleGroundTile : null;
        }

        private bool IsInsideSandPatch(int worldX, int worldY)
        {
            var regionSize = Mathf.Max(1, ScaleWorldUnitsToSample(sandPatchRegionSizeWorld));
            var minRadius = Mathf.Max(1f, ScaleWorldUnitsToSample(sandPatchMinRadiusWorld));
            var maxRadius = Mathf.Max(minRadius, ScaleWorldUnitsToSample(sandPatchMaxRadiusWorld));
            var wobbleScale = Mathf.Max(1, ScaleWorldUnitsToSample(sandPatchWobbleScaleWorld));
            var ribbonScale = Mathf.Max(1, ScaleWorldUnitsToSample(sandPatchRibbonScaleWorld));
            var localErosion = Hash01(seed + 257, worldX / wobbleScale, worldY / wobbleScale);
            var regionX = Mathf.FloorToInt((float)worldX / regionSize);
            var regionY = Mathf.FloorToInt((float)worldY / regionSize);
            var bestCoverage = float.MinValue;

            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    var candidateRegionX = regionX + offsetX;
                    var candidateRegionY = regionY + offsetY;
                    if (Hash01(seed + 211, candidateRegionX, candidateRegionY) > sandPatchRegionChance)
                    {
                        continue;
                    }

                    var centerX = (candidateRegionX * regionSize) + (Hash01(seed + 223, candidateRegionX, candidateRegionY) * regionSize);
                    var centerY = (candidateRegionY * regionSize) + (Hash01(seed + 227, candidateRegionX, candidateRegionY) * regionSize);
                    var radiusX = Mathf.Lerp(minRadius, maxRadius, Hash01(seed + 229, candidateRegionX, candidateRegionY));
                    var radiusY = Mathf.Lerp(minRadius, maxRadius, Hash01(seed + 233, candidateRegionX, candidateRegionY));

                    var warpedWorldX = worldX + ((Hash01(seed + 241, worldX / wobbleScale, worldY / wobbleScale) - 0.5f) * radiusX * sandPatchWobbleStrength);
                    var warpedWorldY = worldY + ((Hash01(seed + 251, worldX / wobbleScale, worldY / wobbleScale) - 0.5f) * radiusY * sandPatchWobbleStrength);
                    var dx = (warpedWorldX - centerX) / Mathf.Max(1f, radiusX);
                    var dy = (warpedWorldY - centerY) / Mathf.Max(1f, radiusY);
                    var edgeThreshold = 1f + ((Hash01(seed + 239, worldX, worldY) - 0.5f) * sandPatchEdgeNoise);
                    var coreDistance = (dx * dx) + (dy * dy);
                    var ribbonNoise = Mathf.Abs((Hash01(seed + 259, worldX / ribbonScale, worldY / ribbonScale) * 2f) - 1f);
                    var ribbonPreference = Mathf.Lerp(1f, ribbonNoise, sandPatchRibbonBias);
                    if (coreDistance <= edgeThreshold)
                    {
                        var edgeScore = 1f - Mathf.Clamp01(coreDistance / Mathf.Max(0.001f, edgeThreshold));
                        bestCoverage = Mathf.Max(bestCoverage, edgeScore * ribbonPreference);
                    }

                    if (Hash01(seed + 263, candidateRegionX, candidateRegionY) > 0.58f)
                    {
                        var lobeCenterX = centerX + ((Hash01(seed + 269, candidateRegionX, candidateRegionY) - 0.5f) * radiusX * 1.1f);
                        var lobeCenterY = centerY + ((Hash01(seed + 271, candidateRegionX, candidateRegionY) - 0.5f) * radiusY * 1.1f);
                        var lobeRadius = Mathf.Lerp(minRadius * 0.28f, maxRadius * 0.52f, Hash01(seed + 277, candidateRegionX, candidateRegionY));
                        var lobeDx = (warpedWorldX - lobeCenterX) / Mathf.Max(1f, lobeRadius);
                        var lobeDy = (warpedWorldY - lobeCenterY) / Mathf.Max(1f, lobeRadius);
                        var lobeThreshold = 1f + ((Hash01(seed + 281, worldX, worldY) - 0.5f) * sandPatchEdgeNoise * 1.2f);
                        var lobeDistance = (lobeDx * lobeDx) + (lobeDy * lobeDy);
                        if (lobeDistance <= lobeThreshold)
                        {
                            var lobeScore = 1f - Mathf.Clamp01(lobeDistance / Mathf.Max(0.001f, lobeThreshold));
                            bestCoverage = Mathf.Max(bestCoverage, lobeScore * 0.9f);
                        }
                    }
                }
            }

            if (bestCoverage <= 0f)
            {
                return false;
            }

            var cutoutNoise = Hash01(seed + 283, worldX / ribbonScale, worldY / ribbonScale);
            var interiorThreshold = Mathf.Lerp(0.08f, 0.72f, sandPatchInteriorCutout);
            if (bestCoverage > interiorThreshold)
            {
                var interiorCarve = Hash01(seed + 293, worldX / ribbonScale, worldY / ribbonScale);
                var carveChance = Mathf.Lerp(0.12f, 0.68f, sandPatchInteriorCutout);
                return localErosion >= sandPatchErosion && interiorCarve >= carveChance;
            }

            var edgeAdmission = Mathf.Lerp(0.16f, 0.82f, bestCoverage);
            return localErosion >= sandPatchErosion * 0.65f && cutoutNoise <= edgeAdmission;
        }

        private void GetLayerSamplePosition(Tilemap targetTilemap, Vector2Int chunkCoord, int localX, int localY, out int worldX, out int worldY)
        {
            var sampleScale = GetWorldSampleScale();
            var cellSize = GetTilemapCellWorldSize(targetTilemap);
            var chunkOriginX = chunkCoord.x * ScaleWorldUnitsToSample(chunkSize);
            var chunkOriginY = chunkCoord.y * ScaleWorldUnitsToSample(chunkSize);
            var cellStep = Mathf.Max(1, Mathf.RoundToInt(cellSize * sampleScale));
            var halfStep = Mathf.Max(0, cellStep / 2);

            worldX = chunkOriginX + (localX * cellStep) + halfStep;
            worldY = chunkOriginY + (localY * cellStep) + halfStep;
        }

        private int GetChunkTileCount(Tilemap targetTilemap)
        {
            var cellsPerChunk = GetCellsPerChunk(targetTilemap);
            return cellsPerChunk * cellsPerChunk;
        }

        private int GetCellsPerChunk(Tilemap targetTilemap)
        {
            var cellSize = GetTilemapCellWorldSize(targetTilemap);
            return Mathf.Max(1, Mathf.RoundToInt(chunkSize / cellSize));
        }

        private float GetTilemapCellWorldSize(Tilemap targetTilemap)
        {
            if (targetTilemap == null || targetTilemap.layoutGrid == null)
            {
                return 1f;
            }

            return Mathf.Max(0.0001f, Mathf.Abs(targetTilemap.layoutGrid.cellSize.x));
        }

        private int GetWorldSampleScale()
        {
            var minCellSize = 1f;
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(tilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(ruleGroundTilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(sandOverlayTilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(sandOverlayOffsetTilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(pebbleTilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(rockTilemap));
            minCellSize = Mathf.Min(minCellSize, GetTilemapCellWorldSize(smoothTilemap));

            return Mathf.Max(1, Mathf.RoundToInt(2f / minCellSize));
        }

        private int ScaleWorldUnitsToSample(float worldUnits)
        {
            return Mathf.RoundToInt(worldUnits * GetWorldSampleScale());
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

        private void ConfigureTilemapRenderers()
        {
            EnableAutomaticChunkCullingBounds(tilemap);
            EnableAutomaticChunkCullingBounds(ruleGroundTilemap);
            EnableAutomaticChunkCullingBounds(sandOverlayTilemap);
            EnableAutomaticChunkCullingBounds(sandOverlayOffsetTilemap);
            EnableAutomaticChunkCullingBounds(pebbleTilemap);
            EnableAutomaticChunkCullingBounds(rockTilemap);
            EnableAutomaticChunkCullingBounds(smoothTilemap);
        }

        private static void EnableAutomaticChunkCullingBounds(Tilemap targetTilemap)
        {
            if (targetTilemap == null)
            {
                return;
            }

            var renderer = targetTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
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
