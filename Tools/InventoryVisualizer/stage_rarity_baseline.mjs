import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const directory=process.argv[2],mode=process.argv[3]??'before';
const plan=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const samples=JSON.parse(await fs.readFile(`${directory}/preview.json`,'utf8'));
const wb=Workbook.create(),sample=wb.worksheets.add('Sample');
sample.getRange('A1:C1').values=[['Item','Tier','Rarity']];
for(const [i,item] of samples.entries()){
  sample.getRange(`A${i+2}:C${i+2}`).values=[[item.name,item.tier,mode==='after'?item.rarity:item.previousRarity]];
  sample.getRange(`A${i+2}:C${i+2}`).format.rowHeight=item.height;
}
for(const [i,width] of samples[0].widths.entries())sample.getCell(0,i).format.columnWidth=width;
// Apply source column widths to the entire bounded sample.
for(const [i,col] of ['A','B','C'].entries())sample.getRange(`${col}1:${col}${samples.length+1}`).format.columnWidth=samples[0].widths[i];
if(mode==='after'){
  const items=wb.worksheets.add('Items');
  for(const [address,value] of Object.entries(plan.changes.Items))items.getRange(address).values=[[value]];
}
wb.recalculate();
const png=await wb.render({sheetName:'Sample',range:`A1:C${samples.length+1}`,scale:1.4,format:'png'});
await fs.writeFile(`${directory}/${mode}.png`,new Uint8Array(await png.arrayBuffer()));
if(mode==='after')await(await SpreadsheetFile.exportXlsx(wb)).save(`${directory}/authored.xlsx`);
console.log(`Rendered ${mode}${mode==='after'?' and authored rarity assignments':''}.`);
