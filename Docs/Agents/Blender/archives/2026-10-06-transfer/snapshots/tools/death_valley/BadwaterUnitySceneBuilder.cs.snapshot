// Run in an isolated Unity 6000.4.0f1 project via -executeMethod.
// Its outputs are copied into the main project only after validation.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BadwaterUnitySceneBuilder
{
    private const string Root = "Assets/_Project/Art/Terrain/BadwaterFourSlices";
    private const string ScenePath = "Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity";
    private const float MinimumElevation = -100f;
    private const float ElevationRange = 1800f;

    public static void BuildFromCli()
    {
        string[] lines = File.ReadAllLines(Root + "/Source/tiles.csv");
        if (lines.Length != 257)
            throw new InvalidDataException("Expected exactly 256 source terrain tiles.");
        Directory.CreateDirectory(Root + "/TerrainData");
        Directory.CreateDirectory(Root + "/TerrainLayers");
        Directory.CreateDirectory("Assets/_Project/Scenes/TopDown3D");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var parent = new GameObject("Badwater Four Slices | UTM 11N center 522448,4008248");
        var terrainGrid = new Terrain[16, 16];
        int focusCount = 0;
        float maxEdgeAdjustment = 0f;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length != 7)
                throw new InvalidDataException("Invalid tile record at line " + (i + 1));
            string id = fields[0];
            int row = int.Parse(fields[1], CultureInfo.InvariantCulture);
            int col = int.Parse(fields[2], CultureInfo.InvariantCulture);
            int spacing = int.Parse(fields[3], CultureInfo.InvariantCulture);
            int samples = int.Parse(fields[4], CultureInfo.InvariantCulture);
            if (row < 0 || row >= 16 || col < 0 || col >= 16 || id != $"r{row:00}_c{col:00}")
                throw new InvalidDataException("Unexpected tile ID or position: " + id);
            bool focus = row >= 12 && row <= 13 && col >= 1 && col <= 2;
            if (focus != (spacing == 1 && samples == 257) && !( !focus && spacing == 2 && samples == 129))
                throw new InvalidDataException("Unexpected focus resolution: " + id);
            if (focus)
                focusCount++;

            string rawPath = Root + "/Source/Heights/" + id + ".bytes";
            byte[] raw = File.ReadAllBytes(rawPath);
            if (raw.Length != samples * samples * 2)
                throw new InvalidDataException("Height byte count mismatch: " + id);
            var heights = new float[samples, samples];
            for (int northRow = 0; northRow < samples; northRow++)
            for (int x = 0; x < samples; x++)
            {
                int offset = (northRow * samples + x) * 2;
                ushort encoded = (ushort)(raw[offset] | raw[offset + 1] << 8);
                heights[samples - 1 - northRow, x] = encoded / 65535f;
            }
            // Unity Terrain has a regular heightmap. At the four focus/context
            // boundaries, match every intermediate 1 m vertex to the 2 m edge
            // interpolation so no sub-metre-to-metre vertical cracks appear.
            if (focus)
            {
                if (row == 12) maxEdgeAdjustment = Math.Max(maxEdgeAdjustment, SmoothHorizontalEdge(heights, samples - 1));
                if (row == 13) maxEdgeAdjustment = Math.Max(maxEdgeAdjustment, SmoothHorizontalEdge(heights, 0));
                if (col == 1) maxEdgeAdjustment = Math.Max(maxEdgeAdjustment, SmoothVerticalEdge(heights, 0));
                if (col == 2) maxEdgeAdjustment = Math.Max(maxEdgeAdjustment, SmoothVerticalEdge(heights, samples - 1));
            }

            var data = new TerrainData();
            data.heightmapResolution = samples;
            data.size = new Vector3(256f, ElevationRange, 256f);
            data.alphamapResolution = 128;
            data.baseMapResolution = 128;
            data.SetHeights(0, 0, heights);
            string dataPath = Root + "/TerrainData/" + id + ".asset";
            AssetDatabase.CreateAsset(data, dataPath);

            string colorPath = Root + "/Source/Colors/" + id + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            if (texture == null || texture.width != 128 || texture.height != 128)
                throw new InvalidDataException("Color texture import failed: " + id);
            var layer = new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = new Vector2(256f, 256f),
                tileOffset = Vector2.zero
            };
            AssetDatabase.CreateAsset(layer, Root + "/TerrainLayers/" + id + ".terrainlayer");
            data.terrainLayers = new[] { layer };
            var alpha = new float[128, 128, 1];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
                alpha[y, x, 0] = 1f;
            data.SetAlphamaps(0, 0, alpha);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = "Badwater_" + (focus ? "1m_" : "2m_") + id;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(col * 256f - 2048f, MinimumElevation, (15 - row) * 256f - 2048f);
            Terrain terrain = go.GetComponent<Terrain>();
            terrain.groupingID = 26911;
            terrain.allowAutoConnect = false;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 3000f;
            terrainGrid[row, col] = terrain;
        }
        if (focusCount != 4)
            throw new InvalidDataException("Missing 1 m focus tiles.");
        for (int row = 0; row < 16; row++)
        for (int col = 0; col < 16; col++)
        {
            Terrain t = terrainGrid[row, col];
            if (t == null) throw new InvalidDataException("Missing tile: " + row + "," + col);
            t.SetNeighbors(col > 0 ? terrainGrid[row, col - 1] : null,
                row > 0 ? terrainGrid[row - 1, col] : null,
                col < 15 ? terrainGrid[row, col + 1] : null,
                row < 15 ? terrainGrid[row + 1, col] : null);
        }

        var sun = new GameObject("Review Sun");
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Could not save " + ScenePath);
        AssetDatabase.Refresh();

        string report = Environment.GetCommandLineArgs().Length > 0 ? FindArg("-badwaterReport") : null;
        if (!string.IsNullOrEmpty(report))
            File.WriteAllText(report, $"{{\"tiles\":256,\"focusTiles\":4,\"maxFocusEdgeAdjustmentMeters\":{(maxEdgeAdjustment * ElevationRange).ToString("R", CultureInfo.InvariantCulture)},\"scene\":\"{ScenePath}\"}}\n");
        Debug.Log($"Badwater scene built: 256 terrains, 4 focus tiles, maximum perimeter seam adjustment {maxEdgeAdjustment * ElevationRange:F3} m");
    }

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
            if (data == null || data.heightmapResolution != samples ||
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
            float[,] readback = data.GetHeights(0, 0, samples, samples);
            for (int northRow = 0; northRow < samples; northRow++)
            for (int x = 0; x < samples; x++)
            {
                int offset = (northRow * samples + x) * 2;
                float expected = (raw[offset] | raw[offset + 1] << 8) / 65535f;
                if (focus && (northRow == 0 && row == 12 || northRow == 256 && row == 13) && x % 2 == 1)
                    expected = (ReadEncoded(raw, samples, northRow, x - 1) + ReadEncoded(raw, samples, northRow, x + 1)) * 0.5f;
                if (focus && (x == 0 && col == 1 || x == 256 && col == 2) && northRow % 2 == 1)
                    expected = (ReadEncoded(raw, samples, northRow - 1, x) + ReadEncoded(raw, samples, northRow + 1, x)) * 0.5f;
                maxHeightError = Math.Max(maxHeightError,
                    Math.Abs(readback[samples - 1 - northRow, x] - expected) * ElevationRange);
            }
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
        if (maxBorderGap > 0.04f) throw new InvalidDataException("Tile border gap: " + maxBorderGap + " m");
        string report = FindArg("-badwaterReport");
        if (!string.IsNullOrEmpty(report))
            File.WriteAllText(report, $"{{\"scene\":\"{scene.path}\",\"tiles\":{terrains.Length},\"focusTiles\":{focusCount},\"maxHeightReadbackErrorMeters\":{maxHeightError.ToString("R", CultureInfo.InvariantCulture)},\"maxBorderGapMeters\":{maxBorderGap.ToString("R", CultureInfo.InvariantCulture)}}}\n");
        Debug.Log($"Badwater scene validated: 256 terrains, max height error {maxHeightError:F4} m, max border gap {maxBorderGap:F4} m");
    }

    public static void ApplyUrpMaterialFromCli()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null) throw new InvalidOperationException("URP Terrain Lit shader is unavailable.");
        string materialPath = Root + "/BadwaterTerrainURP.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "BadwaterTerrainURP" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length != 256) throw new InvalidDataException("Unexpected terrain count before material assignment.");
        foreach (Terrain terrain in terrains) terrain.materialTemplate = material;
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save URP terrain scene.");
        Debug.Log("Assigned shared URP Terrain Lit material to 256 Badwater tiles.");
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

    private static float SmoothHorizontalEdge(float[,] heights, int y)
    {
        float max = 0f;
        for (int x = 1; x < 256; x += 2)
        {
            float expected = (heights[y, x - 1] + heights[y, x + 1]) * 0.5f;
            max = Math.Max(max, Math.Abs(heights[y, x] - expected));
            heights[y, x] = expected;
        }
        return max;
    }

    private static float SmoothVerticalEdge(float[,] heights, int x)
    {
        float max = 0f;
        for (int y = 1; y < 256; y += 2)
        {
            float expected = (heights[y - 1, x] + heights[y + 1, x]) * 0.5f;
            max = Math.Max(max, Math.Abs(heights[y, x] - expected));
            heights[y, x] = expected;
        }
        return max;
    }
}
