// Material tier edits persist to the same catalog workbook used by every view.
export function installTierEditor({state,esc,refresh,toast,busy,setBusy,saveDraft}) {
  let pending=null;
  const key='arc-dust-tier-pending';
  const panel=document.createElement('div');panel.className='category-feedback tier-feedback';panel.hidden=true;
  panel.setAttribute('role','status');panel.setAttribute('aria-live','polite');document.body.append(panel);
  let timer;
  function remember(value){pending=value;try{value?sessionStorage.setItem(key,JSON.stringify(value)):sessionStorage.removeItem(key);}catch{/* Retain the current operation in memory. */}}
  function message(text,action){clearTimeout(timer);panel.replaceChildren();const span=document.createElement('span');span.textContent=text;panel.append(span);panel.hidden=false;if(action){const button=document.createElement('button');button.textContent='Check save';button.onclick=()=>reconcile();panel.append(button);}else if(!pending)timer=setTimeout(()=>panel.hidden=true,8000);}
  async function finish(receipt){remember(null);try{await refresh(true);message(`${receipt.itemName}: tier saved as ${receipt.toTier}.`);}catch{message(`Tier saved as ${receipt.toTier}. Reload the spreadsheet to refresh this view.`);}}
  async function reconcile(reason=''){
    if(!pending)return;
    try{
      const response=await fetch(`/api/tier-operations/${pending.operationId}`,{cache:'no-store'});
      if(!response.ok)throw Error('Status unavailable');
      const result=await response.json();
      if(result.status==='committed'){await finish(result);return;}
      if(['not-found','not-applied'].includes(result.status)){
        remember(null);await refresh(true);message(reason||'Tier was not saved. The latest spreadsheet is loaded; try again.');return;
      }
      message('Tier save is uncertain. Reload and inspect the item before editing again.',true);
      // Explicit inspection permits release without replaying an ambiguous write.
      const inspect=document.createElement('button');inspect.textContent='Reload & inspect';
      inspect.onclick=async()=>{try{await refresh(true);panel.replaceChildren();const text=document.createElement('span');text.textContent='Check this item’s current tier in the inspector.';panel.append(text);const done=document.createElement('button');done.textContent='Reviewed · enable edits';done.onclick=()=>{remember(null);panel.hidden=true;};panel.append(done);}catch(e){toast(e.message);}};
      panel.append(inspect);
    }catch{message('Connection interrupted. Check whether this tier saved before editing again.',true);}
  }
  async function save(form){
    if(busy()||pending){toast('Finish the current spreadsheet action before editing a tier.');return;}
    if(state.owner!=='catalog'||state.view!=='inventory'||!state.tierEditing)return;
    const item=state.items.find(i=>i['Item ID']===form.dataset.item);
    if(!item)return;
    const tier=form.querySelector('select').value;if(tier===item.Tier)return;
    setBusy(true);document.body.classList.add('category-saving');
    form.querySelectorAll('select,button').forEach(e=>e.disabled=true);
    try{
      await saveDraft();
      remember({itemId:item['Item ID'],fromTier:item.Tier,toTier:tier,sourceHash:state.sourceHash,operationId:crypto.randomUUID()});
      message('Saving item tier to spreadsheet…');
      let response,result;
      try{response=await fetch('/api/items/tier',{method:'POST',headers:{'Content-Type':'application/json','X-Inventory-Token':state.mutationToken||''},body:JSON.stringify(pending)});result=await response.json();}
      catch{await reconcile();return;}
      if(!response.ok){await reconcile(result.error||'Tier could not be saved.');return;}
      await finish(result);
    }catch(error){if(pending)await reconcile();else message(error.message||'Tier could not be saved.');}
    finally{setBusy(false);document.body.classList.remove('category-saving');if(form.isConnected){form.querySelector('select').disabled=false;form.querySelector('button').disabled=form.querySelector('select').value===item.Tier;}}
  }
  document.addEventListener('submit',event=>{const form=event.target.closest('.tier-edit-form');if(!form)return;event.preventDefault();save(form);});
  document.addEventListener('change',event=>{const form=event.target.closest('.tier-edit-form');if(form)form.querySelector('button').disabled=event.target.value===form.dataset.current;});
  try{pending=JSON.parse(sessionStorage.getItem(key)||'null');}catch{}
  return {
    pending:()=>Boolean(pending),
    controls(item){return state.owner==='catalog'&&state.view==='inventory'&&state.tierEditing?`<form class="tier-edit-form" data-item="${esc(item['Item ID'])}" data-current="${esc(item.Tier)}"><label>Item tier<select aria-label="Item tier">${(state.tierChoices||[]).map(t=>`<option value="${esc(t)}" ${t===item.Tier?'selected':''}>${esc(t)}</option>`).join('')}</select></label><button type="submit" disabled>Save tier</button><small>Saved to the master spreadsheet. Tier is separate from rarity.</small></form>`:'';},
    async restore(){if(pending)await reconcile();}
  };
}
