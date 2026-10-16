// Standard Gamepad mapping: South 0, bumpers 4/5, triggers 6/7, D-pad 12–15.
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
    const held={select:pressed(0),previousTab:pressed(4),nextTab:pressed(5),previousContext:trigger(6),nextContext:trigger(7)};
    const neutral=!direction&&!Object.values(held).some(Boolean);
    if(!this.armed){if(neutral)this.armed=true;this.held=held;this.direction=null;return [];}
    const edge=Object.keys(held).find(key=>held[key]&&!this.held[key]);
    this.held=held;
    if(edge){this.armed=false;this.direction=null;return [edge];}
    if(direction!==this.direction){this.direction=direction;this.nextRepeat=now+360;return direction?[direction]:[];}
    if(direction&&now>=this.nextRepeat){this.nextRepeat=now+125;return [direction];}
    return [];
  }
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
