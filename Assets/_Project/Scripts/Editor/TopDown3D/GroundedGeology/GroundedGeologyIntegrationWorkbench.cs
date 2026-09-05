using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator.GroundedGeology;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator.GroundedGeology
{
    internal sealed class GroundedGeologyIntegrationWorkbench : EditorWindow
    {
        [SerializeField] private GameObject fixture;
        [SerializeField] private int seed = 1729;
        [SerializeField] private bool seedLocked = true;
        [SerializeField] private float haloMeters = 0.25f;
        [SerializeField] private GroundedGeologyResolutionQuality quality =
            GroundedGeologyResolutionQuality.Standard;
        [SerializeField] private GroundedGeologyFieldKind fieldView =
            GroundedGeologyFieldKind.FootprintSpine;
        [SerializeField] private int cameraIndex;
        [SerializeField] private bool advancedSettingsExpanded;
        [SerializeField] private bool technicalDetailsExpanded;

        private GroundedGeologyFixtureSnapshot snapshot;
        private GroundedGeologyResult result;
        private GroundedGeologyComparisonPreview comparison;
        private string status = "Select a generated Rock Workbench or Rock Formation Workbench to begin.";
        private MessageType statusType = MessageType.Info;

        /// <summary>
        /// Internal calibration surface retained for engineering diagnostics and regression work.
        /// Rock authors use the existing Rock Workbench rather than opening this window directly.
        /// </summary>
        internal static void OpenForDiagnostics()
        {
            var window = GetWindow<GroundedGeologyIntegrationWorkbench>("Grounded Geology");
            window.UseSelectionIfSupported();
            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleExternalPreviewChange;
            EditorApplication.hierarchyChanged += HandleExternalPreviewChange;
        }

        private void OnSelectionChange()
        {
            if (!HasActivePreview) UseSelectionIfSupported();
            Repaint();
        }

        private void OnGUI()
        {
            ReconcilePreviewLifecycle();

            EditorGUILayout.LabelField("Grounded Geology Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Select a generated Rock Workbench in the Hierarchy.\n"
                + "2. Click Create Side-by-Side Preview.\n"
                + "3. Check that A and B have the same size and silhouette.\n\n"
                + "This preview is temporary. It does not change or save the source rock.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var selectedFixture = (GameObject)EditorGUILayout.ObjectField(
                "Source Rock",
                fixture,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                fixture = selectedFixture;
                if (IsSupportedFixture(fixture))
                {
                    status = HasActivePreview
                        ? "Source changed. Refresh the side-by-side preview when you are ready."
                        : "Source ready. Create the side-by-side preview.";
                    statusType = MessageType.Info;
                }
                else
                {
                    status = fixture == null
                        ? "Select a generated Rock Workbench or Rock Formation Workbench to begin."
                        : "That object is not a Rock Workbench root. Select the parent workbench object.";
                    statusType = MessageType.Warning;
                }
            }

            advancedSettingsExpanded = EditorGUILayout.Foldout(
                advancedSettingsExpanded,
                "Advanced Settings",
                true);
            if (advancedSettingsExpanded) DrawAdvancedSettings();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!IsSupportedFixture(fixture)))
                {
                    var actionLabel = HasActivePreview
                        ? "Refresh Side-by-Side Preview"
                        : "Create Side-by-Side Preview";
                    if (GUILayout.Button(actionLabel, GUILayout.Height(30f))) Regenerate();
                }
                using (new EditorGUI.DisabledScope(!HasActivePreview))
                {
                    if (GUILayout.Button("Clear Preview", GUILayout.Width(100f), GUILayout.Height(30f)))
                        CancelPreview();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, statusType);
            if (HasActivePreview)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview Ready", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("A", "Original rock reference");
                EditorGUILayout.LabelField("B", "Grounded Geology unit-scale preview");
                EditorGUILayout.HelpBox(
                    "If A and B look the same, the integration setup is working. "
                    + "The automated size, geometry, and collider checks have already passed.",
                    MessageType.Info);
                technicalDetailsExpanded = EditorGUILayout.Foldout(
                    technicalDetailsExpanded,
                    "Technical Details",
                    true);
                if (technicalDetailsExpanded)
                {
                    DrawDiagnostics();
                    DrawComparisonSlots();
                }
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleExternalPreviewChange;
            EditorApplication.hierarchyChanged -= HandleExternalPreviewChange;
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
                statusType = MessageType.Warning;
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
                var previousRoot = HasActivePreview ? comparison.Root : null;
                if (!GroundedGeologyComparisonBuilder.TryBuild(
                        fixture,
                        newSnapshot.PhysicalBakeFactor,
                        previousRoot,
                        out var newComparison,
                        out var comparisonError))
                {
                    status = comparisonError;
                    statusType = MessageType.Error;
                    return;
                }

                snapshot = newSnapshot;
                result = newResult;
                comparison = newComparison;
                status = "READY — A and B match in the automated geometry and collider checks. "
                    + "Compare them visually in the Scene view. Nothing was saved.";
                statusType = MessageType.Info;
                Selection.activeGameObject = newComparison.TargetB;
                EditorGUIUtility.PingObject(newComparison.TargetB);
            }
            catch (System.Exception exception)
            {
                status = "Preview could not be created: " + exception.Message;
                statusType = MessageType.Error;
            }
        }

        private void CancelPreview()
        {
            var root = comparison != null ? comparison.Root : null;
            comparison = null;
            result = null;
            snapshot = null;
            if (root != null)
            {
                var fallbackSelection = IsSupportedFixture(fixture) ? fixture : null;
                GroundedGeologyComparisonBuilder.Cancel(root, fallbackSelection);
            }
            status = "Preview cleared. Your source rock was not changed.";
            statusType = MessageType.Info;
        }

        private bool HasActivePreview => comparison != null && comparison.IsAlive;

        private static bool IsSupportedFixture(GameObject candidate)
        {
            return candidate != null
                && (candidate.GetComponent<TopDown3DRockWorkbenchAuthoring>() != null
                    || candidate.GetComponent<TopDown3DRockWorkbenchFormationAuthoring>() != null);
        }

        private void UseSelectionIfSupported()
        {
            var selected = Selection.activeGameObject;
            if (!IsSupportedFixture(selected)) return;
            fixture = selected;
            status = "Source ready. Create the side-by-side preview.";
            statusType = MessageType.Info;
        }

        private void HandleExternalPreviewChange()
        {
            ReconcilePreviewLifecycle();
            Repaint();
        }

        private void ReconcilePreviewLifecycle()
        {
            if (comparison == null || comparison.IsAlive) return;
            comparison = null;
            result = null;
            snapshot = null;
            status = "The temporary preview was removed. Select a rock and create another side-by-side preview.";
            statusType = MessageType.Info;
        }

        private void DrawAdvancedSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
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
            }
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
            if (HasActivePreview)
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
            EditorGUILayout.LabelField("Comparison Study", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("A — Original", "Approved rock appearance at its reference scale");
            EditorGUILayout.LabelField("B — Integrated", "Same rock with physical size baked into unit-scale geometry");
            EditorGUILayout.LabelField("Future C Studies", "Geometry and material quality choices are not active yet");
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
