using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator.GroundedGeology;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator.GroundedGeology
{
    internal sealed class GroundedGeologyIntegrationWorkbench : EditorWindow
    {
        private const string MenuRoot = "Booter & BigARM/Top Down 3D/";

        [SerializeField] private GameObject fixture;
        [SerializeField] private int seed = 1729;
        [SerializeField] private bool seedLocked = true;
        [SerializeField] private float haloMeters = 0.25f;
        [SerializeField] private GroundedGeologyResolutionQuality quality =
            GroundedGeologyResolutionQuality.Standard;
        [SerializeField] private GroundedGeologyFieldKind fieldView =
            GroundedGeologyFieldKind.FootprintSpine;
        [SerializeField] private int cameraIndex;

        private GroundedGeologyFixtureSnapshot snapshot;
        private GroundedGeologyResult result;
        private GroundedGeologyComparisonPreview comparison;
        private string status = "Select an existing workbench fixture. Nothing is generated automatically.";

        [MenuItem(MenuRoot + "Grounded Geology Integration Workbench")]
        public static void Open()
        {
            GetWindow<GroundedGeologyIntegrationWorkbench>("Grounded Geology");
        }

        [MenuItem(MenuRoot + "Create Grounded Geology Comparison From Selection")]
        public static void CreateComparisonFromSelection()
        {
            var window = GetWindow<GroundedGeologyIntegrationWorkbench>("Grounded Geology");
            window.fixture = Selection.activeGameObject;
            window.Regenerate();
            window.Show();
            window.Repaint();
        }

        [MenuItem(MenuRoot + "Create Grounded Geology Comparison From Selection", true)]
        private static bool CanCreateComparisonFromSelection()
        {
            var selected = Selection.activeGameObject;
            return selected != null
                && (selected.GetComponent<TopDown3DRockWorkbenchAuthoring>() != null
                    || selected.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>() != null);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "TEMPORARY / NON-CANON M0-M2 SHELL\n"
                + "This window reads existing fixtures and creates disposable comparison objects. "
                + "It does not alter World Creator, terrain, production scenes, source workbenches, or catalogs.",
                MessageType.Warning);

            fixture = (GameObject)EditorGUILayout.ObjectField(
                "Existing Fixture Root",
                fixture,
                typeof(GameObject),
                true);
            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField("Seed", seed);
                seedLocked = GUILayout.Toggle(seedLocked, "Lock Seed", GUILayout.Width(88f));
                using (new EditorGUI.DisabledScope(seedLocked))
                {
                    if (GUILayout.Button("New Seed", GUILayout.Width(82f)))
                        seed = unchecked(seed * 1103515245 + 12345);
                }
            }

            haloMeters = Mathf.Max(0f, EditorGUILayout.FloatField("Bounds Halo (m)", haloMeters));
            quality = (GroundedGeologyResolutionQuality)EditorGUILayout.EnumPopup("Resolution", quality);
            fieldView = DrawSingleFieldPopup(fieldView);
            DrawReviewEnvironment();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Regenerate Reference Metadata + A/B")) Regenerate();
                using (new EditorGUI.DisabledScope(comparison == null || comparison.Root == null))
                {
                    if (GUILayout.Button("Cancel", GUILayout.Width(72f))) CancelPreview();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.Info);
            DrawDiagnostics();
            DrawComparisonSlots();
        }

        private void OnDisable()
        {
            CancelPreview();
        }

        private void Regenerate()
        {
            if (!ExistingWorkbenchFixtureAdapter.TryCapture(
                    fixture,
                    seed,
                    seedLocked,
                    out var newSnapshot,
                    out var captureError))
            {
                status = captureError;
                return;
            }

            try
            {
                var world = new WorldIdentity(
                    seed,
                    new WorldVersionManifest(1, 1, 1, 1, 1, 1, 1));
                var input = new GroundedGeologyGenerationInput(
                    world,
                    newSnapshot.Recipe,
                    newSnapshot.OwnerAddress,
                    new GroundedGeologyEvaluationBounds(
                        newSnapshot.Recipe.LocalPhysicalBounds,
                        haloMeters),
                    GroundedGeologyResolutionProfile.ForQuality(quality),
                    GroundedGeologyFieldKind.All);
                var newResult = GroundedGeologyReferenceCompiler.Compile(input);
                if (!GroundedGeologyComparisonBuilder.TryBuild(
                        fixture,
                        newSnapshot.PhysicalBakeFactor,
                        comparison != null ? comparison.Root : null,
                        out var newComparison,
                        out var comparisonError))
                {
                    status = comparisonError;
                    return;
                }

                snapshot = newSnapshot;
                result = newResult;
                comparison = newComparison;
                status = "Reference metadata and temporary A/B geometry regenerated. "
                    + "Automated equivalence passed; visual acceptance remains user-owned.";
            }
            catch (System.Exception exception)
            {
                status = "Generation stopped without replacing the previous preview: " + exception.Message;
            }
        }

        private void CancelPreview()
        {
            if (comparison != null && comparison.Root != null)
                GroundedGeologyComparisonBuilder.Cancel(comparison.Root);
            comparison = null;
            result = null;
            snapshot = null;
            status = "Temporary preview canceled. The source fixture was not changed.";
        }

        private void DrawReviewEnvironment()
        {
            var cameras = GroundedGeologyReviewEnvironment.Cameras;
            var names = new string[cameras.Count];
            for (var i = 0; i < names.Length; i++) names[i] = cameras[i].Name;
            cameraIndex = Mathf.Clamp(cameraIndex, 0, names.Length - 1);
            cameraIndex = EditorGUILayout.Popup("Fixed Review Camera", cameraIndex, names);
            var camera = cameras[cameraIndex];
            EditorGUILayout.LabelField(
                "Camera Lock",
                $"az {camera.AzimuthDegrees:0.#}°, el {camera.ElevationDegrees:0.#}°, distance ×{camera.DistanceMultiplier:0.##}");
            EditorGUILayout.LabelField(
                "Light Lock",
                $"Euler {GroundedGeologyReviewEnvironment.LockedLightEuler}, intensity {GroundedGeologyReviewEnvironment.LockedLightIntensity:0.##}");
        }

        private void DrawDiagnostics()
        {
            if (snapshot == null || result == null) return;
            EditorGUILayout.LabelField("Fixture", snapshot.Recipe.FixtureKind.ToString());
            EditorGUILayout.LabelField("Root Scale", snapshot.SourceRootScale.ToString("R"));
            if (!string.IsNullOrEmpty(snapshot.Warning))
                EditorGUILayout.HelpBox(snapshot.Warning, MessageType.Warning);
            EditorGUILayout.LabelField("Physical Bounds", snapshot.Recipe.LocalPhysicalBounds.ToString());
            EditorGUILayout.LabelField("Haloed Bounds", result.Bounds.Expanded.ToString());
            EditorGUILayout.LabelField("Result Signature", result.Signature.ToString());
            EditorGUILayout.LabelField("Source Preserved", snapshot.SourceWasPreserved ? "yes" : "NO");
            EditorGUILayout.LabelField(
                "Cells Across Minimum",
                $"requested {result.Resolution.RequestedCellsAcrossMinimum}; effective {result.Resolution.EffectiveCellsAcrossMinimum:0.###}");
            EditorGUILayout.LabelField(
                "Grid",
                $"{result.Resolution.CellsX} × {result.Resolution.CellsY} × {result.Resolution.CellsZ}; "
                + $"{result.Resolution.SamplePointCount:N0} samples; {result.Resolution.EffectiveCellSizeMeters:0.#####} m");
            if (result.Resolution.WasCapped)
                EditorGUILayout.HelpBox("Resolution was reduced by the axis or sample budget.", MessageType.Warning);
            EditorGUILayout.LabelField("Field View", fieldView + " (placeholder / reference-only)");
            if (comparison != null)
            {
                EditorGUILayout.LabelField(
                    "A/B Maximum Sample Delta",
                    $"render {comparison.Metrics.MaximumRenderSampleDelta:R} m; collider {comparison.Metrics.MaximumColliderSampleDelta:R} m");
                EditorGUILayout.LabelField("B Root", comparison.TargetB.transform.localScale.ToString("R"));
            }
        }

        private static void DrawComparisonSlots()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("A / B / C Review Slots", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("A", "Approved individual-rock appearance at visual root 0.1");
            EditorGUILayout.LabelField("B", "Unit-root target with the 0.1 factor baked into meter geometry");
            EditorGUILayout.LabelField("C Geometry", "Draft 24 / Standard 40 / Approval 64 (selection pending)");
            EditorGUILayout.LabelField("C Material", "Meter wavelength interval pending; no material integration in this batch");
        }

        private static GroundedGeologyFieldKind DrawSingleFieldPopup(GroundedGeologyFieldKind current)
        {
            var values = new[]
            {
                GroundedGeologyFieldKind.FootprintSpine,
                GroundedGeologyFieldKind.TerrainUpliftApron,
                GroundedGeologyFieldKind.RockOccupancy,
                GroundedGeologyFieldKind.ContactGap,
                GroundedGeologyFieldKind.CreviceConcavity,
                GroundedGeologyFieldKind.SlopeCurvature,
                GroundedGeologyFieldKind.WindExposureShelter,
                GroundedGeologyFieldKind.Sediment,
                GroundedGeologyFieldKind.Talus,
                GroundedGeologyFieldKind.MaterialWeights,
                GroundedGeologyFieldKind.TraversalCollision,
                GroundedGeologyFieldKind.ChunkOwnershipHalo,
                GroundedGeologyFieldKind.SurfaceNormals
            };
            var names = new string[values.Length];
            var selected = 0;
            for (var i = 0; i < values.Length; i++)
            {
                names[i] = values[i].ToString();
                if (values[i] == current) selected = i;
            }
            return values[EditorGUILayout.Popup("Field View", selected, names)];
        }
    }
}
