// Rarity edits persist to the same catalog workbook used by every view.
export const RARITY_COLORS={Junk:'#90949b',Common:'#f3f1e9',Uncommon:'#65c879',Rare:'#559fff',Epic:'#db63cf',Legendary:'#f5a532'};
export function rarityMarker(item,esc){
  const rarity=item.Rarity;
  return Object.hasOwn(RARITY_COLORS,rarity)?`<span class="rarity-corner" style="--rarity-color:${RARITY_COLORS[rarity]}" role="img" aria-label="Rarity: ${esc(rarity)}" title="Rarity: ${esc(rarity)}"></span>`:'';
}
export function installRarityEditor({state,esc,refresh,toast,busy,setBusy,saveDraft}) {
  let pending=null;
  const key='arc-dust-rarity-pending';
  const panel=document.createElement('div');panel.className='category-feedback rarity-feedback';panel.hidden=true;
  panel.setAttribute('role','status');panel.setAttribute('aria-live','polite');document.body.append(panel);
  let timer;
  function remember(value){pending=value;try{value?sessionStorage.setItem(key,JSON.stringify(value)):sessionStorage.removeItem(key);}catch{/* Retain the current operation in memory. */}}
  function message(text,action){clearTimeout(timer);panel.replaceChildren();const span=document.createElement('span');span.textContent=text;panel.append(span);panel.hidden=false;if(action){const button=document.createElement('button');button.textContent='Check save';button.onclick=()=>reconcile();panel.append(button);}else if(!pending)timer=setTimeout(()=>panel.hidden=true,8000);}
  async function finish(receipt){remember(null);const label=receipt.toRarity||'Unassigned';try{await refresh(true);message(`${receipt.itemName}: rarity saved as ${label}.`);}catch{message(`Rarity saved as ${label}. Reload the spreadsheet to refresh this view.`);}}
  async function reconcile(reason=''){
    if(!pending)return;
    try{
      const response=await fetch(`/api/rarity-operations/${pending.operationId}`,{cache:'no-store'});
      if(!response.ok)throw Error('Status unavailable');
      const result=await response.json();
      if(result.status==='committed'){await finish(result);return;}
      if(['not-found','not-applied'].includes(result.status)){
        remember(null);await refresh(true);message(reason||'Rarity was not saved. The latest spreadsheet is loaded; try again.');return;
      }
      message('Rarity save is uncertain. Reload and inspect the item before editing again.',true);
      // Explicit inspection permits release without replaying an ambiguous write.
      const inspect=document.createElement('button');inspect.textContent='Reload & inspect';
      inspect.onclick=async()=>{try{await refresh(true);panel.replaceChildren();const text=document.createElement('span');text.textContent='Check this item’s current rarity in the inspector.';panel.append(text);const done=document.createElement('button');done.textContent='Reviewed · enable edits';done.onclick=()=>{remember(null);panel.hidden=true;};panel.append(done);}catch(e){toast(e.message);}};
      panel.append(inspect);
    }catch{message('Connection interrupted. Check whether this rarity saved before editing again.',true);}
  }
  async function save(form){
    if(busy()||pending){toast('Finish the current spreadsheet action before editing a rarity.');return;}
    if(state.owner!=='catalog'||state.view!=='inventory'||!state.rarityEditing)return;
    const item=state.items.find(i=>i['Item ID']===form.dataset.item);
    if(!item)return;
    const rarity=form.querySelector('select').value;if(rarity===(item.Rarity||''))return;
    setBusy(true);document.body.classList.add('category-saving');
    form.querySelectorAll('select,button').forEach(e=>e.disabled=true);
    try{
      await saveDraft();
      remember({itemId:item['Item ID'],fromRarity:(item.Rarity||''),toRarity:rarity,sourceHash:state.sourceHash,operationId:crypto.randomUUID()});
      message('Saving item rarity to spreadsheet…');
      let response,result;
      try{response=await fetch('/api/items/rarity',{method:'POST',headers:{'Content-Type':'application/json','X-Inventory-Token':state.mutationToken||''},body:JSON.stringify(pending)});result=await response.json();}
      catch{await reconcile();return;}
      if(!response.ok){await reconcile(result.error||'Rarity could not be saved.');return;}
      await finish(result);
    }catch(error){if(pending)await reconcile();else message(error.message||'Rarity could not be saved.');}
    finally{setBusy(false);document.body.classList.remove('category-saving');if(form.isConnected){form.querySelector('select').disabled=false;form.querySelector('button').disabled=form.querySelector('select').value===(item.Rarity||'');}}
  }
  document.addEventListener('submit',event=>{const form=event.target.closest('.rarity-edit-form');if(!form)return;event.preventDefault();save(form);});
  document.addEventListener('change',event=>{const form=event.target.closest('.rarity-edit-form');if(form)form.querySelector('button').disabled=event.target.value===form.dataset.current;});
  try{pending=JSON.parse(sessionStorage.getItem(key)||'null');}catch{}
  return {
    pending:()=>Boolean(pending),
    controls(item){
      if(state.owner!=='catalog'||state.view!=='inventory'||!state.rarityEditing)return '';
      const current=item.Rarity||'',choices=state.rarityChoices||[];
      return `<form class="rarity-edit-form" data-item="${esc(item['Item ID'])}" data-current="${esc(current)}"><label>Item rarity<select aria-label="Item rarity"><option value="" ${!current?'selected':''}>Unassigned</option>${current&&!choices.includes(current)?`<option value="${esc(current)}" selected>${esc(current)} (current)</option>`:''}${choices.map(rarity=>`<option value="${esc(rarity)}" ${rarity===current?'selected':''}>${esc(rarity)}</option>`).join('')}</select></label><button type="submit" disabled>Save rarity</button><small>Saved to the master spreadsheet. Rarity is separate from tier.</small></form>`;
    },
    async restore(){if(pending)await reconcile();}
  };
}
