using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandTerrainWorkspaceTests
    {
        [Test]
        public void NeighbourhoodIncludesExactlyNineCellsWithDiagonals()
        {
            var centre = new Vector2Int(-9, 2);
            int visible = 0;
            for (int x = -2; x <= 2; x++) for (int z = -2; z <= 2; z++)
                if (GreaterWastelandTerrainWorkspace.InNeighbourhood(centre + new Vector2Int(x, z), centre)) visible++;
            Assert.That(visible, Is.EqualTo(9));
            Assert.That(GreaterWastelandTerrainWorkspace.InNeighbourhood(centre + Vector2Int.one, centre), Is.True);
        }

        [Test]
        public void CrossingSectorBoundaryRecentresIncludingNegativeCoordinates()
        {
            var before = GreaterWastelandTerrainWorkspace.WorldSectorCell(new Vector3(-.01f, 999, 1023.99f));
            var after = GreaterWastelandTerrainWorkspace.WorldSectorCell(new Vector3(0, -999, 1024));
            Assert.That(before, Is.EqualTo(new Vector2Int(-1, 0)));
            Assert.That(after, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(GreaterWastelandTerrainWorkspace.InNeighbourhood(new Vector2Int(-2, -1), before), Is.True);
            Assert.That(GreaterWastelandTerrainWorkspace.InNeighbourhood(new Vector2Int(-2, -1), after), Is.False);
            Assert.That(GreaterWastelandTerrainWorkspace.InNeighbourhood(new Vector2Int(1, 2), after), Is.True);
        }

        [Test]
        public void SectorCellsUseSectionOriginAcrossNegativeWorldCoordinates()
        {
            Vector3 origin = new(-22528, -100, -2048);
            Assert.That(GreaterWastelandTerrainWorkspace.SectorCell(origin + new Vector3(768, 2000, 768), origin), Is.EqualTo(Vector2Int.zero));
            Assert.That(GreaterWastelandTerrainWorkspace.SectorCell(origin + new Vector3(1024, 0, 1024), origin), Is.EqualTo(Vector2Int.one));
        }

        [Test]
        public void OrganizationPreservesTerrainStateAndIsIdempotent()
        {
            var owner = new GameObject("Workspace organization test");
            var connectivity = owner.AddComponent<BadwaterTerrainConnectivity>();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            try
            {
                for (int z = 0; z < 4; z++) for (int x = 0; x < 8; x++)
                {
                    GameObject go = Terrain.CreateTerrainGameObject(data);
                    go.name = $"Tile_{z}_{x}";
                    go.transform.SetParent(owner.transform, false);
                    go.transform.position = new Vector3(-2048 + x * 256, -85, -2048 + z * 256);
                    go.AddComponent<TopDown3DGroundSurface>();
                }
                var before = owner.GetComponentsInChildren<Terrain>().ToDictionary(t => t, t => t.transform.position);
                GreaterWastelandTerrainWorkspace.Organize(owner.transform);
                GreaterWastelandTerrainWorkspace.Organize(owner.transform);
                connectivity.Reconnect();
                Assert.That(owner.transform.childCount, Is.EqualTo(1));
                Transform core = owner.transform.Find("badwater_core");
                Assert.That(core.childCount, Is.EqualTo(2));
                foreach (Transform sector in core) Assert.That(sector.childCount, Is.EqualTo(16));
                foreach (var pair in before)
                {
                    Assert.That(pair.Key.transform.position, Is.EqualTo(pair.Value));
                    Assert.That(pair.Key.terrainData, Is.SameAs(data));
                    Assert.That(pair.Key.enabled && pair.Key.gameObject.activeInHierarchy, Is.True);
                    Assert.That(pair.Key.GetComponent<TerrainCollider>().terrainData, Is.SameAs(data));
                    Assert.That(pair.Key.GetComponent<TopDown3DGroundSurface>(), Is.Not.Null);
                    Assert.That(pair.Key.GetComponentInParent<BadwaterTerrainConnectivity>(), Is.SameAs(connectivity));
                }
                Terrain west = before.Keys.Single(t => t.name == "Tile_0_3");
                Terrain east = before.Keys.Single(t => t.name == "Tile_0_4");
                Assert.That(west.transform.parent, Is.Not.SameAs(east.transform.parent));
                Assert.That(west.rightNeighbor, Is.SameAs(east));
                Assert.That(east.leftNeighbor, Is.SameAs(west));
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(data); }
        }

        [Test]
        public void OrganizationPreservesNamedGeographicSectionAndOwner()
        {
            var owner = new GameObject("Workspace section test");
            var section = new GameObject("northwest");
            section.transform.SetParent(owner.transform, false);
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            try
            {
                var tile = Terrain.CreateTerrainGameObject(data);
                tile.transform.SetParent(section.transform, false);
                GreaterWastelandTerrainWorkspace.Organize(owner.transform);
                Assert.That(section.transform.parent, Is.SameAs(owner.transform));
                Assert.That(section.name, Is.EqualTo("northwest"));
                Assert.That(tile.transform.parent.parent, Is.SameAs(section.transform));
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(data); }
        }
    }
}
