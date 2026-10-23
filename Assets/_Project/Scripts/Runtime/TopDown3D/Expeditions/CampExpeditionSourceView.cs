using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>A disposable view of the durable node, including exhausted sites.</summary>
    [DisallowMultipleComponent]
    public sealed class CampExpeditionSourceView : MonoBehaviour
    {
        [SerializeField] private string sourceName, collectVerb, toolVerb;
        [SerializeField] private Transform workAnchor;
        [SerializeField] private GameObject recoverable, lodgedDebris;
        [SerializeField] private Collider targetVolume, body;
        public string SourceName => sourceName;
        public string Verb(bool tool) => tool ? toolVerb : collectVerb;
        public Transform WorkAnchor => workAnchor;
        public bool Valid => !string.IsNullOrWhiteSpace(sourceName) && !string.IsNullOrWhiteSpace(collectVerb)
            && !string.IsNullOrWhiteSpace(toolVerb) && workAnchor != null && recoverable != null
            && lodgedDebris != null && targetVolume != null && body != null
            && targetVolume.transform.IsChildOf(transform) && body.transform.IsChildOf(transform);
        public bool Owns(Collider shape) => shape != null && (shape == targetVolume || shape == body);
        public void Configure(string label, string collect, string tool, Transform anchor, GameObject material,
            GameObject debris, Collider target, Collider solid)
        { sourceName = label; collectVerb = collect; toolVerb = tool; workAnchor = anchor; recoverable = material; lodgedDebris = debris; targetVolume = target; body = solid; }
        public void Present(CampExpeditionNodeRecord record)
        {
            bool available = record != null && record.remaining > 0;
            recoverable.SetActive(available);
            lodgedDebris.SetActive(available && record.requiresTool);
            targetVolume.enabled = available;
            body.enabled = available;
        }
        public CampExpeditionSource Bind(CampExpeditionSession session, CampExpeditionNodeRecord record)
        {
            var source = GetComponent<CampExpeditionSource>();
            if (source == null) source = gameObject.AddComponent<CampExpeditionSource>();
            source.Configure(session, record.key, this);
            Present(record);
            return source;
        }
    }
}
