import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';

const directory=process.argv[2],mode=process.argv[3]||'after';
const {spec,existing}=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const workbook=Workbook.create();
const review=workbook.worksheets.add('Review');
const samples=mode==='before'?existing.slice(0,6).map(item=>[item['Item name'],item.Tier,item.Rarity,item.Purpose]):
  [2,3,4].flatMap(rank=>spec.items.filter(item=>item.fields.Tier===`T${rank}`).slice(0,3).map(item=>[item.name,item.fields.Tier,item.fields.Rarity,item.purpose]));
review.getRange(`A1:D${samples.length+1}`).values=[['Biological material','Tier','Rarity','Description'],...samples];
review.showGridLines=false;
review.getRange(`A1:D${samples.length+1}`).format.font={name:'Arial',size:10};
review.getRange('A1:D1').format={fill:'#27352F',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'},rowHeight:26};
for(const [column,width]of [['A',38],['B',8],['C',16],['D',86]])review.getRange(`${column}1:${column}${samples.length+1}`).format.columnWidth=width;
review.getRange(`A2:D${samples.length+1}`).format.wrapText=true;
review.getRange(`A2:D${samples.length+1}`).format.verticalAlignment='top';
review.getRange(`A2:D${samples.length+1}`).format.rowHeight=76;
if(mode!=='before'){
  const inputs=workbook.worksheets.add('Catalog inputs');
  inputs.getRange('A1:G51').values=[['ID','Name','Purpose','Icon direction','Material','Use','Specific fields'],...spec.items.map(item=>[item.id,item.name,item.purpose,item.icon,item.material,item.use,JSON.stringify(item.fields)])];
  const defaults=workbook.worksheets.add('Rarity defaults');
  defaults.getRange('A1:C51').values=[['ID','Tier','Rarity'],...existing.map(item=>[item['Item ID'],item.Tier,spec.rarityDefaults[item.Tier]])];
}
workbook.recalculate();
const result=await workbook.inspect({kind:'table',range:`Review!A1:D${samples.length+1}`,include:'values',tableMaxRows:4,tableMaxCols:4,maxChars:1600});
console.log(result.ndjson);
const errors=await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:20},maxChars:1000});
console.log(errors.ndjson);
const image=await workbook.render({sheetName:'Review',range:`A1:D${samples.length+1}`,scale:1.4,format:'png'});
await fs.writeFile(`${directory}/${mode}.png`,new Uint8Array(await image.arrayBuffer()));
if(mode!=='before')await(await SpreadsheetFile.exportXlsx(workbook)).save(`${directory}/authored.xlsx`);
console.log(`Rendered ${mode}; ${mode==='before'?'existing source sample':'authored 50 proposals and 50 rarity defaults'}.`);
