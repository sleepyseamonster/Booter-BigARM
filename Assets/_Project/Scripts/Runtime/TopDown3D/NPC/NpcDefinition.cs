using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/NPC/Definition")]
    public sealed class NpcDefinition : ScriptableObject
    {
        [SerializeField] private string typeId = "lab.npc.legger.skittish";
        [SerializeField] private string displayName = "Lager";
        [SerializeField] private SkittishNpcProfile profile;
        public string TypeId => typeId;
        public string DisplayName => displayName;
        public SkittishNpcProfile Profile => profile;
        public void Configure(SkittishNpcProfile value) => profile = value;
        public bool Validate(out string reason)
        {
            reason = "Missing NPC definition, identity or profile.";
            return !string.IsNullOrWhiteSpace(typeId) && !string.IsNullOrWhiteSpace(displayName)
                && profile != null && profile.Snapshot() is NpcTuning value && value.Validate(out reason);
        }
    }
}
