using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Rebuilds the aligned PBR maps used by the layered Rock Workbench material from
    /// project-owned source albedo. The generated maps are deterministic and tileable.
    /// </summary>
    internal static class TopDown3DRockWorkbenchTextureBuilder
    {
        private const int TextureSize = 1024;
        private const string TextureRoot =
            "Assets/_Project/Art/Environment/Rocks/Workbench/Layered";
        private const string SourceRoot = TextureRoot + "/Source";
        private const string MaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";

        internal const string TopAlbedoPath = TextureRoot + "/RockWorkbenchTop_Albedo.png";
        internal const string TopNormalPath = TextureRoot + "/RockWorkbenchTop_Normal.png";
        internal const string TopSurfacePath = TextureRoot + "/RockWorkbenchTop_Surface.png";
        internal const string SideAlbedoPath = TextureRoot + "/RockWorkbenchSide_Albedo.png";
        internal const string SideNormalPath = TextureRoot + "/RockWorkbenchSide_Normal.png";
        internal const string SideSurfacePath = TextureRoot + "/RockWorkbenchSide_Surface.png";
        internal const string CrackMaskPath = TextureRoot + "/RockWorkbenchCrack_Mask.png";
        internal const string GritAlbedoPath = TextureRoot + "/RockWorkbenchGrit_Albedo.png";
        internal const string GritNormalPath = TextureRoot + "/RockWorkbenchGrit_Normal.png";
        internal const string GritSurfacePath = TextureRoot + "/RockWorkbenchGrit_Surface.png";

        private const string TopSourcePath = SourceRoot + "/RockWorkbenchTop_Source.png";
        private const string SideSourcePath = SourceRoot + "/RockWorkbenchSide_Source.png";
        private const string GritSourcePath = SourceRoot + "/RockWorkbenchGrit_Source.png";

        [MenuItem("Tools/Booter & BigARM/Rock Workbench/Rebuild Layered Textures")]
        public static void GenerateLayeredTextures()
        {
            var topAlbedoExists = File.Exists(TopAlbedoPath);
            var sideAlbedoExists = File.Exists(SideAlbedoPath);
            var topSource = LoadSource(topAlbedoExists ? TopAlbedoPath : TopSourcePath);
            var sideSource = LoadSource(sideAlbedoExists ? SideAlbedoPath : SideSourcePath);
            if (topSource == null || sideSource == null)
            {
                throw new InvalidOperationException(
                    "The Rock Workbench top and side source textures must both exist before rebuilding layered textures.");
            }

            try
            {
                Directory.CreateDirectory(TextureRoot);
                BuildSurfaceSet(topSource, TopAlbedoPath, TopNormalPath, TopSurfacePath, 3.8f, !topAlbedoExists);
                BuildSurfaceSet(sideSource, SideAlbedoPath, SideNormalPath, SideSurfacePath, 4.6f, !sideAlbedoExists);
                WriteTexture(CrackMaskPath, BuildCrackMask());

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureAlbedo(TopAlbedoPath);
                ConfigureAlbedo(SideAlbedoPath);
                ConfigureNormal(TopNormalPath);
                ConfigureNormal(SideNormalPath);
                ConfigureLinear(TopSurfacePath, "R=AO G=Roughness B=Height");
                ConfigureLinear(SideSurfacePath, "R=AO G=Roughness B=Height");
                ConfigureLinear(CrackMaskPath, "R=Crack G=CrackHalo B=MineralDeposit");
                AssignMaterialTextures();
                AssetDatabase.SaveAssets();
                Debug.Log("[Rock Workbench] Layered top, side, and crack textures rebuilt.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(topSource);
                UnityEngine.Object.DestroyImmediate(sideSource);
            }
        }

        [MenuItem("Tools/Booter & BigARM/Rock Workbench/Rebuild Side Grit Textures")]
        public static void GenerateGritTextures()
        {
            var gritSource = LoadSource(GritSourcePath);
            if (gritSource == null)
            {
                throw new InvalidOperationException(
                    "The Rock Workbench grit source texture must exist before rebuilding the side grit textures.");
            }

            try
            {
                Directory.CreateDirectory(TextureRoot);
                BuildSurfaceSet(
                    gritSource,
                    GritAlbedoPath,
                    GritNormalPath,
                    GritSurfacePath,
                    7.2f,
                    true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureAlbedo(GritAlbedoPath);
                ConfigureNormal(GritNormalPath);
                ConfigureLinear(GritSurfacePath, "R=AO G=Roughness B=Height");
                AssignGritMaterialTextures();
                AssetDatabase.SaveAssets();
                Debug.Log("[Rock Workbench] Independent side grit textures rebuilt.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gritSource);
            }
        }

        private static Texture2D LoadSource(string assetPath)
        {
            if (!File.Exists(assetPath)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
                wrapMode = TextureWrapMode.Repeat
            };
            return ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath), false)
                ? texture
                : null;
        }

        private static void BuildSurfaceSet(
            Texture2D source,
            string albedoPath,
            string normalPath,
            string surfacePath,
            float normalStrength,
            bool writeAlbedo)
        {
            var albedo = new Color[TextureSize * TextureSize];
            var height = new float[albedo.Length];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var u = (x + 0.5f) / TextureSize;
                    var v = (y + 0.5f) / TextureSize;
                    var color = SampleSeamless(source, u, v);
                    var index = x + y * TextureSize;
                    albedo[index] = new Color(color.r, color.g, color.b, 1f);
                    height[index] = Mathf.Clamp01(
                        color.r * 0.24f + color.g * 0.62f + color.b * 0.14f);
                }
            }

            var normal = new Color[albedo.Length];
            var surface = new Color[albedo.Length];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var index = x + y * TextureSize;
                    var left = HeightAt(height, x - 1, y);
                    var right = HeightAt(height, x + 1, y);
                    var down = HeightAt(height, x, y - 1);
                    var up = HeightAt(height, x, y + 1);
                    var normalVector = new Vector3(
                        (left - right) * normalStrength,
                        (down - up) * normalStrength,
                        1f).normalized;
                    normal[index] = new Color(
                        normalVector.x * 0.5f + 0.5f,
                        normalVector.y * 0.5f + 0.5f,
                        normalVector.z * 0.5f + 0.5f,
                        1f);

                    var neighborhood = (left + right + down + up) * 0.25f;
                    var localDetail = Mathf.Clamp01(Mathf.Abs(height[index] - neighborhood) * 18f);
                    var ao = Mathf.Lerp(0.68f, 1f, Mathf.SmoothStep(0f, 1f, height[index]));
                    var roughness = Mathf.Lerp(0.56f, 0.94f, localDetail);
                    surface[index] = new Color(ao, roughness, height[index], 1f);
                }
            }

            if (writeAlbedo) WriteTexture(albedoPath, CreateTexture(albedo, false));
            WriteTexture(normalPath, CreateTexture(normal, true));
            WriteTexture(surfacePath, CreateTexture(surface, true));
        }

        private static Color SampleSeamless(Texture2D source, float u, float v)
        {
            var xEdgeDistance = Mathf.Min(u, 1f - u);
            var yEdgeDistance = Mathf.Min(v, 1f - v);
            var xInterior = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(xEdgeDistance / 0.18f));
            var yInterior = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(yEdgeDistance / 0.18f));
            var centeredX = source.GetPixelBilinear(Mathf.Repeat(u + 0.5f, 1f), v);
            var original = source.GetPixelBilinear(u, v);
            var xBlended = Color.Lerp(centeredX, original, xInterior);
            var centeredXY = source.GetPixelBilinear(
                Mathf.Repeat(u + 0.5f, 1f),
                Mathf.Repeat(v + 0.5f, 1f));
            var centeredY = source.GetPixelBilinear(u, Mathf.Repeat(v + 0.5f, 1f));
            var yShiftedXBlend = Color.Lerp(centeredXY, centeredY, xInterior);
            return Color.Lerp(yShiftedXBlend, xBlended, yInterior);
        }

        private static Texture2D BuildCrackMask()
        {
            const int cells = 11;
            var colors = new Color[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var u = (x + 0.5f) / TextureSize;
                    var v = (y + 0.5f) / TextureSize;
                    var warpedU = Mathf.Repeat(u + Mathf.Sin(v * Mathf.PI * 6f) * 0.018f, 1f);
                    var warpedV = Mathf.Repeat(v + Mathf.Sin(u * Mathf.PI * 4f + 1.7f) * 0.012f, 1f);
                    var cellX = Mathf.FloorToInt(warpedU * cells);
                    var cellY = Mathf.FloorToInt(warpedV * cells);
                    var nearest = float.MaxValue;
                    var secondNearest = float.MaxValue;
                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (var offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            var candidateX = cellX + offsetX;
                            var candidateY = cellY + offsetY;
                            var wrappedX = PositiveModulo(candidateX, cells);
                            var wrappedY = PositiveModulo(candidateY, cells);
                            var point = new Vector2(
                                candidateX + 0.18f + Hash01(wrappedX, wrappedY, 17) * 0.64f,
                                candidateY + 0.18f + Hash01(wrappedX, wrappedY, 53) * 0.64f);
                            var sample = new Vector2(warpedU * cells, warpedV * cells);
                            var distance = Vector2.SqrMagnitude(sample - point);
                            if (distance < nearest)
                            {
                                secondNearest = nearest;
                                nearest = distance;
                            }
                            else if (distance < secondNearest)
                            {
                                secondNearest = distance;
                            }
                        }
                    }

                    var edgeDistance = Mathf.Sqrt(secondNearest) - Mathf.Sqrt(nearest);
                    var crack = 1f - Mathf.SmoothStep(0.018f, 0.075f, edgeDistance);
                    var halo = 1f - Mathf.SmoothStep(0.05f, 0.19f, edgeDistance);
                    var deposit = Mathf.SmoothStep(
                        0.58f,
                        0.84f,
                        ValueNoise(warpedU * 5f + 13.2f, warpedV * 5f - 7.4f));
                    colors[x + y * TextureSize] = new Color(crack, halo, deposit, 1f);
                }
            }
            return CreateTexture(colors, true);
        }

        private static float ValueNoise(float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = Mathf.SmoothStep(0f, 1f, x - x0);
            var ty = Mathf.SmoothStep(0f, 1f, y - y0);
            var a = Hash01(x0, y0, 91);
            var b = Hash01(x0 + 1, y0, 91);
            var c = Hash01(x0, y0 + 1, 91);
            var d = Hash01(x0 + 1, y0 + 1, 91);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash01(int x, int y, int salt)
        {
            unchecked
            {
                var value = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
                value = (value ^ (value >> 13)) * 1274126177u;
                return ((value ^ (value >> 16)) & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static float HeightAt(float[] height, int x, int y)
        {
            x = PositiveModulo(x, TextureSize);
            y = PositiveModulo(y, TextureSize);
            return height[x + y * TextureSize];
        }

        private static Texture2D CreateTexture(Color[] colors, bool linear)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, linear);
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }

        private static void WriteTexture(string path, Texture2D texture)
        {
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureAlbedo(string path)
        {
            Configure(path, true, TextureImporterType.Default, string.Empty);
        }

        private static void ConfigureNormal(string path)
        {
            Configure(path, false, TextureImporterType.NormalMap, string.Empty);
        }

        private static void ConfigureLinear(string path, string userData)
        {
            Configure(path, false, TextureImporterType.Default, userData);
        }

        private static void Configure(
            string path,
            bool srgb,
            TextureImporterType textureType,
            string userData)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Texture importer missing for {path}.");
            importer.textureType = textureType;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.userData = userData;
            importer.SaveAndReimport();
        }

        private static void AssignMaterialTextures()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new InvalidOperationException("Rock Workbench material was not found.");

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SideAlbedoPath));
            material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SideNormalPath));
            material.SetTexture("_SurfaceMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SideSurfacePath));
            material.SetTexture("_TopBaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TopAlbedoPath));
            material.SetTexture("_TopNormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TopNormalPath));
            material.SetTexture("_TopSurfaceMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TopSurfacePath));
            material.SetTexture("_CrackMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CrackMaskPath));
            AssignGritMaterialTextures(material);
            EditorUtility.SetDirty(material);
        }

        private static void AssignGritMaterialTextures()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new InvalidOperationException("Rock Workbench material was not found.");
            AssignGritMaterialTextures(material);
            EditorUtility.SetDirty(material);
        }

        private static void AssignGritMaterialTextures(Material material)
        {
            var gritAlbedo = LoadTextureAsset(GritAlbedoPath);
            var gritNormal = LoadTextureAsset(GritNormalPath);
            var gritSurface = LoadTextureAsset(GritSurfacePath);
            if (gritAlbedo == null || gritNormal == null || gritSurface == null) return;
            material.SetTexture("_GritBaseMap", gritAlbedo);
            material.SetTexture("_GritNormalMap", gritNormal);
            material.SetTexture("_GritSurfaceMap", gritSurface);
        }

        private static Texture2D LoadTextureAsset(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Texture2D subAsset) return subAsset;
            }
            return null;
        }
    }
}
