import {InventoryPadInput,spatialTarget} from './gamepad-core.mjs';

export function startInventoryGamepad(){
  const input=new InventoryPadInput(),hint=document.querySelector('#pad-status');
  let lastFocus=null,editing=null,focusKey=null,running=true,frame;
  const controls='button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),a[href]';
  const scope=()=>document.querySelector('dialog[open]')||document.querySelector('.shell');
  const visible=el=>el.getClientRects().length>0&&!el.closest('[hidden]')&&getComputedStyle(el).visibility!=='hidden';
  const candidates=()=>[...scope().querySelectorAll(controls)].filter(visible);
  const key=el=>el?.id?`#${CSS.escape(el.id)}`:['entry','owner','page','recipe','action','transfer','menu','near','project','radial','setting','reference','craftLink','remove'].map(k=>el?.dataset[k]!==undefined?`[data-${k.replace(/[A-Z]/g,c=>'-'+c.toLowerCase())}="${CSS.escape(el.dataset[k])}"]`:null).find(Boolean);
  function focus(el){
    if(!el)return;
    if(lastFocus===el&&document.activeElement===el){el.classList.add('pad-focus');return;}
    lastFocus?.classList.remove('pad-focus');lastFocus=el;focusKey=key(el);
    el.classList.add('pad-focus');el.focus({preventScroll:true});el.scrollIntoView({block:'nearest',inline:'nearest'});
  }
  function current(){
    const items=candidates();
    const retained=focusKey&&scope().querySelector(focusKey);
    const target=items.includes(document.activeElement)?document.activeElement:retained&&items.includes(retained)?retained:items.find(el=>el.matches('[data-entry],.kit-slot:not(:disabled),.recipe-card,[data-menu="resume"]'))||items[0];
    if(editing&&!editing.isConnected){editing=target?.matches('select,input[type="number"]')?target:null;editing?.classList.add('pad-editing');}
    focus(target);return target;
  }
  function navigate(direction){
    const items=candidates(),active=current();if(!active)return;
    if(editing===active){
      if(direction==='up'||direction==='down'){
        const delta=direction==='up'?-1:1;
        if(active.tagName==='SELECT'){
          const enabled=[...active.options].filter(o=>!o.disabled),at=enabled.indexOf(active.selectedOptions[0]);
          const option=enabled[Math.max(0,Math.min(enabled.length-1,at+delta))];
          if(option&&option.index!==active.selectedIndex){active.selectedIndex=option.index;active.dispatchEvent(new Event('change',{bubbles:true}));}
        }else if(active.type==='number'){
          if(delta>0)active.stepUp();else active.stepDown();active.dispatchEvent(new Event('change',{bubbles:true}));
        }
      }
      return;
    }
    const rects=items.map(el=>{const r=el.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2,width:r.width,height:r.height};});
    const next=spatialTarget(rects,items.indexOf(active),direction);if(next>=0)focus(items[next]);
  }
  function select(){
    const active=current();if(!active)return;
    if(active.tagName==='SELECT'||active.type==='number'){
      editing=editing===active?null:active;
      active.classList.toggle('pad-editing',editing===active);return;
    }
    if(active.matches('input[type="search"],input[type="text"],textarea')){active.focus();hint.textContent='Search text uses your keyboard · D-pad / stick returns to navigation';return;}
    editing=null;active.click();
    // Existing item commands may rebuild their controls or open a modal.
    current();
  }
  function tab(action){
    if(document.querySelector('dialog[open]'))return;
    editing?.classList.remove('pad-editing');editing=null;
    const main=action.endsWith('Tab'),selector=main?'nav [data-page]':'.rail [data-owner],.rail [data-recipe]';
    const items=[...document.querySelectorAll(selector)].filter(visible);if(!items.length)return;
    const at=items.findIndex(el=>el.classList.contains('active')),delta=action.startsWith('previous')?-1:1;
    const next=items[(Math.max(0,at)+delta+items.length)%items.length];next.click();
    const replacement=key(next);focus(replacement?document.querySelector(replacement):next);
  }
  function isActive(){
    if(document.visibilityState!=='visible')return false;
    try{return window.parent===window?document.hasFocus():window.parent.document.hasFocus()&&visible(window.frameElement);}
    catch{return document.hasFocus();}
  }
  function tick(now){
    if(!running)return;
    try{
      const pads=navigator.getGamepads?.()||[],available=[...pads].filter(p=>p&&p.connected!==false);
      const pad=available.find(p=>`${p.index}:${p.id}`===input.device)||available.find(p=>p.mapping==='standard');
      const active=isActive();
      if(!pad){input.reset();hint.textContent=available.length?'Controller mapping unsupported · use a standard-mapped controller':'Connect a controller, then press a button';nativeInput();}
      else{
        const actions=input.sample(pad,now,active);
        if(active){
          hint.textContent=input.armed?'D-pad / LS move · South select · LB/RB tabs · LT/RT context':'Release controls to continue';
          if(document.body.classList.contains('pad-mode')){
            // Recover logical focus after a view rebuild; keep modal focus trapped.
            current();
          }
          for(const action of actions){document.body.classList.add('pad-mode');if(['up','down','left','right'].includes(action))navigate(action);else if(action==='select')select();else tab(action);}
        }else{input.reset();nativeInput();}
      }
    }catch{input.reset();nativeInput();hint.textContent='Controller access unavailable in this browser';}
    frame=requestAnimationFrame(tick);
  }
  // Pointer/keyboard interaction keeps native behaviour and relinquishes controller focus.
  function nativeInput(){document.body.classList.remove('pad-mode');lastFocus?.classList.remove('pad-focus');editing?.classList.remove('pad-editing');editing=null;focusKey=null;}
  document.addEventListener('pointerdown',nativeInput);document.addEventListener('keydown',nativeInput);
  frame=requestAnimationFrame(tick);
  return ()=>{running=false;cancelAnimationFrame(frame);input.reset();document.removeEventListener('pointerdown',nativeInput);document.removeEventListener('keydown',nativeInput);nativeInput();};
}
