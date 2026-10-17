import {IDS,RECIPES,amount,accessible,craftBlocker} from './game-model.mjs';
import {itemIcon} from './item-icons.mjs';

const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export const CAMP_SERVICES=Object.freeze([
  {id:'storage',name:'Item box',hint:'Store, retrieve & inspect',icon:'▤'},
  {id:'inventory',name:'Booter equipment',hint:'Equipment & supply pockets',icon:'◈'},
  {id:'legger',name:'Legger cargo',hint:'Manage the expedition haul',icon:'◇'},
  {id:'workstations',name:'Workstations',hint:'Craft, refine & prepare',icon:'⚒'},
  {id:'projects',name:'Tracked projects',hint:'Plan your gathering',icon:'⌁'},
  {id:'reference',name:'Item almanac',hint:'Known & undiscovered items',icon:'⊞'}
]);
const recipeStations={'author.station.workbench':'battery','author.station.dust_refinery':'charge'};
export function campStations(catalog){
  const stations=[...catalog.values()].filter(i=>i.Category==='Crafting stations').map(i=>({id:i['Item ID'],name:i['Item name'],purpose:i.Purpose||'Station details are awaiting design.',recipe:recipeStations[i['Item ID']]||null,proposal:i['Design status']!=='Author direction'}));
  // Cooking is author-directed, but the current catalogue can omit its former firebox row.
  // This is a menu address only; it creates no item, stock, recipe or station ownership.
  if(!stations.some(s=>/cooking/i.test(s.name)))stations.push({id:'camp-menu.cooking',name:'Cooking station',purpose:'Prepare food at camp. Ingredients, preparation, heat and atmosphere requirements are still to be designed.',recipe:null,proposal:false});
  const order=['author.station.workbench','author.station.dust_refinery','author.station.ironstone_refinery','camp-menu.cooking'];
  return stations.sort((a,b)=>(order.indexOf(a.id)<0?99:order.indexOf(a.id))-(order.indexOf(b.id)<0?99:order.indexOf(b.id))||a.name.localeCompare(b.name));
}
export function resolveCampStation(stations,id){return stations.find(s=>s.id===id)||stations[0];}

