// Item name edits persist to the master catalog workbook.
export function installNameEditor({state,esc,refresh,toast,busy,setBusy,saveDraft}) {
  let pending=null;
  const key='arc-dust-name-pending';
  const panel=document.createElement('div');panel.className='category-feedback name-feedback';panel.hidden=true;
  panel.setAttribute('role','status');panel.setAttribute('aria-live','polite');document.body.append(panel);
  let timer;
  function remember(value){pending=value;try{value?sessionStorage.setItem(key,JSON.stringify(value)):sessionStorage.removeItem(key);}catch{/* Retain the current operation in memory. */}}
  function message(text,action){clearTimeout(timer);panel.replaceChildren();const span=document.createElement('span');span.textContent=text;panel.append(span);panel.hidden=false;if(action){const button=document.createElement('button');button.textContent='Check save';button.onclick=()=>reconcile();panel.append(button);}else if(!pending)timer=setTimeout(()=>panel.hidden=true,8000);}
  async function finish(receipt){remember(null);const label=receipt.toName;try{await refresh(true);message(`Item renamed to ${label}.`);}catch{message(`Name saved as ${label}. Reload the spreadsheet to refresh this view.`);}}
  async function reconcile(reason=''){
    if(!pending)return;
    try{
      const response=await fetch(`/api/name-operations/${pending.operationId}`,{cache:'no-store'});
      if(!response.ok)throw Error('Status unavailable');
      const result=await response.json();
      if(result.status==='committed'){await finish(result);return;}
      if(['not-found','not-applied'].includes(result.status)){
        remember(null);await refresh(true);message(reason||'Name was not saved. The latest spreadsheet is loaded; try again.');return;
      }
      message('Name save is uncertain. Reload and inspect the item before editing again.',true);
      // Explicit inspection permits release without replaying an ambiguous write.
      const inspect=document.createElement('button');inspect.textContent='Reload & inspect';
      inspect.onclick=async()=>{try{await refresh(true);panel.replaceChildren();const text=document.createElement('span');text.textContent='Check this item’s current name in the inspector.';panel.append(text);const done=document.createElement('button');done.textContent='Reviewed · enable edits';done.onclick=()=>{remember(null);panel.hidden=true;};panel.append(done);}catch(e){toast(e.message);}};
      panel.append(inspect);
    }catch{message('Connection interrupted. Check whether this name saved before editing again.',true);}
  }
  async function save(form){
    if(busy()||pending){toast('Finish the current spreadsheet action before editing a name.');return;}
    if(state.owner!=='catalog'||state.view!=='inventory'||!state.nameEditing)return;
    const item=state.items.find(i=>i['Item ID']===form.dataset.item);
    if(!item)return;
    const name=form.querySelector('textarea').value.trim();if(!name||name===item['Item name'])return;
    setBusy(true);document.body.classList.add('category-saving');
    form.querySelectorAll('textarea,button').forEach(e=>e.disabled=true);
    try{
      await saveDraft();
      remember({itemId:item['Item ID'],fromName:item['Item name'],toName:name,sourceHash:state.sourceHash,operationId:crypto.randomUUID()});
      message('Saving item name to spreadsheet…');
      let response,result;
      try{response=await fetch('/api/items/name',{method:'POST',headers:{'Content-Type':'application/json','X-Inventory-Token':state.mutationToken||''},body:JSON.stringify(pending)});result=await response.json();}
      catch{await reconcile();return;}
      if(!response.ok){await reconcile(result.error||'Name could not be saved.');return;}
      await finish(result);
    }catch(error){if(pending)await reconcile();else message(error.message||'Name could not be saved.');}
    finally{setBusy(false);document.body.classList.remove('category-saving');if(form.isConnected){form.querySelector('textarea').disabled=false;form.querySelector('button').disabled=form.querySelector('textarea').value===item['Item name'];}}
  }
  document.addEventListener('keydown',event=>{const form=event.target.closest('.name-edit-form');if(form&&event.key==='Enter'){event.preventDefault();if(!form.querySelector('button').disabled)form.requestSubmit();}});
  document.addEventListener('submit',event=>{const form=event.target.closest('.name-edit-form');if(!form)return;event.preventDefault();save(form);});
  document.addEventListener('input',event=>{const form=event.target.closest('.name-edit-form');if(form)form.querySelector('button').disabled=!event.target.value.trim()||event.target.value.trim()===form.dataset.current;});
  try{pending=JSON.parse(sessionStorage.getItem(key)||'null');}catch{}
  return {
    pending:()=>Boolean(pending),
    dirty:()=>{const form=document.querySelector('.name-edit-form');return Boolean(form&&form.querySelector('textarea').value.trim()!==form.dataset.current);},
    controls(item){
      if(state.owner!=='catalog'||state.view!=='inventory'||!state.nameEditing)return `<h2>${esc(item['Item name'])}</h2>`;
      const current=item['Item name'];
      return `<form class="name-edit-form" data-item="${esc(item['Item ID'])}" data-current="${esc(current)}"><label for="item-name">Item name</label><textarea id="item-name" name="itemName" rows="2" required maxlength="200" autocomplete="off">${esc(current)}</textarea><button type="submit" disabled>Save name</button><small>Saved to the master spreadsheet.</small></form>`;
    },
    async restore(){if(pending)await reconcile();}
  };
}
