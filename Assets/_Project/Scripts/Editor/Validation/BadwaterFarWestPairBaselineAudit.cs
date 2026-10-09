using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Read-only capture of the saved pre-expansion scene in an isolated copy.</summary>
    public static class BadwaterFarWestPairBaselineAudit
    {
        public static void CaptureFromCli()
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            if (Directory.Exists(Path.Combine(project, ".git")) || File.Exists(Path.Combine(project, ".git"))) throw new InvalidOperationException("Capture saved assets in an isolated copy.");
            string output = null;
            var args = Environment.GetCommandLineArgs();
            for (int i=0; i<args.Length-1; i++) if (args[i]=="-farWestPairCapture") output=args[i+1];
            if (string.IsNullOrEmpty(output) || Directory.Exists(output)) throw new IOException("Fresh baseline output required.");
            var scene = EditorSceneManager.OpenScene(BadwaterFarWestPairSource.ProductionScene);
            var terrains = BadwaterFarWestPairSource.Terrains(scene);
            if (terrains.Count != 2048) throw new InvalidDataException("Expected the full accepted pre-expansion grid.");
            // Prove the accepted full grid, exact seams, neighbor topology and saved asset hashes
            // before emitting a new baseline. This is read-only and does not sample gameplay.
            BadwaterNextWestPairValidator.ValidateScene(scene, false);
            if (scene.isDirty) throw new InvalidDataException("Unsaved scene cannot establish a saved baseline.");
            Directory.CreateDirectory(output);
            var lines = new List<string>();
            foreach (var t in terrains)
            {
                if (t.terrainData.size != new Vector3(256,1800,256) || t.terrainData.heightmapResolution != 257)
                    throw new InvalidDataException("Unsupported retained geometry.");
                if (EditorUtility.IsDirty(t.terrainData)) throw new InvalidDataException("Unsaved TerrainData cannot establish a saved baseline.");
                var p=t.transform.position;
                int e=Mathf.RoundToInt(p.x)+522448, n=Mathf.RoundToInt(p.z)+4008248;
                string id=$"e{e}_n{n}";
                var h=t.terrainData.GetHeights(0,0,257,257); var bytes=new byte[h.Length*4];
                Buffer.BlockCopy(h,0,bytes,0,bytes.Length);
                File.WriteAllBytes(Path.Combine(output,id+".bytes"),bytes);
                string path=AssetDatabase.GetAssetPath(t.terrainData);
                lines.Add($"{id},{e},{n},{path},{AssetDatabase.AssetPathToGUID(path)},{BadwaterFarWestPairSource.Hash(BadwaterFarWestPairSource.ProjectFile(path))},{BadwaterFarWestPairSource.Hash(BadwaterFarWestPairSource.ProjectFile(path+".meta"))}");
            }
            File.WriteAllLines(Path.Combine(output,"tiles.csv"),lines);
            Debug.Log("FAR_WEST_PAIR_BASELINE_CAPTURED: "+terrains.Count);
        }
    }
}
