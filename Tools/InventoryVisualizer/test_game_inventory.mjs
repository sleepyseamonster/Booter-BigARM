import test from 'node:test';
import assert from 'node:assert/strict';
import {IDS,freshState,restoreState,transfer,craft,craftBlocker,amount,load} from './dist/game-model.mjs';

const catalog=new Map(Object.values(IDS).map(id=>[id,{'Mass (game units)':id===IDS.ore?1.25:.8,'Stack limit':id===IDS.ore?99:1}]));
catalog.set('resource.airborne_dust',{'Mass (game units)':.05});
const total=(s,id)=>amount(s,id);
test('transfer conserves quantity and merges stacks without changing the source state',()=>{
  const before=freshState(),snapshot=JSON.stringify(before),after=transfer(before,'haul','chest',catalog);
  assert.equal(JSON.stringify(before),snapshot);assert.equal(total(after,IDS.ore),14);
  assert.equal(after.entries.filter(e=>e.item===IDS.ore).length,1);
  assert.equal(after.entries.find(e=>e.item===IDS.ore).owner,'chest');
});
test('resources, filled canisters, occupied equipment and attached algae cannot enter invalid ownership',()=>{
  const state=freshState(),before=JSON.stringify(state);
  assert.throws(()=>transfer(state,'haul','booter',catalog),/resource/);
  assert.throws(()=>transfer(state,'filled-canister','booter',catalog),/Filled/);
  const stored=transfer(state,'fitted-0','legger',catalog);
  assert.equal(transfer(stored,'fitted-0','booter',catalog).entries.find(e=>e.key==='fitted-0').instance,'study-equipment-0');
  assert.throws(()=>transfer(state,'fitted-1','chest',catalog),/attachment/);
  assert.equal(JSON.stringify(state),before);
});
test('access and protection reject transfers and crafting without consuming stock',()=>{
  const state=freshState();state.near.legger=false;
  assert.throws(()=>transfer(state,'haul','chest',catalog),/accessible/);
  state.protected.push('chest-ore');assert.throws(()=>craft(state,'battery',catalog),/unprotected/);
  assert.equal(total(state,IDS.ore),14);
  state.protected=[];state.near.chest=false;assert.match(craftBlocker(state,'battery',catalog),/out of reach/);
});
test('full output pockets roll back all craft inputs including contained dust',()=>{
  let state=freshState();state=craft(state,'battery',catalog);state=craft(state,'battery',catalog);
  // Third additional pocket is the last free output destination.
  state=craft(state,'charge',catalog);const before=JSON.stringify(state);
  state.entries.push({key:'extra-ore',item:IDS.ore,qty:4,owner:'chest'});
  const filled=JSON.stringify(state);assert.throws(()=>craft(state,'battery',catalog),/pocket/);assert.equal(JSON.stringify(state),filled);
  assert.equal(state.entries.find(e=>e.key==='filled-canister').dust,3);
  assert.notEqual(before,filled);
  state.entries.push({key:'extra-battery',item:IDS.empty,qty:1,owner:'legger'});
  state.entries.find(e=>e.key==='filled-canister').dust=10;
  state.protected=state.entries.filter(e=>e.owner==='booter'&&e.item===IDS.empty).map(e=>e.key);
  const beforeCharge=JSON.stringify(state);assert.throws(()=>craft(state,'charge',catalog),/pocket/);assert.equal(JSON.stringify(state),beforeCharge);
});
test('refining retains canister identity and unspent dust; full and protected failures are atomic',()=>{
  const state=freshState(),after=craft(state,'charge',catalog);
  assert.equal(after.entries.find(e=>e.key==='filled-canister').instance,'study-canister-spare');
  assert.equal(after.entries.find(e=>e.key==='filled-canister').dust,3);
  assert.equal(total(after,IDS.charged),1);assert.equal(total(after,IDS.empty),0);
  assert.equal(state.entries.find(e=>e.key==='filled-canister').dust,13);
  state.protected.push('filled-canister');assert.throws(()=>craft(state,'charge',catalog),/canister/);
});
test('cargo capacity and actual contained mass are checked before committing',()=>{
  const state=freshState();const l=load(state,'legger',catalog);assert.equal(l.unknown,false);assert.ok(Math.abs(l.mass-9.75)<1e-9);
  state.entries.find(e=>e.key==='haul').qty=73;
  assert.throws(()=>transfer(state,'fitted-0','legger',catalog),/92/);
  assert.equal(state.entries.find(e=>e.key==='fitted-0').owner,'booter');
});
test('study save round-trip rejects duplicate identity, bad Booter addresses and corrupt projects',()=>{
  const state=freshState();assert.deepEqual(restoreState(JSON.parse(JSON.stringify(state))),state);
  const duplicate=structuredClone(state);duplicate.entries[1].instance=duplicate.entries[0].instance;
  assert.throws(()=>restoreState(duplicate),/Duplicate/);
  const misplaced=structuredClone(state);misplaced.entries.find(e=>e.key==='haul').owner='booter';misplaced.entries.find(e=>e.key==='haul').slot=7;
  assert.throws(()=>restoreState(misplaced),/Booter/);
  const bad=structuredClone(state);bad.projects=[{recipe:'unknown',qty:1}];assert.throws(()=>restoreState(bad),/project/);
});
