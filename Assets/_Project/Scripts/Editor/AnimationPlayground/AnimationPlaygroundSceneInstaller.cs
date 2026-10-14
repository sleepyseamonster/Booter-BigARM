using System;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundSceneInstaller
    {
        public const string RootName = "Animation Playground";
        public static TopDown3DAnimationPlaygroundSession InstallSession()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path != TopDown3DAnimationPlaygroundSession.ScenePath)
                throw new InvalidOperationException("Install into the already-loaded Camp Loop Testing scene in Edit Mode.");
            var existing = TopDown3DAnimationPlaygroundSession.Find(scene);
            if (existing != null) return existing;
            if (scene.isDirty) throw new InvalidOperationException("Preserve dirty scene work before first playground installation.");
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == RootName) throw new InvalidOperationException("Playground root is already owned by another configuration.");
            var owner = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(owner, scene);
            Undo.RegisterCreatedObjectUndo(owner, "Create animation playground session");
            var session = Undo.AddComponent<TopDown3DAnimationPlaygroundSession>(owner);
            EditorSceneManager.MarkSceneDirty(scene);
            return session;
        }
    }
}
