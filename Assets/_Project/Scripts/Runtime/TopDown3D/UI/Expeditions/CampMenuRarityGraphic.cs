using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuRarityGraphic : MaskableGraphic
    {
        internal static bool TryColor(string rarity,out Color value)
        {
            string hex=rarity switch{"Junk"=>"#666a70e6","Common"=>"#f3f1e9","Uncommon"=>"#65c879","Rare"=>"#559fff","Epic"=>"#db63cf","Legendary"=>"#f5a532",_=>null};
            value=Color.clear;return hex!=null&&ColorUtility.TryParseHtmlString(hex,out value);
        }
        internal void SetRarity(string rarity){bool known=TryColor(rarity,out var tint);color=tint;gameObject.SetActive(known);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddTriangle(0,1,2);}
    }
}
