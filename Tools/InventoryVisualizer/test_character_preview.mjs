import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const directory=new URL('./dist/preview/',import.meta.url);
const metadata=JSON.parse(readFileSync(new URL('character.json',directory),'utf8'));
const mesh=readFileSync(new URL('character.bin',directory));
test('preview mesh has valid finite geometry and complete triangle ranges',()=>{
  assert.equal(metadata.version,1);assert.equal(metadata.stride,24);assert.equal(mesh.length,metadata.vertices*24);
  let end=0;
  for(const part of metadata.parts){assert.equal(part.first,end);assert.equal(part.count%3,0);assert.ok(part.count>0);end+=part.count;}
  assert.equal(end,metadata.vertices);
  for(let offset=0;offset<mesh.length;offset+=4)assert.ok(Number.isFinite(mesh.readFloatLE(offset)));
});
test('standing preview has both arms lowered, is centered, and has a 1.8m inspection height',()=>{
  const body=metadata.parts.find(p=>p.part==='body'),low=[Infinity,Infinity,Infinity],high=[-Infinity,-Infinity,-Infinity];
  for(let i=body.first;i<body.first+body.count;i++)for(let axis=0;axis<3;axis++){const value=mesh.readFloatLE(i*24+axis*4);low[axis]=Math.min(low[axis],value);high[axis]=Math.max(high[axis],value);}
  assert.ok(high[0]-low[0]<.9,'T-pose or displaced arm returned');
  assert.ok(Math.abs(high[0]+low[0])<.03,'Character moved off its gear origin');
  assert.ok(Math.abs(high[1]-low[1]-1.8)<.01);assert.ok(Math.abs(low[1])<.01);
  assert.ok(metadata.parts.some(p=>p.part==='equipment.cloak'));
});
test('browser export provenance matches the original source assets',()=>{
  const root=new URL('../../',import.meta.url);
  for(const source of metadata.sourceFiles){const bytes=readFileSync(fileURLToPath(new URL(source.path,root)));assert.equal(createHash('sha256').update(bytes).digest('hex'),source.sha256);}
});
