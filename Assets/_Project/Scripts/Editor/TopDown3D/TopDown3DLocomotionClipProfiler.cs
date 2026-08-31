using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class TopDown3DLocomotionClipProfiler
    {
        private const int SampleCount = 121;
        private const float ContactHeightAllowance = 0.045f;
        private const float ContactSpeedLimit = 0.45f;

        [MenuItem("Booter & BigARM/Top Down 3D/Report Locomotion Clip Profile")]
        public static void ReportCanonicalProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
                TopDown3DPrototypeBuilder.LocomotionClipProfilePath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                TopDown3DPrototypeBuilder.PrototypeHumanoidModelPath);
            if (profile == null || model == null)
            {
                throw new InvalidOperationException(
                    "The canonical locomotion profile and prototype Humanoid must exist before profiling.");
            }

            var report = new StringBuilder("Booter locomotion qualification report\n");
            foreach (TopDown3DLocomotionClipRole role in Enum.GetValues(
                         typeof(TopDown3DLocomotionClipRole)))
            {
                var entry = profile.GetRequiredEntry(role);
                var measurement = Measure(model, entry.Clip);
                report.Append(role)
                    .Append(": clip=").Append(entry.Clip.name)
                    .Append(", rootDrift=").Append(measurement.RootDrift.ToString("F4"))
                    .Append("m, facingDrift=").Append(measurement.FacingDrift.ToString("F2"))
                    .Append("deg, loopGap=").Append(measurement.LoopGap.ToString("F4"))
                    .Append("m, leftCandidates=").Append(FormatPhases(measurement.LeftContactCandidates))
                    .Append(", rightCandidates=").Append(FormatPhases(measurement.RightContactCandidates))
                    .AppendLine();
            }

            Debug.Log(report.ToString(), profile);
        }

        public static void ApplyApprovedLoopContacts(
            TopDown3DLocomotionClipProfile profile,
            TopDown3DLocomotionClipRole role,
            IReadOnlyList<float> leftContacts,
            IReadOnlyList<float> rightContacts)
        {
            if (profile == null || leftContacts == null || rightContacts == null
                || leftContacts.Count != 2 || rightContacts.Count != 2)
            {
                throw new ArgumentException("Approved loop contacts require one profile and two phases per foot.");
            }

            Undo.RecordObject(profile, "Apply approved locomotion contacts");
            var serializedProfile = new SerializedObject(profile);
            var clips = serializedProfile.FindProperty("clips");
            for (var index = 0; index < clips.arraySize; index++)
            {
                var entry = clips.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("role").enumValueIndex != (int)role)
                {
                    continue;
                }

                WritePhases(entry.FindPropertyRelative("leftContactPhases"), leftContacts);
                WritePhases(entry.FindPropertyRelative("rightContactPhases"), rightContacts);
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                return;
            }

            throw new InvalidOperationException($"The canonical profile has no '{role}' entry.");
        }

        private static ClipMeasurement Measure(GameObject model, AnimationClip clip)
        {
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(instance, previewScene);
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    throw new InvalidOperationException("Clip profiling requires the production Humanoid avatar.");
                }

                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (hips == null || leftFoot == null || rightFoot == null)
                {
                    throw new InvalidOperationException("Clip profiling requires hips and both Humanoid feet.");
                }

                var leftPositions = new Vector3[SampleCount];
                var rightPositions = new Vector3[SampleCount];
                var rootPositions = new Vector3[SampleCount];
                var facing = new Quaternion[SampleCount];
                for (var sample = 0; sample < SampleCount; sample++)
                {
                    var phase = sample / (SampleCount - 1f);
                    clip.SampleAnimation(instance, phase * clip.length);
                    leftPositions[sample] = instance.transform.InverseTransformPoint(leftFoot.position);
                    rightPositions[sample] = instance.transform.InverseTransformPoint(rightFoot.position);
                    rootPositions[sample] = instance.transform.InverseTransformPoint(hips.position);
                    facing[sample] = hips.rotation;
                }

                return new ClipMeasurement(
                    Vector3.Distance(rootPositions[0], rootPositions[SampleCount - 1]),
                    Quaternion.Angle(facing[0], facing[SampleCount - 1]),
                    Mathf.Max(
                        Vector3.Distance(leftPositions[0], leftPositions[SampleCount - 1]),
                        Vector3.Distance(rightPositions[0], rightPositions[SampleCount - 1])),
                    FindContactCandidates(leftPositions, clip.length),
                    FindContactCandidates(rightPositions, clip.length));
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static float[] FindContactCandidates(Vector3[] positions, float clipLength)
        {
            var minimumHeight = positions.Min(position => position.y);
            var candidates = new bool[positions.Length];
            var sampleDuration = Mathf.Max(0.0001f, clipLength / (positions.Length - 1f));
            for (var index = 1; index < positions.Length - 1; index++)
            {
                var speed = Vector3.Distance(positions[index - 1], positions[index + 1])
                    / (sampleDuration * 2f);
                candidates[index] = positions[index].y <= minimumHeight + ContactHeightAllowance
                    && speed <= ContactSpeedLimit;
            }

            var phases = new List<float>();
            for (var index = 1; index < candidates.Length - 1; index++)
            {
                if (!candidates[index] || candidates[index - 1])
                {
                    continue;
                }

                var end = index;
                while (end + 1 < candidates.Length && candidates[end + 1])
                {
                    end++;
                }

                phases.Add((index + end) * 0.5f / (candidates.Length - 1f));
                index = end;
            }

            return phases.ToArray();
        }

        private static void WritePhases(SerializedProperty property, IReadOnlyList<float> phases)
        {
            property.arraySize = phases.Count;
            for (var index = 0; index < phases.Count; index++)
            {
                property.GetArrayElementAtIndex(index).floatValue = Mathf.Repeat(phases[index], 1f);
            }
        }

        private static string FormatPhases(IEnumerable<float> phases)
        {
            return "[" + string.Join(", ", phases.Select(phase => phase.ToString("F3"))) + "]";
        }

        private readonly struct ClipMeasurement
        {
            public ClipMeasurement(
                float rootDrift,
                float facingDrift,
                float loopGap,
                float[] leftContactCandidates,
                float[] rightContactCandidates)
            {
                RootDrift = rootDrift;
                FacingDrift = facingDrift;
                LoopGap = loopGap;
                LeftContactCandidates = leftContactCandidates;
                RightContactCandidates = rightContactCandidates;
            }

            public float RootDrift { get; }
            public float FacingDrift { get; }
            public float LoopGap { get; }
            public float[] LeftContactCandidates { get; }
            public float[] RightContactCandidates { get; }
        }
    }
}
