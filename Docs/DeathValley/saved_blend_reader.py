"""Read-only recovery of packed images and mesh attributes from Blender 5.2 files.

Deliberately bounded to the saved study's modern little-endian format. This is
binary extraction, not Blender execution or a general scene evaluator.
Format reference: https://developer.blender.org/docs/release_notes/5.0/core/
"""
import ctypes
import os
import re
import struct
from pathlib import Path


def decompress(data):
    if data.startswith(b'BLENDER'):
        return data
    if not data.startswith(b'\x28\xb5\x2f\xfd'):
        raise ValueError('Unsupported Blender compression')
    # Reuse the pinned rasterio dependency; no package installation is needed.
    import rasterio
    folder = Path(rasterio.__file__).parent.parent/'rasterio.libs'
    libraries = list(folder.glob('zstd-*.dll'))
    if os.name != 'nt' or len(libraries) != 1:
        raise ValueError('Compressed recovery needs the pinned Windows rasterio Zstd library')
    with os.add_dll_directory(str(folder)):
        library = ctypes.CDLL(str(libraries[0]))
        for name in ('ZSTD_findFrameCompressedSize','ZSTD_getFrameContentSize'):
            function=getattr(library,name)
            function.argtypes=[ctypes.c_void_p,ctypes.c_size_t];function.restype=ctypes.c_size_t
        library.ZSTD_isError.argtypes=[ctypes.c_size_t];library.ZSTD_isError.restype=ctypes.c_uint
        library.ZSTD_decompress.argtypes=[ctypes.c_void_p,ctypes.c_size_t,ctypes.c_void_p,ctypes.c_size_t]
        library.ZSTD_decompress.restype=ctypes.c_size_t
        source=ctypes.create_string_buffer(data);offset=0;parts=[];total=0
        while offset<len(data):
            pointer=ctypes.addressof(source)+offset
            length=library.ZSTD_findFrameCompressedSize(pointer,len(data)-offset)
            if library.ZSTD_isError(length) or length<=0 or length>len(data)-offset:
                raise ValueError('Invalid Zstd frame')
            size=library.ZSTD_getFrameContentSize(pointer,length);total+=size
            if size>256*1024*1024 or total>1024*1024*1024:
                raise ValueError('Unsupported or oversized compressed study')
            output=ctypes.create_string_buffer(size)
            actual=library.ZSTD_decompress(output,size,pointer,length)
            if actual!=size:raise ValueError('Incomplete Zstd frame')
            parts.append(output.raw);offset+=length
        return b''.join(parts)


