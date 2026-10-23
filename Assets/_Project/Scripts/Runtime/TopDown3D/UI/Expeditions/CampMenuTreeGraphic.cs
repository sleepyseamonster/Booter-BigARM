using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Browser-projected recipe connections, drawn by the same native Canvas as the cards.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuTreeGraphic : MaskableGraphic
    {
        public CampExpeditionDefinition definition;
        private CampMenuTree tree;
        private string selected;
        private HashSet<string> inputs=new(),outputs=new();
        private readonly List<Vector2> points=new();
        internal static readonly Color InputColor=new(.463f,.914f,.933f),OutputColor=new(.769f,.604f,1);
        internal void SetTree(CampMenuTree value){tree=value;selected=null;SetVerticesDirty();}
        internal void SetSelection(string id,HashSet<string> required,HashSet<string> consumers){selected=id;inputs=required;outputs=consumers;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(tree!=null)
            {
                var lookup=tree.nodes.ToDictionary(node=>node.id,StringComparer.Ordinal);
                foreach(var item in tree.nodes)foreach(var id in item.inputs)
                {
                    var child=lookup[id];Color stroke=color;
                    if(selected!=null)
                    {if(inputs.Contains(id)&&(item.id==selected||inputs.Contains(item.id)))stroke=InputColor;else if(id==selected&&outputs.Contains(item.id))stroke=OutputColor;else continue;stroke.a=color.a;}
                    bool dashed=tree.edges.Any(edge=>edge.source==item.id&&edge.target==id&&edge.kind!="part");
                    Draw(vh,new Vector2(child.x+145,-child.y-119),new Vector2(item.x,-item.y-119),stroke,dashed);
                }
                return;
            }
            if(definition==null)return;
            foreach(var item in definition.items.Where(item=>item.visibleInTree))foreach(var input in item.inputs)
            {var child=definition.Find(input.itemId);if(child?.visibleInTree==true)Draw(vh,new Vector2(child.treeX+145,-child.treeY-119),new Vector2(item.treeX,-item.treeY-119),color,false);}
        }
        private void Draw(VertexHelper vh,Vector2 input,Vector2 consumer,Color stroke,bool dashed)
        {
            points.Clear();points.Add(consumer);float middle=(input.x+consumer.x)*.5f;
            float radius=Mathf.Max(0,Mathf.Min(80,Mathf.Abs(input.y-consumer.y)*.5f,(consumer.x-input.x)*.25f)),direction=Mathf.Sign(input.y-consumer.y);
            if(radius>0)
            {
                points.Add(new Vector2(middle+radius,consumer.y));Quadratic(new Vector2(middle,consumer.y),new Vector2(middle,consumer.y+direction*radius));
                points.Add(new Vector2(middle,input.y-direction*radius));Quadratic(new Vector2(middle,input.y),new Vector2(middle-radius,input.y));
            }
            points.Add(input);float distance=0;
            for(int i=1;i<points.Count;i++)
            {
                var a=points[i-1];var b=points[i];float length=Vector2.Distance(a,b);if(length<.001f)continue;
                if(!dashed)Line(vh,a,b,stroke);
                else for(float at=0;at<length;)
                {float phase=(distance+at)%20,step=Mathf.Min(length-at,(phase<12?12:20)-phase);if(step<.001f)step=Mathf.Min(.001f,length-at);if(phase<12)Line(vh,Vector2.Lerp(a,b,at/length),Vector2.Lerp(a,b,(at+step)/length),stroke);at+=step;}
                distance+=length;
            }
        }
        private void Quadratic(Vector2 control,Vector2 end)
        {var start=points[points.Count-1];for(int i=1;i<=8;i++){float t=i/8f,u=1-t;points.Add(u*u*start+2*u*t*control+t*t*end);}}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color stroke)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*1.6f;int index=vh.currentVertCount;
            vh.AddVert(a-n,stroke,Vector2.zero);vh.AddVert(a+n,stroke,Vector2.zero);vh.AddVert(b+n,stroke,Vector2.zero);vh.AddVert(b-n,stroke,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
    }
}
