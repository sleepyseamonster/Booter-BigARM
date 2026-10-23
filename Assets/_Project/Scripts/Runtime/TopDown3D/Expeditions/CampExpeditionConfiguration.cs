using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName="Booter & BigARM/Camp Expedition Configuration")]
    public sealed class CampExpeditionConfiguration:ScriptableObject
    {
        public bool enabledForCampLoop;
        public CampExpeditionDefinition definition;
        public GameObject menuPrefab;
        public string terrainIdentity="CampLoopTesting/native-terrain.v1";
        public int startingStorageSlots=24;
        public long startingCommons=50,overflowFeePerSlot=1;
        public float autosaveSeconds=30,gameDaySeconds=1200;
        public string ContentFingerprint => CampExpeditionFile.Hash(JsonUtility.ToJson(definition.items.SelectContent())+"/layout-"+CampExpeditionLayout.Version+"/population-"+CampExpeditionPopulation.Version+"/"+terrainIdentity);
    }
    internal static class CampExpeditionContentProjection
    {
        [System.Serializable] internal sealed class Content {public Row[] items;}
        [System.Serializable] internal sealed class Row {public string id,equipment,category;public CampExpeditionIngredient[] inputs;public int maxStack,slot,carry,packing;public float mass;}
        internal static Content SelectContent(this CampExpeditionItem[] items)
        {
            var rows=new Row[items.Length];
            for(int i=0;i<items.Length;i++)rows[i]=new Row{id=items[i].id,equipment=items[i].equipmentAddress,inputs=items[i].inputs,maxStack=items[i].runtimeDefinition.MaxStack,mass=items[i].runtimeDefinition.Mass,category=items[i].runtimeDefinition.Category,slot=(int)items[i].runtimeDefinition.EquipmentSlot,carry=(int)items[i].runtimeDefinition.CarryPreference,packing=(int)items[i].runtimeDefinition.PackingPreference};
            System.Array.Sort(rows,(a,b)=>string.CompareOrdinal(a.id,b.id));return new Content{items=rows};
        }
    }
    public static class CampExpeditionFeature
    {
        private static CampExpeditionConfiguration configuration;
        public static CampExpeditionConfiguration Configuration=>configuration!=null?configuration:configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");
        public static bool Enabled(Scene scene)=>Application.isPlaying && scene.IsValid() && scene.path==CampExpeditionDefinition.ScenePath && Configuration!=null && Configuration.enabledForCampLoop;
        public static bool FieldAllowed(Scene scene)=>!Enabled(scene)||CampExpeditionSession.Find(scene)?.CanSimulateField==true;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>configuration=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]private static void Register(){var _=Configuration;SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Current()=>Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        private static void Loaded(Scene scene,LoadSceneMode mode){if(Enabled(scene))CampExpeditionSession.Install(scene);}
    }
}
