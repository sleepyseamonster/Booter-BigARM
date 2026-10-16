// Isolated browser study. Never sends mutations to the workbook or Unity.
export const IDS = Object.freeze({ore:'resource.ironstone_ore',dust:'tool.micro_dust_harvester',algae:'equipment.algae_canister',grapple:'equipment.grappling_hook',dig:'equipment.digging_tool',cloak:'equipment.cloak',jacket:'equipment.jacket',camp:'tool.popup_camp',empty:'equipment.arc_battery_empty',charged:'equipment.arc_battery_charged'});
export const EQUIPMENT = Object.freeze([['Dust canister',IDS.dust],['Algae canister',IDS.algae],['Grappling hook',IDS.grapple],['Digging tool',IDS.dig],['Cloak',IDS.cloak],['Jacket',IDS.jacket]]);
export const OWNERS = Object.freeze({booter:'Booter',legger:'Legger cargo',chest:'Camp chest'});
export const RECIPES = Object.freeze([
  {id:'battery',name:'Empty Arc Battery',station:'Workbench',output:IDS.empty,cost:4,description:'4 Ironstone Ore → 1 empty battery. Uses this camp chest.'},
  {id:'charge',name:'Charged Arc Battery',station:'Refinery',output:IDS.charged,cost:10,description:'10 contained dust + 1 empty battery → 1 charged battery. Canister is retained.'}
]);
const clone = value => JSON.parse(JSON.stringify(value));
export function freshState(){
  return {version:1,near:{legger:true,chest:true,station:true},entries:[
    ...EQUIPMENT.map(([label,item],i)=>({key:`fitted-${i}`,item,qty:1,owner:'booter',slot:i,instance:`study-equipment-${i}`,dust:0})),
    {key:'camp',item:IDS.camp,qty:1,owner:'booter',slot:6},
    {key:'haul',item:IDS.ore,qty:6,owner:'legger'},
    {key:'chest-ore',item:IDS.ore,qty:8,owner:'chest'},
    {key:'empty-battery',item:IDS.empty,qty:1,owner:'legger'},
    {key:'filled-canister',item:IDS.dust,qty:1,owner:'legger',instance:'study-canister-spare',dust:13}
  ],protected:[],favourites:[],projects:[],radial:['Inventory','Deploy dust canister','Make camp','Algae reserve'],sequence:0};
}
export function restoreState(value){
  if(!value||value.version!==1||!Array.isArray(value.entries)||value.entries.length>1000)throw Error('Unsupported study save. Reset the study to recover.');
  const keys=new Set(),instances=new Set(),slots=new Set();
  for(const e of value.entries){
    if(!e||typeof e.key!=='string'||keys.has(e.key)||typeof e.item!=='string'||!OWNERS[e.owner]||!Number.isSafeInteger(e.qty)||e.qty<1||e.qty>99999||!Number.isFinite(e.dust??0)||(e.dust??0)<0)throw Error('Invalid study item.');
    keys.add(e.key);
    if(e.instance){if(typeof e.instance!=='string'||instances.has(e.instance)||e.qty!==1)throw Error('Duplicate equipment identity.');instances.add(e.instance);}
    if(e.owner==='booter'){
      if(!Number.isInteger(e.slot)||e.slot<0||e.slot>9||slots.has(e.slot)||e.qty!==1)throw Error('Invalid Booter address.');
      slots.add(e.slot);
      if(e.slot<6?EQUIPMENT[e.slot][1]!==e.item:![IDS.camp,IDS.empty,IDS.charged].includes(e.item))throw Error('Wrong item in Booter space.');
      if((e.dust??0)>0)throw Error('Filled canister cannot be carried by Booter.');
    }
    if((e.dust??0)>0&&e.item!==IDS.dust)throw Error('Invalid contained dust.');
    if(EQUIPMENT.some(([,id])=>id===e.item)&&!e.instance)throw Error('Equipment identity missing.');
  }
  for(const [owner,limit] of [['legger',12],['chest',24]])if(value.entries.filter(e=>e.owner===owner).length>limit)throw Error('Stored inventory exceeds its capacity.');
  if(!value.near||['legger','chest','station'].some(k=>typeof value.near[k]!=='boolean')||!Number.isSafeInteger(value.sequence)||value.sequence<0)throw Error('Invalid study access.');
  for(const field of ['protected','favourites'])if(!Array.isArray(value[field])||value[field].some(k=>!keys.has(k)))throw Error('Invalid study markers.');
  if(!Array.isArray(value.projects)||value.projects.some(p=>!RECIPES.some(r=>r.id===p.recipe)||!Number.isInteger(p.qty)||p.qty<1||p.qty>99)||new Set(value.projects.map(p=>p.recipe)).size!==value.projects.length)throw Error('Invalid tracked project.');
  if(!Array.isArray(value.radial)||value.radial.length!==4||value.radial.some(s=>typeof s!=='string'||s.length>50))throw Error('Invalid shortcuts.');
  return clone(value);
}
export const accessible = (state,owner) => owner==='booter'||state.near[owner]===true;
export function amount(state,item,usable=false){return state.entries.filter(e=>e.item===item&&(!usable||accessible(state,e.owner)&&!state.protected.includes(e.key))).reduce((n,e)=>n+e.qty,0);}
export function load(state,owner,catalog){
  let mass=0,unknown=false;
  for(const e of state.entries.filter(e=>e.owner===owner)){
    const definition=catalog.get(e.item),unit=definition?.['Mass (game units)'];
    if(unit==null||!Number.isFinite(Number(unit)))unknown=true;else mass+=Number(unit)*e.qty;
    // Filled dust uses the authored dust resource mass, not the canister's mass.
    if(e.dust){const dustMass=catalog.get('resource.airborne_dust')?.['Mass (game units)'];if(dustMass==null)unknown=true;else mass+=e.dust*Number(dustMass);}
  }
  return {mass,unknown,slots:state.entries.filter(e=>e.owner===owner).length};
}
function validateCapacity(state,catalog){
  for(const owner of ['legger','chest']){
    const l=load(state,owner,catalog);if(l.slots>(owner==='legger'?12:24))throw Error(`${OWNERS[owner]} has no free storage spaces.`);
    if(owner==='legger'&&(l.unknown||l.mass>92))throw Error(l.unknown?'Cargo mass is undefined for this item.':'Cargo would exceed the 92 mass limit.');
  }
}
function addSupply(state,item){const slot=[6,7,8,9].find(i=>!state.entries.some(e=>e.owner==='booter'&&e.slot===i));if(slot===undefined)throw Error('Booter has no free supply pocket.');state.entries.push({key:`made-${++state.sequence}`,item,qty:1,owner:'booter',slot});}
export function transfer(state,key,destination,catalog){
  if(!OWNERS[destination])throw Error('Choose a valid destination.');
  const next=clone(state),entry=next.entries.find(e=>e.key===key);if(!entry)throw Error('This item is no longer here.');
  if(entry.owner===destination)throw Error('Already at this destination.');
  if(!accessible(next,entry.owner)||!accessible(next,destination))throw Error('Both owners must be physically accessible.');
  if(next.protected.includes(key))throw Error('Unprotect this item before transferring.');
  if(entry.item===IDS.algae&&entry.owner==='booter')throw Error('The living algae attachment cannot be removed.');
  if(destination==='booter'){
    if((entry.dust??0)>0)throw Error('Filled dust canisters belong in cargo or storage.');
    const fitted=EQUIPMENT.findIndex(([,id])=>id===entry.item);
    if(fitted>=0){if(next.entries.some(e=>e.owner==='booter'&&e.slot===fitted))throw Error('That equipment space is occupied. Store the fitted piece first.');entry.slot=fitted;}
    else if([IDS.camp,IDS.empty,IDS.charged].includes(entry.item)){const slot=[6,7,8,9].find(i=>!next.entries.some(e=>e.owner==='booter'&&e.slot===i));if(slot===undefined)throw Error('No supply pocket available.');entry.slot=slot;}
    else throw Error('Booter cannot carry resource items.');
  }else delete entry.slot;
  entry.owner=destination;
  if(!entry.instance){
    const max=Number(catalog.get(entry.item)?.['Stack limit']);
    if(Number.isSafeInteger(max)&&max>1)for(const other of next.entries){
      if(other!==entry&&other.owner===destination&&other.item===entry.item&&!other.instance&&!next.protected.includes(other.key)){
        const moved=Math.min(entry.qty,Math.max(0,max-other.qty));other.qty+=moved;entry.qty-=moved;
      }
    }
    next.entries=next.entries.filter(e=>e.qty>0);
    next.protected=next.protected.filter(k=>next.entries.some(e=>e.key===k));
    next.favourites=next.favourites.filter(k=>next.entries.some(e=>e.key===k));
  }
  validateCapacity(next,catalog);return next;
}
function performCraft(state,recipeId,catalog){
  const next=clone(state),recipe=RECIPES.find(r=>r.id===recipeId);if(!recipe)throw Error('Recipe unavailable.');
  if(!catalog.has(recipe.output))throw Error('Output is missing from the current catalogue.');
  if(!next.near.station)throw Error(`${recipe.station} is out of reach.`);
  const usable=e=>accessible(next,e.owner)&&!next.protected.includes(e.key);
  if(recipeId==='battery'){
    if(!next.near.chest)throw Error('Camp chest is out of reach.');
    const sources=next.entries.filter(e=>e.owner==='chest'&&e.item===IDS.ore&&usable(e));
    if(sources.reduce((n,e)=>n+e.qty,0)<4)throw Error('Need 4 unprotected Ironstone Ore in the camp chest.');
    let remaining=4;for(const e of sources){const take=Math.min(remaining,e.qty);e.qty-=take;remaining-=take;}
  }else{
    const canister=next.entries.find(e=>e.item===IDS.dust&&e.dust>=10&&usable(e));
    const battery=next.entries.find(e=>e.item===IDS.empty&&usable(e));
    if(!canister)throw Error('Need an accessible, unprotected canister containing at least 10 dust.');
    if(!battery)throw Error('Need an accessible, unprotected empty battery.');
    canister.dust-=10;battery.qty--;
  }
  next.entries=next.entries.filter(e=>e.qty>0);
  next.favourites=next.favourites.filter(k=>next.entries.some(e=>e.key===k));
  addSupply(next,recipe.output);validateCapacity(next,catalog);return next;
}
export function craft(state,recipeId,catalog){return performCraft(state,recipeId,catalog);}
export function craftBlocker(state,recipeId,catalog){try{performCraft(state,recipeId,catalog);return '';}catch(e){return e.message;}}
