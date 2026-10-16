using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundDodgeAuthoring
    {
        public const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/DodgeProfile.asset";
        public const string SourcePath = "Assets/_Project/Art/Animations/Mixamo/Traversal/Evade/StandToRollSource.fbx";
        public const string ClipPath = "Assets/_Project/Art/Animations/Mixamo/Derived/CampDodgeRollGrounded.anim";
        public const float SourceStart = .1f, SourceEnd = 2.1f, Duration = .95f;

        private static AnimationCurve Linear(Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        public static object Prepare()
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
                || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Prepare dodge art only in the marked background candidate.");
            if (File.Exists(ProfilePath) || File.Exists(ProfilePath + ".meta") || File.Exists(ClipPath) || File.Exists(ClipPath + ".meta"))
                throw new IOException("Preserve existing dodge outputs and GUIDs.");
            var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            var source = AnimationPlaygroundClipDerivatives.LoadClip(SourcePath);
            var nativeZ = AnimationUtility.GetEditorCurve(source, AnimationUtility.GetCurveBindings(source).Single(b => b.propertyName == "RootT.z"));
            var start = nativeZ.Evaluate(SourceStart); var end = nativeZ.Evaluate(SourceEnd);
            if (end - start < 1) throw new InvalidDataException("The selected roll lacks its complete forward travel.");
            var clip = AnimationPlaygroundClipDerivatives.CreateRange(SourcePath, "CampDodgeRollGrounded", SourceStart, SourceEnd);
            var ratio = Duration / (SourceEnd - SourceStart);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var original = AnimationUtility.GetEditorCurve(clip, binding);
                var keys = original.keys;
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i].time *= ratio;
                    if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z") keys[i].value = 0;
                }
                AnimationUtility.SetEditorCurve(clip, binding, Linear(keys));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.stopTime = Duration;
            settings.loopTime = false; settings.loopBlend = false; settings.loopBlendPositionY = true;
            settings.keepOriginalPositionY = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            var modelPath = AssetDatabase.GetAssetPath(bindings.targetModel);
            // Measure the retargeted RootT.y response; do not assume a prior avatar's human scale.
            var rootY = AnimationUtility.GetCurveBindings(clip).Single(b => b.propertyName == "RootT.y");
            var first = AnimationPlaygroundMotionSampler.Measure(modelPath, ClipPath, bindings.visualScale, bindings.visualOffset, 90);
            var before = first.frames[0].bodyMinimum.y;
            var curveY = AnimationUtility.GetEditorCurve(clip, rootY);
            var originalY = curveY.keys;
            AnimationUtility.SetEditorCurve(clip, rootY, Linear(originalY.Select(k => new Keyframe(k.time, k.value + .01f)).ToArray()));
            var shifted = AnimationPlaygroundMotionSampler.Measure(modelPath, ClipPath, bindings.visualScale, bindings.visualOffset, 90);
            var worldPerUnit = (shifted.frames[0].bodyMinimum.y - before) / .01f;
            AnimationUtility.SetEditorCurve(clip, rootY, Linear(originalY));
            if (!float.IsFinite(worldPerUnit) || worldPerUnit < .1f || worldPerUnit > 2)
                throw new InvalidDataException("Unexpected retargeted vertical scale.");
            AnimationPlaygroundMotionSampler.MotionMeasurement measured = null;
            for (var pass = 0; pass < 3; pass++)
            {
                measured = AnimationPlaygroundMotionSampler.Measure(modelPath, ClipPath, bindings.visualScale, bindings.visualOffset, 90);
                curveY = AnimationUtility.GetEditorCurve(clip, rootY);
                var keys = measured.frames.Select(f => new Keyframe(f.seconds,
                    curveY.Evaluate(f.seconds) + (pass == 0 ? -.907f - f.bodyMinimum.y : Mathf.Max(0, -.907f - f.bodyMinimum.y)) / worldPerUnit)).ToArray();
                AnimationUtility.SetEditorCurve(clip, rootY, Linear(keys));
            }
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            measured = AnimationPlaygroundMotionSampler.Measure(modelPath, ClipPath, bindings.visualScale, bindings.visualOffset, 90);
            if (!measured.finite || measured.frames.Min(f => f.bodyMinimum.y) < -.910f)
                throw new InvalidDataException("The complete grounded roll has an unbounded body envelope.");
            var profile = ScriptableObject.CreateInstance<AnimationPlaygroundDodgeProfile>();
            profile.targetModel = bindings.targetModel; profile.roll = clip;
            profile.bodyProjection = bindings.continuationReview.nativeBodyProjection;
            var travel = new Keyframe[129]; var previous = 0f;
            for (var i = 0; i < travel.Length; i++)
            {
                var phase = i / 128f;
                var value = Mathf.Clamp01((nativeZ.Evaluate(Mathf.Lerp(SourceStart, SourceEnd, phase)) - start) / (end - start));
                previous = Mathf.Max(previous, value); travel[i] = new Keyframe(phase, previous);
            }
            travel[0].value = 0; travel[travel.Length - 1].value = 1; profile.travel = Linear(travel);
            if (!profile.Validate(bindings.targetModel, out var reason, false)) throw new InvalidDataException(reason);
            // This flag describes measured source/geometry qualification, not author gameplay acceptance.
            profile.qualified = true; AssetDatabase.CreateAsset(profile, ProfilePath); AssetDatabase.SaveAssetIfDirty(profile);
            bindings.dodge = profile; bindings.dodgeRequired = true;
            if (!bindings.ValidatePlayer(out reason)) throw new InvalidDataException(reason);
            EditorUtility.SetDirty(bindings); AssetDatabase.SaveAssetIfDirty(bindings);
            return new { source = SourcePath, sourceStart = SourceStart, sourceEnd = SourceEnd,
                duration = clip.length, profile.distance, worldPerUnit,
                minimumBody = measured.frames.Min(f => f.bodyMinimum.y), maximumBody = measured.frames.Max(f => f.bodyMaximum.y),
                retainedFastRun = bindings.forwardReviewOverride.gaits[3].nominalSpeed,
                retainedSprint = bindings.forwardReviewOverride.gaits[4].nominalSpeed };
        }
    }
}
