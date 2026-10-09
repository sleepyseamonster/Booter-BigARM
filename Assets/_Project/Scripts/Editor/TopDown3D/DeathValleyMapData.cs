using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    [Serializable] public sealed class DeathValleyMapRecord
    {
        public string id, label, legacy_id, geographic_key, quarter, height_source;
        public double[] bounds_m;
        public float spacing_m, source_spacing_m;
        public string Key => !string.IsNullOrEmpty(geographic_key) ? geographic_key : string.IsNullOrEmpty(legacy_id) ? id : legacy_id;
        public string Label => !string.IsNullOrEmpty(label) ? label : !string.IsNullOrEmpty(legacy_id) ? legacy_id : Key;
        public bool Contains(double east, double north) => east >= bounds_m[0] && east <= bounds_m[2] && north >= bounds_m[1] && north <= bounds_m[3];
    }

    [Serializable] public sealed class DeathValleyMapCatalog
    {
        public int schema_version;
        public string working_crs, audited_utc;
        public DeathValleyMapRecord[] regions, unity_tiles, regional_source_tiles, expansion_candidates;
        public DeathValleyMapScene unity_scene;
    }

    [Serializable] public sealed class DeathValleyMapScene { public string guid; }

    [Serializable] public sealed class DeathValleyMapImage
    {
        public string id, file, sha256;
        public double[] bounds_m;
        public int width, height;
        public float spacing_m, blend_band_m;
    }

    [Serializable] public sealed class DeathValleyMapManifest
    {
        public int schema_version, width, height;
        public double[] bounds_m, unity_origin_m;
        public float spacing_m;
        public string working_crs, grid_file, grid_sha256, scene_guid, retrieved_utc, encoding;
        public string height_source;
        public DeathValleyMapImage[] textures;
    }

    /// <summary>Projected metres are authoritative; preview kilometres are temporary display coordinates.</summary>
    public sealed class DeathValleyMapData
    {
        public readonly DeathValleyMapCatalog Catalog;
        public readonly DeathValleyMapManifest Manifest;
        public readonly float[] Heights;
        public readonly string Root;
        public readonly byte[][] ImageryBytes;
        public bool HasImagery => ImageryBytes != null;
        public const int Stride = 4;

        public DeathValleyMapData(string root)
        {
            Root = Path.GetFullPath(root);
            string docs = Path.Combine(Root, "Docs");
            string Find(string name)
            {
                string[] matches = Directory.GetFiles(docs, name, SearchOption.AllDirectories)
                    .Where(p => !p.Replace('\\', '/').ToLowerInvariant().Contains("/archives/")).ToArray();
                if (matches.Length != 1) throw new IOException($"Expected one {name} under Docs; found {matches.Length}. Finish moves or resolve snapshots before loading.");
                return matches[0];
            }
            Catalog = JsonUtility.FromJson<DeathValleyMapCatalog>(File.ReadAllText(Find("coverage_catalog.json")));
            string manifestPath = Find("developer_map.json");
            Manifest = JsonUtility.FromJson<DeathValleyMapManifest>(File.ReadAllText(manifestPath));
            if (Catalog == null || Manifest == null || Catalog.schema_version != 1 || Manifest.schema_version != 1 ||
                Catalog.working_crs != "EPSG:26911" || Manifest.working_crs != "EPSG:26911")
                throw new InvalidDataException("Unsupported developer map schema or CRS.");
            if (Manifest.encoding != "little-endian float32, north-to-south rows, west-to-east columns" ||
                Catalog.unity_scene == null || string.IsNullOrEmpty(Manifest.scene_guid) || Manifest.scene_guid != Catalog.unity_scene.guid)
                throw new InvalidDataException("Grid encoding or scene identity disagrees with the coverage catalog.");
            ValidateBounds(Manifest.bounds_m);
            if (Manifest.width < 2 || Manifest.height < 2 || Manifest.spacing_m <= 0 ||
                Manifest.width > 2000 || Manifest.height > 2000 || (Manifest.width-1)%Stride != 0 || (Manifest.height-1)%Stride != 0 ||
                Math.Abs((Manifest.width-1)*Manifest.spacing_m-(Manifest.bounds_m[2]-Manifest.bounds_m[0])) > .01 ||
                Math.Abs((Manifest.height-1)*Manifest.spacing_m-(Manifest.bounds_m[3]-Manifest.bounds_m[1])) > .01)
                throw new InvalidDataException("Invalid overview sample grid.");
            if (Catalog.regions == null || Catalog.unity_tiles == null || Catalog.regional_source_tiles == null || Catalog.expansion_candidates == null)
                throw new InvalidDataException("Incomplete coverage catalog.");
            var keys = new HashSet<string>();
            foreach (var r in AllRecords)
            {
                ValidateBounds(r.bounds_m);
                if (string.IsNullOrEmpty(r.Key) || !keys.Add(r.Key)) throw new InvalidDataException("Duplicate or missing map record key.");
                if (r.bounds_m[0] < Manifest.bounds_m[0] || r.bounds_m[1] < Manifest.bounds_m[1] || r.bounds_m[2] > Manifest.bounds_m[2] || r.bounds_m[3] > Manifest.bounds_m[3])
                    throw new InvalidDataException("Coverage footprint outside relief grid.");
            }
            GetPlayableBounds(Catalog.unity_tiles);
            var built = Catalog.regions.Single(r => r.id == "badwater");
            if (Manifest.unity_origin_m == null || Manifest.unity_origin_m.Length != 2 ||
                Manifest.unity_origin_m[0] != 522448 || Manifest.unity_origin_m[1] != 4008248 ||
                !built.bounds_m.SequenceEqual(new double[] { 520400, 4006200, 524496, 4010296 }))
                throw new InvalidDataException("Unity origin disagrees with retained terrain bounds.");
            string folder=Path.GetDirectoryName(manifestPath);
            byte[] bytes=ReadVerifiedFile(folder,Manifest.grid_file,Manifest.grid_sha256);
            if (bytes.Length != Manifest.width*Manifest.height*4 || !BitConverter.IsLittleEndian)
                throw new InvalidDataException("Unexpected float grid encoding or byte count.");
            Heights = new float[Manifest.width*Manifest.height];
            Buffer.BlockCopy(bytes, 0, Heights, 0, bytes.Length);
            if (Heights.Any(h => float.IsNaN(h) || float.IsInfinity(h) || h < -1000 || h > 10000))
                throw new InvalidDataException("Overview has missing or implausible samples.");
            if (Manifest.textures != null)
            {
                string[] expected={"expanded_region","corridor","pilot","canyon"};
                if (Manifest.textures.Length!=4) throw new InvalidDataException("Incomplete Blender imagery stack.");
                ImageryBytes=new byte[4][];
                for(int i=0;i<4;i++)
                {
                    var image=Manifest.textures[i];ValidateBounds(image.bounds_m);
                    var region=Catalog.regions.Single(r=>r.id==expected[i]);
                    if(image.id!=expected[i] || !image.bounds_m.SequenceEqual(region.bounds_m) || image.width<1 || image.height<1 ||
                        image.width>8192 || image.height>8192 || image.spacing_m<=0 ||
                        Math.Abs(image.width*image.spacing_m-(image.bounds_m[2]-image.bounds_m[0]))>.01 ||
                        Math.Abs(image.height*image.spacing_m-(image.bounds_m[3]-image.bounds_m[1]))>.01 ||
                        image.blend_band_m!=(i==0?0:i==1?1000:300))
                        throw new InvalidDataException("Blender image identity, bounds, grid or blending differs from its study.");
                    ImageryBytes[i]=ReadVerifiedFile(folder,image.file,image.sha256);
                }
            }
        }

        public static byte[] ReadVerifiedFile(string folder,string name,string expectedHash)
        {
            if(string.IsNullOrEmpty(name) || Path.IsPathRooted(name))throw new InvalidDataException("Expected a relative map source path.");
            string path=Path.GetFullPath(Path.Combine(folder,name));
            string allowed=Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!path.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Map source escapes its descriptor directory.");
            byte[] bytes=File.ReadAllBytes(path);
            using(var hash=SHA256.Create())
                if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=expectedHash)
                    throw new InvalidDataException("Map source hash changed: "+name);
            return bytes;
        }

        public double[] PlayableBounds => GetPlayableBounds(Catalog.unity_tiles);

        public static double[] GetPlayableBounds(DeathValleyMapRecord[] tiles)
        {
            if (tiles == null || (tiles.Length != 256 && tiles.Length != 1024 && tiles.Length != 1536 && tiles.Length != 2048 && tiles.Length != 2560 && tiles.Length != 3072 && tiles.Length != 3328 && tiles.Length != 3584 && tiles.Length != 3840 && tiles.Length != 4096 && tiles.Length != 4352 && tiles.Length != 4608)) throw new InvalidDataException("Incomplete playable tile grid.");
            int rowStep=tiles.Length==3584 ? 1 : tiles.Length==3840 ? 2 : tiles.Length==4096 ? 3 : tiles.Length==4352 ? 4 : tiles.Length==4608 ? 5 : 0;
            var unique = new HashSet<string>();
            foreach (var tile in tiles)
            {
                ValidateBounds(tile.bounds_m);
                if (tiles.Length == 3328 || rowStep>0)
                {
                    double col = (tile.bounds_m[0]-499920)/256, row = (tile.bounds_m[1]-4006200)/256;
                    bool baseline = col >= 0 && col < 96 && row >= 0 && row < 32;
                    bool northCap = col >= 0 && col < 16*(rowStep+1) && row >= 32 && row < 48;
                    if (!baseline && !northCap) throw new InvalidDataException("Tile outside the exact baseline plus northern cap union.");
                }
                if (tile.bounds_m[2]-tile.bounds_m[0] != 256 || tile.bounds_m[3]-tile.bounds_m[1] != 256 ||
                    (tile.bounds_m[0]-520400)%256 != 0 || (tile.bounds_m[1]-4006200)%256 != 0 ||
                    !unique.Add(tile.bounds_m[0].ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"/"+tile.bounds_m[1].ToString("R",System.Globalization.CultureInfo.InvariantCulture)))
                    throw new InvalidDataException("Overlapping or misaligned playable tiles.");
            }
            var bounds = new[] { tiles.Min(t=>t.bounds_m[0]), tiles.Min(t=>t.bounds_m[1]), tiles.Max(t=>t.bounds_m[2]), tiles.Max(t=>t.bounds_m[3]) };
            double[] expected=tiles.Length==256 ? new double[] {520400,4006200,524496,4010296} : tiles.Length==1024 ? new double[] {516304,4006200,524496,4014392} : tiles.Length==1536 ? new double[] {512208,4006200,524496,4014392} : tiles.Length==2048 ? new double[] {508112,4006200,524496,4014392} : tiles.Length==2560 ? new double[] {504016,4006200,524496,4014392} : tiles.Length==3072 ? new double[] {499920,4006200,524496,4014392} : new double[] {499920,4006200,524496,4018488};
            if (!bounds.SequenceEqual(expected)) throw new InvalidDataException("Playable footprint disagrees with accepted sections.");
            return bounds;
        }

        public IEnumerable<DeathValleyMapRecord> AllRecords => Catalog.regions.Concat(Catalog.unity_tiles).Concat(Catalog.regional_source_tiles).Concat(Catalog.expansion_candidates);
        public static void ValidateBounds(double[] b)
        {
            if (b == null || b.Length != 4 || b.Any(v => double.IsNaN(v) || double.IsInfinity(v)) || b[0] >= b[2] || b[1] >= b[3])
                throw new InvalidDataException("Invalid projected bounds.");
        }
        public Vector3 Project(double east, double north, float height, float exaggeration) => new Vector3(
            (float)((east-(Manifest.bounds_m[0]+Manifest.bounds_m[2])/2)/1000), height*exaggeration/1000,
            (float)((north-(Manifest.bounds_m[1]+Manifest.bounds_m[3])/2)/1000));
        public Vector2 UnityPosition(double east, double north) => new Vector2((float)(east-Manifest.unity_origin_m[0]), (float)(north-Manifest.unity_origin_m[1]));
        public double[] Unproject(Vector3 point) => new[] {(Manifest.bounds_m[0]+Manifest.bounds_m[2])/2+point.x*1000d,
            (Manifest.bounds_m[1]+Manifest.bounds_m[3])/2+point.z*1000d};

        // Piecewise planar interpolation matches the displayed triangle surface exactly.
        public float SurfaceHeight(double east, double north)
        {
            float col = Mathf.Clamp((float)((east-Manifest.bounds_m[0])/(Manifest.spacing_m*Stride)), 0, (Manifest.width-1)/Stride);
            float row = Mathf.Clamp((float)((Manifest.bounds_m[3]-north)/(Manifest.spacing_m*Stride)), 0, (Manifest.height-1)/Stride);
            int c = Math.Min((int)col,(Manifest.width-1)/Stride-1), r = Math.Min((int)row,(Manifest.height-1)/Stride-1);
            float u = col-c, v = row-r;
            float a=Heights[r*Stride*Manifest.width+c*Stride], b=Heights[r*Stride*Manifest.width+(c+1)*Stride];
            float d=Heights[(r+1)*Stride*Manifest.width+c*Stride], e=Heights[(r+1)*Stride*Manifest.width+(c+1)*Stride];
            return u+v <= 1 ? a+(b-a)*u+(d-a)*v : e+(d-e)*(1-u)+(b-e)*(1-v);
        }

        // Split at cell edges AND triangle diagonals so straight overlay segments
        // stay above the displayed surface between endpoints, including steep slopes.
        public Vector3[] EdgeVertices(double east0,double north0,double east1,double north1,float exaggeration)
        {
            double size=Manifest.spacing_m*Stride;
            double c0=(east0-Manifest.bounds_m[0])/size,c1=(east1-Manifest.bounds_m[0])/size;
            double r0=(Manifest.bounds_m[3]-north0)/size,r1=(Manifest.bounds_m[3]-north1)/size;
            var cuts=new SortedSet<double>{0,1};
            void Split(double a,double b)
            {
                if(Math.Abs(a-b)<1e-10)return;
                for(int k=(int)Math.Floor(Math.Min(a,b))+1;k<Math.Max(a,b);k++)
                {double t=(k-a)/(b-a);if(t>0 && t<1)cuts.Add(t);}
            }
            Split(c0,c1);Split(r0,r1);Split(c0+r0,c1+r1);
            return cuts.Select(t=>
            {
                double east=east0+(east1-east0)*t,north=north0+(north1-north0)*t;
                return Project(east,north,SurfaceHeight(east,north),exaggeration)+Vector3.up*.025f;
            }).ToArray();
        }

        public Mesh BuildRelief(float exaggeration,bool aerial=false)
        {
            int cols=(Manifest.width-1)/Stride+1, rows=(Manifest.height-1)/Stride+1;
            var vertices=new Vector3[cols*rows]; var colors=new Color[vertices.Length];
            var triangles=new int[(cols-1)*(rows-1)*6];
            for (int r=0;r<rows;r++) for (int c=0;c<cols;c++)
            {
                int i=r*cols+c;
                float height=Heights[r*Stride*Manifest.width+c*Stride];
                vertices[i]=Project(Manifest.bounds_m[0]+c*Stride*Manifest.spacing_m,Manifest.bounds_m[3]-r*Stride*Manifest.spacing_m,height,exaggeration);
                colors[i]=aerial ? Color.white : Color.Lerp(new Color(.32f,.40f,.36f),new Color(.86f,.77f,.58f),Mathf.InverseLerp(-85,3000,height));
            }
            int n=0;
            for (int r=0;r<rows-1;r++) for (int c=0;c<cols-1;c++)
            { int a=r*cols+c; triangles[n++]=a;triangles[n++]=a+1;triangles[n++]=a+cols;
              triangles[n++]=a+1;triangles[n++]=a+cols+1;triangles[n++]=a+cols; }
            var mesh=new Mesh {name="Death Valley developer relief",hideFlags=HideFlags.HideAndDontSave,indexFormat=IndexFormat.UInt32};
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
            Vector3 light=new Vector3(-.6f,1,.4f).normalized;
            Vector3[] normals=mesh.normals;
            for(int i=0;i<colors.Length;i++) colors[i]*=(aerial?.72f:.48f)+(aerial?.28f:.52f)*Mathf.Max(0,Vector3.Dot(normals[i],light));
            var uv=new Vector2[vertices.Length];
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)uv[r*cols+c]=new Vector2(c/(float)(cols-1),1-r/(float)(rows-1));
            mesh.uv=uv;mesh.colors=colors;mesh.RecalculateBounds();return mesh;
        }

        public static bool IntersectTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
        {
            distance=0;Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(ray.direction,e2);
            float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-8f)return false;
            Vector3 s=ray.origin-a;float u=Vector3.Dot(s,p)/det;if(u<0||u>1)return false;
            Vector3 q=Vector3.Cross(s,e1);float v=Vector3.Dot(ray.direction,q)/det;if(v<0||u+v>1)return false;
            distance=Vector3.Dot(e2,q)/det;return distance>=0;
        }
        public static bool Pick(Ray ray, Vector3[] vertices, int[] indices, out Vector3 point)
        {
            float nearest=float.PositiveInfinity;
            for(int i=0;i<indices.Length;i+=3)
                if(IntersectTriangle(ray,vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]],out float d)) nearest=Mathf.Min(nearest,d);
            point=ray.GetPoint(float.IsInfinity(nearest)?0:nearest);return !float.IsInfinity(nearest);
        }
    }
}
