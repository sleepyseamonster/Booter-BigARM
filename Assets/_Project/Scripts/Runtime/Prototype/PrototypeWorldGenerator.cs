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
        [SerializeField] private Tilemap pebbleTilemap;
        [SerializeField] private Sprite[] pebbleTileSprites;
        [SerializeField] private Tilemap rockTilemap;
        [SerializeField] private Sprite[] rockTileSprites;
        [SerializeField] private Tilemap smoothTilemap;
        [SerializeField] private Sprite[] smoothTileSprites;
        [SerializeField] private int seed = 12345;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int chunkRadius = 4;
        [SerializeField] private int chunkOperationsPerFrame = 4;

        private readonly HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> chunkOperationQueue = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedChunks = new HashSet<Vector2Int>();
        private Tilemap tilemap;
        private TileBase[] runtimeTiles;
        private TileBase[] runtimePebbleTiles;
        private TileBase[] runtimeRockTiles;
        private TileBase[] runtimeSmoothTiles;
        private TileBase[] emptyChunkTiles;
        private TileBase[] chunkTileBuffer;
        private TileBase[] pebbleChunkTileBuffer;
        private TileBase[] rockChunkTileBuffer;
        private TileBase[] smoothChunkTileBuffer;
        private TileBase[] emptyPebbleChunkTiles;
        private TileBase[] emptyRockChunkTiles;
        private TileBase[] emptySmoothChunkTiles;
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
            tilemap.ClearAllTiles();
            if (pebbleTilemap != null)
            {
                pebbleTilemap.ClearAllTiles();
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
                    DrawPebbleCluster(texture, tint, 10, 10, 3);
                    DrawPebbleCluster(texture, tint, 18, 14, 2);
                    DrawPebbleCluster(texture, tint, 22, 22, 4);
                    break;
                case DetailPattern.PebblesPatch:
                    DrawPebblePatch(texture, tint, 6, 7, 24, 23, 3);
                    break;
                case DetailPattern.PebblesMixed:
                    DrawPebblePatch(texture, tint, 5, 6, 26, 25, 4);
                    DrawPebbleCluster(texture, tint, 14, 20, 5);
                    break;
                case DetailPattern.PebblesSparse:
                    DrawPebbleCluster(texture, tint, 10, 11, 2);
                    DrawPebbleCluster(texture, tint, 22, 18, 3);
                    break;
                case DetailPattern.RocksSmall:
                    DrawRockCluster(texture, tint, 11, 12, 4);
                    DrawRockCluster(texture, tint, 21, 20, 3);
                    break;
                case DetailPattern.RocksPatch:
                    DrawRockPatch(texture, tint, 6, 7, 24, 24, 4);
                    break;
                case DetailPattern.RocksMixed:
                    DrawRockPatch(texture, tint, 5, 6, 25, 23, 3);
                    DrawRockCluster(texture, tint, 19, 15, 5);
                    break;
                case DetailPattern.RocksSparse:
                    DrawRockCluster(texture, tint, 12, 10, 2);
                    DrawRockCluster(texture, tint, 22, 24, 3);
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

        private static void DrawPebblePatch(Texture2D texture, Color32 tint, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    DrawPebbleCluster(texture, tint, x, y, Mathf.Max(1, step - 1));
                }
            }
        }

        private static void DrawPebbleCluster(Texture2D texture, Color32 tint, int centerX, int centerY, int radius)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (distance > radius + 1)
                    {
                        continue;
                    }

                    Put(texture, centerX + x, centerY + y, tint);
                }
            }
        }

        private static void DrawRockPatch(Texture2D texture, Color32 tint, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    DrawRockCluster(texture, tint, x, y, Mathf.Max(2, step - 1));
                }
            }
        }

        private static void DrawRockCluster(Texture2D texture, Color32 tint, int centerX, int centerY, int radius)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var chebyshev = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (chebyshev > radius)
                    {
                        continue;
                    }

                    if (chebyshev == radius && ((x + y) & 1) == 1)
                    {
                        continue;
                    }

                    Put(texture, centerX + x, centerY + y, tint);
                }
            }
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

        private void EnsureChunkBuffers()
        {
            var tileCount = chunkSize * chunkSize;
            if (chunkTileBuffer != null && chunkTileBuffer.Length == tileCount)
            {
                return;
            }

            chunkTileBuffer = new TileBase[tileCount];
            pebbleChunkTileBuffer = new TileBase[tileCount];
            rockChunkTileBuffer = new TileBase[tileCount];
            smoothChunkTileBuffer = new TileBase[tileCount];
            emptyChunkTiles = new TileBase[tileCount];
            emptyPebbleChunkTiles = new TileBase[tileCount];
            emptyRockChunkTiles = new TileBase[tileCount];
            emptySmoothChunkTiles = new TileBase[tileCount];
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
            tilemap.SetTilesBlock(bounds, chunkTileBuffer);
            FillPebbleChunkBuffer(chunkCoord);
            FillRockChunkBuffer(chunkCoord);
            FillSmoothChunkBuffer(chunkCoord);
            if (pebbleTilemap != null)
            {
                pebbleTilemap.SetTilesBlock(bounds, pebbleChunkTileBuffer);
            }
            if (rockTilemap != null)
            {
                rockTilemap.SetTilesBlock(bounds, rockChunkTileBuffer);
            }
            if (smoothTilemap != null)
            {
                smoothTilemap.SetTilesBlock(bounds, smoothChunkTileBuffer);
            }
            visibleChunks.Add(chunkCoord);
        }

        private void UnloadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var bounds = GetChunkBounds(chunkCoord);
            tilemap.SetTilesBlock(bounds, emptyChunkTiles);
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

        private void FillPebbleChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    pebbleChunkTileBuffer[index++] = SelectLayerTile(runtimePebbleTiles, worldX, worldY, seed + 17, 0.55f, 8, 0.2f);
                }
            }
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
                    rockChunkTileBuffer[index++] = SelectLayerTile(runtimeRockTiles, worldX, worldY, seed + 43, 0.78f, 12, 0.16f);
                }
            }
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

        private TileBase SelectTile(int worldX, int worldY)
        {
            if (runtimeTiles == null || runtimeTiles.Length == 0)
            {
                return null;
            }

            if (runtimeTiles.Length == 1)
            {
                return runtimeTiles[0];
            }

            var regionNoise = Hash01(seed, worldX / 4, worldY / 4);
            var detailNoise = Hash01(seed + 53, worldX, worldY);

            var variant = Mathf.FloorToInt(regionNoise * runtimeTiles.Length);
            variant = Mathf.Clamp(variant, 0, runtimeTiles.Length - 1);

            if (detailNoise < 0.12f && runtimeTiles.Length > 1)
            {
                variant = (variant + 1) % runtimeTiles.Length;
            }

            return runtimeTiles[variant];
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
