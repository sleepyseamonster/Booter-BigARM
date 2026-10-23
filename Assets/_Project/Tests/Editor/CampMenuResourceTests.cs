using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuResourceTests
    {
        private static CampMenuCatalog Sample(int count=80)=>new(new CampMenuCatalogData{catalogRevision="test",items=Enumerable.Range(0,count).Select(i=>new CampMenuCatalogItem{id="private.id."+i,name="Secret name "+i,category=i%2==0?"Tools":"Resources",purpose="Hidden purpose "+i,search="Alias "+i}).ToArray()});
        [Test] public void UndiscoveredRecordsHaveOnlyPublicAddressesAndNeverMatchSearch()
        {
            var catalog=Sample();var index=CampMenuResources.Almanac(catalog,new[]{"private.id.1","missing.saved.id"});
            Assert.That(index.known,Is.EqualTo(1));Assert.That(index.total,Is.EqualTo(80));Assert.That(index.entries.Where(e=>!e.Known).All(e=>e.item==null&&e.category!=null&&e.ordinal>0),Is.True);
            foreach(var query in new[]{"Secret name 2","Hidden purpose 2","Alias 2","Tools","private.id.1"})Assert.That(CampMenuResources.Almanac(catalog,new[]{"private.id.1"},query:query).matches,Is.Zero,query);
            Assert.That(CampMenuResources.Almanac(catalog,new[]{"private.id.1"},query:"Alias 1").entries.Single().slot,Is.EqualTo(1));
        }
        [Test] public void FiltersPaginationAndLocateUsePublicSlotsAndClampStalePages()
        {
            var catalog=Sample();var known=new[]{"private.id.2","private.id.7"};var first=CampMenuResources.Almanac(catalog,known);
            Assert.That(first.entries.Take(2).All(e=>e.Known),Is.True);Assert.That(first.pages,Is.EqualTo(3));
            var last=CampMenuResources.Almanac(catalog,known,page:999);Assert.That(last.page,Is.EqualTo(2));Assert.That(last.entries.Length,Is.EqualTo(8));
            var filtered=CampMenuResources.Almanac(catalog,known,category:"Tools",status:"undiscovered",catalogOrder:true,locateSlot:78);Assert.That(filtered.matches,Is.EqualTo(39));Assert.That(filtered.entries.Any(e=>e.slot==78),Is.True);Assert.That(filtered.entries.All(e=>!e.Known&&e.category=="Tools"),Is.True);
            Assert.That(CampMenuResources.Almanac(catalog,known,query:"impossible",page:999).page,Is.Zero);Assert.That(CampMenuResources.Almanac(catalog,known,pageSize:1000).entries.Length,Is.EqualTo(80));
            Assert.That(catalog.Find("private.id.2").name,Is.EqualTo("Secret name 2"));Assert.That(known.Length,Is.EqualTo(2));
        }
        [Test] public void GuideSearchUsesAllTermsAndNativeCopyWithoutChangingTheBrowserExport()
        {
            var catalog=CampMenuCatalog.Load();string original=catalog.GuideTopics.Single(t=>t.id=="controls").sections[1].text;
            Assert.That(CampMenuResources.FindTopics(catalog,"  PHAGIA  algae ").Single().id,Is.EqualTo("survival"));Assert.That(CampMenuResources.FindTopics(catalog,"nothing_matches_123"),Is.Empty);
            Assert.That(CampMenuResources.FindTopics(catalog,"north face").Single().id,Is.EqualTo("controls"));Assert.That(CampMenuResources.FindTopics(catalog,"F1"),Is.Empty);Assert.That(catalog.GuideTopics.Single(t=>t.id=="controls").sections[1].text,Is.EqualTo(original));
        }
        [TestCase(1,-1,1)][TestCase(1,1,1.25f)][TestCase(2,1,2)][TestCase(2,-1,1.75f)]
        public void SchematicZoomIsBounded(float current,int direction,float expected)=>Assert.That(CampMenuResources.MapZoom(current,direction),Is.EqualTo(expected));
        [Test] public void AlmanacFactsComeFromTheDisplayCatalogWithoutQualifyingRuntimeRecipes()
        {
            var catalog=CampMenuCatalog.Load();Assert.That(catalog.Items.Count(i=>!string.IsNullOrEmpty(i.mass)),Is.EqualTo(16));Assert.That(catalog.Items.Count(i=>!string.IsNullOrEmpty(i.type)),Is.EqualTo(913));Assert.That(catalog.Items.Count(i=>!string.IsNullOrEmpty(i.stackLimit)),Is.EqualTo(16));Assert.That(catalog.Items.Any(i=>string.IsNullOrEmpty(i.mass)),Is.True,"Missing authored facts must stay missing.");
        }
    }
}
