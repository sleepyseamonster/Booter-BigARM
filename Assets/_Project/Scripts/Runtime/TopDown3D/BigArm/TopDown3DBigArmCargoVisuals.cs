using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DBigArmCargoVisuals : MonoBehaviour
    {
        [SerializeField] private TopDown3DBigArmCargo cargo;
        private readonly List<Transform> anchors = new List<Transform>();
        private readonly List<GameObject> visuals = new List<GameObject>();

        public void Configure(TopDown3DBigArmCargo source)
        { cargo = source; EnsureAnchors(); Refresh(); }

        private void Awake() { cargo ??= GetComponent<TopDown3DBigArmCargo>(); EnsureAnchors(); }
        private void LateUpdate() { Refresh(); }

        private void EnsureAnchors()
        {
            if (cargo == null || cargo.PackingSettings == null) return;
            while (anchors.Count < cargo.PackingSettings.Mounts.Count)
            {
                var index = anchors.Count; var anchorObject = new GameObject($"Cargo Mount Anchor {index + 1}"); anchorObject.transform.SetParent(transform, false);
                var mount = cargo.PackingSettings.Mounts[index];
                var x = mount.Side == TopDown3DMountSide.Left ? -0.72f : 0.72f;
                var y = 0.45f + mount.Height * 0.35f;
                var z = (index % 3 - 1) * 0.32f;
                anchorObject.transform.localPosition = new Vector3(x, y, z); anchors.Add(anchorObject.transform);
            }
            while (visuals.Count < anchors.Count)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "Mounted Cargo"; visual.transform.SetParent(transform, false); var collider = visual.GetComponent<Collider>(); if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); visuals.Add(visual);
            }
        }

        private void Refresh()
        {
            if (cargo == null || cargo.State == null) return;
            EnsureAnchors();
            for (var i = 0; i < visuals.Count; i++)
            {
                var slot = i < cargo.State.Slots.Count ? cargo.State.Slots[i] : null; var active = slot != null && !slot.IsEmpty; var visual = visuals[i]; visual.SetActive(active); if (!active) continue;
                visual.transform.position = anchors[i].position; var fullness = Mathf.Clamp01(slot.Quantity / (float)cargo.ItemCatalog.GetRequiredDefinition(slot.ItemId).MaxStack); visual.transform.localScale = new Vector3(0.46f, 0.32f + fullness * 0.28f, 0.46f);
            }
        }
    }
}
