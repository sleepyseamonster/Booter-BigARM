using System;
using System.IO;
using System.Globalization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BooterBigArm.Editor
{
    // Read-only methods mirrored from the canonical isolated terrain builder for this audit.
    public static class BadwaterTerrainReadbackAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private const string Root = "Assets/_Project/Art/Terrain/BadwaterFourSlices";
        private const float MinimumElevation = -100f;
        private const float ElevationRange = 1800f;
    public static void ValidateFromCli()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length != 256)
            throw new InvalidDataException("Scene terrain count: " + terrains.Length);
        var grid = new Terrain[16, 16];
        float maxHeightError = 0f;
        int focusCount = 0;
        foreach (Terrain terrain in terrains)
        {
            string id = terrain.name.Substring(terrain.name.LastIndexOf("r", StringComparison.Ordinal));
            int row = int.Parse(id.Substring(1, 2), CultureInfo.InvariantCulture);
            int col = int.Parse(id.Substring(5, 2), CultureInfo.InvariantCulture);
            if (grid[row, col] != null) throw new InvalidDataException("Duplicate tile " + id);
            grid[row, col] = terrain;
            bool focus = row >= 12 && row <= 13 && col >= 1 && col <= 2;
            if (focus) focusCount++;
            int samples = focus ? 257 : 129;
            TerrainData data = terrain.terrainData;
            if (data == null || data.heightmapResolution != BadwaterHeightmapStitching.RenderResolution ||
                data.size != new Vector3(256f, ElevationRange, 256f))
                throw new InvalidDataException("Terrain data mismatch: " + id);
            if (terrain.GetComponent<TerrainCollider>()?.terrainData != data)
                throw new InvalidDataException("Collider mismatch: " + id);
            if (terrain.transform.position != new Vector3(col * 256f - 2048f, MinimumElevation, (15 - row) * 256f - 2048f))
                throw new InvalidDataException("Tile placement mismatch: " + id);
            if (data.terrainLayers.Length != 1 || data.terrainLayers[0]?.diffuseTexture == null)
                throw new InvalidDataException("Missing geographic color: " + id);
            if (terrain.materialTemplate == null || terrain.materialTemplate.shader.name != "Universal Render Pipeline/Terrain/Lit")
                throw new InvalidDataException("Missing URP Terrain Lit material: " + id);

            byte[] raw = File.ReadAllBytes(Root + "/Source/Heights/" + id + ".bytes");
            if (!terrain.allowAutoConnect || terrain.groupingID != 26911)
                throw new InvalidDataException("Persistent neighbor stitching is disabled: " + id);
            float[,] expectedGrid = BadwaterHeightmapStitching.PromoteContextGrid(
                BadwaterHeightmapStitching.DecodeSource(raw, samples, row, col));
            float[,] readback = data.GetHeights(0, 0, 257, 257);
            for (int z = 0; z < 257; z++)
            for (int x = 0; x < 257; x++)
                maxHeightError = Math.Max(maxHeightError,
                    Math.Abs(readback[z, x] - expectedGrid[z, x]) * ElevationRange);
        }
        if (focusCount != 4 || maxHeightError > 0.04f)
            throw new InvalidDataException($"Focus count or height readback mismatch: {focusCount}, {maxHeightError} m");
        float maxBorderGap = 0f;
        for (int row = 0; row < 16; row++)
        for (int col = 0; col < 16; col++)
        {
            Terrain tile = grid[row, col];
            if (tile == null) throw new InvalidDataException("Missing tile " + row + "," + col);
            if (col < 15) maxBorderGap = Math.Max(maxBorderGap, EdgeGap(tile, grid[row, col + 1], true));
            if (row < 15) maxBorderGap = Math.Max(maxBorderGap, EdgeGap(tile, grid[row + 1, col], false));
        }
        if (maxBorderGap != 0f) throw new InvalidDataException("Tile border gap: " + maxBorderGap + " m");
        string report = FindArg("-badwaterReport");
        if (!string.IsNullOrEmpty(report))
            File.WriteAllText(report, $"{{\"scene\":\"{scene.path}\",\"tiles\":{terrains.Length},\"focusTiles\":{focusCount},\"maxHeightReadbackErrorMeters\":{maxHeightError.ToString("R", CultureInfo.InvariantCulture)},\"maxBorderGapMeters\":{maxBorderGap.ToString("R", CultureInfo.InvariantCulture)}}}\n");
        Debug.Log($"Badwater scene validated: 256 terrains, max height error {maxHeightError:F4} m, max border gap {maxBorderGap:F4} m");
    }

    private static float ReadEncoded(byte[] raw, int samples, int northRow, int x)
    {
        int offset = (northRow * samples + x) * 2;
        return (raw[offset] | raw[offset + 1] << 8) / 65535f;
    }

    private static float EdgeGap(Terrain a, Terrain b, bool eastWest)
    {
        int ar = a.terrainData.heightmapResolution;
        int br = b.terrainData.heightmapResolution;
        float[,] ah = a.terrainData.GetHeights(0, 0, ar, ar);
        float[,] bh = b.terrainData.GetHeights(0, 0, br, br);
        int sharedSteps = Math.Max(ar, br) - 1;
        float maximum = 0f;
        for (int step = 0; step <= sharedSteps; step++)
        {
            float ai = step * (ar - 1f) / sharedSteps;
            float bi = step * (br - 1f) / sharedSteps;
            float av = eastWest ? InterpolateEdge(ah, ar, ar - 1, ai, true) : InterpolateEdge(ah, ar, 0, ai, false);
            float bv = eastWest ? InterpolateEdge(bh, br, 0, bi, true) : InterpolateEdge(bh, br, br - 1, bi, false);
            maximum = Math.Max(maximum, Math.Abs(av - bv) * ElevationRange);
        }
        return maximum;
    }

    private static float InterpolateEdge(float[,] h, int size, int fixedIndex, float index, bool vertical)
    {
        int lo = Mathf.FloorToInt(index);
        int hi = Math.Min(size - 1, lo + 1);
        return vertical ? Mathf.Lerp(h[lo, fixedIndex], h[hi, fixedIndex], index - lo)
                        : Mathf.Lerp(h[fixedIndex, lo], h[fixedIndex, hi], index - lo);
    }

    private static string FindArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
    }
}
