using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CampPresentationMigration
    {
        public const int Version = 1;
        public const string Settings = "Assets/_Project/Settings/Rendering/CampPresentation";
        public const string ScenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        private static T Find<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
        public static string DryRun()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Migration requires the open Camp scene in compiled Edit Mode.");
            var installed = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TopDown3DEnvironmentRig>(true)).ToArray();
            if (installed.Length != 0)
            {
                var rig = installed.Length == 1 ? installed[0] : null;
                var binding = rig != null && rig.sky != null && rig.sky.outputCamera != null
                    ? rig.sky.outputCamera.GetComponent<TopDown3DSceneRenderingOverride>() : null;
                if (rig == null || rig.migrationVersion != Version || rig.clock == null || rig.sunLight == null || rig.sunLight.profile == null
                    || rig.ambient == null || rig.ambient.profile == null || rig.sky == null || rig.sky.profile == null || !rig.sky.profile.IsValid
                    || rig.compatibilitySun == null || rig.compatibilitySun.EnvironmentModules != rig
                    || binding == null || !binding.ModularOpticsConfigured || TopDown3DSceneRenderingOverride.CameraOpticsFor(rig.sky.outputCamera) == null)
                    throw new InvalidOperationException("Partial or invalid migration; preserve it and inspect before retry.");
                return "Version 1 already installed; no migration needed.";
            }
            var camera = Find<Camera>(scene); var sun = Find<PerpetualTwilightSun>(scene);
            if (!TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var source) || source.nativeOptics == null || !source.nativeOptics.IsValid)
                throw new InvalidOperationException("Requires one valid legacy Camp profile and optics asset.");
            if (AssetDatabase.IsValidFolder(Settings)) throw new InvalidOperationException("Destination exists without installed rig; inspect partial migration before retry.");
            return "Version 1: retain all source assets/GUIDs; clone native optics; copy lighting, sky and dormant blur values; preserve scaled twilight phase/path, camera pose and actor state; group existing objects without renaming. Effective blur=" + source.nativeOptics.depthMode + "; sun phase=" + sun.Cycle01;
        }
        public static string InstallCurrentScene()
        {
            string report = DryRun();
            if (report.StartsWith("Version 1 already")) return report;
            var scene = SceneManager.GetActiveScene(); var roots = scene.GetRootGameObjects();
            var camera = Find<Camera>(scene); var sun = Find<PerpetualTwilightSun>(scene);
            var look = Find<TopDown3DCampLook>(scene);
            TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var old);
            var sunData = new SerializedObject(sun);
            float F(string name) => sunData.FindProperty(name).floatValue;
            bool B(string name) => sunData.FindProperty(name).boolValue;
            CampLookEditorPreview.Release();
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Modular Camp presentation v1");
            var environment = New("Environment", null); var lighting = New("Lighting", environment.transform);
            var clock = Undo.AddComponent<TopDown3DTwilightClock>(New("Twilight Cycle", lighting.transform));
            clock.Configure(B("cycleEnabled"), F("cycleDurationSeconds"), F("cycle01"), F("centerElevationDegrees"),
                F("elevationAmplitudeDegrees"), F("centerAzimuthDegrees"), F("azimuthAmplitudeDegrees"));
            var direct = Undo.AddComponent<TopDown3DSunLight>(sun.gameObject);
            var fill = Undo.AddComponent<TopDown3DAmbientLighting>(New("Ambient Lighting", lighting.transform));
            var sky = Undo.AddComponent<TopDown3DSkyPresentation>(New("Sky", environment.transform)); sky.outputCamera = camera;
            var rig = Undo.AddComponent<TopDown3DEnvironmentRig>(environment); rig.compatibilitySun = sun;
            rig.clock = clock; rig.sunLight = direct; rig.ambient = fill; rig.sky = sky;
            EnsureFolder(Settings);
            var native = UnityEngine.Object.Instantiate(old.nativeOptics); native.name = "Camp Native Optics"; Save(native, "NativeOptics");
            var optics = ScriptableObject.CreateInstance<TopDown3DCameraOpticsProfile>(); optics.nativeOptics = native;
            optics.blurMode = native.depthMode != DepthOfFieldMode.Off ? TopDown3DWorldBlurMode.DepthOfField
                : old.tiltShiftEnabled ? TopDown3DWorldBlurMode.TiltShift : TopDown3DWorldBlurMode.Off;
            optics.streakEnabled = old.streakEnabled;
            optics.flareEnabled = old.flareEnabled; optics.flareIntensity = old.flareIntensity;
            optics.useReferenceResolutionTiltShift = old.useReferenceResolutionTiltShift;
            optics.tiltShiftDownsample = old.tiltShiftDownsample; optics.tiltShiftReferenceHeight = old.tiltShiftReferenceHeight;
            optics.tiltShiftBlurRadiusPixels = old.tiltShiftBlurRadiusPixels; optics.tiltShiftBandAngleDegrees = old.tiltShiftBandAngleDegrees;
            optics.tiltShiftFocusCenter = old.tiltShiftFocusCenter; optics.tiltShiftSharpBandWidth = old.tiltShiftSharpBandWidth;
            optics.tiltShiftFeatherWidth = old.tiltShiftFeatherWidth;
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            var tilt = renderer.rendererFeatures.OfType<TopDown3DTiltShiftFeature>().Single();
            optics.legacyDownsample = tilt.Downsample; optics.legacyFocusCenter = tilt.FocusCenter;
            optics.legacySharpBandWidth = tilt.SharpBandWidth; optics.legacyFeatherWidth = tilt.FeatherWidth; optics.legacyBlurRadius = tilt.BlurRadius;
            Save(optics, "CameraOptics");
            var directProfile = ScriptableObject.CreateInstance<TopDown3DSunLightingProfile>();
            directProfile.deepColor = old.deepSunColor; directProfile.brightColor = old.brightSunColor;
            directProfile.deepIntensity = F("deepTwilightIntensity"); directProfile.brightIntensity = F("brightTwilightIntensity");
            directProfile.shadowStrength = F("shadowStrength"); directProfile.shadowBias = F("shadowBias"); directProfile.shadowNormalBias = F("shadowNormalBias");
            Save(directProfile, "SunLighting"); direct.profile = directProfile;
            var ambient = ScriptableObject.CreateInstance<TopDown3DAmbientLightingProfile>();
            ambient.deepSky = old.deepSkyFill; ambient.brightSky = old.brightSkyFill;
            ambient.deepEquator = old.deepEquatorFill; ambient.brightEquator = old.brightEquatorFill;
            ambient.deepGround = old.deepGroundFill; ambient.brightGround = old.brightGroundFill;
            ambient.reflection = old.sunlessReflection; ambient.deepReflectionIntensity = old.deepSkyExposure; ambient.brightReflectionIntensity = old.brightSkyExposure;
            Save(ambient, "AmbientLighting"); fill.profile = ambient;
            var skyProfile = ScriptableObject.CreateInstance<TopDown3DSkyProfile>(); skyProfile.material = old.skyMaterial;
            skyProfile.deepExposure = old.deepSkyExposure; skyProfile.brightExposure = old.brightSkyExposure;
            skyProfile.sunAngularDiameterDegrees = native.sunAngularDiameterDegrees;
            Save(skyProfile, "Sky"); sky.profile = skyProfile;
            Undo.RecordObject(sun, "Bind environment modules"); sun.ConfigureModules(rig);
            var binding = camera.GetComponent<TopDown3DSceneRenderingOverride>(); Undo.RecordObject(binding, "Bind camera optics");
            binding.ConfigureAtmosphere(null); binding.ConfigureOptics(optics);
            var cameraRoot = New("Camera", null); Parent(camera.gameObject, cameraRoot.transform); Parent(sun.gameObject, lighting.transform);
            var development = New("Development", null); Parent(look.gameObject, development.transform);
            var systems = New("Systems", null); var actors = New("Actors", null); var world = New("World", null); var ui = New("UI", null);
            var atmosphere = New("Atmosphere", environment.transform);
            foreach (var root in roots)
            {
                if (root == look.gameObject) continue;
                if (root.GetComponent<TopDown3DDustAtmosphere>() != null) Parent(root, atmosphere.transform);
                else if (root.GetComponent<TopDown3DInputRouter>() != null) Parent(root, systems.transform);
                else if (root.GetComponent<TopDown3DPlayerMotor>() != null || root.GetComponent<TopDown3DBigArmFollower>() != null || root.name == "Camp Loop Lager Trial") Parent(root, actors.transform);
                else if (root.GetComponent<Canvas>() != null || root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) Parent(root, ui.transform);
                else if (root.name == "Animation Playground" || root.name == "Legger Riding Study") Parent(root, development.transform);
                else Parent(root, world.transform);
            }
            Undo.CollapseUndoOperations(undo);
            rig.migrationVersion = Version;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            CampLookEditorPreview.Refresh();
            return report + "; installed in memory. Scene not saved.";
        }
        public static void RegisterRendererFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            if (renderer.rendererFeatures.OfType<TopDown3DCameraOpticsFeature>().Any()) return;
            var feature = ScriptableObject.CreateInstance<TopDown3DCameraOpticsFeature>(); feature.name = "Camera Optics";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id);
            var serialized = new SerializedObject(renderer);
            var features = serialized.FindProperty("m_RendererFeatures");
            var map = serialized.FindProperty("m_RendererFeatureMap");
            if (id == 0 || features == null || map == null) throw new InvalidOperationException("Renderer feature serialization unavailable");
            features.InsertArrayElementAtIndex(0); features.GetArrayElementAtIndex(0).objectReferenceValue = feature;
            map.InsertArrayElementAtIndex(0); map.GetArrayElementAtIndex(0).longValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo(); renderer.SetDirty(); EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
        }
        private static GameObject New(string name, Transform parent)
        {
            var value = new GameObject(name); Undo.RegisterCreatedObjectUndo(value, "Create presentation group");
            if (parent != null) value.transform.SetParent(parent, false); return value;
        }
        private static void Parent(GameObject value, Transform parent) => Undo.SetTransformParent(value.transform, parent, "Organize Camp hierarchy");
        private static void Save(UnityEngine.Object asset, string name) => AssetDatabase.CreateAsset(asset, Settings + "/" + name + ".asset");
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, split)); AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
    }
}
