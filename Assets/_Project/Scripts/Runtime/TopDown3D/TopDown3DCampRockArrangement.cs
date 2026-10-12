using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Identity for a fixed decorative arrangement. No generation or mutable gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DCampRockArrangement : MonoBehaviour
    {
        [Serializable] public sealed class Member
        {
            public string placementId, assetId;
            public Transform instance;
            public Vector3 groundAnchor;
        }
        [SerializeField] private string worldId = GreaterWastelandInventorySnapshot.WorldIdentity;
        [SerializeField] private string siteId = "camp.starting";
        [SerializeField] private int layoutVersion = 1;
        [SerializeField] private Vector3 campAnchor;
        [SerializeField] private Member[] members = Array.Empty<Member>();
        public string WorldId => worldId;
        public string SiteId => siteId;
        public int LayoutVersion => layoutVersion;
        public Vector3 CampAnchor => campAnchor;
        public Member[] Members => (Member[])members.Clone();
        internal void Configure(Vector3 anchor, Member[] placements)
        { campAnchor = anchor; members = placements; }
    }
}
