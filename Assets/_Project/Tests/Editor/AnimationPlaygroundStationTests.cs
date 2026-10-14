using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundStationTests
    {
        [Test] public void PreparedLayoutRejectsChangedOrEscapingTerrainIdentities()
        {
            var path="Docs/Evidence/Gameplay/AnimationPlaygroundImplementation20261013/Stations/layout.json";
            var layout=JsonUtility.FromJson<AnimationPlaygroundLayoutAuthoring.Layout>(System.IO.File.ReadAllText(path));
            Assert.That(AnimationPlaygroundLayoutAuthoring.TerrainIdentitiesMatch(layout),Is.True);
            var hash=layout.terrains[0].sha256;layout.terrains[0].sha256=new string('0',64);
            Assert.That(AnimationPlaygroundLayoutAuthoring.TerrainIdentitiesMatch(layout),Is.False);
            layout.terrains[0].sha256=hash;layout.terrains[0].path="../Outside.asset";
            Assert.That(AnimationPlaygroundLayoutAuthoring.TerrainIdentitiesMatch(layout),Is.False);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        [TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)]
        public void AuthoredPrefabsHaveOwnedBoundedFixturesAndNoPrematurePhysicalActions(int number)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AnimationPlaygroundStationAuthoring.StationPath(number));
            Assert.That(prefab,Is.Not.Null,"Prepare the twelve station assets before running LAB1 checks.");
            var station=prefab.GetComponent<TopDown3DAnimationPlaygroundStation>();
            Assert.That(station.StationId,Is.EqualTo("AP"+number.ToString("00")));
            Assert.That(station.Footprint,Is.EqualTo(new Vector2(28,24))); Assert.That(station.Entry,Is.Not.Null);Assert.That(station.SafeReturn,Is.Not.Null);
            Assert.That(station.Targets.Count(value=>value.ActionKind==AnimationPlaygroundActionKind.Preview),Is.EqualTo(1));
            Assert.That(station.Targets.Count(value=>value.ActionKind==AnimationPlaygroundActionKind.Reset),Is.EqualTo(1));
            Assert.That(station.Targets.Select(value=>value.StableId).Distinct().Count(),Is.EqualTo(station.Targets.Length));
            Assert.That(station.Props.Select(value=>value.StableId).Distinct().Count(),Is.EqualTo(station.Props.Length));
            foreach(var target in station.Targets)
            {
                var state=new SerializedObject(target).FindProperty("available").boolValue;
                if(target.ActionKind!=AnimationPlaygroundActionKind.Preview && target.ActionKind!=AnimationPlaygroundActionKind.Reset)
                {
                    Assert.That(state,Is.False);
                    Assert.That(target.GetComponentsInChildren<Collider>(true).All(value=>value.isTrigger),Is.True,"Physical intent markers must not obstruct the lane.");
                }
            }
            Assert.That(prefab.GetComponentsInChildren<Animator>(true).Length,Is.Zero);
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,Is.Zero);
            foreach(var body in prefab.GetComponentsInChildren<Rigidbody>(true)) Assert.That(TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(body.transform),Is.True);
        }

        [Test] public void PropLeaseAndResetRestoreOnlyRegisteredAuthoredState()
        {
            var parent=new GameObject("Authored station fixture");var propObject=new GameObject("Registered prop fixture");propObject.transform.SetParent(parent.transform,false);
            var untouched=GameObject.CreatePrimitive(PrimitiveType.Cube);untouched.transform.position=new Vector3(60000,20,60000);
            var body=propObject.AddComponent<Rigidbody>();body.useGravity=true;body.isKinematic=false;
            var collider=propObject.AddComponent<BoxCollider>();var prop=propObject.AddComponent<AnimationPlaygroundProp>();prop.Configure("AP05.ScrapFixture");
            var registry=new AnimationPlaygroundResetRegistry();var owner=new object();prop.Bind(registry);
            try
            {
                Assert.That(prop.TryAcquire(null),Is.False);Assert.That(prop.TryAcquire(owner),Is.True);Assert.That(prop.TryAcquire(new object()),Is.False);
                Assert.That(prop.Release(new object()),Is.False);
                var capture=prop.Epoch;propObject.transform.position=Vector3.one*10;body.useGravity=false;body.isKinematic=true;collider.enabled=false;
                Assert.That(registry.TryReset(),Is.True,registry.LastFailure);
                Assert.That(prop.HasLease,Is.False);Assert.That(prop.Epoch,Is.GreaterThan(capture));Assert.That(propObject.transform.localPosition,Is.EqualTo(Vector3.zero));
                Assert.That(body.useGravity,Is.True);Assert.That(body.isKinematic,Is.False);Assert.That(collider.enabled,Is.True);
                Assert.That(untouched.transform.position,Is.EqualTo(new Vector3(60000,20,60000)));
                Assert.That(prop.TryAcquire(owner),Is.True);prop.Unbind();Assert.That(prop.HasLease,Is.False);Assert.That(prop.TryAcquire(owner),Is.False);
            }
            finally{Object.DestroyImmediate(parent);Object.DestroyImmediate(untouched);}
        }

        [Test] public void DuplicatePropIdentityCannotAcquireASecondResetOwner()
        {
            var a=new GameObject("First");var b=new GameObject("Second");var parent=new GameObject("Authored parent");a.transform.SetParent(parent.transform);b.transform.SetParent(parent.transform);
            try
            {
                var first=a.AddComponent<AnimationPlaygroundProp>();first.Configure("AP06.Crate");var second=b.AddComponent<AnimationPlaygroundProp>();second.Configure("AP06.Crate");
                var registry=new AnimationPlaygroundResetRegistry();first.Bind(registry);
                Assert.Throws<InvalidOperationException>(()=>second.Bind(registry));Assert.That(second.TryAcquire(new object()),Is.False);
            }
            finally{Object.DestroyImmediate(parent);}
        }

        [TestCase("AP00",false)][TestCase("AP01",true)][TestCase("AP12",true)][TestCase("AP13",false)][TestCase("AP1",false)]
        public void StationIdentityMatchesTheTwelveAuthoredAreas(string id,bool valid)
            => Assert.That(TopDown3DAnimationPlaygroundStation.IsStationId(id),Is.EqualTo(valid));

        [Test,Category("PrivateAnimationQualification")]
        public void InstalledStationsFilterPaletteInvalidateCapturesAndResetOwnedProps()
        {
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);InputActionAsset actions=null;
            try
            {
                var session=TopDown3DAnimationPlaygroundSession.Find(scene);Assert.That(session.Stations.Length,Is.EqualTo(12));
                Invoke(session,"Awake");var player=session.Player;Invoke(player.GetComponent<TopDown3DPlayerAnimationDriver>(),"Awake");
                var input=player.InputRouter;actions=AnimationPlaygroundInputFactory.Create(input.InputActions);input.Configure(actions);input.Initialize();
                Invoke(session,"Start");Assert.That(session.Readiness,Is.EqualTo(AnimationPlaygroundReadiness.VisualReady),session.UnavailableReason);
                var menu=TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(scene);
                var station=session.Stations.Single(value=>value.StationId=="AP03");
                var inspect=station.Targets.Single(value=>value.ActionKind==AnimationPlaygroundActionKind.Preview);var old=new TopDown3DInteractionTarget(inspect);
                Assert.That(session.TryBegin(old),Is.True);Assert.That(menu.IsOpen,Is.True);
                var choices=(System.Collections.Generic.List<AnimationPlaygroundCatalogEntry>)typeof(TopDown3DMenuController).GetField("animationChoices",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
                Assert.That(choices.Count,Is.GreaterThan(0));Assert.That(choices.All(value=>value.stationId=="AP03" && !value.restrictedReference),Is.True);menu.Close();
                menu.OpenAnimationPaletteForStation("AP07");Assert.That(choices.Count,Is.Zero);
                Assert.That(menu.GetComponent<TopDown3DMenuView>(),Is.Not.Null);menu.Close();
                var prop=session.Stations.Single(value=>value.StationId=="AP06").Props.Single();
                var initial=prop.transform.position;prop.transform.position+=Vector3.right*2;Assert.That(prop.TryAcquire(new object()),Is.True);
                Physics.SyncTransforms();session.ResetSession();
                Assert.That(session.ResetRegistry.LastFailure,Is.Null);Assert.That(old.IsValid,Is.False);Assert.That(prop.HasLease,Is.False);
                Assert.That(prop.transform.position,Is.EqualTo(initial));Assert.That(input.LaboratoryResetBlocked,Is.False);
                Assert.That(session.Stations.All(value=>value.Epoch>0),Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>(true)).Count(),Is.EqualTo(12));
            }
            finally{AnimationPlaygroundPlayerIntegrationTests.CloseFixtureScene(scene);if(actions!=null)Object.DestroyImmediate(actions);}
        }
        private static void Invoke(object value,string method)=>value.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
    }
}
