import test from 'node:test';
import assert from 'node:assert/strict';
import {IDS,freshState,craft} from './dist/game-model.mjs';
import {restoreDiscoveries,discoverOwned,almanacIndex,almanacView} from './dist/game-almanac.mjs';

const definitions=()=>new Map([
  ['known',{'Item ID':'known','Item name':'Field tool',Category:'Tools',Purpose:'A useful tool.'}],
  ['secret-id',{'Item ID':'secret-id','Item name':'Secret machine',Category:'Tools',Purpose:'Hidden detail',Rarity:'Legendary','Search aliases':'secret alias','Search terms':'hidden keyword'}]
]);

test('undiscovered records conceal identity, metadata and artwork in the projected data and HTML',()=>{
  const catalog=definitions(),discovered=new Set(['known']);
  const entry=almanacIndex(catalog,discovered).entries.find(e=>!e.known);
  assert.deepEqual(entry,{slot:1,category:'Tools',ordinal:2,known:false});
  const view=almanacView({catalog,discovered,state:freshState(),selectedSlot:1});
  for(const secret of ['secret-id','Secret machine','Hidden detail','Legendary','secret alias','hidden keyword'])assert.ok(!view.includes(secret),secret);
  assert.match(view,/>\?\?\?</);assert.match(view,/This entry has not been discovered/);
});

test('search and sort cannot reveal undiscovered names, aliases, purposes or IDs',()=>{
  const catalog=definitions(),discovered=new Set(['known']);
  for(const query of ['Secret machine','secret alias','hidden keyword','Hidden detail','secret-id'])assert.equal(almanacIndex(catalog,discovered,{query}).matches,0);
  assert.equal(almanacIndex(catalog,discovered,{query:'field'}).matches,1);
  catalog.get('secret-id')['Item name']='AAAA';
  assert.equal(almanacIndex(catalog,discovered).entries[0].id,'known');
  assert.equal(almanacIndex(catalog,discovered,{status:'undiscovered'}).matches,1);
});

test('pagination reaches every item exactly once, filters categories and locates linked records beyond page one',()=>{
  const catalog=new Map(Array.from({length:135},(_,i)=>['item-'+i,{'Item ID':'item-'+i,'Item name':'Item '+i,Category:i<100?'Materials':'Equipment'}]));
  const discovered=new Set(catalog.keys()),first=almanacIndex(catalog,discovered),slots=[];
  for(let page=0;page<first.pages;page++)slots.push(...almanacIndex(catalog,discovered,{page}).entries.map(e=>e.slot));
  assert.equal(slots.length,135);assert.equal(new Set(slots).size,135);
  assert.equal(almanacIndex(catalog,discovered,{category:'Materials'}).matches,100);
  const linked=almanacIndex(catalog,discovered,{category:'Materials',locateSlot:99});
  assert.ok(linked.entries.some(e=>e.slot===99));
  assert.equal(almanacIndex(catalog,discovered,{page:999}).page,first.pages-1);
});

test('discovery accumulates from possessions, contained dust and successful crafting without changing inventory',()=>{
  const state=freshState(),before=JSON.stringify(state),initial=discoverOwned(new Set(['past-item']),state);
  assert.ok(initial.has('past-item'));assert.ok(initial.has('resource.airborne_dust'));assert.equal(JSON.stringify(state),before);
  const catalog=new Map(Object.values(IDS).map(id=>[id,{'Item ID':id,'Mass (game units)':.8}]));
  catalog.set('resource.airborne_dust',{'Mass (game units)':.05});
  const charged=craft(state,'charge',catalog),after=discoverOwned(initial,charged);
  assert.ok(after.has(IDS.charged));assert.ok(after.has(IDS.empty));
  assert.equal(initial.has(IDS.charged),false);
  assert.deepEqual([...discoverOwned(after,{entries:[]})],[...after]);
});

test('discovery saves retain absent definitions and reject malformed records',()=>{
  const ids=new Set(['known','removed-from-catalog']);
  assert.deepEqual(restoreDiscoveries({version:1,ids:[...ids]}),ids);
  for(const invalid of [null,{version:2,ids:[]},{version:1,ids:[null]},{version:1,ids:['']},{version:1,ids:'known'}])assert.throws(()=>restoreDiscoveries(invalid),/Invalid/);
  const catalog=definitions();catalog.get('known')['Item name']='<img src=x onerror=alert(1)>';
  const html=almanacView({catalog,discovered:ids,state:freshState()});
  assert.match(html,/&lt;img/);assert.doesNotMatch(html,/<img/);
});

test('the dust record counts contents in their existing canisters rather than reporting them absent',()=>{
  const state=freshState(),id='resource.airborne_dust',catalog=new Map([[id,{'Item ID':id,'Item name':'Airborne dust',Category:'Atmospheric resources'}]]);
  const view=almanacView({catalog,discovered:new Set([id]),state});
  assert.match(view,/<dt>Currently owned<\/dt><dd>13<\/dd>/);
  assert.match(view,/Legger cargo · 13 in a canister/);
  assert.doesNotMatch(view,/None currently owned/);
});
