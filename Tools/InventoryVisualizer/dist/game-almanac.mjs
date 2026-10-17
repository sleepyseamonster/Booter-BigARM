import {OWNERS,RECIPES,amount} from './game-model.mjs';
import {itemIcon} from './item-icons.mjs';

const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export const DISCOVERY_SAVE='arc-dust-game-almanac-v1';
export function restoreDiscoveries(value){
  if(!value||value.version!==1||!Array.isArray(value.ids)||value.ids.length>20000||value.ids.some(id=>typeof id!=='string'||!id||id.length>200))throw Error('Invalid almanac discovery record.');
  return new Set(value.ids);
}
export function discoverOwned(previous,state){
  const next=new Set(previous);
  for(const entry of state.entries){next.add(entry.item);if(entry.dust>0)next.add('resource.airborne_dust');}
  return next;
}

// Unknown entries contain only their public category and catalogue address.
// Their names, IDs, aliases, artwork and item facts never enter the rendered view.
export function almanacIndex(catalog,discovered,{category='',status='all',query='',order='discovered',page=0,pageSize=36,locateSlot=null}={}){
  const categories=new Map(),ordinals=new Map();
  const entries=[...catalog.values()].map((item,slot)=>{
    const family=item.Category||'Uncategorized',known=discovered.has(item['Item ID']);
    const summary=categories.get(family)||{name:family,total:0,known:0};summary.total++;if(known)summary.known++;categories.set(family,summary);
    const ordinal=(ordinals.get(family)||0)+1;ordinals.set(family,ordinal);
    return known?{slot,category:family,ordinal,known,id:item['Item ID'],item}:{slot,category:family,ordinal,known};
  });
  const text=query.trim().toLowerCase();
  let matches=entries.filter(e=>(!category||e.category===category)&&(status==='all'||(status==='discovered'?e.known:!e.known))&&(!text||e.known&&[e.item['Item name'],e.category,e.item.Purpose,e.item['Search aliases'],e.item['Search terms']].join(' ').toLowerCase().includes(text)));
  const byAddress=(a,b)=>a.category.localeCompare(b.category)||a.ordinal-b.ordinal;
  matches.sort((a,b)=>order==='catalog'?byAddress(a,b):Number(b.known)-Number(a.known)||(a.known&&b.known?String(a.item['Item name']).localeCompare(String(b.item['Item name'])):byAddress(a,b)));
  const size=Number.isInteger(pageSize)&&pageSize>0?Math.min(100,pageSize):36,pages=Math.max(1,Math.ceil(matches.length/size));
  const located=matches.findIndex(e=>e.slot===locateSlot),requested=located>=0?Math.floor(located/size):page,current=Math.max(0,Math.min(pages-1,Number.isInteger(requested)?requested:0));
  return {categories:[...categories.values()].sort((a,b)=>a.name.localeCompare(b.name)),known:entries.filter(e=>e.known).length,total:entries.length,matches:matches.length,pages,page:current,start:current*size,entries:matches.slice(current*size,(current+1)*size)};
}

function entryDetail(entry,state,discovered){
  if(!entry)return '<aside class="almanac-detail"><div class="section-label">ITEM RECORD</div><p class="empty">No entries match this view.</p></aside>';
  if(!entry.known)return `<aside class="almanac-detail"><div class="section-label">UNDISCOVERED <span>RECORD ${String(entry.ordinal).padStart(3,'0')}</span></div><div class="almanac-art unknown-art" aria-hidden="true">?</div><h2>???</h2><p class="almanac-type">${esc(entry.category)}</p><p>This entry has not been discovered.</p><div class="almanac-note">Find or craft this item to record its name, appearance and details here.</div></aside>`;
  const item=entry.item,locations=state.entries.filter(e=>e.item===entry.id),contents=entry.id==='resource.airborne_dust'?state.entries.filter(e=>e.dust>0):[],owned=amount(state,entry.id)+contents.reduce((n,e)=>n+e.dust,0),uses=RECIPES.filter(r=>discovered.has(r.output)&&(r.output===entry.id||r.id==='battery'&&entry.id==='resource.ironstone_ore'||r.id==='charge'&&['equipment.arc_battery_empty','tool.micro_dust_harvester'].includes(entry.id)));
  const facts=[['Category',entry.category],['Type',item.Subtype||item['Item type']],['Rarity',item.Rarity],['Value tier',item.Tier],['Unit mass',item['Mass (game units)']],['Stack limit',item['Stack limit']]].filter(([,v])=>v!==null&&v!==undefined&&v!=='');
  return `<aside class="almanac-detail"><div class="section-label">DISCOVERED <span>RECORD ${String(entry.ordinal).padStart(3,'0')}</span></div><div class="almanac-art">${itemIcon(entry.id)}</div><h2>${esc(item['Item name'])}</h2><p class="almanac-type">${esc(entry.category)}</p><p>${esc(item.Purpose||'No description recorded.')}</p><dl>${facts.map(([label,value])=>`<dt>${label}</dt><dd>${esc(value)}</dd>`).join('')}<dt>Currently owned</dt><dd>${owned}</dd></dl>${locations.length||contents.length?`<div class="almanac-note">${[...locations.map(e=>`${esc(OWNERS[e.owner])} · ${e.qty}${e.dust?` · ${e.dust} contained dust`:''}`),...contents.map(e=>`${esc(OWNERS[e.owner])} · ${e.dust} in a canister`)].join('<br>')}</div>`:'<div class="almanac-note">Previously discovered. None currently owned.</div>'}${[['Acquisition',item.Acquisition],['Crafting & processing',item['Crafting / processing']],['Limitations',item.Limitations]].filter(([,v])=>v).map(([label,value])=>`<details><summary>${label}</summary><p>${esc(value)}</p></details>`).join('')}${uses.map(r=>`<button class="help-link" data-craft-link="${r.id}">${r.output===entry.id?'Make':'Used in'}: ${esc(r.name)} ↗</button>`).join('')}</aside>`;
}

