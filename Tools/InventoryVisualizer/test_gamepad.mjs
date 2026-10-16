import test from 'node:test';
import assert from 'node:assert/strict';
import {InventoryPadInput,spatialTarget} from './dist/gamepad-core.mjs';
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
test('bumpers and analog triggers tab independently and never repeat while held',()=>{
  for(const [button,action]of [[4,'previousTab'],[5,'nextTab'],[6,'previousContext'],[7,'nextContext']]){
    const input=armed();assert.deepEqual(input.sample(pad([button]),1),[action]);assert.deepEqual(input.sample(pad([button]),1000),[]);
  }
  const input=armed(),partial=pad();partial.buttons[7]={pressed:true,value:.3};assert.deepEqual(input.sample(partial,1),[]);
  partial.buttons[7].value=.7;assert.deepEqual(input.sample(partial,2),['nextContext']);
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
