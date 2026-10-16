using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundGarmentFloorTests
    {
        [Test] public void ProjectionKeepsTheSkinnedVertexOnASlopedPlaneWithoutMovingBones()
        {
            var chest=Matrix4x4.TRS(new Vector3(.2f,-.6f,.1f),Quaternion.Euler(50,0,35),Vector3.one*.9f);
            var hips=Matrix4x4.TRS(new Vector3(-.1f,-.2f,.3f),Quaternion.Euler(10,20,-15),Vector3.one*.9f);
            var matrix=AnimationPlaygroundGarmentFloor.Blend(chest,hips,.4f);var plane=new Plane(new Vector3(0,1,.2f).normalized,Vector3.zero);
            Assert.That(AnimationPlaygroundGarmentFloor.TryProject(Vector3.zero,matrix,plane,.01f,.7f,out var point,out var displacement),Is.True);
            Assert.That(displacement,Is.GreaterThan(0));Assert.That(plane.GetDistanceToPoint(matrix.MultiplyPoint3x4(point)),Is.EqualTo(.01f).Within(.00001f));
            Assert.That(chest.GetColumn(3).y,Is.EqualTo(-.6f));
        }
        [Test] public void ExcessiveDisplacementAndSingularSkinMatricesAreRejected()
        {
            Assert.That(AnimationPlaygroundGarmentFloor.TryProject(Vector3.down,Matrix4x4.identity,new Plane(Vector3.up,0),0,.6f,out var point,out _),Is.False);
            Assert.That(point,Is.EqualTo(Vector3.down));
            Assert.That(AnimationPlaygroundGarmentFloor.TryProject(Vector3.zero,Matrix4x4.zero,new Plane(Vector3.up,0),0,.6f,out _,out _),Is.False);
        }
        [TestCase(SkinQuality.Bone1)] [TestCase(SkinQuality.Bone2)]
        public void NativeHeightProjectionKeepsBonesAndTheOriginalDisplacementCap(SkinQuality quality)
        {
            var actor=new GameObject("Native garment fixture");var chest=new GameObject("Chest");var hips=new GameObject("Hips");
            chest.transform.SetParent(actor.transform,false);hips.transform.SetParent(actor.transform,false);
            chest.transform.localPosition=Vector3.down*.2f;hips.transform.localPosition=Vector3.down*.4f;
            var mesh=new Mesh();var source=new[]{Vector3.up*.5f,new Vector3(.1f,.5f,0),new Vector3(0,.5f,.1f)};
            mesh.vertices=source;mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.identity};
            var weight=new BoneWeight{boneIndex0=1,weight0=.8f,boneIndex1=0,weight1=.2f};mesh.boneWeights=new[]{weight,weight,weight};
            var skin=actor.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=new[]{chest.transform,hips.transform};skin.quality=quality;
            var floor=new AnimationPlaygroundGarmentFloor(skin);
            try
            {
                float Height(Vector3 point)=>.2f+point.x*.5f;
                floor.ApplyTerrain(new Plane(Vector3.up,0),.001f,.35f,p=>Height(p));
                Assert.That(floor.ExceededLimit,Is.False);Assert.That(floor.MaximumDisplacement,Is.InRange(.05f,.2f));
                var matrix=quality==SkinQuality.Bone1 ? hips.transform.localToWorldMatrix
                    : AnimationPlaygroundGarmentFloor.Blend(chest.transform.localToWorldMatrix,hips.transform.localToWorldMatrix,.2f);
                foreach(var vertex in mesh.vertices){var world=matrix.MultiplyPoint3x4(vertex);Assert.That(world.y,Is.EqualTo(Height(world)+.001f).Within(.00001f));}
                Assert.That(chest.transform.localPosition.y,Is.EqualTo(-.2f));Assert.That(hips.transform.localPosition.y,Is.EqualTo(-.4f));
                floor.ApplyTerrain(new Plane(Vector3.up,0),.001f,.35f,p=>1);
                Assert.That(floor.ExceededLimit,Is.True);CollectionAssert.AreEqual(source,mesh.vertices);
                floor.ApplyTerrain(new Plane(Vector3.up,0),.001f,.35f,p=>float.NaN);
                Assert.That(floor.ExceededLimit,Is.True);CollectionAssert.AreEqual(source,mesh.vertices);
                floor.Restore();CollectionAssert.AreEqual(source,mesh.vertices);
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(actor);}
        }
        [Test] public void RuntimeCopyRespectsSkinQualityAndRestoresItsOriginalGeometry()
        {
            var actor=new GameObject("Garment constraint fixture");var a=new GameObject("Chest");var b=new GameObject("Hips");
            a.transform.SetParent(actor.transform,false);b.transform.SetParent(actor.transform,false);a.transform.localPosition=Vector3.down*.2f;b.transform.localPosition=Vector3.down*.4f;
            var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right*.1f,Vector3.forward*.1f};
            mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.identity};
            var weight=new BoneWeight{boneIndex0=1,weight0=.8f,boneIndex1=0,weight1=.2f};mesh.boneWeights=new[]{weight,weight,weight};
            var skin=actor.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=new[]{a.transform,b.transform};skin.quality=SkinQuality.Bone1;
            var constraint=new AnimationPlaygroundGarmentFloor(skin);
            try
            {
                constraint.Apply(new Plane(Vector3.up,0),.01f,.6f);
                Assert.That(constraint.ExceededLimit,Is.False);Assert.That(mesh.vertices[0].y,Is.EqualTo(.41f).Within(.00001f));
                Assert.That(a.transform.localPosition.y,Is.EqualTo(-.2f));Assert.That(b.transform.localPosition.y,Is.EqualTo(-.4f));
                skin.quality=SkinQuality.Bone2;constraint.Apply(new Plane(Vector3.up,0),.01f,.6f);
                Assert.That(mesh.vertices[0].y,Is.EqualTo(.37f).Within(.00001f));constraint.Restore();Assert.That(mesh.vertices[0],Is.EqualTo(Vector3.zero));
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(actor);}
        }
    }
}
