using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DAlgaeControllerTests
    {
        private Scene scene;
        private GameObject player;
        private InputActionAsset inputAsset;
        private TopDown3DInputRouter router;
        private TopDown3DPlayerMotor motor;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DAlgaeCanisterState state;
        private TopDown3DAlgaeConsumptionController controller;
        [SetUp] public void Setup()
        {
            scene=EditorSceneManager.NewPreviewScene();player=new GameObject("Algae controller test");player.SetActive(false);
            SceneManager.MoveGameObjectToScene(player,scene);
            motor=player.AddComponent<TopDown3DPlayerMotor>();
            var inputRoot=new GameObject("Separate scene input");inputRoot.SetActive(false);SceneManager.MoveGameObjectToScene(inputRoot,scene);
            router=inputRoot.AddComponent<TopDown3DInputRouter>();
            inputAsset=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            router.Configure(inputAsset);router.Initialize();
            motor.Configure(router,null);
            vitals=player.AddComponent<TopDown3DSurvivalVitals>();vitals.ResetToFull();
            TopDown3DSurvivalHud.TryInstallForScene(scene);
            controller=TopDown3DAlgaeConsumptionController.TryInstallForScene(scene);state=controller.State;
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,50);
        }
        [TearDown] public void Cleanup()
        {
            typeof(TopDown3DInputRouter).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(router,null);
            EditorSceneManager.ClosePreviewScene(scene);Object.DestroyImmediate(inputAsset);
        }
        [Test] public void SharedCooldownUsesUnscaledTimeAndFailuresDoNotSpend()
        {
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,0),Is.True);
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,.1),Is.False);
            Assert.That(state.Remaining,Is.EqualTo(95));
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,.36),Is.True);Assert.That(state.Remaining,Is.EqualTo(90));
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Eat,.8),Is.False);Assert.That(state.Remaining,Is.EqualTo(90));
            vitals.SetValue(TopDown3DSurvivalVital.Hunger,50);
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Eat,.8),Is.True);
        }
        [Test] public void ModesAndConstraintsBlockUseWithoutStartingCooldown()
        {
            router.EnterInventoryMode();Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,0),Is.False);
            router.EnterGameplayMode();motor.TryBeginActionConstraint(this);
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,0),Is.False);motor.EndActionConstraint(this);
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,0),Is.True);Assert.That(state.Remaining,Is.EqualTo(95));
        }
        [Test] public void DustPreviewBlocksConsumption()
        {
            var actions=player.AddComponent<TopDown3DPlayerActionController>();
            var preview=new GameObject("Test dust preview");SceneManager.MoveGameObjectToScene(preview,scene);
            typeof(TopDown3DPlayerActionController).GetField("placementPreview",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(actions,preview);
            controller.Configure(router,vitals,state);
            Assert.That(controller.TryUse(TopDown3DAlgaeUse.Drink,0),Is.False);Assert.That(state.Remaining,Is.EqualTo(100));
        }
        [Test] public void InstallationAndHudRebuildKeepOneReserveAndNoExtraCanvas()
        {
            controller.TryUse(TopDown3DAlgaeUse.Drink,0);
            var hud=TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller);
            Assert.That(TopDown3DAlgaeConsumptionController.TryInstallForScene(scene),Is.SameAs(controller));
            Assert.That(TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller),Is.SameAs(hud));
            Assert.That(state.Remaining,Is.EqualTo(95));Assert.That(player.GetComponents<TopDown3DAlgaeCanisterState>().Length,Is.EqualTo(1));
            var pad=hud.GetComponent<TopDown3DActionDpadHud>();
            Assert.That(pad.Graphic.GetComponent<CanvasRenderer>(),Is.Not.Null,"D-pad geometry needs a renderer, not only a graphic component");
            Assert.That(pad.GetIcon(TopDown3DDpadDirection.Left),Is.EqualTo(TopDown3DDpadIcon.Droplet));
            Assert.That(pad.GetIcon(TopDown3DDpadDirection.Down),Is.EqualTo(TopDown3DDpadIcon.Vial));
            Assert.That(pad.GetComponentsInChildren<Canvas>().Length,Is.Zero);
            Assert.That(hud.ContentsText,Does.Contain("95"));
            foreach(var graphic in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>())Assert.That(graphic.raycastTarget,Is.False);
        }
        [Test] public void ModalTransitionAfterQueuedInputCancelsDose()
        {
            typeof(TopDown3DAlgaeConsumptionController).GetMethod("Queue",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,new object[]{TopDown3DAlgaeUse.Drink});
            router.EnterInventoryMode();router.EnterGameplayMode();
            typeof(TopDown3DAlgaeConsumptionController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,null);
            Assert.That(state.Remaining,Is.EqualTo(100));
        }
        [Test] public void SurvivalIconGeometryFacesCanvas()
        {
            var hud=TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller);
            var graphic=hud.GetComponent<TopDown3DActionDpadHud>().Graphic;
            var mesh=new Mesh();
            try
            {
                using(var vertices=new UnityEngine.UI.VertexHelper())
                {
                    typeof(TopDown3DActionDpadGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(UnityEngine.UI.VertexHelper)},null).Invoke(graphic,new object[]{vertices});
                    vertices.FillMesh(mesh);
                }
                Assert.That(mesh.vertexCount,Is.GreaterThan(0));var points=mesh.vertices;var triangles=mesh.triangles;
                for(var i=0;i<triangles.Length;i+=3)
                    Assert.That(Vector3.Cross(points[triangles[i+1]]-points[triangles[i]],points[triangles[i+2]]-points[triangles[i]]).z,Is.LessThan(0));
            }
            finally {Object.DestroyImmediate(mesh);}
        }
        [Test] public void UnavailableActionsHaveTextReasons()
        {
            var hud=TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller);
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,100);hud.RefreshPrompts();
            Assert.That(hud.transform.Find("Algae canister/Drink").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("Full"));
            router.EnterInventoryMode();hud.RefreshPrompts();
            Assert.That(hud.transform.Find("Algae canister/Drink").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("Menu"));
        }
        [Test] public void RestoreClearsUseFeedbackWithoutRefilling()
        {
            var hud=TopDown3DAlgaeCanisterHud.TryInstallForScene(scene,controller);
            controller.TryUse(TopDown3DAlgaeUse.Drink,0);
            var outcome=hud.transform.Find("Algae canister/Outcome").GetComponent<UnityEngine.UI.Text>();
            Assert.That(outcome.text,Is.Not.Empty);
            state.ApplySnapshot(new TopDown3DAlgaeCanisterSnapshot{remaining=0,capacity=100});
            Assert.That(outcome.text,Is.Empty);Assert.That(hud.ContentsText,Does.Contain("0 / 100"));
        }
    }
}
