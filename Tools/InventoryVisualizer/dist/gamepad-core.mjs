// Standard Gamepad mapping: South 0, East/back 1, bumpers 4/5, triggers 6/7, D-pad 12–15.
// Kept free of DOM and browser globals so input timing and safety can be verified.
export class InventoryPadInput {
  constructor(){this.reset();}
  reset(){this.device=null;this.armed=false;this.direction=null;this.nextRepeat=0;this.held={};}
  sample(pad,now,active=true){
    if(!active||!pad||pad.connected===false||pad.mapping!=='standard'){this.reset();return [];}
    const device=`${pad.index}:${pad.id}`;
    if(this.device!==device){this.reset();this.device=device;}
    const pressed=i=>pad.buttons?.[i]?.pressed===true||(pad.buttons?.[i]?.value??0)>.55;
    const trigger=i=>(pad.buttons?.[i]?.value??(pad.buttons?.[i]?.pressed?1:0))>.55;
    const dx=Number(pressed(15))-Number(pressed(14)),dy=Number(pressed(13))-Number(pressed(12));
    const x=Number.isFinite(pad.axes?.[0])?pad.axes[0]:0,y=Number.isFinite(pad.axes?.[1])?pad.axes[1]:0;
    let direction=null;
    if(dx||dy)direction=dy?(dy<0?'up':'down'):(dx<0?'left':'right');
    else if(Math.max(Math.abs(x),Math.abs(y))>.55)direction=Math.abs(x)>Math.abs(y)?(x<0?'left':'right'):(y<0?'up':'down');
    const held={select:pressed(0),back:pressed(1),previousTab:pressed(4)||trigger(6),nextTab:pressed(5)||trigger(7)};
    const right=rightStickScroll(pad,16);
    const neutral=!direction&&!Object.values(held).some(Boolean)&&!right.x&&!right.y;
    if(!this.armed){if(neutral)this.armed=true;this.held=held;this.direction=null;return [];}
    const edge=Object.keys(held).find(key=>held[key]&&!this.held[key]);
    this.held=held;
    if(edge){this.armed=false;this.direction=null;return [edge];}
    if(direction!==this.direction){this.direction=direction;this.nextRepeat=now+360;return direction?[direction]:[];}
    if(direction&&now>=this.nextRepeat){this.nextRepeat=now+125;return [direction];}
    return [];
  }
}

// Frame-rate independent scrolling; a small dead zone removes right-stick drift.
export function rightStickScroll(pad,deltaMs,active=true){
  if(!active||!pad||pad.connected===false||pad.mapping!=='standard')return {x:0,y:0};
  const axis=i=>{
    const value=Number.isFinite(pad.axes?.[i])?Math.max(-1,Math.min(1,pad.axes[i])):0;
    return Math.abs(value)<=.22?0:Math.sign(value)*(Math.abs(value)-.22)/.78;
  };
  const seconds=Math.max(0,Math.min(50,Number.isFinite(deltaMs)?deltaMs:0))/1000;
  return {x:axis(2)*850*seconds,y:axis(3)*850*seconds};
}

export function revealScroll(rect,viewportHeight,footerHeight=0,headerHeight=0){
  const top=headerHeight+16,bottom=Math.max(top+1,viewportHeight-footerHeight-16);
  if(rect.height>bottom-top)return rect.top-top;
  if(rect.bottom>bottom)return rect.bottom-bottom;
  if(rect.top<top)return rect.top-top;
  return 0;
}

export function spatialTarget(rects,current,direction){
  const origin=rects[current];if(!origin)return rects.length?0:-1;
  const horizontal=direction==='left'||direction==='right',sign=direction==='left'||direction==='up'?-1:1;
  let best=-1,score=Infinity;
  for(let i=0;i<rects.length;i++){
    if(i===current)continue;
    const r=rects[i],dx=r.x-origin.x,dy=r.y-origin.y;
    const forward=(horizontal?dx:dy)*sign,side=Math.abs(horizontal?dy:dx);
    if(forward<3)continue;
    // Prefer the same row/column; permit crossing panes when no aligned control exists.
    const aligned=side<Math.max(14,(horizontal?(origin.height+r.height):(origin.width+r.width))*.35);
    const candidate=forward+side*3+(aligned?0:1000);
    if(candidate<score){score=candidate;best=i;}
  }
  return best;
}