function workstationView(state,catalog,stations,stationId){
  const station=resolveCampStation(stations,stationId);
  if(!station)return '<p class="empty">No workstation definitions are available.</p>';
  const r=RECIPES.find(r=>r.id===station.recipe),blocker=r?craftBlocker(state,r.id,catalog):'';
  const ore=state.entries.filter(e=>e.owner==='chest'&&e.item===IDS.ore&&accessible(state,e.owner)&&!state.protected.includes(e.key)).reduce((n,e)=>n+e.qty,0);
  const canisters=state.entries.filter(e=>e.item===IDS.dust&&accessible(state,e.owner)&&!state.protected.includes(e.key));
  const rows=r?.id==='battery'?[['Ironstone Ore',ore,4,'In the accessible camp chest']]:r?[['Contained Arc Dust',Math.max(0,...canisters.map(e=>e.dust||0)),10,'In one accessible, unprotected canister'],['Empty Arc Battery',amount(state,IDS.empty,true),1,'Across accessible, unprotected stock']]:[];
  return `<div class="camp-workstations"><div class="station-picker"><label for="camp-station-picker">WORKSTATION</label><select id="camp-station-picker" data-camp-station-select>${stations.map(s=>`<option value="${esc(s.id)}" ${station.id===s.id?'selected':''}>${esc(s.name)}</option>`).join('')}</select></div><section class="station-list" aria-label="Camp workstations"><div class="camp-section-label">STATIONS <span>${stations.length}</span></div>${stations.map(s=>`<button id="camp-station-${esc(s.id)}" data-camp-station="${esc(s.id)}" aria-pressed="${station.id===s.id}" class="station-row ${station.id===s.id?'selected':''}"><span class="station-mark" aria-hidden="true">${s.recipe==='charge'?'ϟ':s.recipe==='battery'?'⚒':s.name==='Cooking station'?'◉':'◇'}</span><span><strong>${esc(s.name)}</strong><small>${s.recipe?(craftBlocker(state,s.recipe,catalog)?'Requirements unmet':'Ready to use'):s.proposal?'Proposed facility':'Recipe not modelled'}</small></span><span aria-hidden="true">›</span></button>`).join('')}</section><article class="station-detail" aria-labelledby="station-title"><div class="camp-section-label">${r?'SAMPLE OPERATION':'STATION PREVIEW'}<span>${r?'01 RECIPE':station.proposal?'PROPOSAL':'PLANNED'}</span></div><div class="station-art">${itemIcon(r?.output||station.id)}</div><h2 id="station-title">${esc(station.name)}</h2><p>${esc(station.purpose)}</p>${r?`<div class="station-recipe"><span class="eyebrow">${r.id==='battery'?'ASSEMBLE':'REFINE & CHARGE'}</span><h3>${esc(r.name)}</h3>${rows.map(([name,have,need,note])=>`<div class="camp-requirement"><span>${name}<small>${note}</small></span><strong class="${have>=need?'good':'blocked'}">${have} / ${need}</strong></div>`).join('')}<div class="camp-requirement"><span>Output destination<small>One free supply pocket required</small></span><strong>Booter</strong></div><p class="station-status ${blocker?'blocked':'good'}" role="status">${esc(blocker||'Ready to make · all requirements met.')}</p><div class="camp-recipe-actions"><button class="camp-primary" data-action="craft" ${blocker?'disabled':''}>${r.id==='battery'?'Craft one battery':'Refine & charge'} <span>→</span></button><button data-action="track">${state.projects.some(p=>p.recipe===r.id)?'Project tracked ✓':'Track project +'}</button></div></div>`:`<div class="station-unavailable"><span class="eyebrow">RECIPE NOT YET AVAILABLE</span><h3>${station.name==='Cooking station'?'Cooking menu reserved.':'This station is ready for UI planning.'}</h3><p>${station.name==='Cooking station'?'Food, ingredients and cooking methods need their own recipe design.':'Select this facility to inspect its role. Qualified inputs, output amounts and operating requirements have not been modelled here.'}</p><button disabled>No executable recipe</button></div>`}</article></div>`;
}

export function campView({state,catalog,service='workstations',stationId,content=''}){
  const stations=campStations(catalog),active=CAMP_SERVICES.find(s=>s.id===service)||CAMP_SERVICES.find(s=>s.id==='workstations');
  service=active.id;
  return `<section class="camp-hub"><div class="camp-title"><div><span class="eyebrow">EXPEDITION / CRAFTING</span><h1>Crafting<span>Camp Loop</span></h1></div><button id="leave-camp" data-camp-exit>Close crafting <span>↗</span></button></div><div class="camp-layout"><aside class="camp-rail" aria-label="Crafting services"><div class="camp-identity"><span>△</span><div><strong>Camp Loop</strong><small>FIELD PREPARATION</small></div></div>${CAMP_SERVICES.map(s=>`<button id="camp-service-${s.id}" data-camp-service="${s.id}" aria-current="${service===s.id?'page':'false'}" class="camp-service ${service===s.id?'active':''}"><span class="service-icon" aria-hidden="true">${s.icon}</span><span><strong>${s.name}</strong><small>${s.hint}</small></span><span class="service-arrow" aria-hidden="true">›</span></button>`).join('')}<div class="camp-presence"><span class="camp-section-label">LOCAL ACCESS</span>${[['chest','Camp chest'],['legger','Legger cargo'],['station','Workstations']].map(([id,label])=>`<div><span>${label}</span><span class="${state.near[id]?'good':'blocked'}">${state.near[id]?'● Nearby':'○ Out of reach'}</span></div>`).join('')}<button data-page="settings">Adjust study access ↗</button></div></aside><div class="camp-body" data-camp-screen="${esc(service)}"><div class="camp-breadcrumb"><span>CRAFTING / ${esc(active.name.toUpperCase())}</span></div>${service==='workstations'?workstationView(state,catalog,stations,stationId):content}</div></div></section>`;
}
