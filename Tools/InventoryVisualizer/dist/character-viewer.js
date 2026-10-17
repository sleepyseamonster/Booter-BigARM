// Small local WebGL viewer. Existing project character mesh; no external runtime or world state.
export class StandingCharacterViewer {
  constructor(onInspect){
    this.onInspect=onInspect;this.angle=-18;this.parts=[];this.fitted=new Set();this.selected=null;
    this.canvas=document.createElement('canvas');this.canvas.className='character-canvas';this.canvas.setAttribute('aria-label','Rotatable standing character preview. Drag horizontally to turn; use the rotate buttons for keyboard or controller.');
    this.canvas.addEventListener('pointerdown',e=>{this.drag={x:e.clientX,y:e.clientY,start:e.clientX,angle:this.angle};this.canvas.setPointerCapture(e.pointerId);});
    this.canvas.addEventListener('pointermove',e=>{if(this.drag){this.angle=this.drag.angle+(e.clientX-this.drag.start)*.65;this.draw();}});
    this.canvas.addEventListener('pointerup',e=>{if(this.drag&&Math.hypot(e.clientX-this.drag.x,e.clientY-this.drag.y)<5)this.pick(e);this.drag=null;});
    this.canvas.addEventListener('pointercancel',()=>this.drag=null);
    this.canvas.addEventListener('webglcontextlost',e=>{e.preventDefault();this.fail('3D preview paused. Reload to restore the viewer.');});
    this.resize=new ResizeObserver(()=>this.draw());this.resize.observe(this.canvas);
  }
  mount(host,fitted,selected){this.host=host;this.fitted=new Set(fitted);this.selected=selected;host.prepend(this.canvas);if(this.failure)this.error(this.failure);else if(!this.loading)this.load();this.draw();}
  turn(amount){this.angle=amount===null?-18:this.angle+amount;this.draw();}
  error(text){const status=this.host?.querySelector('.viewer-state');if(status)status.textContent=text;}
  fail(text){this.failure=text;this.ready=false;this.error(text);}
  async load(){
    this.loading=true;
    try{
      const gl=this.canvas.getContext('webgl',{alpha:true,antialias:true,preserveDrawingBuffer:true});if(!gl)throw Error('3D preview unavailable. Equipment slots remain usable.');this.gl=gl;
      const shader=(type,source)=>{const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error('Character shader unavailable.');return s;};
      const program=gl.createProgram();gl.attachShader(program,shader(gl.VERTEX_SHADER,`attribute vec3 position;attribute vec3 normal;uniform float yaw;uniform float aspect;uniform float zoom;varying vec3 N;varying vec3 P;void main(){float c=cos(yaw),s=sin(yaw);mat3 turn=mat3(c,0.,-s,0.,1.,0.,s,0.,c);vec3 p=turn*position;N=turn*normal;P=p;float y=(p.y-.94)*.98-p.z*.10;gl_Position=vec4(p.x/(1.12*aspect)*zoom,y/1.12*zoom,-p.z*.15,1.);}`));
      gl.attachShader(program,shader(gl.FRAGMENT_SHADER,`precision mediump float;varying vec3 N;varying vec3 P;uniform vec3 color;uniform float selected;void main(){vec3 n=normalize(N);float key=max(0.,dot(n,normalize(vec3(-.5,.8,1.))));float rim=pow(1.-abs(n.z),2.)*.28;vec3 lit=color*(.32+key*.88)+vec3(.16,.20,.25)*rim;lit=mix(lit,vec3(.95,.72,.32)*(.65+key*.4),selected*.65);gl_FragColor=vec4(pow(lit,vec3(.86)),1.);}`));
      gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw Error('Character renderer unavailable.');this.program=program;
      this.locations={};for(const n of ['yaw','aspect','zoom','color','selected'])this.locations[n]=gl.getUniformLocation(program,n);
      this.positions=gl.getAttribLocation(program,'position');this.normals=gl.getAttribLocation(program,'normal');
      const [metadataResponse,meshResponse]=await Promise.all([fetch('preview/character.json'),fetch('preview/character.bin')]);
      if(!metadataResponse.ok||!meshResponse.ok)throw Error('Character asset unavailable.');
      const metadata=await metadataResponse.json(),bytes=await meshResponse.arrayBuffer();
      this.buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,this.buffer);gl.bufferData(gl.ARRAY_BUFFER,bytes,gl.STATIC_DRAW);
      this.parts=metadata.parts.map(p=>({...p,buffer:this.buffer}));this.buildGear();this.ready=true;this.draw();
    }catch(error){this.fail(error.message);}
  }
  buildGear(){
    const gl=this.gl,vertices=[],parts=[];
    const box=(part,c,size,color)=>{
      const start=vertices.length/6,faces=[[0,0,1],[0,0,-1],[1,0,0],[-1,0,0],[0,1,0],[0,-1,0]];
      for(const n of faces){const u=n[0]?[0,1,0]:[1,0,0],v=[n[1]*u[2]-n[2]*u[1],n[2]*u[0]-n[0]*u[2],n[0]*u[1]-n[1]*u[0]];
        for(const [a,b] of [[-1,-1],[1,-1],[1,1],[-1,-1],[1,1],[-1,1]])vertices.push(...c.map((x,i)=>x+(n[i]+a*u[i]+b*v[i])*size[i]/2),...n);
      }parts.push({part,first:start,count:vertices.length/6-start,color});
    };
    const cylinder=(part,c,r,h,color)=>{
      const start=vertices.length/6;
      for(let i=0;i<16;i++){const a=i*Math.PI/8,b=(i+1)*Math.PI/8;
        for(const [angle,y]of [[a,-1],[b,-1],[b,1],[a,-1],[b,1],[a,1]])vertices.push(c[0]+Math.cos(angle)*r,c[1]+y*h/2,c[2]+Math.sin(angle)*r,Math.cos(angle),0,Math.sin(angle));
        for(const y of [-1,1])for(const angle of [null,a,b])vertices.push(c[0]+(angle===null?0:Math.cos(angle)*r),c[1]+y*h/2,c[2]+(angle===null?0:Math.sin(angle)*r),0,y,0);
      }parts.push({part,first:start,count:vertices.length/6-start,color});
    };
    const vestStart=vertices.length/6,rings=[[1.06,.15,.105],[1.16,.185,.145],[1.37,.20,.145],[1.47,.16,.10]];
    for(let row=0;row<rings.length-1;row++)for(let i=0;i<20;i++){
      for(const [ring,side] of [[row,i],[row,i+1],[row+1,i+1],[row,i],[row+1,i+1],[row+1,i]]){
        const [y,rx,rz]=rings[ring],a=side*Math.PI/10,nx=Math.cos(a),nz=Math.sin(a);
        vertices.push(nx*rx,y,.035+nz*rz,nx,0,nz);
      }
    }parts.push({part:'equipment.jacket',first:vestStart,count:vertices.length/6-vestStart,color:[.27,.32,.34]});
    box('equipment.jacket',[0,1.10,.12],[.33,.08,.04],[.19,.20,.19]);
    for(const [part,x,color] of [['tool.micro_dust_harvester',.23,[.47,.47,.40]],['equipment.algae_canister',-.23,[.32,.43,.33]]]){
      cylinder(part,[x,.98,.07],.055,.24,color);cylinder(part,[x,1.105,.07],.045,.025,[.60,.59,.50]);
      box(part,[x,.98,.12],[.035,.11,.015],[.16,.22,.21]);
    }
    box('equipment.grappling_hook',[-.22,.88,-.12],[.10,.14,.06],[.43,.44,.40]);
    box('equipment.digging_tool',[.23,1.13,-.17],[.032,.66,.035],[.30,.28,.22]);
    box('equipment.digging_tool',[.23,1.47,-.17],[.20,.07,.055],[.51,.49,.42]);
    const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(vertices),gl.STATIC_DRAW);
    this.parts.push(...parts.map(p=>({...p,buffer})));
  }
  draw(){
    if(!this.ready||!this.canvas.isConnected)return;
    const gl=this.gl,rect=this.canvas.getBoundingClientRect(),dpr=Math.min(devicePixelRatio||1,2);
    const width=Math.max(1,Math.round(rect.width*dpr)),height=Math.max(1,Math.round(rect.height*dpr));
    if(this.canvas.width!==width||this.canvas.height!==height){this.canvas.width=width;this.canvas.height=height;}
    gl.viewport(0,0,width,height);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.enable(gl.DEPTH_TEST);gl.disable(gl.CULL_FACE);gl.useProgram(this.program);
    gl.uniform1f(this.locations.yaw,this.angle*Math.PI/180);gl.uniform1f(this.locations.aspect,width/height);gl.uniform1f(this.locations.zoom,1);
    for(const part of this.parts){
      if(part.part!=='body'&&!this.fitted.has(part.part))continue;
      gl.bindBuffer(gl.ARRAY_BUFFER,part.buffer);gl.vertexAttribPointer(this.positions,3,gl.FLOAT,false,24,0);gl.enableVertexAttribArray(this.positions);gl.vertexAttribPointer(this.normals,3,gl.FLOAT,false,24,12);gl.enableVertexAttribArray(this.normals);
      gl.uniform3fv(this.locations.color,part.color);gl.uniform1f(this.locations.selected,part.part===this.selected?1:0);gl.drawArrays(gl.TRIANGLES,part.first,part.count);
    }
    this.canvas.dataset.angle=String(Math.round((this.angle%360+360)%360));
    this.error('Drag to rotate · Select a slot to inspect gear');
    const angle=this.host.querySelector('.viewer-angle');if(angle)angle.textContent=this.canvas.dataset.angle+'°';
  }
  pick(event){
    const rect=this.canvas.getBoundingClientRect(),yaw=this.angle*Math.PI/180,aspect=rect.width/rect.height;
    const targets=[['equipment.algae_canister',-.23,.98,.07],['tool.micro_dust_harvester',.23,.98,.07],['equipment.grappling_hook',-.22,.88,-.12],['equipment.digging_tool',.23,1.4,-.17],['equipment.jacket',0,1.3,.12],['equipment.cloak',0,1.20,-.24]];
    let closest=null,distance=42;
    for(const [id,x,y,z]of targets){if(!this.fitted.has(id))continue;const px=Math.cos(yaw)*x+Math.sin(yaw)*z,pz=-Math.sin(yaw)*x+Math.cos(yaw)*z;
      const sx=rect.left+(px/(1.12*aspect)+1)*rect.width/2,sy=rect.top+(1-((y-.94)*.98-pz*.10)/1.12)*rect.height/2;
      const d=Math.hypot(sx-event.clientX,sy-event.clientY);if(d<distance){distance=d;closest=id;}
    }
    if(closest)this.onInspect(closest);
  }
}
