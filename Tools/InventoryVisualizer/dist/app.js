import {installCategoryMoves} from './category-moves.js';
import {installNameEditor} from './name-editor.js';
import {installTierEditor} from './tier-editor.js';
import {installRarityEditor,rarityMarker,RARITY_COLORS} from './rarity-editor.js';
const RARITY_ORDER=new Map(Object.keys(RARITY_COLORS).map((rarity,index)=>[rarity,index]));
const $ = selector => document.querySelector(selector);
const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const state = {items:[], art:{}, category:'', owner:'catalog', selected:null, view:'menu', list:false};
const id = item => item['Item ID'];
const name = item => item['Item name'];
let database;
let toastTimer;
function toast(message){ $('#toast').textContent=message; $('#toast').classList.add('show'); clearTimeout(toastTimer); toastTimer=setTimeout(()=>$('#toast').classList.remove('show'),3800); }
function report(error){ console.error(error); toast(error.message || 'The operation could not be completed.'); }
function safeRun(fn){ return (...args)=>Promise.resolve().then(()=>fn(...args)).catch(report); }
async function openStorage(){
  database = await new Promise((resolve,reject)=>{const req=indexedDB.open('arc-dust-inventory-study',1);req.onupgradeneeded=()=>req.result.createObjectStore('art',{keyPath:'id'});req.onsuccess=()=>resolve(req.result);req.onerror=()=>reject(new Error('Browser storage unavailable. Saved item images cannot be loaded.'));});
  const records=await new Promise((resolve,reject)=>{const req=database.transaction('art').objectStore('art').getAll();req.onsuccess=()=>resolve(req.result);req.onerror=()=>reject(req.error);});
  state.art=Object.fromEntries(records.map(record=>[record.id,record]));
}
function recordFor(key){return state.art[key] || {id:key,status:'missing',notes:'',image:null};}
function glyph(item){
  const category=item.Category || '';
  let shape='<path d="m19 70 8-31 31-20 25 19 6 31-32 15Z" fill="currentColor" opacity=".32"/><path d="m19 70 38 14-9-32-21-13m21 13 10-33m-10 33 35-14m-35 14 41 17"/>';
  if(/component|mechanism|stock|salvage|attachments/i.test(category))shape='<path d="M20 37 65 22 85 38 40 55Z" fill="currentColor" opacity=".25"/><path d="M20 37v31l20 15V55m0 28 45-17V38L65 22 20 37l20 18 45-17M56 49v28M68 44v29"/><path d="m26 46 8 5m-8 9 8 5"/>';
  if(/container|consum|cooking ingredients|processing|atmospheric/i.test(category))shape='<path d="M35 30h31l8 14v34Q50 89 27 78V44Z" fill="currentColor" opacity=".25"/><path d="M35 30h31l8 14v34Q50 89 27 78V44Zm-8 14q24 13 47 0M35 30V20h31v10M38 59v14m6-16v18m16-18v18"/>';
  if(/equipment|supplies|armor|clothing/i.test(category))shape='<path d="m34 22 16 8 17-8 17 18-13 14-6-7v36H36V47l-7 7-13-14Z" fill="currentColor" opacity=".25"/><path d="m34 22 16 8 17-8 17 18-13 14-6-7v36H36V47l-7 7-13-14Zm16 8v53m-14-23h29"/>';
  if(category==='Tools')shape='<path d="m29 74 35-40 8 7-35 41Z" fill="currentColor" opacity=".3"/><path d="m29 74 35-40 8 7-35 41Zm18-48 12-12 27 24-12 12Z"/>';
  if(category==='Crafting stations')shape='<path d="M23 82V39l13-17h28l13 17v43Z" fill="currentColor" opacity=".25"/><path d="M23 82V39l13-17h28l13 17v43Zm0-43h54M36 82V62a14 14 0 0 1 28 0v20M31 22h38M40 22V12h20v10M19 82h62"/>';
  if(category==='Ammunition')shape='<path d="M26 77V39l9-17 9 17v38Zm30 0V39l9-17 9 17v38Z" fill="currentColor" opacity=".25"/><path d="M26 77V39l9-17 9 17v38Zm30 0V39l9-17 9 17v38ZM26 41h18M56 41h18M23 77h24M53 77h24"/>';
  if(category==='Camp items')shape='<path d="m15 80 35-58 35 58Z" fill="currentColor" opacity=".25"/><path d="m15 80 35-58 35 58Zm35-58v58m0-35L34 80m16-35 16 35M10 80h80M23 53l-9-7m63 7 9-7"/>';
  if(category==='Accessories')shape='<path d="M28 35h44l5 46H23Z" fill="currentColor" opacity=".25"/><path d="M28 35h44l5 46H23Zm10 0V24q12-12 24 0v11M26 48h48M42 48v12h16V48"/>';
  if(/weapon/i.test(category))shape='<path d="m29 70 40-42 12-8-6 16-36 43Z" fill="currentColor" opacity=".3"/><path d="m29 70 40-42 12-8-6 16-36 43m-16-19 24 24m-14-10-13 14m-5-4 11 11"/>';
  return `<svg viewBox="0 0 100 100" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round">${shape}</svg>`;
}
function imageFor(item,hero=false){const art=recordFor(id(item));return art.image?`<img draggable="false" src="${esc(art.image)}" alt="${esc(name(item))}" ${hero?'':'loading="lazy"'}>`:glyph(item);}
function tierLabel(item){return /^T\d$/.test(item.Tier)?item.Tier:item.Tier==='Unassigned'?'T?':'—';}
function ownerItems(){return state.items.filter(item=>state.owner==='booter'?!['Legger armor','Legger attachments'].includes(item.Category)&&/equipment|armor|tools|accessories|attachments|clothing|field supplies|camp items|ammunition|consumables|cooking ingredients|containers/i.test(item.Category):state.owner==='legger'?!/booter only/i.test(item['Carrier metadata']||''):true);}
function visibleItems(){
  const query=$('#search').value.toLocaleLowerCase().trim().split(/\s+/).filter(Boolean);
  const tier=$('#tier').value,design=$('#design').value,artFilter=$('#art-filter').value;
  return ownerItems().filter(item=>{
    const art=recordFor(id(item));
    if(state.category&&item.Category!==state.category)return false;
    if(tier&&item.Tier!==tier)return false;
    if(design&&item['Design status']!==design)return false;
    if(artFilter==='missing'&&art.image)return false;
    if(artFilter==='image'&&!art.image)return false;
    if(['queued','review','approved'].includes(artFilter)&&art.status!==artFilter)return false;
    return query.every(term=>Object.values(item).join(' ').toLocaleLowerCase().includes(term));
  }).sort((a,b)=>{
    const sort=$('#sort').value;
    return (sort==='tier'?Number(a['Tier sort order']??99)-Number(b['Tier sort order']??99):sort==='rarity'?(RARITY_ORDER.get(a.Rarity)??99)-(RARITY_ORDER.get(b.Rarity)??99):sort==='category'?String(a.Category).localeCompare(String(b.Category)):0)||name(a).localeCompare(name(b));
  });
}
function renderCategories(){
  const items=ownerItems();const counts={};for(const item of items)counts[item.Category]=(counts[item.Category]||0)+1;
  if(state.owner==='catalog')for(const category of state.categories||[])counts[category]??=0;
  $('#categories').innerHTML=`<button data-category="" class="${!state.category?'active':''}"><span>All items</span><small>${items.length}</small></button>`+Object.keys(counts).sort().map(category=>`<button data-category="${esc(category)}" class="${state.category===category?'active':''}"><span>${esc(category)}</span><small>${counts[category]}</small></button>`).join('');
}
function renderItems(){
  const items=visibleItems();
  $('#result-count').textContent=`${items.length} items${state.category?' / '+state.category:''}`;
  // Keep category totals visible even when search or filters narrow the grid.
  const sectionItems=ownerItems().filter(item=>!state.category||item.Category===state.category);
  const tierCounts=new Map();
  const tierRarities=new Map();
  for(const item of sectionItems){
    const tier=item.Tier||'Unassigned';tierCounts.set(tier,(tierCounts.get(tier)||0)+1);
    const rarity=!item.Rarity?'Unassigned':Object.hasOwn(RARITY_COLORS,item.Rarity)?item.Rarity:'Other / legacy';
    if(!tierRarities.has(tier))tierRarities.set(tier,new Map());
    const counts=tierRarities.get(tier);counts.set(rarity,(counts.get(rarity)||0)+1);
  }
  const sectionTiers=[...new Set([...(state.tierChoices||[]),...tierCounts.keys()])];
  const numericTiers=sectionTiers.filter(tier=>/^T\d+$/.test(tier)).sort((a,b)=>Number(a.slice(1))-Number(b.slice(1)));
  const otherTiers=sectionTiers.filter(tier=>!/^T\d+$/.test(tier)&&tierCounts.has(tier)).sort();
  const rarityOrder=[...Object.keys(RARITY_COLORS),'Unassigned','Other / legacy'];
  $('#tier-distribution').innerHTML=`<span class="tier-distribution-label">${esc(state.category||'All items')} · tier distribution</span><dl>${[...numericTiers,...otherTiers].map(tier=>{
    const counts=tierRarities.get(tier)||new Map();
    const breakdown=rarityOrder.filter(rarity=>counts.has(rarity)).map(rarity=>`<span class="tier-rarity-count" role="img" aria-label="${esc(`${tier}: ${counts.get(rarity)} ${rarity} items`)}" title="${esc(rarity)}: ${counts.get(rarity)}"><span class="rarity-key${Object.hasOwn(RARITY_COLORS,rarity)?'':' rarity-key-unknown'}"${Object.hasOwn(RARITY_COLORS,rarity)?` style="--rarity-color:${RARITY_COLORS[rarity]}"`:''} aria-hidden="true"></span>${counts.get(rarity)}</span>`).join('');
    return `<div><dt>${esc(tier)}</dt><dd class="tier-total"><span>${tierCounts.get(tier)||0}</span>${breakdown?`<span class="tier-rarity-breakdown">${breakdown}</span>`:''}</dd></div>`;
  }).join('')}</dl>`;
  const rarityCounts=new Map([...Object.keys(RARITY_COLORS),'Unassigned'].map(rarity=>[rarity,0]));
  for(const item of sectionItems){
    const rarity=!item.Rarity?'Unassigned':Object.hasOwn(RARITY_COLORS,item.Rarity)?item.Rarity:'Other / legacy';
    rarityCounts.set(rarity,(rarityCounts.get(rarity)||0)+1);
  }
  $('#rarity-distribution').innerHTML=`<span class="tier-distribution-label">${esc(state.category||'All items')} · rarity distribution</span><dl>${[...rarityCounts].map(([rarity,count])=>`<div><dt>${Object.hasOwn(RARITY_COLORS,rarity)?`<span class="rarity-key" style="--rarity-color:${RARITY_COLORS[rarity]}" aria-hidden="true"></span>`:''}${esc(rarity)}</dt><dd>${count}</dd></div>`).join('')}</dl>`;
  if(!items.some(item=>id(item)===state.selected))state.selected=items.length?id(items[0]):null;
  $('#items').classList.toggle('list',state.list);
  $('#items').innerHTML=items.length?items.map(item=>{
    const key=id(item),art=recordFor(key),kept=(state.protectedIds||[]).includes(key),reviewed=(state.reviewIds||[]).includes(key);
    return `<article class="item-card ${state.selected===key?'selected':''} ${kept?'kept':''} ${reviewed?'needs-review':''}" data-move-item="${esc(key)}" draggable="${state.owner==='catalog'&&state.view==='inventory'&&state.categoryEditing===true}"><button class="item-select" data-id="${esc(key)}" aria-label="Inspect ${esc(name(item))}" aria-pressed="${state.selected===key}"><div class="art">${imageFor(item)}<span class="art-badge">${tierLabel(item)}</span>${art.status!=='missing'||art.image?'<span class="art-status"></span>':''}${!art.image?'<span class="placeholder-tag">PLACEHOLDER</span>':''}</div><div class="card-copy"><strong>${esc(name(item))}</strong><small>${esc(item.Subtype||item.Category)}</small></div>${rarityMarker(item,esc)}</button><div class="item-controls"><button class="review-item ${reviewed?'checked':''}" data-action="review" data-item="${esc(key)}" aria-label="${reviewed?'Unmark review for':'Mark for review:'} ${esc(name(item))}" aria-pressed="${reviewed}" title="${reviewed?'Marked for review · click to clear':'Mark this item for review'}"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/>${reviewed?'<path d="m7 12 3.5 3.5 6.5-7"/>':''}</svg></button>${!reviewed?`<button class="keep-item ${kept?'checked':''}" data-action="protect" data-item="${esc(key)}" aria-label="${kept?'Unkeep':'Keep'} ${esc(name(item))}" aria-pressed="${kept}" title="${kept?'Kept · click to allow deletion':'Keep this item · prevent deletion'}"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/>${kept?'<path d="m7 12 3.5 3.5 6.5-7"/>':''}</svg></button>`:''}${!kept?`<button class="delete-item" data-action="delete" data-item="${esc(key)}" aria-label="Delete ${esc(name(item))} from spreadsheet" title="Permanently delete from spreadsheet"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 6h16M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7m4-7v7"/></svg></button>`:''}</div></article>`;
  }).join(''):'<div class="empty">No items match these filters.<br><br>Try another category or reset the filters.</div>';
  renderDetail();
}
function renderDetail(){
  const item=state.items.find(item=>id(item)===state.selected);
  if(!item){$('#detail').innerHTML='<div class="detail-body"><p class="description" style="padding-top:30px">Select an item to inspect its definition and artwork.</p></div>';return;}
  const art=recordFor(id(item));
  const field=(label,value)=>`<dt>${esc(label)}</dt><dd>${esc(value??'Not defined')}</dd>`;
  const acquisitionProcessing=[['Acquisition',item.Acquisition],['Processing',item['Crafting / processing']]].filter(([,text])=>text).map(([label,text])=>`<p><strong>${label}:</strong> ${esc(text)}</p>`).join('');
  $('#detail').innerHTML=`<div class="detail-top"><span>ITEM INSPECTION</span><span>${esc(tierLabel(item))} / ${art.image?'IMAGE':'NO IMAGE'}</span></div><div class="detail-hero">${imageFor(item,true)}${!art.image?'<span class="placeholder-tag">CATEGORY PLACEHOLDER · NOT ITEM ART</span>':''}</div><div class="detail-body"><div class="eyebrow">${esc(item.Category)}</div>${nameEditor.controls(item)}<div class="pills"><span class="pill">Tier: ${esc(item.Tier||'Unassigned')}</span></div><section class="item-description" aria-label="Item description"><h3>Description</h3><p class="description">${esc(item.Purpose||'Description not yet recorded in the master register.')}</p></section><dl>${field('Material',item['Material family'])}${field('Form',item['Physical form'])}${field('Primary use',item['Primary use'])}${field('Stage',item['Processing stage'])}${field('Rarity',item.Rarity)}${field('Scope',item['Implementation scope'])}${field('Carrier',item['Carrier metadata'])}${field('Stack limit',item['Stack limit'])}${field('Mass (game units)',item['Mass (game units)'])}</dl><section class="item-acquisition" aria-label="Acquisition and processing"><h3>Acquisition &amp; processing</h3>${acquisitionProcessing||'<p>Acquisition and processing details have not been recorded.</p>'}</section></div>`;
  $('#detail .item-acquisition').insertAdjacentHTML('afterend',categoryMoves.controls(item));
  $('#detail .item-description').insertAdjacentHTML('beforebegin',tierEditor.controls(item));
  $('#detail .item-description').insertAdjacentHTML('beforebegin',rarityEditor.controls(item));
  const description=$('#detail .item-description');
  for(const [label,text]of [['Use',item['Primary use']],['Behavior',item['Supported behavior']],['Limits',item.Limitations]]){
    if(!text)continue;
    const p=document.createElement('p');p.className='description-detail';
    const strong=document.createElement('strong');strong.textContent=`${label}: `;p.append(strong,document.createTextNode(text));description.append(p);
  }
  if($('#detail').dataset.itemId!==id(item))$('#detail').scrollTop=0;
  $('#detail').dataset.itemId=id(item);

}
function resetFilters(){state.category='';for(const selector of ['#search','#tier','#design','#art-filter'])$(selector).value='';renderCategories();renderItems();}
function setView(view){
  if(view==='art')view='inventory';
  if(!['menu','inventory','game'].includes(view))view='menu';state.view=view;
  $('#menu').hidden=view!=='menu';$('#workspace').hidden=view==='menu'||view==='game';
  $('#game-study').hidden=view!=='game';
  document.body.classList.toggle('game-host',view==='game');
  if(view==='game'){const frame=$('#game-study iframe');if(!frame.getAttribute('src'))frame.src=frame.dataset.src;}
  document.querySelectorAll('nav [data-view]').forEach(button=>{button.classList.toggle('active',button.dataset.view===view);button.setAttribute('aria-current',button.dataset.view===view?'page':'false');});
  document.querySelector('.category-drag-hint')?.remove();
  if(view==='inventory'&&state.owner==='catalog')document.querySelector('.review-hint')?.insertAdjacentHTML('beforeend','<span class="category-drag-hint"> · Drag an item onto a category to move it.</span>');
  renderItems();
  if(location.hash!==`#${view}`)history.replaceState(null,'',`#${view}`);
}
function setOwner(owner){
  state.owner=owner;state.category='';
  document.querySelectorAll('[data-owner]').forEach(button=>{button.classList.toggle('active',button.dataset.owner===owner);button.setAttribute('aria-pressed',button.dataset.owner===owner);});
  $('#loadout').hidden=owner!=='booter';
  $('#owner-note').textContent=owner==='booter'?'Candidate library for equipment and supplies; eligibility is not simulated.':owner==='legger'?'Cargo browsing study. No live cargo, transfer, range or capacity simulation.':'Browse the complete item register.';
  document.querySelector('.category-drag-hint')?.remove();
  if(state.view==='inventory'&&owner==='catalog')document.querySelector('.review-hint')?.insertAdjacentHTML('beforeend','<span class="category-drag-hint"> · Drag an item onto a category to move it.</span>');
  renderCategories();renderItems();
}
const prefs={theme:'iron',density:'145',textSize:'15',texture:'0.04'};
function applyPrefs(){document.body.classList.remove('oxide','chalk');if(prefs.theme!=='iron')document.body.classList.add(prefs.theme);document.documentElement.style.setProperty('--tile',`${prefs.density}px`);document.documentElement.style.fontSize=`${prefs.textSize}px`;document.documentElement.style.setProperty('--texture',prefs.texture);}
function savePrefs(){try{localStorage.setItem('arc-dust-appearance',JSON.stringify(prefs));}catch{toast('Appearance could not be saved in this browser.');}applyPrefs();}
function appearance(){ $('#settings').showModal(); }
document.querySelectorAll('[data-view]').forEach(button=>button.onclick=()=>setView(button.dataset.view));
$('.brand').onclick=()=>setView('menu');
window.addEventListener('hashchange',()=>setView(location.hash.slice(1)));
$('#appearance').onclick=appearance;$('#menu-appearance').onclick=appearance;
for(const [selector,key]of [['#theme','theme'],['#density','density'],['#text-size','textSize'],['#texture','texture']])$(selector).oninput=event=>{prefs[key]=event.target.value;savePrefs();};
document.querySelectorAll('[data-owner]').forEach(button=>button.onclick=()=>setOwner(button.dataset.owner));
$('#categories').onclick=event=>{const button=event.target.closest('[data-category]');if(!button)return;state.category=button.dataset.category;renderCategories();renderItems();};
$('#items').onclick=safeRun(async event=>{
  const action=event.target.closest('[data-action]');
  if(action){await reviewAction(action.dataset.action,action.dataset.item);return;}
  const button=event.target.closest('[data-id]');if(!button)return;state.selected=button.dataset.id;
  document.querySelectorAll('.item-select').forEach(card=>{card.closest('.item-card').classList.toggle('selected',card.dataset.id===state.selected);card.setAttribute('aria-pressed',card.dataset.id===state.selected);});renderDetail();$('#detail').scrollTop=0;
});
$('#search').oninput=renderItems;for(const selector of ['#tier','#design','#art-filter','#sort'])$(selector).onchange=renderItems;
$('#clear').onclick=resetFilters;$('#grid-toggle').onclick=()=>{state.list=!state.list;renderItems();};
document.addEventListener('keydown',event=>{
  if(state.view==='game')return;
  if($('#settings').open)return;
  if(/INPUT|SELECT|TEXTAREA/.test(event.target.tagName)){if(event.key==='Escape')event.target.blur();return;}
  if(event.key==='Escape'){setView('menu');return;}
  if(event.key==='/'){event.preventDefault();setView('inventory');$('#search').focus();return;}
  if(state.view==='menu'||!event.target.closest('#items')||!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key))return;
  const cards=[...document.querySelectorAll('.item-select')];const index=cards.indexOf(event.target.closest('.item-select'));if(index<0)return;
  const columns=state.list?1:Math.max(1,getComputedStyle($('#items')).gridTemplateColumns.split(' ').length);
  const delta={ArrowLeft:-1,ArrowRight:1,ArrowUp:-columns,ArrowDown:columns}[event.key];const next=cards[Math.max(0,Math.min(cards.length-1,index+delta))];if(next){event.preventDefault();next.click();next.focus();}
});
async function init(){
  $('#reload-data').disabled=true;
  try{const stored=JSON.parse(localStorage.getItem('arc-dust-appearance')||'null');if(stored){if(['iron','oxide','chalk'].includes(stored.theme))prefs.theme=stored.theme;for(const [key,min,max]of [['density',110,205],['textSize',14,19],['texture',0,.14]])if(Number.isFinite(Number(stored[key])))prefs[key]=String(Math.min(max,Math.max(min,Number(stored[key]))));}}catch{}
  applyPrefs();for(const [selector,key]of [['#theme','theme'],['#density','density'],['#text-size','textSize'],['#texture','texture']])$(selector).value=prefs[key];
  try{await openStorage();}catch(error){report(error);}
  $('#equipment-slots').innerHTML=['Dust canister','Algae canister','Grappling hook','Cloak','Jacket','Excavation tool'].map(slot=>`<button data-slot="${esc(slot)}">${esc(slot)}<small>UNFILLED / STUDY</small></button>`).join('');
  $('#equipment-slots').onclick=event=>{const slot=event.target.closest('[data-slot]');if(!slot)return;resetFilters();$('#search').value=slot.dataset.slot;renderItems();};
  renderCategories();renderItems();setView(location.hash.slice(1)||'menu');
  await refreshCatalog();
}
function syncStatus(message){$('#sync-status').textContent=message;}
let syncBusy=false;
async function reviewAction(action,key){
  if(categoryMoves.pending()||tierEditor.pending()||rarityEditor.pending()||nameEditor.pending()){toast('Check the pending spreadsheet save before changing another item.');return;}
  if(syncBusy){toast('Spreadsheet action in progress · try again in a moment');return;}
  const kept=(state.protectedIds||[]).includes(key),reviewed=(state.reviewIds||[]).includes(key);
  if(action==='delete'&&kept){toast('This item is kept. Uncheck it before deleting.');return;}
  syncBusy=true;
  try{
    const response=await fetch(`/api/items/${action}`,{method:'POST',headers:{'Content-Type':'application/json','X-Inventory-Token':state.mutationToken||''},body:JSON.stringify({itemId:key,sourceHash:state.sourceHash,protected:!kept,reviewed:!reviewed})});
    const result=await response.json();
    if(!response.ok)throw new Error(result.error||'Item could not be changed.');
    state.protectedIds=result.protectedIds;state.reviewIds=result.reviewIds||[];state.reviewRevision=result.reviewRevision;
    if(action==='protect'||action==='review'){
      renderItems();
      [...document.querySelectorAll('#items [data-action]')].find(button=>button.dataset.action===action&&button.dataset.item===key)?.focus({preventScroll:true});
      toast(action==='review'?(!reviewed?'Item marked for review':'Review mark cleared'):(!kept?'Item kept · deletion disabled':'Keep removed · deletion allowed'));
    }else{
      // A successful server response is required before the item disappears.
      if(database){try{await new Promise((resolve,reject)=>{const tx=database.transaction('art','readwrite');tx.objectStore('art').delete(key);tx.oncomplete=resolve;tx.onerror=()=>reject(tx.error);});}catch{/* A local artwork cleanup failure cannot undo a committed workbook deletion. */}delete state.art[key];}
      await refreshCatalog();toast('Item deleted from the master spreadsheet');
    }
  }finally{syncBusy=false;}
}
$('#reload-data').onclick=safeRun(async()=>{
  if(syncBusy){toast('Spreadsheet check already in progress');return;}
  syncBusy=true;$('#reload-data').disabled=true;syncStatus('Reading spreadsheet…');
  try{
    await refreshCatalog();toast('Latest saved spreadsheet loaded');
  }catch(error){syncStatus('Spreadsheet unavailable · retrying; keeping current items');throw error;}
  finally{syncBusy=false;$('#reload-data').disabled=false;}
});
async function refreshCatalog(force=false){
  if(!force&&(state.categoryDrag||state.categorySaving||nameEditor.dirty()))return;
  const response=await fetch('/api/catalog',{cache:'no-store'});
  if(!response.ok)throw new Error('The master workbook could not be loaded.');
  const data=await response.json();
  // Never rebuild the inspector while its controls are in use.
  if(!force&&(state.categoryDrag||state.categorySaving||nameEditor.dirty()||document.activeElement?.closest('#detail') || $('#settings').open || ['tier','design'].includes(document.activeElement?.id))){
    syncStatus('Spreadsheet update ready · waiting for editing to finish');return;
  }
  if(!Array.isArray(data.items)||typeof data.sha256!=='string')throw new Error('Invalid catalog response.');
  const changed=Boolean(state.sourceHash&&state.sourceHash!==data.sha256);
  const focus=document.activeElement;
  const focusItem=focus?.dataset.id,focusCategory=focus?.dataset.category;
  const gridScroll=$('#items').scrollTop,detailScroll=$('#detail').scrollTop;
  const openDetails=[...document.querySelectorAll('#detail details[open]')].map(el=>el.querySelector('summary').textContent);
  state.items=data.items;state.categories=data.categories||[];state.categoryEditing=data.categoryEditing===true;state.sourceHash=data.sha256;state.protectedIds=data.protectedIds||[];state.reviewIds=data.reviewIds||[];state.reviewRevision=data.reviewRevision;state.mutationToken=data.mutationToken;$('#total').textContent=state.items.length;
  state.tierChoices=data.tierChoices||[];state.tierEditing=data.tierEditing===true;
  state.rarityChoices=data.rarityChoices||[];state.rarityEditing=data.rarityEditing===true;
  state.nameEditing=data.nameEditing===true;
  for(const [selector,key,label]of [['#tier','Tier','All tiers'],['#design','Design status','All statuses']]){
    const selected=$(selector).value;
    const values=[...new Set(state.items.map(item=>item[key]).filter(Boolean))].sort();
    $(selector).innerHTML=`<option value="">${label}</option>`+values.map(value=>`<option value="${esc(value)}">${esc(value)}</option>`).join('');
    $(selector).value=values.includes(selected)?selected:'';
  }
  if(!(state.owner==='catalog'?state.categories.includes(state.category):ownerItems().some(item=>item.Category===state.category)))state.category='';
  renderCategories();renderItems();
  $('#items').scrollTop=gridScroll;$('#detail').scrollTop=detailScroll;
  document.querySelectorAll('#detail details').forEach(el=>{el.open=openDetails.includes(el.querySelector('summary').textContent);});
  if(focusItem!==undefined||focusCategory!==undefined){
    const candidates=focusItem!==undefined?[...document.querySelectorAll('.item-select')]:[...document.querySelectorAll('[data-category]')];
    (candidates.find(el=>focusItem!==undefined?el.dataset.id===focusItem:el.dataset.category===focusCategory)||$('#search')).focus({preventScroll:true});
  }
  syncStatus('Spreadsheet · live');
  if(changed)toast('Spreadsheet updated · item library refreshed');
}
async function checkSpreadsheet(){
  if(syncBusy||state.categoryDrag||state.categorySaving||nameEditor.dirty()||document.hidden)return;
  syncBusy=true;
  try{
    const response=await fetch('/api/revision',{cache:'no-store'});
    if(!response.ok)throw new Error('Spreadsheet unavailable');
    const revision=await response.json();
    if(revision.sha256!==state.sourceHash||revision.reviewRevision!==state.reviewRevision||revision.mutationToken!==state.mutationToken)await refreshCatalog();
    else syncStatus('Spreadsheet · live');
  }catch{syncStatus('Spreadsheet unavailable · retrying; keeping current items');}
  finally{syncBusy=false;}
}
const categoryMoves=installCategoryMoves({state,esc,refresh:refreshCatalog,toast,busy:()=>syncBusy||tierEditor.pending()||rarityEditor.pending()||nameEditor.pending(),setBusy:value=>{syncBusy=value;state.categorySaving=value;},saveDraft:async()=>{}});
const tierEditor=installTierEditor({state,esc,refresh:refreshCatalog,toast,busy:()=>syncBusy||categoryMoves.pending()||rarityEditor.pending()||nameEditor.pending(),setBusy:value=>{syncBusy=value;state.categorySaving=value;},saveDraft:async()=>{}});
const rarityEditor=installRarityEditor({state,esc,refresh:refreshCatalog,toast,busy:()=>syncBusy||categoryMoves.pending()||tierEditor.pending()||nameEditor.pending(),setBusy:value=>{syncBusy=value;state.categorySaving=value;},saveDraft:async()=>{}});
const nameEditor=installNameEditor({state,esc,refresh:refreshCatalog,toast,busy:()=>syncBusy||categoryMoves.pending()||tierEditor.pending()||rarityEditor.pending(),setBusy:value=>{syncBusy=value;state.categorySaving=value;},saveDraft:async()=>{}});
init().then(async()=>{await categoryMoves.restore();await tierEditor.restore();await rarityEditor.restore();await nameEditor.restore();}).catch(error=>{$('#result-count').textContent=error.message;report(error);}).finally(()=>{
  $('#reload-data').disabled=false;
  setInterval(checkSpreadsheet,3000);
  document.addEventListener('visibilitychange',()=>{if(!document.hidden)checkSpreadsheet();});
});
