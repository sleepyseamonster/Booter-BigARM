using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    /// <summary>Explicit, isolated-candidate authoring. Never opens or saves the author's scene.</summary>
    public static class CampExpeditionAuthoring
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab";
        public const string SettingsPath = "Assets/_Project/Settings/UI/CampExpedition/";
        private const string ArtPath = "Assets/_Project/UI/CampExpedition/";
        private static CampExpeditionMenuStyle style;
        private static CampExpeditionDefinition definition;
        private static CampExpeditionMenuView view;
        private static readonly List<CampMenuPageBinding> pages = new();

        public static void BuildFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying || !File.Exists(".camp-expedition-validation"))
                throw new InvalidOperationException("Author only in the marked, non-Play validation candidate.");
            AssetDatabase.Refresh(); ImportArt();
            style = LoadOrCreate<CampExpeditionMenuStyle>(SettingsPath + "MenuStyle.asset");
            style.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            style.backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + "game-inventory-background.png");
            style.grain = Sprite("inventory-surface-grain"); style.iron = Sprite("inventory-iron-surface"); style.character = Sprite("character-placeholder");
            var keys = new[] { "back", "frame", "chest", "legs", "head", "hands", "feet", "weapon", "tool", "attachment-0", "attachment-1" };
            var glyphs = new[] { "equipment-cloak", "slot-body-frame", "equipment-jacket", "slot-pants", "slot-headgear", "slot-gloves", "slot-boots", "slot-weapon", "equipment-digging_tool", "empty", "empty" };
            style.glyphs = keys.Select((k,i) => new CampExpeditionMenuStyle.Glyph { key=k, sprite=Sprite("glyph-"+glyphs[i]) })
                .Concat(new[]{"menu","view","select","west","back","lb","rb","lt","rt"}.Select(k=>new CampExpeditionMenuStyle.Glyph{key="prompt-"+k,sprite=Sprite("prompt-"+k)})).ToArray();
            definition = LoadOrCreate<CampExpeditionDefinition>(SettingsPath + "ExpeditionDefinition.asset");
            JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath+"MenuSource.json"),definition);
            foreach (var item in definition.items) item.art = Sprite(item.artName);
            definition.sourceFingerprint = File.ReadAllText("DocsReferenceFingerprint.txt").Trim();
            QualifyRuntimeItems();
            EditorUtility.SetDirty(style); EditorUtility.SetDirty(definition);
            var root = new GameObject("Camp Expedition Menu", typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup),typeof(CampExpeditionMenuView));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder=500;
                root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920,1080);
                view=root.GetComponent<CampExpeditionMenuView>();view.style=style;view.definition=definition;
                var background=Rect("Outpost Backdrop",root.transform);Stretch(background);
                var raw=background.gameObject.AddComponent<RawImage>();raw.texture=style.backdrop;raw.raycastTarget=false;
                var scrim=Rect("Backdrop Scrim",root.transform);Stretch(scrim);Fill(scrim,new Color(24/255f,27/255f,26/255f,.96f));
                var safe=Rect("Safe Area",root.transform);Stretch(safe);view.safeArea=safe;
                var top=Panel("Primary",safe,new Color(24/255f,27/255f,26/255f,.96f));Anchor(top,0,1,1,1,0,-63,0,0);
                PromptIcon(top,"lb",56,17,40,30);
                Control(top,"primary:play","Play",125,12,52,38);
                Control(top,"primary:outpost","Outpost",187,12,82,38);
                Control(top,"primary:crafting","Crafting",283,12,85,38);PromptIcon(top,"rb",382,17,40,30);
                var inventoryTabs=Rect("InventoryTabs",safe);Anchor(inventoryTabs,0,1,1,1,52,-114,-52,-73);
                Control(inventoryTabs,"carrier:booter","Booter",65,0,86,38);Control(inventoryTabs,"carrier:legger","Legger",196,0,86,38);
                PromptIcon(inventoryTabs,"lt",0,0,32,40);PromptIcon(inventoryTabs,"rt",326,0,32,40);
                Tabs(safe,"CraftingTabs",new[]{"workstations","tree","crafting-equipment","tracked","legger-equipment","projects"},new[]{"Workshops","Crafting tree","Booter equipment","Tracked items","Legger equipment","Tracked projects"});
                Tabs(safe,"OutpostTabs",new[]{"storage","jobs"},new[]{"Storage","Job board"});
                var host=Rect("Screen Host",safe);Anchor(host,0,0,1,1,52,105,-65,-121);view.content=host;
                pages.Clear();
                Play(host);Equipment(host);Cargo(host);Inspector(host);Picker(host);Tree(host);
                foreach(var key in new[]{"inventory","crafting-equipment","storage"})Page(host,key);
                var review=Page(host,"review");
                var all=Control(review,"send-all","Send all to storage",0,0,320,48);Anchor(all.GetComponent<RectTransform>(),0,0,.28f,0,0,62,0,110);
                var keep=Control(review,"continue","Continue · keep remaining cargo",0,0,320,48);Anchor(keep.GetComponent<RectTransform>(),0,0,.28f,0,0,0,0,48);
                var tracked=Page(host,"tracked");var results=Panel("Reverse uses",tracked,style.surface);Anchor(results,0,0,.165f,1,0,0,0,0);Header(results,"CRAFTING RESULTS",0,1);
                var allTracked=Control(results,"tracked-filter:all","All tracked items",0,0,250,45);Anchor(allTracked.GetComponent<RectTransform>(),0,1,1,1,18,-110,-18,-65);
                var projectTracked=Control(results,"tracked-filter:project","Iron-headed multi-tool",0,0,250,65);Anchor(projectTracked.GetComponent<RectTransform>(),0,1,1,1,18,-200,-18,-135);
                var bookmarks=Panel("Bookmarked items",tracked,style.surface);Anchor(bookmarks,.18f,0,.814f,1,0,0,0,0);Header(bookmarks,"TRACKED ITEMS",0,1);
                for(int i=0;i<15;i++){var item=Card(bookmarks,"bookmark:"+i,"",null,145,220);Place(item.GetComponent<RectTransform>(),16+i%5*157,60+i/5*232,145,220);}
                var projects=Page(host,"projects");var projectCard=Panel("Project",projects,style.surface);Anchor(projectCard,0,1,.615f,1,0,-160,0,0);Label(projectCard,"Iron-headed multi-tool · 1",22,20,700,44,22);Label(projectCard,"Gather and bank the project materials.",22,80,700,50,18);
                var checklist=Panel("Gathering list",projects,style.surface);Anchor(checklist,.63f,1,1,1,0,-440,0,0);Label(checklist,"COMBINED REQUIREMENTS",22,20,600,30,16).color=style.accent;Label(checklist,"Gathering list",22,55,600,40,24);
                view.projectChecklist=Label(checklist,"",22,110,600,300,18);Anchor(view.projectChecklist.rectTransform,0,0,1,1,22,20,-22,-110);
                Workstations(host);
                Simple(host,"legger-equipment","LEGGER EQUIPMENT","Equipment fitting is unavailable. Open Inventory → Legger to manage cargo.");
                Simple(host,"jobs","JOB BOARD","No posted jobs are available.");
                Simple(host,"guild","GUILD REQUISITION","Requisitions are unavailable.");
                Simple(host,"mess","MESS HALL","Meal and recovery services are unavailable.");
                ResourcesPage(host);
                var quote=Panel("TransactionQuote",host,new Color(.045f,.055f,.05f,.98f));Stretch(quote);
                Label(quote,"REVIEW TRANSFER",28,20,800,40,24);
                var quoteViewport=Rect("Manifest viewport",quote);Anchor(quoteViewport,0,0,1,1,28,155,-28,-90);quoteViewport.gameObject.AddComponent<RectMask2D>();view.quoteViewport=quoteViewport;
                view.quoteBody=Label(quoteViewport,"",0,0,1200,560,18);Anchor(view.quoteBody.rectTransform,0,1,1,1,0,-560,0,0);view.quoteBody.rectTransform.pivot=new Vector2(.5f,1);view.quoteBody.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var quoteScroll=quoteViewport.gameObject.AddComponent<ScrollRect>();quoteScroll.viewport=quoteViewport;quoteScroll.content=view.quoteBody.rectTransform;quoteScroll.horizontal=false;
                view.quoteSummary=Label(quote,"",28,0,1200,45,18);Anchor(view.quoteSummary.rectTransform,0,0,1,0,28,95,-28,140);view.quoteSummary.color=style.accent;
                var confirm=Control(quote,"quote-confirm","Confirm transfer",0,0,280,48);Anchor(confirm.GetComponent<RectTransform>(),1,0,1,0,-600,20,-310,68);
                var cancel=Control(quote,"quote-cancel","Cancel",0,0,280,48);Anchor(cancel.GetComponent<RectTransform>(),1,0,1,0,-295,20,-15,68);quote.gameObject.SetActive(false);
                var foot=Panel("Footer",safe,style.background);Anchor(foot,0,0,1,0,0,0,0,61);view.footer=foot;
                var prompts=Rect("Prompts",foot);prompts.anchorMin=prompts.anchorMax=new Vector2(1,.5f);prompts.pivot=new Vector2(1,.5f);prompts.anchoredPosition=new Vector2(-52,0);prompts.sizeDelta=new Vector2(1300,48);
                var horizontal=prompts.gameObject.AddComponent<HorizontalLayoutGroup>();horizontal.spacing=12;horizontal.childForceExpandWidth=false;horizontal.childForceExpandHeight=false;horizontal.childControlHeight=true;horizontal.childControlWidth=true;horizontal.childAlignment=TextAnchor.MiddleRight;
                prompts.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
                FooterPrompt(prompts,"page:start","Start menu","menu");FooterPrompt(prompts,"page:inventory","Inventory","north");FooterPrompt(prompts,"hint:select","Select","select");FooterPrompt(prompts,"hint:west","Unequip","west");FooterPrompt(prompts,"hint:back","Back","back");FooterPrompt(prompts,"hint:navigate","Navigate","navigate");FooterPrompt(prompts,"hint:pan","Pan canvas","ls");FooterPrompt(prompts,"hint:scroll","Scroll","rs");FooterPrompt(prompts,"hint:tabs","Primary tabs","bumpers");FooterPrompt(prompts,"hint:subtabs","Sub-tabs","triggers");
                view.balance=Label(top,"0 Commons",0,16,230,30,16);Anchor(view.balance.rectTransform,1,0,1,1,-255,16,-25,-16);
                view.status=Label(safe,"",52,0,1400,40,16);Anchor(view.status.rectTransform,0,0,1,0,52,65,-65,105);view.status.color=style.accent;
                view.pages=pages.ToArray();
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
                var configuration=LoadOrCreate<CampExpeditionConfiguration>(SettingsPath+"Resources/CampExpeditionConfiguration.asset");
                configuration.enabledForCampLoop=true;configuration.definition=definition;configuration.menuPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var terrainSources=AssetDatabase.GetDependencies(CampExpeditionDefinition.ScenePath,true).Where(p=>AssetDatabase.GetMainAssetTypeAtPath(p)==typeof(TerrainData)).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p+"/"+AssetDatabase.GetAssetDependencyHash(p));
                configuration.terrainIdentity="CampLoopTesting/"+CampExpeditionFile.Hash(File.ReadAllText(CampExpeditionDefinition.ScenePath)+string.Join("\n",terrainSources));
                EditorUtility.SetDirty(configuration);AssetDatabase.SaveAssets();
                Debug.Log("Camp expedition native prefab authored: "+PrefabPath);
            }
            finally { root.SetActive(false); UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Play(RectTransform host)
        {
            var page=Page(host,"play");
            var tasks=new[]{"QUEST PROGRESS","ITEM PROGRESS","TRACKED ITEMS"};
            for(int i=0;i<3;i++)
            {
                var panel=Panel(tasks[i],page,new Color(style.background.r,style.background.g,style.background.b,.72f));Place(panel,0,-29+i*84,280,70);
                Label(panel,tasks[i],18,12,244,24,14);
                var body=Label(panel,i==0?"—":i==1?"T1 material project":"—",18,37,244,30,16);
                if(i==1)view.projectBody=body;
            }
            var play=Control(page,"play","PLAY",0,0,300,76);play.goldAction=true;play.title.fontSize=28;play.title.fontStyle=FontStyle.Bold;Anchor(play.GetComponent<RectTransform>(),1,0,1,0,-300,-20,0,56);
            var chevron=Panel("Play arrow",play.transform,style.background);Place(chevron,13,14,48,48);Label(chevron,"›",8,0,40,45,40);
        }
        private static void Equipment(RectTransform host)
        {
            var layout=Rect("BooterLayout",host);Stretch(layout);
            var portrait=Panel("Character",layout,style.surface);Anchor(portrait,0,0,.28f,1,0,0,0,-31);portrait.GetComponent<CampMenuPanelGraphic>().corner=12;
            Header(layout,"CHARACTER",0,.28f);
            var image=Rect("Character silhouette",portrait);Anchor(image,0,0,1,1,12,45,-12,-15);var art=image.gameObject.AddComponent<Image>();art.sprite=style.character;art.preserveAspect=true;art.raycastTarget=false;
            var grain=Rect("Material",portrait);Stretch(grain);var texture=grain.gameObject.AddComponent<Image>();texture.sprite=style.grain;texture.type=Image.Type.Tiled;texture.color=new Color(1,1,1,.12f);texture.raycastTarget=false;grain.SetAsFirstSibling();
            var caption=Label(portrait,"EMPTY CHARACTER SPACE",0,0,430,32,16);Anchor(caption.rectTransform,0,0,1,0,12,5,-12,37);caption.alignment=TextAnchor.MiddleCenter;
            var equipment=Rect("Equipment",layout);Anchor(equipment,.314f,0,.694f,1,0,0,0,0);Header(equipment,"EQUIPMENT",0,1);
            var grid=Rect("Equipment Grid",equipment);Anchor(grid,0,1,1,1,0,-526,0,-31);grid.pivot=new Vector2(.5f,1);view.inventoryGrid=grid;
            Equip(grid,"back","BACK",0,.19f,0,.485f);Equip(grid,"frame","BODY FRAME",0,.19f,.505f,1);
            Equip(grid,"chest","CHEST",.205f,.56f,0,.485f);Equip(grid,"legs","LEGS",.205f,.56f,.505f,1);
            Equip(grid,"head","HEAD",.575f,.805f,0,.314f);Equip(grid,"hands","HANDS",.575f,.805f,.338f,.652f);Equip(grid,"feet","FEET",.575f,.805f,.676f,1);
            Equip(grid,"weapon","WEAPON",.82f,1,0,.485f);Equip(grid,"tool","MULTI-TOOL",.82f,1,.505f,1);
            var attachments=Rect("Attachments",equipment);Anchor(attachments,0,1,1,1,0,-710,0,-571);attachments.pivot=new Vector2(.5f,1);view.inventoryAttachments=attachments;
            var header=Label(attachments,"ATTACHMENTS                       1 / 2",0,-38,600,30,16);header.gameObject.name="Attachment heading";header.color=style.gold;
            Equip(attachments,"attachment-0","",0,.19f,0,1);Equip(attachments,"attachment-1","",.205f,.395f,0,1);
        }
        private static void Cargo(RectTransform host)
        {
            var layout=Rect("CargoLayout",host);Stretch(layout);
            var summary=Rect("Cargo summary",layout);Anchor(summary,0,1,.195f,1,0,-380,0,0);
            view.cargoSummary=Label(summary,"COMPANION\n\n♜\n\nThe Legger\n\n0 cargo spaces used",0,5,340,365,18);
            var grid=Rect("Cargo Grid",layout);Anchor(grid,.215f,0,.694f,1,0,0,0,-38);Header(layout,"CARGO",.215f,.694f);view.cargoGrid=grid;
            var toolbar=Rect("Storage toolbar",layout);Anchor(toolbar,.215f,1,.694f,1,0,-137,0,-89);view.storageToolbar=toolbar;
            var search=Panel("Search",toolbar,style.background);Anchor(search,0,0,.57f,1,0,0,0,0);var field=search.gameObject.AddComponent<InputField>();field.textComponent=Label(search,"",10,8,450,32,16);Stretch(field.textComponent.rectTransform,10);var placeholder=Label(search,"Search this inventory…",10,8,450,32,16);Stretch(placeholder.rectTransform,10);placeholder.color=style.muted;field.placeholder=placeholder;field.targetGraphic=search.GetComponent<CampMenuPanelGraphic>();view.storageSearch=field;
            var filter=Control(toolbar,"storage-filter","All categories",0,0,150,42);Anchor(filter.GetComponent<RectTransform>(),.585f,0,.785f,1,0,0,0,0);
            var sort=Control(toolbar,"storage-sort","Name A–Z",0,0,150,42);Anchor(sort.GetComponent<RectTransform>(),.8f,0,1,1,0,0,0,0);
            var scroll=grid.gameObject.AddComponent<ScrollRect>();var viewport=Rect("Viewport",grid);Stretch(viewport);viewport.gameObject.AddComponent<RectMask2D>();
            var rows=Rect("Slots",viewport);Anchor(rows,0,1,1,1,0,-1600,0,0);rows.pivot=new Vector2(.5f,1);
            var flow=rows.gameObject.AddComponent<GridLayoutGroup>();flow.cellSize=new Vector2(125,125);flow.spacing=new Vector2(12,12);flow.constraint=GridLayoutGroup.Constraint.FixedColumnCount;flow.constraintCount=4;
            scroll.viewport=viewport;scroll.content=rows;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<48;i++){var card=Card(rows,"cargo:"+i,"+",null,125,145);card.cornerLabel=Label(card.transform,(i+1).ToString("00"),10,5,100,24,14);}
        }
        private static void Inspector(RectTransform host)
        {
            var right=Rect("ItemInspector",host);Anchor(right,.74f,0,1,1,0,0,0,0);Header(right,"INSPECT",0,1);
            view.inspectorTitle=Label(right,"INSPECT",22,55,440,60,22);Anchor(view.inspectorTitle.rectTransform,0,1,1,1,22,-115,-12,-55);
            var art=Rect("Artwork",right);Anchor(art,0,1,1,1,25,-345,-25,-125);view.inspectorArt=art.gameObject.AddComponent<Image>();view.inspectorArt.preserveAspect=true;view.inspectorArt.raycastTarget=false;
            view.inspectorCount=Label(right,"",22,365,400,32,16);Anchor(view.inspectorCount.rectTransform,0,1,1,1,22,-397,-12,-365);
            var bodyViewport=Rect("Description viewport",right);Anchor(bodyViewport,0,1,1,1,22,-655,-12,-415);bodyViewport.gameObject.AddComponent<RectMask2D>();view.inspectorBodyViewport=bodyViewport;
            view.inspectorBody=Label(bodyViewport,"Select an item.",0,0,400,220,18);Anchor(view.inspectorBody.rectTransform,0,1,1,1,0,-220,0,0);view.inspectorBody.rectTransform.pivot=new Vector2(.5f,1);
            view.inspectorBody.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var bodyScroll=bodyViewport.gameObject.AddComponent<ScrollRect>();bodyScroll.viewport=bodyViewport;bodyScroll.content=view.inspectorBody.rectTransform;bodyScroll.horizontal=false;
            var track=Control(right,"track","Track item",0,0,210,45);Anchor(track.GetComponent<RectTransform>(),0,0,1,0,22,60,-22,105);
            var transfer=Control(right,"transfer","Transfer item",0,0,210,45);Anchor(transfer.GetComponent<RectTransform>(),0,0,1,0,22,5,-22,50);
        }
        private static void Picker(RectTransform host)
        {
            var picker=Panel("EquipmentPicker",host,style.background);Anchor(picker,.74f,0,1,1,0,0,0,0);Header(picker,"SELECT EQUIPMENT",0,1);
            var list=Rect("Candidates",picker);Anchor(list,0,0,1,1,15,50,-15,-40);
            var flow=list.gameObject.AddComponent<GridLayoutGroup>();flow.cellSize=new Vector2(135,130);flow.spacing=new Vector2(10,10);flow.constraint=GridLayoutGroup.Constraint.FixedColumnCount;flow.constraintCount=3;
            for(int i=0;i<24;i++)Card(list,"candidate:"+i,"",null,135,130);
            var back=Control(picker,"back","Back",0,0,160,38);Anchor(back.GetComponent<RectTransform>(),0,0,1,0,15,5,-15,43);
        }
        private static void Tree(RectTransform host)
        {
            var page=Page(host,"tree");
            var left=Panel("Item browser",page,style.surface);Anchor(left,0,0,.246f,1,0,0,0,0);Header(left,"ITEMS",0,1);
            var title=Control(left,"node:"+definition.rootId,"Iron-headed multi-tool",12,45,290,110);title.title.fontSize=18;Anchor(title.GetComponent<RectTransform>(),0,1,1,1,12,-155,-12,-45);
            var metadata=Label(left,"T1  /  Common\n\nGather materials",16,170,280,110,16);Anchor(metadata.rectTransform,0,1,1,1,16,-300,-16,-170);
            var middle=Panel("Tree viewport",page,new Color(24/255f,27/255f,26/255f,.92f));Anchor(middle,.268f,0,.77f,1,0,0,0,0);
            var scroll=middle.gameObject.AddComponent<ScrollRect>();middle.gameObject.AddComponent<RectMask2D>();
            var tree=Rect("Recipe canvas",middle);tree.anchorMin=tree.anchorMax=tree.pivot=new Vector2(0,1);tree.sizeDelta=new Vector2(definition.treeWidth,definition.treeHeight);tree.localScale=Vector3.one*.6f;
            var wires=tree.gameObject.AddComponent<CampMenuTreeGraphic>();wires.definition=definition;wires.color=style.muted;wires.raycastTarget=false;
            foreach(var item in definition.items.Where(i=>i.visibleInTree))
            {
                var card=Card(tree,"node:"+item.id+":tree",item.name,item.art,145,238); // Unique instance; route still uses the stable item ID.
                card.key="tree-node:"+item.id;card.GetComponent<RectTransform>().anchorMin=card.GetComponent<RectTransform>().anchorMax=new Vector2(0,1);
                card.GetComponent<RectTransform>().pivot=new Vector2(0,1);card.GetComponent<RectTransform>().anchoredPosition=new Vector2(item.treeX,-item.treeY);card.title.fontSize=14;
            }
            scroll.viewport=middle;scroll.content=tree;scroll.movementType=ScrollRect.MovementType.Clamped;
            var zoomIn=Control(page,"tree-zoom-in","+",0,0,48,40);Anchor(zoomIn.GetComponent<RectTransform>(),.68f,0,.68f,0,0,12,48,52);
            var zoomOut=Control(page,"tree-zoom-out","−",0,0,48,40);Anchor(zoomOut.GetComponent<RectTransform>(),.68f,0,.68f,0,-60,12,-12,52);
        }
        private static void Simple(RectTransform host,string key,string heading,string body)
        {var page=Page(host,key);var panel=Panel(heading,page,style.surface);Anchor(panel,0,0,1,1,0,0,0,0);Label(panel,heading,22,15,800,36,22);Label(panel,body,22,80,900,180,18);}
        private static void ResourcesPage(RectTransform host)
        {
            var page=Page(host,"start");var inner=Rect("Resources",page);Anchor(inner,.037f,0,.963f,1,0,0,0,0);
            Label(inner,"ARC & DUST / PLAYER RESOURCES",0,-25,900,25,14).color=style.accent;Label(inner,"Start menu",0,0,900,38,28);Label(inner,"Your guide to the wasteland, your bearings, and your settings.",0,35,1100,40,18);
            var labels=new[]{"Wasteland Reference Guide","Map","Settings"};var commands=new[]{"guide","map","settings"};
            var bodies=new[]{"Survival, equipment, workshops, threats, exploration, and the item almanac.","Review your bearings and place a waypoint on the schematic map.","Adjust readability, interface motion, and your shortcuts."};
            for(int i=0;i<3;i++)
            {var card=Control(inner,commands[i],labels[i],0,0,540,180);Anchor(card.GetComponent<RectTransform>(),i*.337f,1,i*.337f+.325f,1,0,-264,0,-84);Anchor(card.title.rectTransform,0,1,1,1,26,-55,-26,-25);card.title.fontSize=16;card.title.color=style.accent;card.title.alignment=TextAnchor.UpperLeft;var body=Label(card.transform,bodies[i],26,70,480,80,18);Anchor(body.rectTransform,0,1,1,1,26,-130,-26,-70);}
            Control(inner,"back:resources","← Return to previous screen",0,292,260,44);
            var quit=Control(inner,"quit","Save and quit",0,0,260,44);Anchor(quit.GetComponent<RectTransform>(),1,1,1,1,-260,-336,0,-292);
            Control(inner,"recover-pending","Finish validated pending save",0,365,440,52);Control(inner,"recover-backup","Restore backup · retain originals",0,430,440,52);
        }
        private static void Workstations(RectTransform host)
        {
            var page=Page(host,"workstations");var list=Rect("Station list",page);Anchor(list,0,0,.307f,1,7,0,0,0);Header(list,"WORKSHOPS",0,1);
            var scroll=list.gameObject.AddComponent<ScrollRect>();list.gameObject.AddComponent<RectMask2D>();var rows=Rect("Stations",list);Anchor(rows,0,1,1,1,0,-(definition.stations.Length*76+38),0,-38);rows.pivot=new Vector2(.5f,1);
            for(int i=0;i<definition.stations.Length;i++){var station=definition.stations[i];var control=Control(rows,"station:"+station.id,station.name,0,i*76,550,72);Anchor(control.GetComponent<RectTransform>(),0,1,1,1,0,-(i*76+72),0,-i*76);control.title.alignment=TextAnchor.UpperLeft;Stretch(control.title.rectTransform,18);}
            scroll.viewport=list;scroll.content=rows;scroll.horizontal=false;
            var detail=Panel("Station detail",page,style.surface);Anchor(detail,.328f,1,1,1,0,-337,0,0);view.stationTitle=Label(detail,"Workshops",20,32,1000,44,24);view.stationBody=Label(detail,"Workshops are not available yet.",20,100,1000,130,18);Anchor(view.stationBody.rectTransform,0,1,1,1,20,-250,-20,-100);
        }
        private static RectTransform Page(RectTransform host,string key){var r=Rect("Page "+key,host);Stretch(r);pages.Add(new CampMenuPageBinding{key=key,root=r.gameObject});return r;}
        private static void Tabs(RectTransform safe,string name,string[] keys,string[] labels)
        {var r=Rect(name,safe);Anchor(r,0,1,1,1,52,-114,-52,-73);PromptIcon(r,"lt",0,0,32,40);float x=65;for(int i=0;i<keys.Length;i++){float w=labels[i].Length*8.1f+30;Control(r,"page:"+keys[i],labels[i],x,0,w,38);x+=w+12;}PromptIcon(r,"rt",x+10,0,32,40);}
        private static void PromptIcon(Transform parent,string key,float x,float y,float width,float height)
        {var r=Rect("Prompt "+key,parent);Place(r,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.sprite=style.FindGlyph("prompt-"+key);image.preserveAspect=true;image.raycastTarget=false;}
        private static void FooterPrompt(Transform parent,string key,string label,string glyph)
        {
            var control=Control(parent,key,label,0,0,140,45);control.prompt=true;control.tab=false;control.Button.interactable=!key.StartsWith("hint:");control.title.fontSize=16;control.title.alignment=TextAnchor.MiddleLeft;
            bool pair=glyph is "bumpers" or "triggers" or "navigate";float inset=pair?78:42;Anchor(control.title.rectTransform,0,0,1,1,inset,3,-3,-3);
            if(glyph=="north")CampExpeditionMenuView.ConfigureInventoryPrompt(control,style);
            else if(glyph=="navigate"){var nav=Label(control.transform,"+ / LS",0,9,72,30,16);nav.name="Navigation glyph";}
            else if(glyph is "rs" or "ls"){var ring=Panel("Stick",control.transform,Color.clear);ring.GetComponent<CampMenuPanelGraphic>().corner=14;Place(ring,0,7,32,32);var rs=Label(ring,glyph.ToUpperInvariant(),0,0,32,32,14);rs.alignment=TextAnchor.MiddleCenter;}
            else if(pair){PromptIcon(control.transform,glyph=="bumpers"?"lb":"lt",0,7,32,32);PromptIcon(control.transform,glyph=="bumpers"?"rb":"rt",36,7,32,32);}
            else PromptIcon(control.transform,glyph,0,7,32,32);
            var layout=control.gameObject.AddComponent<LayoutElement>();layout.preferredWidth=140;layout.preferredHeight=45;
        }
        private static void Equip(RectTransform parent,string slot,string label,float x0,float x1,float y0,float y1)
        {var c=Card(parent,"equip:"+slot,"Empty",style.FindGlyph(slot),130,240);Anchor(c.GetComponent<RectTransform>(),x0,1-y1,x1,1-y0,0,0,0,0);c.cornerLabel=Label(c.transform,label,10,5,200,25,14);Anchor(c.cornerLabel.rectTransform,0,1,1,1,10,-30,-6,-5);}
        private static CampMenuControl Card(Transform parent,string key,string label,Sprite sprite,float width,float height)
        {
            var c=Control(parent,key,label,0,0,width,height);
            var surface=Rect("Iron surface",c.transform);Stretch(surface,2);surface.SetAsFirstSibling();var iron=surface.gameObject.AddComponent<Image>();iron.sprite=style.iron;iron.color=new Color(1,1,1,.06f);iron.raycastTarget=false;
            var art=Rect("Artwork",c.transform);Anchor(art,0,.27f,1,.82f,10,0,-10,0);c.art=art.gameObject.AddComponent<Image>();c.art.sprite=sprite;c.art.enabled=sprite!=null;c.art.preserveAspect=true;c.art.raycastTarget=false;
            Anchor(c.title.rectTransform,0,0,1,.28f,8,4,-8,0);c.title.alignment=TextAnchor.UpperCenter;c.title.fontSize=16;
            c.subtitle=Label(c.transform,"",0,0,100,20,14);Anchor(c.subtitle.rectTransform,0,0,1,0,8,4,-8,24);c.subtitle.alignment=TextAnchor.LowerRight;
            return c;
        }
        private static CampMenuControl Control(Transform parent,string key,string label,float x,float y,float width,float height)
        {var r=Panel(key,parent,style.surface);Place(r,x,y,width,height);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<CampMenuPanelGraphic>();var c=r.gameObject.AddComponent<CampMenuControl>();c.tab=key.StartsWith("primary:")||key.StartsWith("page:")||key.StartsWith("carrier:");c.Configure(style,key,style.surface);c.title=Label(r,label,8,2,width-16,height-4,18);Anchor(c.title.rectTransform,0,0,1,1,8,2,-8,-2);c.title.alignment=TextAnchor.MiddleCenter;return c;}
        private static Text Label(Transform parent,string text,float x,float y,float width,float height,int size)
        {var r=Rect("Text",parent);Place(r,x,y,width,height);var t=r.gameObject.AddComponent<Text>();t.font=style.font;t.fontSize=size;t.text=text;t.color=style.ink;t.raycastTarget=false;t.alignment=TextAnchor.UpperLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private static void Header(RectTransform parent,string title,float x0,float x1)
        {var r=Rect(title,parent);Anchor(r,x0,1,x1,1,0,-21,0,0);Fill(r,style.background);var t=Label(r,title,0,0,300,21,16);Stretch(t.rectTransform);t.color=style.ink;}
        private static RectTransform Panel(string name,Transform parent,Color color)
        {
            var r=Rect(name,parent);r.gameObject.AddComponent<CampMenuPanelGraphic>().SetFrame(color,style.line);
            if(color.a>.5f&&style.grain!=null){var grain=Rect("Surface grain",r);Stretch(grain,2);var image=grain.gameObject.AddComponent<Image>();image.sprite=style.grain;image.type=Image.Type.Tiled;image.color=new Color(1,1,1,.06f);image.raycastTarget=false;}
            return r;
        }
        private static void Fill(RectTransform r,Color color){var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;}
        private static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        private static void Stretch(RectTransform r,float inset=0){Anchor(r,0,0,1,1,inset,inset,-inset,-inset);}
        private static void Anchor(RectTransform r,float x0,float y0,float x1,float y1,float l,float b,float right,float top){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=new Vector2(l,b);r.offsetMax=new Vector2(right,top);}
        private static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+name+".png");
        private static T LoadOrCreate<T>(string path)where T:ScriptableObject{var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value!=null)return value;Directory.CreateDirectory(Path.GetDirectoryName(path));value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value;}
        private static void ImportArt()
        {
            foreach(var file in Directory.GetFiles(ArtPath,"*.png"))
            {var p=file.Replace('\\','/');var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.maxTextureSize=2048;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();}
        }
        private static void QualifyRuntimeItems()
        {
            var items=new List<TopDown3DItemDefinition>();
            foreach(var item in definition.items)
            {
                var itemPath=SettingsPath+"Items/"+item.id.Replace('.','_')+".asset";
                var asset=LoadOrCreate<TopDown3DItemDefinition>(itemPath);
                bool resource=item.visibleInTree && item.inputs.Length==0;
                var slot=item.equipmentAddress switch
                {
                    "tool"=>TopDown3DEquipmentSlot.DiggingTool,"frame"=>TopDown3DEquipmentSlot.BodyFrame,
                    "attachment"=>TopDown3DEquipmentSlot.AlgaeCanister,_=>TopDown3DEquipmentSlot.None
                };
                // Private trial packing/quantities, never written back to the source catalog.
                asset.Configure(item.id,item.name,item.purpose,resource?"Resource":item.category,item.art,slot==TopDown3DEquipmentSlot.None?20:1,
                    resource?1:.75f,resource?TopDown3DCarryPreference.BigArmOnly:TopDown3DCarryPreference.Either);
                asset.ConfigureLoadout(slot);EditorUtility.SetDirty(asset);item.runtimeDefinition=asset;items.Add(asset);
            }
            definition.itemCatalog=LoadOrCreate<TopDown3DItemCatalog>(SettingsPath+"ExpeditionCatalog.asset");
            definition.itemCatalog.Configure(items);EditorUtility.SetDirty(definition.itemCatalog);
        }
    }
}