export function almanacView({catalog,discovered,state,options={},selectedSlot=null}){
  const view=almanacIndex(catalog,discovered,options),selected=view.entries.find(e=>e.slot===selectedSlot)||view.entries[0];
  const categoryButton=(name,known,total)=>`<button id="almanac-category-${name?encodeURIComponent(name):'all'}" data-almanac-category="${esc(name)}" class="${(options.category||'')===name?'active':''}" aria-pressed="${(options.category||'')===name}"><span>${esc(name||'All items')}</span><small>${known} / ${total}</small></button>`;
  return `<section class="almanac"><div class="almanac-heading"><div><span class="eyebrow">REFERENCE / FIELD RECORDS</span><h1>Item almanac</h1><p>Every item has a place. Discovery fills in the details.</p></div><div class="almanac-progress"><strong>${view.known}<span> / ${view.total}</span></strong><small>ITEMS DISCOVERED</small><div class="meter"><span style="width:${view.total?view.known/view.total*100:0}%"></span></div></div></div><div class="almanac-layout"><aside class="almanac-categories" aria-label="Almanac categories"><div class="section-label">CATEGORIES <span>${view.categories.length}</span></div>${categoryButton('',view.known,view.total)}${view.categories.map(c=>categoryButton(c.name,c.known,c.total)).join('')}</aside><section class="almanac-collection" aria-label="Item entries"><div class="almanac-tools"><input id="game-search" type="search" aria-label="Search discovered items" placeholder="Search discovered items…" value="${esc(options.query||'')}"><select id="almanac-status" aria-label="Discovery status"><option value="all" ${!options.status||options.status==='all'?'selected':''}>All entries</option><option value="discovered" ${options.status==='discovered'?'selected':''}>Discovered</option><option value="undiscovered" ${options.status==='undiscovered'?'selected':''}>Undiscovered</option></select><select id="almanac-order" aria-label="Almanac order"><option value="discovered" ${options.order!=='catalog'?'selected':''}>Discovered first</option><option value="catalog" ${options.order==='catalog'?'selected':''}>Category order</option></select></div><div class="almanac-mobile-category"><label for="almanac-category-picker">Category</label><select id="almanac-category-picker"><option value="">All items · ${view.known} / ${view.total}</option>${view.categories.map(c=>`<option value="${esc(c.name)}" ${options.category===c.name?'selected':''}>${esc(c.name)} · ${c.known} / ${c.total}</option>`).join('')}</select></div><div class="almanac-results"><span>${esc(options.category||'All items')} <small>· ${view.matches} ${view.matches===1?'entry':'entries'}</small></span><small>${options.query?'Only discovered records can match search.':'? Undiscovered records stay concealed.'}</small></div><div class="almanac-grid">${view.entries.map(e=>`<button id="almanac-entry-${e.slot}" data-almanac-slot="${e.slot}" class="almanac-entry ${e.known?'known':'unknown'} ${selected?.slot===e.slot?'selected':''}" aria-pressed="${selected?.slot===e.slot}" aria-label="${e.known?`Inspect ${esc(e.item['Item name'])}`:`Undiscovered ${esc(e.category)} record ${e.ordinal}`}"><span class="almanac-record">${String(e.ordinal).padStart(3,'0')} <span>${e.known?'◆':'○'}</span></span><span class="almanac-entry-art" aria-hidden="true">${e.known?itemIcon(e.id):'?'}</span><strong>${e.known?esc(e.item['Item name']):'???'}</strong><small>${esc(e.category)}</small></button>`).join('')||'<p class="empty">No entries match. Try another category, discovery filter or search.</p>'}</div><div class="almanac-pagination"><button id="almanac-previous" data-almanac-page="${view.page-1}" ${view.page===0?'disabled':''}>← Previous</button><span>${view.matches?`${view.start+1}–${view.start+view.entries.length}`:'0'} of ${view.matches}<small>Page ${view.page+1} / ${view.pages}</small></span><button id="almanac-next" data-almanac-page="${view.page+1}" ${view.page>=view.pages-1?'disabled':''}>Next →</button></div></section>${entryDetail(selected,state,discovered)}</div></section>`;
}
