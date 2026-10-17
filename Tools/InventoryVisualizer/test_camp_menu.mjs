import test from 'node:test';
import assert from 'node:assert/strict';
import {IDS,freshState} from './dist/game-model.mjs';
import {campStations,resolveCampStation,campView} from './dist/game-camp.mjs';

function catalog(){return new Map([
  [IDS.empty,{'Item ID':IDS.empty,'Item name':'Empty battery','Mass (game units)':.4}],
  [IDS.charged,{'Item ID':IDS.charged,'Item name':'Charged battery','Mass (game units)':.4}],
  [IDS.ore,{'Item ID':IDS.ore,'Mass (game units)':1.25}],
  [IDS.dust,{'Item ID':IDS.dust,'Mass (game units)':.8}],
  ['resource.airborne_dust',{'Item ID':'resource.airborne_dust','Mass (game units)':.35}],
  ['author.station.workbench',{'Item ID':'author.station.workbench','Item name':'Workbench',Category:'Crafting stations','Design status':'Author direction'}],
  ['author.station.dust_refinery',{'Item ID':'author.station.dust_refinery','Item name':'Dust refinery',Category:'Crafting stations','Design status':'Author direction'}]
]);}

test('missing default station resolves its ID and recipe together in the default Workstations view',()=>{
  const definitions=catalog();definitions.delete('author.station.workbench');
  const station=resolveCampStation(campStations(definitions),'author.station.workbench');
  assert.equal(station.id,'author.station.dust_refinery');assert.equal(station.recipe,'charge');
  const view=campView({state:freshState(),catalog:definitions,service:'workstations',stationId:station.id});
  assert.match(view,/Charged battery|Charged Arc Battery/);assert.doesNotMatch(view,/Craft one battery/);
  const fallback=campView({state:freshState(),catalog:definitions,service:'overview'});
  assert.match(fallback,/data-camp-screen="workstations"/);
  assert.doesNotMatch(fallback,/camp-service-overview|camp-back|camp-landscape/);
  assert.match(fallback,/Charged Arc Battery/);
  definitions.delete('author.station.dust_refinery');
  const unavailable=campView({state:freshState(),catalog:definitions});
  assert.match(unavailable,/No executable recipe/);assert.doesNotMatch(unavailable,/data-action="craft"/);
});

test('catalogue facilities remain inspectable without granting arbitrary executable recipes',()=>{
  const definitions=catalog();
  definitions.set('proposal.station.custom',{'Item ID':'proposal.station.custom','Item name':'Custom station',Category:'Crafting stations',recipe:'battery'});
  assert.equal(campStations(definitions).find(s=>s.id==='proposal.station.custom').recipe,null);
  const state=freshState(),before=JSON.stringify(state);
  const view=campView({state,catalog:definitions,service:'workstations',stationId:'proposal.station.custom'});
  assert.match(view,/No executable recipe/);assert.doesNotMatch(view,/data-action="craft"/);
  assert.equal(JSON.stringify(state),before);
  assert.ok(campStations(new Map()).some(s=>s.name==='Cooking station'&&!s.recipe));
});

test('camp previews escape catalogue text and show protected-stock and full-output blockers',()=>{
  const definitions=catalog(),state=freshState();
  definitions.get('author.station.workbench')['Item name']='<img src=x onerror=alert(1)>';
  state.protected.push('chest-ore');
  let view=campView({state,catalog:definitions,service:'workstations',stationId:'author.station.workbench'});
  assert.match(view,/&lt;img/);assert.doesNotMatch(view,/<img/);
  assert.match(view,/Need 4 unprotected Ironstone Ore/);
  assert.match(view,/data-action="craft" disabled/);
  state.protected=[];
  for(let slot=7;slot<10;slot++)state.entries.push({key:`full-${slot}`,item:IDS.empty,owner:'booter',qty:1,slot});
  view=campView({state,catalog:definitions,service:'workstations',stationId:'author.station.workbench'});
  assert.match(view,/no free supply pocket/);assert.match(view,/data-action="craft" disabled/);
});
