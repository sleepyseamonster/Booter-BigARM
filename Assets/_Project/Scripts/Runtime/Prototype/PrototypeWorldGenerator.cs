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
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private Sprite[] overlayTileSprites;
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
        private TileBase[] runtimeOverlayTiles;
        private TileBase[] emptyChunkTiles;
        private TileBase[] emptyOverlayChunkTiles;
        private TileBase[] chunkTileBuffer;
        private TileBase[] overlayChunkTileBuffer;
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
            if (overlayTilemap != null)
            {
                overlayTilemap.ClearAllTiles();
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
            if (runtimeTiles != null && runtimeOverlayTiles != null && tileSprites != null && overlayTileSprites != null && runtimeTiles.Length == tileSprites.Length && runtimeOverlayTiles.Length == overlayTileSprites.Length)
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

            if (overlayTileSprites == null || overlayTileSprites.Length == 0)
            {
                runtimeOverlayTiles = CreateFallbackOverlayTiles();
            }
            else
            {
                runtimeOverlayTiles = new TileBase[overlayTileSprites.Length];
                for (var i = 0; i < overlayTileSprites.Length; i++)
                {
                    var sprite = overlayTileSprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;
                    tile.flags = TileFlags.None;
                    tile.name = $"PrototypeRuntimeOverlayTile_{i}";
                    tile.hideFlags = HideFlags.HideAndDontSave;
                    runtimeOverlayTiles[i] = tile;
                }
            }

            EnsureChunkBuffers();
        }

        private TileBase[] CreateFallbackBaseTiles()
        {
            var sprites = new[]
            {
                CreateSolidSprite(new Color32(201, 173, 102, 255)),
                CreateSolidSprite(new Color32(96, 151, 78, 255)),
                CreateSolidSprite(new Color32(122, 126, 133, 255)),
                CreateSolidSprite(new Color32(88, 126, 98, 255))
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

        private TileBase[] CreateFallbackOverlayTiles()
        {
            var sprites = new[]
            {
                CreateDetailSprite(new Color32(32, 28, 18, 120), DetailPattern.Speckles),
                CreateDetailSprite(new Color32(57, 41, 18, 110), DetailPattern.Cracks),
                CreateDetailSprite(new Color32(18, 36, 20, 110), DetailPattern.Tufts),
                CreateDetailSprite(new Color32(52, 43, 34, 100), DetailPattern.Flecks)
            };

            var tiles = new TileBase[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.flags = TileFlags.None;
                tile.name = $"PrototypeFallbackOverlayTile_{i}";
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
                case DetailPattern.Speckles:
                    for (var y = 4; y < 28; y += 3)
                    {
                        for (var x = 4; x < 28; x += 3)
                        {
                            if (((x + y) & 1) == 0)
                            {
                                texture.SetPixel(x, y, tint);
                            }
                        }
                    }
                    break;
                case DetailPattern.Cracks:
                    for (var i = 5; i < 27; i++)
                    {
                        texture.SetPixel(i, i / 2 + 4, tint);
                        texture.SetPixel(31 - i, i / 2 + 5, tint);
                    }
                    break;
                case DetailPattern.Tufts:
                    for (var i = 0; i < 6; i++)
                    {
                        var baseX = 6 + i * 4;
                        texture.SetPixel(baseX, 18, tint);
                        texture.SetPixel(baseX + 1, 17, tint);
                        texture.SetPixel(baseX + 1, 16, tint);
                        texture.SetPixel(baseX + 2, 17, tint);
                    }
                    break;
                case DetailPattern.Flecks:
                    for (var y = 6; y < 26; y += 4)
                    {
                        for (var x = 6; x < 26; x += 4)
                        {
                            texture.SetPixel(x, y, tint);
                            if ((x + y) % 8 == 0)
                            {
                                texture.SetPixel(x + 1, y, tint);
                            }
                        }
                    }
                    break;
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        }

        private void EnsureChunkBuffers()
        {
            var tileCount = chunkSize * chunkSize;
            if (chunkTileBuffer != null && chunkTileBuffer.Length == tileCount)
            {
                return;
            }

            chunkTileBuffer = new TileBase[tileCount];
            overlayChunkTileBuffer = new TileBase[tileCount];
            emptyChunkTiles = new TileBase[tileCount];
            emptyOverlayChunkTiles = new TileBase[tileCount];
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
            if (overlayTilemap != null)
            {
                FillOverlayChunkBuffer(chunkCoord);
                overlayTilemap.SetTilesBlock(bounds, overlayChunkTileBuffer);
            }
            visibleChunks.Add(chunkCoord);
        }

        private void UnloadChunk(Vector2Int chunkCoord)
        {
            EnsureChunkBuffers();

            var bounds = GetChunkBounds(chunkCoord);
            tilemap.SetTilesBlock(bounds, emptyChunkTiles);
            if (overlayTilemap != null)
            {
                overlayTilemap.SetTilesBlock(bounds, emptyOverlayChunkTiles);
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

        private void FillOverlayChunkBuffer(Vector2Int chunkCoord)
        {
            var index = 0;
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var worldX = chunkCoord.x * chunkSize + localX;
                    var worldY = chunkCoord.y * chunkSize + localY;
                    overlayChunkTileBuffer[index++] = SelectOverlayTile(worldX, worldY);
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

        private TileBase SelectOverlayTile(int worldX, int worldY)
        {
            if (runtimeOverlayTiles == null || runtimeOverlayTiles.Length == 0)
            {
                return null;
            }

            var noise = Hash01(seed + 97, worldX, worldY);
            if (noise < 0.48f)
            {
                return null;
            }

            var variantNoise = Hash01(seed + 101, worldX / 2, worldY / 2);
            var variant = Mathf.FloorToInt(variantNoise * runtimeOverlayTiles.Length);
            variant = Mathf.Clamp(variant, 0, runtimeOverlayTiles.Length - 1);
            return runtimeOverlayTiles[variant];
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
            Speckles,
            Cracks,
            Tufts,
            Flecks
        }
    }
}
