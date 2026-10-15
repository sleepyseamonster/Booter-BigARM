using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DSceneGroup { UI, Systems, World }

    // Resolve by scene on each creation; no cached references survive scene unload/reload.
    public static class TopDown3DSceneHierarchy
    {
        public static void Place(GameObject instance, Scene scene, TopDown3DSceneGroup group)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("Requires a loaded scene.", nameof(scene));
            if (!Enum.IsDefined(typeof(TopDown3DSceneGroup), group)) throw new ArgumentOutOfRangeException(nameof(group));
            string name = group.ToString();
            Transform parent = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != name) continue;
                if (parent != null) throw new InvalidOperationException("Multiple hierarchy groups named " + name + " in " + scene.name);
                if (!root.activeSelf) throw new InvalidOperationException("Hierarchy group " + name + " must be active.");
                parent = root.transform;
            }
            if (parent == null)
            {
                var owner = new GameObject(name);
                SceneManager.MoveGameObjectToScene(owner, scene);
                parent = owner.transform;
            }
            if (instance == parent.gameObject || parent.IsChildOf(instance.transform))
                throw new InvalidOperationException("Cannot place a hierarchy group inside itself.");
            if (instance.scene != scene)
            {
                instance.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(instance, scene);
            }
            instance.transform.SetParent(parent, true);
        }
    }
}
