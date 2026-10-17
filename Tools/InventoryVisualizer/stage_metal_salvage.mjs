import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';

const directory=process.argv[2],mode=process.argv[3]||'after';
const {spec}=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const workbook=Workbook.create();
const review=workbook.worksheets.add('Review');
const items=mode==='before'?JSON.parse(await fs.readFile(`${directory}/existing.json`,'utf8')):
  spec.items.map(item=>({name:item.name,tier:item.fields.Tier||spec.defaults.Tier,rarity:item.fields.Rarity||spec.defaults.Rarity,purpose:item.purpose}));
review.getRange(`A1:D${items.length+1}`).values=[['Metal salvage','Tier','Rarity','Description'],...items.map(item=>[item.name,item.tier,item.rarity,item.purpose])];
review.showGridLines=false;
review.getRange(`A1:D${items.length+1}`).format.font={name:'Arial',size:10};
review.getRange('A1:D1').format={fill:'#27352F',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'},rowHeight:26};
for(const [column,width]of [['A',38],['B',8],['C',16],['D',90]])review.getRange(`${column}1:${column}${items.length+1}`).format.columnWidth=width;
review.getRange(`A2:D${items.length+1}`).format.wrapText=true;
review.getRange(`A2:D${items.length+1}`).format.verticalAlignment='top';
review.getRange(`A2:D${items.length+1}`).format.rowHeight=80;
if(mode!=='before'){
  const inputs=workbook.worksheets.add('Catalog inputs');
  inputs.getRange('A1:G11').values=[['ID','Name','Purpose','Icon direction','Material','Use','Specific fields'],...spec.items.map(item=>[item.id,item.name,item.purpose,item.icon,item.material,item.use,JSON.stringify(item.fields)])];
}
workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:`Review!A1:D${items.length+1}`,include:'values',tableMaxRows:11,tableMaxCols:3,maxChars:2800})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:20},maxChars:1000})).ndjson);
const image=await workbook.render({sheetName:'Review',range:`A1:D${items.length+1}`,scale:1.3,format:'png'});
await fs.writeFile(`${directory}/${mode}.png`,new Uint8Array(await image.arrayBuffer()));
if(mode!=='before')await(await SpreadsheetFile.exportXlsx(workbook)).save(`${directory}/authored.xlsx`);
console.log(`Rendered ${mode} metal salvage review.`);
