// Catalogue editing only. Physical inventory transfers live in the separate game study.
export function installCategoryMoves({state, esc, refresh, toast, busy, setBusy, saveDraft}) {
  let dragging=null, suppressClickUntil=0, pending=null, undo=null;
  const pendingKey='arc-dust-category-pending', undoKey='arc-dust-category-undo';
  const panel=document.createElement('div');panel.className='category-feedback';panel.hidden=true;
  panel.setAttribute('role','status');panel.setAttribute('aria-live','polite');document.body.append(panel);
  function remember(key,value){try{value?sessionStorage.setItem(key,JSON.stringify(value)):sessionStorage.removeItem(key);}catch{/* Current session still retains the operation. */}}
  function message(text,button,action){panel.replaceChildren();const span=document.createElement('span');span.textContent=text;panel.append(span);if(button){const b=document.createElement('button');b.textContent=button;b.onclick=action;panel.append(b);}panel.hidden=false;}
  function clearDrag(){dragging=null;state.categoryDrag=false;document.body.classList.remove('category-dragging');document.querySelectorAll('.drop-target,.drag-source').forEach(e=>e.classList.remove('drop-target','drag-source'));if(!busy()&&!pending){panel.hidden=true;if(undo)showUndo();}}
  function showUndo(){if(undo)message(`${undo.itemName} moved to ${undo.toCategory}.`,'Undo',()=>run({itemId:undo.itemId,fromCategory:undo.toCategory,toCategory:undo.fromCategory,undoOf:undo.operationId}));}
  function saveUndo(receipt){undo=receipt.noop||receipt.undoOf?null:receipt;remember(undoKey,undo);if(undo)showUndo();else message(receipt.noop?'Already in that category.':`${receipt.itemName} moved back to ${receipt.toCategory}.`);}
  async function finish(receipt){pending=null;remember(pendingKey,null);saveUndo(receipt);try{await refresh(true);}catch{message('Category saved. Reload the spreadsheet to refresh this view.','Reload',()=>refresh(true).catch(e=>toast(e.message)));}}
  async function reconcile(reason=''){
    if(!pending)return;
    try{
      const response=await fetch(`/api/category-operations/${pending.operationId}`,{cache:'no-store'});
      if(!response.ok)throw Error('Save status unavailable.');
      const result=await response.json();
      if(result.status==='committed'){await finish(result);return;}
      if(['not-found','not-applied'].includes(result.status)){pending=null;remember(pendingKey,null);await refresh(true);message(reason||'Move was not saved. The latest spreadsheet is loaded; try the move again.');return;}
      message('Save outcome is uncertain. Reload and inspect the item before another move.','Reload & inspect',async()=>{
        try{await refresh(true);message('Review the item’s current category in the inspector.','Reviewed · enable moves',()=>{pending=null;remember(pendingKey,null);panel.hidden=true;});}catch(e){toast(e.message);}
      });
    }catch{message('Connection interrupted. Check whether the category was saved before another move.','Check save',()=>reconcile());}
  }
  async function run(move){
    if(busy()||pending){toast('Finish the current spreadsheet action before moving another item.');return;}
    if(state.owner!=='catalog'||state.view!=='inventory'||!state.categoryEditing){toast('Open Inventory → Item library to move or undo a category.');return;}
    if(move.fromCategory===move.toCategory)return;
    setBusy(true);document.body.classList.add('category-saving');
    try{
      await saveDraft();
      // Undo uses the latest revision but requires the original move's destination.
      if(move.undoOf){const r=await fetch('/api/revision',{cache:'no-store'});if(!r.ok)throw Error('Spreadsheet unavailable.');state.sourceHash=(await r.json()).sha256;}
      pending={...move,sourceHash:state.sourceHash,operationId:crypto.randomUUID()};remember(pendingKey,pending);
      message('Saving category to spreadsheet…');
      let response;
      try{response=await fetch('/api/items/category',{method:'POST',headers:{'Content-Type':'application/json','X-Inventory-Token':state.mutationToken||''},body:JSON.stringify(pending)});}
      catch{await reconcile();return;}
      let result;try{result=await response.json();}catch{await reconcile();return;}
      if(!response.ok){await reconcile(result.error||'Category could not be saved.');return;}
      await finish(result);
    }catch(error){if(pending)await reconcile();else message(error.message||'Category could not be saved.');}
    finally{setBusy(false);document.body.classList.remove('category-saving');}
  }
  document.addEventListener('submit',event=>{
    const form=event.target.closest('.category-move-form');if(!form)return;event.preventDefault();
    const item=state.items.find(i=>i['Item ID']===form.dataset.item);
    if(item)run({itemId:item['Item ID'],fromCategory:item.Category,toCategory:form.querySelector('select').value});
  });
  document.querySelector('#items').addEventListener('click',event=>{if(Date.now()<suppressClickUntil){event.preventDefault();event.stopImmediatePropagation();}},true);
  document.querySelector('#items').addEventListener('dragstart',event=>{
    const card=event.target.closest('.item-card');
    if(!card||event.target.closest('.item-controls')||busy()||pending||state.owner!=='catalog'||state.view!=='inventory'||!state.categoryEditing){event.preventDefault();return;}
    const item=state.items.find(i=>i['Item ID']===card.dataset.moveItem);if(!item){event.preventDefault();return;}
    dragging={itemId:item['Item ID'],fromCategory:item.Category,name:item['Item name']};state.categoryDrag=true;
    event.dataTransfer.effectAllowed='move';event.dataTransfer.setData('application/x-arc-dust-item',dragging.itemId);
    event.dataTransfer.setData('text/plain',dragging.name);card.classList.add('drag-source');document.body.classList.add('category-dragging');
    message(`Move ${dragging.name}: drop onto a category on the left. Escape cancels.`);
  });
  document.addEventListener('dragover',event=>{
    if(!dragging)return;
    const button=event.target.closest('#categories [data-category]');
    document.querySelectorAll('.drop-target').forEach(e=>{if(e!==button)e.classList.remove('drop-target');});
    if(button&&button.dataset.category&&button.dataset.category!==dragging.fromCategory){event.preventDefault();event.dataTransfer.dropEffect='move';button.classList.add('drop-target');message(`Move ${dragging.name} to ${button.dataset.category}`);}
    else event.dataTransfer.dropEffect='none';
    // The rail is part of the scrolling page; also support its narrow-screen horizontal scroller.
    if(event.clientY<65)window.scrollBy(0,-18);else if(event.clientY>window.innerHeight-65)window.scrollBy(0,18);
    const rail=document.querySelector('#categories');const box=rail.getBoundingClientRect();
    if(event.clientY>=box.top&&event.clientY<=box.bottom&&rail.scrollWidth>rail.clientWidth){if(event.clientX<box.left+35)rail.scrollLeft-=16;else if(event.clientX>box.right-35)rail.scrollLeft+=16;}
  });
  document.addEventListener('drop',event=>{
    if(!dragging)return;event.preventDefault();const button=event.target.closest('#categories [data-category]');const move=dragging;
    const target=button?.dataset.category;clearDrag();suppressClickUntil=Date.now()+400;
    if(target&&target!==move.fromCategory)run({itemId:move.itemId,fromCategory:move.fromCategory,toCategory:target});
  });
  document.addEventListener('dragend',()=>{if(dragging){suppressClickUntil=Date.now()+400;clearDrag();}});
  window.addEventListener('blur',()=>{if(dragging)clearDrag();});
  document.addEventListener('keydown',event=>{if(event.key==='Escape'&&dragging){event.preventDefault();event.stopImmediatePropagation();clearDrag();}},true);
  try{pending=JSON.parse(sessionStorage.getItem(pendingKey)||'null');undo=JSON.parse(sessionStorage.getItem(undoKey)||'null');}catch{pending=null;undo=null;}
  return {
    pending(){return Boolean(pending);},
    controls(item){return state.owner==='catalog'&&state.view==='inventory'&&state.categoryEditing?`<form class="category-move-form" data-item="${esc(item['Item ID'])}"><label>Move to category<select aria-label="Destination category">${(state.categories||[]).map(c=>`<option value="${esc(c)}" ${c===item.Category?'selected':''}>${esc(c)}</option>`).join('')}</select></label><button type="submit">Move item</button><small>Updates the master spreadsheet. Kept items can be moved.</small></form>`:'';},
    async restore(){if(pending)await reconcile();else if(undo)showUndo();}
  };
}