class SavedBlend:
    def __init__(self, data):
        data=decompress(data)
        if data[:17]!=b'BLENDER17-01v0502':raise ValueError('Recovery supports this Blender 5.2 study format only')
        self.blocks=[];self.index={};self.local={};self.owners={};self.context=None;offset=17;owner=None
        while offset<len(data):
            if offset+32>len(data):raise ValueError('Truncated block header')
            code,dna,address,length,count=struct.unpack_from('<4siQqq',data,offset);offset+=32
            if length<0 or count<0 or offset+length>len(data):raise ValueError('Invalid block size')
            block={'code':code,'dna':dna,'address':address,'count':count,'data':memoryview(data)[offset:offset+length]}
            self.blocks.append(block);offset+=length
            if code!=b'DATA':owner=address
            self.owners[id(block['data'])]=owner
            if address:
                self.index.setdefault(address,[]).append(block)
                if code==b'DATA':
                    key=(owner,address)
                    if key in self.local and self.local[key]['data']!=block['data']:raise ValueError('Ambiguous Blender address within an ID')
                    self.local[key]=block
            if code==b'ENDB':break
        if offset!=len(data) or self.blocks[-1]['code']!=b'ENDB':raise ValueError('Incomplete Blender file')
        dna_blocks=[b for b in self.blocks if b['code']==b'DNA1']
        if len(dna_blocks)!=1:raise ValueError('Expected one Blender DNA schema')
        dna=bytes(dna_blocks[0]['data']);offset=8
        if dna[:8]!=b'SDNANAME':raise ValueError('Invalid DNA schema')
        def strings():
            nonlocal offset
            count=struct.unpack_from('<I',dna,offset)[0];offset+=4;result=[]
            for _ in range(count):
                end=dna.index(b'\0',offset);result.append(dna[offset:end].decode());offset=end+1
            offset=(offset+3)//4*4;return result
        names=strings()
        if dna[offset:offset+4]!=b'TYPE':raise ValueError('Invalid DNA types')
        offset+=4;types=strings()
        if dna[offset:offset+4]!=b'TLEN':raise ValueError('Invalid DNA lengths')
        offset+=4;lengths=struct.unpack_from('<'+'H'*len(types),dna,offset);offset+=2*len(types);offset=(offset+3)//4*4
        if dna[offset:offset+4]!=b'STRC':raise ValueError('Invalid DNA structs')
        offset+=4;count=struct.unpack_from('<I',dna,offset)[0];offset+=4;self.fields={};self.sizes={}
        for _ in range(count):
            type_id,n=struct.unpack_from('<HH',dna,offset);offset+=4;fields={};position=0
            for _ in range(n):
                field_type,name_id=struct.unpack_from('<HH',dna,offset);offset+=4;name=names[name_id];number=1
                for item in re.findall(r'\[(\d+)\]',name):number*=int(item)
                size=(8 if '*' in name else lengths[field_type])*number
                fields[name]=(position,size);position+=size
            if position!=lengths[type_id]:raise ValueError('Unsupported DNA field alignment')
            self.fields[types[type_id]]=fields;self.sizes[types[type_id]]=position

    def field(self,data,kind,name):
        offset,size=self.fields[kind][name]
        if offset+size>len(data):raise ValueError('Truncated Blender struct')
        return data[offset:offset+size]

    def number(self,data,kind,name,fmt='<Q'):
        return struct.unpack(fmt,self.field(data,kind,name))[0]

    def text(self,data,kind,name):
        return bytes(self.field(data,kind,name)).split(b'\0')[0].decode()

    def pointer(self,address):
        if address not in self.index:raise ValueError('Missing Blender pointer target')
        if (self.context,address) in self.local:return self.local[self.context,address]['data']
        matches=self.index[address]
        if any(block['data']!=matches[0]['data'] for block in matches[1:]):raise ValueError('Pointer requires its Blender ID context')
        return matches[0]['data']

    def properties(self,data,kind):
        self.context=self.owners[id(data)]
        identity=self.field(data,kind,'id');root=self.pointer(self.number(identity,'ID','*properties'))
        group=self.field(self.field(root,'IDProperty','data'),'IDPropertyData','group')
        current=struct.unpack_from('<Q',group)[0];result={};seen=set()
        while current:
            if current in seen:raise ValueError('Cyclic property list')
            seen.add(current);prop=self.pointer(current)
            if self.number(prop,'IDProperty','type','<b')==0:
                value=self.field(prop,'IDProperty','data')
                result[self.text(prop,'IDProperty','name[64]')]=bytes(self.pointer(self.number(value,'IDPropertyData','*pointer'))).split(b'\0')[0].decode()
            current=self.number(prop,'IDProperty','*next')
        return result

    def positions(self,mesh):
        self.context=self.owners[id(mesh)]
        attributes=self.field(mesh,'Mesh','attribute_storage')
        count=self.number(attributes,'AttributeStorage','dna_attributes_num','<i')
        array=self.pointer(self.number(attributes,'AttributeStorage','*dna_attributes'));size=self.sizes['Attribute']
        for i in range(count):
            attribute=array[i*size:(i+1)*size]
            name=bytes(self.pointer(self.number(attribute,'Attribute','*name'))).split(b'\0')[0]
            if name!=b'position':continue
            if self.number(attribute,'Attribute','data_type','<h')!=7 or self.number(attribute,'Attribute','storage_type','<b')!=0:
                raise ValueError('Unsupported position attribute')
            storage=self.pointer(self.number(attribute,'Attribute','*data'))
            if self.number(storage,'AttributeArray','size','<q')!=self.number(mesh,'Mesh','totvert','<i'):
                raise ValueError('Position count differs from mesh')
            return self.pointer(self.number(storage,'AttributeArray','*data'))
        raise ValueError('Mesh has no position attribute')

    def packed_image(self,image):
        self.context=self.owners[id(image)]
        packed_list=self.field(image,'Image','packedfiles');first,last=struct.unpack('<QQ',packed_list)
        if not first:return None
        if first!=last:raise ValueError('Multiple packed image views are unsupported')
        packed=self.pointer(self.number(self.pointer(first),'ImagePackedFile','*packedfile'))
        size=self.number(packed,'PackedFile','size','<i');payload=self.pointer(self.number(packed,'PackedFile','*data'))
        if size<=0 or size!=len(payload):raise ValueError('Invalid packed image length')
        return bytes(payload)
