using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuCatalogTests
    {
        private static CampMenuCatalogData Fixture()=>new(){catalogRevision="test",items=new[]{
            new CampMenuCatalogItem{id="a",name="Tool Alpha",category="Tools"},new CampMenuCatalogItem{id="b",name="Tool Beta",category="Tools"},
            new CampMenuCatalogItem{id="ore",name="Iron ore",category="Materials",search="ironstone"}},trees=new[]{
            new CampMenuTree{rootId="a",width=500,height=500,nodes=new[]{new CampMenuTreeNode{id="a",inputs=new[]{"ore"}},new CampMenuTreeNode{id="ore"}}},
            new CampMenuTree{rootId="b",width=500,height=500,nodes=new[]{new CampMenuTreeNode{id="b"},new CampMenuTreeNode{id="ore"}}}}};
        [Test] public void ReverseUsesRespectTreeSpecificConnections()
        {var catalog=new CampMenuCatalog(Fixture());CollectionAssert.AreEqual(new[]{"a"},catalog.ResultsUsing(new[]{"ore"}));Assert.That(catalog.Tree("b").nodes.Single(n=>n.id=="b").inputs,Is.Empty);Assert.That(catalog.ResultsUsing(new[]{"a"}),Is.Empty);}
        [Test] public void SearchMatchesAliasesAndCategoryWithoutRuntimeDefinitions()
        {var catalog=new CampMenuCatalog(Fixture());Assert.That(catalog.Search("ironstone","Materials").Single().id,Is.EqualTo("ore"));Assert.That(catalog.Search("ironstone","Tools"),Is.Empty);Assert.That(catalog.Find("removed"),Is.Null);}
        [Test] public void RelationsUseOnlyThisTreesConnectedInputsAndImmediateConsumers()
        {var catalog=new CampMenuCatalog(Fixture());CollectionAssert.AreEquivalent(new[]{"ore"},catalog.Inputs("a","a"));Assert.That(catalog.Inputs("b","b"),Is.Empty);CollectionAssert.AreEquivalent(new[]{"a"},catalog.Outputs("a","ore"));Assert.That(catalog.Outputs("b","ore"),Is.Empty);Assert.That(catalog.Inputs("a",null),Is.Empty);}
        [Test] public void DuplicateIdentitiesAndUnresolvedDisplayedInputsAreRejected()
        {var data=Fixture();data.items[1].id="a";Assert.Throws<ArgumentException>(()=>new CampMenuCatalog(data));data=Fixture();data.trees[0].nodes[0].inputs=new[]{"missing"};Assert.Throws<ArgumentException>(()=>new CampMenuCatalog(data));}
        [Test] public void FrozenDisplayCatalogLoadsWithoutRequiringTheAuthoringServer()
        {var catalog=CampMenuCatalog.Load();Assert.That(catalog,Is.Not.Null);Assert.That(catalog.Items.Count(),Is.GreaterThan(900));Assert.That(catalog.Trees.Count(),Is.GreaterThan(70));Assert.That(catalog.Find(CampExpeditionDefinition.HarnessId),Is.Not.Null);Assert.That(catalog.GuideTopics.Any(topic=>topic.id=="survival"),Is.True);Assert.That(CampMenuCatalog.Load(),Is.SameAs(catalog));}
    }
}
