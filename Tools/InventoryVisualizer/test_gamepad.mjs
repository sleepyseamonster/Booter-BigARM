import test from 'node:test';
import assert from 'node:assert/strict';
import {InventoryPadInput,spatialTarget,rightStickScroll,revealScroll} from './dist/gamepad-core.mjs';
const pad=(buttons=[],axes=[0,0],overrides={})=>({index:0,id:'test-controller',mapping:'standard',connected:true,axes,buttons:Array.from({length:16},(_,i)=>({pressed:buttons.includes(i),value:buttons.includes(i)?1:0})),...overrides});
function armed(){const input=new InventoryPadInput();assert.deepEqual(input.sample(pad(),0),[]);return input;}
test('D-pad and dominant left stick use the same four navigation actions',()=>{
  for(const [button,direction]of [[12,'up'],[13,'down'],[14,'left'],[15,'right']])assert.deepEqual(armed().sample(pad([button]),1),[direction]);
  for(const [axes,direction]of [[[.9,.2],'right'],[[-.9,0],'left'],[[.1,-.8],'up'],[[0,.8],'down']])assert.deepEqual(armed().sample(pad([],axes),1),[direction]);
  assert.deepEqual(armed().sample(pad([12],[.9,0]),1),['up']);
});
test('stick drift stays neutral; held directions repeat after a delay',()=>{
  const input=armed();assert.deepEqual(input.sample(pad([],[.35,-.45]),1),[]);
  assert.deepEqual(input.sample(pad([13]),10),['down']);
  assert.deepEqual(input.sample(pad([13]),369),[]);assert.deepEqual(input.sample(pad([13]),370),['down']);
  assert.deepEqual(input.sample(pad([13]),494),[]);assert.deepEqual(input.sample(pad([13]),495),['down']);
  assert.deepEqual(input.sample(pad([14]),500),['left']);
});
test('South is one selection per release, including while another control remains held',()=>{
  const input=armed();assert.deepEqual(input.sample(pad([0]),1),['select']);
  assert.deepEqual(input.sample(pad([0]),1000),[]);assert.deepEqual(input.sample(pad([0,13]),1100),[]);
  assert.deepEqual(input.sample(pad([13]),1200),[]);assert.deepEqual(input.sample(pad(),1300),[]);
  assert.deepEqual(input.sample(pad([0]),1400),['select']);
});

test('East backs out once per release and cannot bypass neutral rearming',()=>{
  const input=new InventoryPadInput();
  assert.deepEqual(input.sample(pad([1]),0),[]);
  input.sample(pad(),1);
  assert.deepEqual(input.sample(pad([1]),2),['back']);
  assert.deepEqual(input.sample(pad([1]),500),[]);
  assert.deepEqual(input.sample(pad([1,0]),600),[]);
  input.sample(pad(),700);
  assert.deepEqual(input.sample(pad([1]),800),['back']);
});
test('bumpers and analog triggers tab independently and never repeat while held',()=>{
  for(const [button,action]of [[4,'previousTab'],[5,'nextTab'],[6,'previousTab'],[7,'nextTab']]){
    const input=armed();assert.deepEqual(input.sample(pad([button]),1),[action]);assert.deepEqual(input.sample(pad([button]),1000),[]);
  }
  const input=armed(),partial=pad();partial.buttons[7]={pressed:true,value:.3};assert.deepEqual(input.sample(partial,1),[]);
  partial.buttons[7].value=.7;assert.deepEqual(input.sample(partial,2),['nextTab']);
});
test('connect, focus loss, reconnect and device changes require a neutral frame',()=>{
  const input=new InventoryPadInput();assert.deepEqual(input.sample(pad([0]),1),[]);input.sample(pad(),2);
  assert.deepEqual(input.sample(pad([0]),3),['select']);input.sample(pad(),4);
  assert.deepEqual(input.sample(pad([0]),5,false),[]);assert.deepEqual(input.sample(pad([0]),6),[]);
  input.sample(pad(),7);assert.deepEqual(input.sample(pad([0]),8),['select']);
  input.sample(null,9);assert.deepEqual(input.sample(pad([0]),10),[]);
  input.sample(pad(),11);assert.deepEqual(input.sample(pad([0],[0,0],{id:'replacement'}),12),[]);
});
test('unmapped, disconnected and malformed-axis devices cannot issue unsafe input',()=>{
  assert.deepEqual(armed().sample(pad([0],[0,0],{mapping:''}),1),[]);
  assert.deepEqual(armed().sample(pad([0],[0,0],{connected:false}),1),[]);
  assert.deepEqual(armed().sample(pad([],[NaN,Infinity]),1),[]);
});
test('spatial navigation respects grid rows and crosses to another pane',()=>{
  const rects=[{x:50,y:50,width:80,height:80},{x:150,y:50,width:80,height:80},{x:50,y:150,width:80,height:80},{x:150,y:150,width:80,height:80},{x:350,y:70,width:80,height:40}];
  assert.equal(spatialTarget(rects,0,'right'),1);assert.equal(spatialTarget(rects,0,'down'),2);
  assert.equal(spatialTarget(rects,3,'up'),1);assert.equal(spatialTarget(rects,1,'right'),4);
  assert.equal(spatialTarget(rects,0,'left'),-1);assert.equal(spatialTarget([],0,'down'),-1);
});
test('navigation includes controls below the viewport rather than stopping at the screen edge',()=>{
  const rows=Array.from({length:30},(_,i)=>({x:100,y:50+i*90,width:180,height:70}));
  assert.equal(spatialTarget(rows,7,'down'),8);assert.equal(spatialTarget(rows,28,'down'),29);
});
test('focus reveal accounts for the sticky footer, top margin and oversized controls',()=>{
  assert.equal(revealScroll({top:620,bottom:700,height:80},720,80),76);
  assert.equal(revealScroll({top:10,bottom:70,height:60},720,80),-6);
  assert.equal(revealScroll({top:100,bottom:180,height:80},720,80),0);
  assert.equal(revealScroll({top:900,bottom:1800,height:900},720,80),884);
  assert.equal(revealScroll({top:35,bottom:95,height:60},720,80,65),-46);
});
test('right stick scroll is proportional, frame-rate independent and drift-free',()=>{
  assert.deepEqual(rightStickScroll(pad([],[0,0,.2,-.2]),16),{x:0,y:0});
  const down=rightStickScroll(pad([],[0,0,0,1]),20);
  assert.equal(down.y,17);assert.equal(down.x,0);
  assert.equal(rightStickScroll(pad([],[0,0,0,1]),10).y*2,down.y);
  assert.ok(rightStickScroll(pad([],[0,0,0,.6]),20).y<down.y);
  assert.equal(rightStickScroll(pad([],[0,0,-1,-1]),20).x,-17);
  assert.equal(rightStickScroll(pad([],[0,0,0,1]),5000).y,42.5);
  assert.deepEqual(rightStickScroll(pad([],[0,0,NaN,Infinity]),20),{x:0,y:0});
  assert.deepEqual(rightStickScroll(pad([],[0,0,0,1]),20,false),{x:0,y:0});
});
test('right stick held on entry cannot bypass the neutral input barrier',()=>{
  const input=new InventoryPadInput();input.sample(pad([],[0,0,0,1]),0);assert.equal(input.armed,false);
  input.sample(pad(),1);assert.equal(input.armed,true);
  input.sample(pad([0]),2);assert.equal(input.armed,false);
  input.sample(pad([],[0,0,0,1]),3);assert.equal(input.armed,false);
});
