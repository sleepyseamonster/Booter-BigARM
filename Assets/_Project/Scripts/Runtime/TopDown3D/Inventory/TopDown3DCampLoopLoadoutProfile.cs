using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Camp Loop Loadout Profile")]
    public sealed class TopDown3DCampLoopLoadoutProfile : ScriptableObject
    {
        public TopDown3DItemCatalog sourceCatalog;
        public TopDown3DItemDefinition[] definitions = Array.Empty<TopDown3DItemDefinition>();
        public TopDown3DStartingInventoryItem[] startingEquipment = Array.Empty<TopDown3DStartingInventoryItem>();

        public static TopDown3DCampLoopLoadoutProfile For(Scene scene, TopDown3DItemCatalog catalog)
        {
            if (!TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene)) return null;
            var profile = Resources.Load<TopDown3DCampLoopLoadoutProfile>("CampLoopLoadoutProfile");
            return profile != null && profile.sourceCatalog == catalog ? profile : null;
        }
        public static TopDown3DItemCatalog Resolve(Scene scene, TopDown3DItemCatalog catalog)
        {
            var profile = For(scene, catalog);
            if (profile != null) catalog.ApplyLoadoutProfile(profile);
            return catalog;
        }
    }
}
