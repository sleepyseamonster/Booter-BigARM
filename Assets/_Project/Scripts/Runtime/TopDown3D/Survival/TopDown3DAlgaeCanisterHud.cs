using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DAlgaeCanisterHud : MonoBehaviour
    {
        private TopDown3DAlgaeConsumptionController controller;
        private TopDown3DActionDpadHud dpad;
        private TopDown3DSurvivalHud survivalHud;
        private TopDown3DMenuController menu;
        private Text contents, feedback;
        private Text dustLabel;
        private readonly Text[] labels = new Text[3];
        private readonly string[] prompts = new string[3], lastReasons = new string[3];
        private Image fill;
        private double displayedFill, lastRemaining = -1;
        private float feedbackUntil, pulseUntil, nextPrompts;
        private TopDown3DDpadDirection pulseDirection;
        private bool subscribed;
        private bool pulseActive;
        private float gaugeStarted = -1;
        private double gaugeFrom;
        public RectTransform DetailsRect { get; private set; }
        public string ContentsText => contents != null ? contents.text : string.Empty;

        public void Configure(TopDown3DAlgaeConsumptionController owner, TopDown3DActionDpadHud pad)
        {
            Unsubscribe(); controller = owner; dpad = pad;
            survivalHud = TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalHud>(gameObject.scene);
            if (DetailsRect == null) Build();
            dpad.Graphic.SetSurvivalIcons(true);
            displayedFill = controller.State.Remaining; RefreshContents(); RefreshPrompts(); Subscribe();
        }
        private void Build()
        {
            DetailsRect = Rect("Algae canister", transform, new Vector2(dpad.Size+16,0), new Vector2(235,196));
            var backdrop=Rect("Backdrop",DetailsRect,new Vector2(-10,-10),new Vector2(255,216)).gameObject.AddComponent<Image>();
            backdrop.color=new Color(.027f,.034f,.033f,.85f);backdrop.raycastTarget=false;
            contents = Label("Contents",DetailsRect,new Vector2(0,172),new Vector2(235,24),18);
            var cap = Rect("Canister cap",DetailsRect,new Vector2(9,151),new Vector2(24,8)).gameObject.AddComponent<Image>(); cap.color=new Color(.7f,.67f,.54f); cap.raycastTarget=false;
            var shell=Rect("Canister shell",DetailsRect,new Vector2(0,34),new Vector2(42,117)).gameObject.AddComponent<Image>();
            shell.color=new Color(.65f,.59f,.44f); shell.raycastTarget=false;
            var track=Rect("Contents track",shell.transform,new Vector2(4,4),new Vector2(34,109)).gameObject.AddComponent<Image>();
            track.color=new Color(.035f,.055f,.045f); track.raycastTarget=false;
            fill=Rect("Algae fill",track.transform,Vector2.zero,new Vector2(34,109)).gameObject.AddComponent<Image>();
            fill.color=new Color(.32f,.64f,.39f); fill.raycastTarget=false;
            labels[0]=Label("Drink",DetailsRect,new Vector2(54,112),new Vector2(181,29),16);
            labels[1]=Label("Eat",DetailsRect,new Vector2(54,76),new Vector2(181,29),16);
            labels[2]=Label("Treat",DetailsRect,new Vector2(54,140),new Vector2(181,29),16);
            feedback=Label("Outcome",DetailsRect,Vector2.zero,new Vector2(235,32),14);
            dustLabel=Label("Dust control",transform,new Vector2(0,-23),new Vector2(dpad.Size,20),13);
            dustLabel.alignment=TextAnchor.MiddleCenter;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero; rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        private static Text Label(string name,Transform parent,Vector2 position,Vector2 size,int fontSize)
        {
            var text=Rect(name,parent,position,size).gameObject.AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=fontSize;
            text.color=new Color(.94f,.89f,.77f); text.alignment=TextAnchor.MiddleLeft;
            text.raycastTarget=false; text.horizontalOverflow=HorizontalWrapMode.Wrap; return text;
        }
        private void Update()
        {
            if (controller == null) return;
            if(menu==null && Time.unscaledTime>=nextPrompts) menu=TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            var reduced=menu != null && menu.ReducedMotion;
            displayedFill=reduced || gaugeStarted<0 ? controller.State.Remaining
                : Mathf.Lerp((float)gaugeFrom,(float)controller.State.Remaining,Mathf.Clamp01((Time.unscaledTime-gaugeStarted)/.25f));
            if(gaugeStarted>=0 && Time.unscaledTime-gaugeStarted>=.25f) gaugeStarted=-1;
            fill.rectTransform.sizeDelta=new Vector2(34,109*(float)(displayedFill/controller.State.Settings.capacity));
            if (controller.State.Remaining != lastRemaining) RefreshContents();
            RefreshAvailability();
            if(Time.unscaledTime>=nextPrompts){RefreshPrompts();nextPrompts=Time.unscaledTime+.5f;}
            if(Time.unscaledTime>=feedbackUntil) feedback.text=string.Empty;
            if(pulseActive && (Time.unscaledTime>=pulseUntil || controller.Input.Mode!=TopDown3DInputMode.Gameplay))
            {dpad.SetDirectionPressed(pulseDirection,false);pulseActive=false;}
        }
        private void RefreshContents()
        {
            lastRemaining=controller.State.Remaining;
            contents.text=$"ALGAE {lastRemaining:0.#} / {controller.State.Settings.capacity:0.#}";
        }
        public void RefreshPrompts()
        {
            if(controller==null) return;
            prompts[0]=$"Drink [{controller.Input.GetBindingDisplayName("Gameplay/DrinkAlgae","Left").Replace("D-Pad ",string.Empty)}]";
            prompts[1]=$"Eat [{controller.Input.GetBindingDisplayName("Gameplay/EatAlgae","Right").Replace("D-Pad ",string.Empty)}]";
            prompts[2]=$"Treat [{controller.Input.GetBindingDisplayName("Gameplay/TreatWithAlgae","Up").Replace("D-Pad ",string.Empty)}]";
            for(var i=0;i<3;i++)lastReasons[i]=null;
            RefreshAvailability();
            dustLabel.text=$"{controller.Input.GetBindingDisplayName("Gameplay/DeployCanister","Down")} · Dust";
        }
        private void RefreshAvailability()
        {
            for(var i=0;i<3;i++)
            {
                var reason=controller.UnavailableReason((TopDown3DAlgaeUse)i);
                if(lastReasons[i]!=reason)
                {
                    var shortReason=reason=="Canister empty"?"Empty":reason=="Finish current action"?"Busy"
                        :reason=="Unavailable in menus or camp"?"Menu":reason=="Canister unavailable"?"No kit"
                        :reason=="Cannot treat after death"?"Dead":"Full";
                    labels[i].text=prompts[i]+(reason.Length==0?string.Empty:" · "+shortReason);lastReasons[i]=reason;
                }
                labels[i].color=reason.Length>0?new Color(.6f,.59f,.54f):i==0?new Color(.32f,.77f,.79f):i==1?new Color(.9f,.7f,.3f):new Color(.95f,.4f,.33f);
            }
        }
        private void Consumed(TopDown3DAlgaeUse use,double gain,double cost)
        {
            RefreshContents();
            var name=use==TopDown3DAlgaeUse.Drink?"Thirst":use==TopDown3DAlgaeUse.Eat?"Hunger":"Health";
            feedback.text=$"{name} +{gain:0.##} · Algae −{cost:0.##}"; feedbackUntil=Time.unscaledTime+2;
            gaugeFrom=displayedFill; gaugeStarted=Time.unscaledTime;
            dpad.SetDirectionPressed(pulseDirection,false);
            pulseDirection=use==TopDown3DAlgaeUse.Drink?TopDown3DDpadDirection.Left:use==TopDown3DAlgaeUse.Eat?TopDown3DDpadDirection.Right:TopDown3DDpadDirection.Up;
            pulseUntil=Time.unscaledTime+.18f;
            pulseActive=menu==null || !menu.ReducedMotion;
            if(pulseActive) dpad.SetDirectionPressed(pulseDirection,true);
            survivalHud?.PresentRestoration(use,(float)gain,menu!=null&&menu.ReducedMotion);
        }
        private void Rejected(string reason) {feedback.text=reason;feedbackUntil=Time.unscaledTime+2;}
        private void ResetPresentation()
        {
            feedback.text=string.Empty;feedbackUntil=0;gaugeStarted=-1;displayedFill=controller.State.Remaining;
            if(pulseActive)dpad.SetDirectionPressed(pulseDirection,false);pulseActive=false;RefreshContents();
        }
        private void Subscribe()
        {
            if(subscribed||controller==null)return;
            controller.Consumed+=Consumed;controller.Rejected+=Rejected;
            controller.State.Restored+=ResetPresentation;controller.Vitals.ResetPerformed+=ResetPresentation;subscribed=true;
        }
        private void Unsubscribe()
        {
            if(!subscribed||controller==null)return;
            controller.Consumed-=Consumed;controller.Rejected-=Rejected;
            controller.State.Restored-=ResetPresentation;controller.Vitals.ResetPerformed-=ResetPresentation;subscribed=false;
        }
        private void OnEnable()=>Subscribe();
        private void OnDisable(){Unsubscribe();if(dpad!=null)dpad.SetDirectionPressed(pulseDirection,false);}
        public static TopDown3DAlgaeCanisterHud TryInstallForScene(Scene scene,TopDown3DAlgaeConsumptionController controller)
        {
            var pad=TopDown3DActionDpadHud.TryInstallForScene(scene);
            if(pad==null || controller==null)return null;
            var hud=pad.GetComponent<TopDown3DAlgaeCanisterHud>() ?? pad.gameObject.AddComponent<TopDown3DAlgaeCanisterHud>();
            hud.Configure(controller,pad);return hud;
        }
    }
}
