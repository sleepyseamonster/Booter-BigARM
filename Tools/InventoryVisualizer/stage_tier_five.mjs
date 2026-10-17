import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const directory=process.argv[2],mode=process.argv[3]||'after';
const plan=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const samples=JSON.parse(await fs.readFile(`${directory}/preview.json`,'utf8'));
const workbook=Workbook.create(),review=workbook.worksheets.add('Review');
review.getRange(`A1:C${samples.length+1}`).values=[['Ancient salvage','Tier','Rarity'],...samples.map(item=>[item.name,mode==='before'?item.previous:item.tier,item.rarity])];
review.showGridLines=false;
review.getRange(`A1:C${samples.length+1}`).format.font={name:'Arial',size:10};
review.getRange('A1:C1').format={fill:'#27352F',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'},rowHeight:26};
for(const [column,width]of [['A',48],['B',20],['C',16]])review.getRange(`${column}1:${column}${samples.length+1}`).format.columnWidth=width;
review.getRange(`A2:C${samples.length+1}`).format.wrapText=true;
review.getRange(`A2:C${samples.length+1}`).format.rowHeight=36;
if(mode!=='before'){
  for(const [name,cells]of Object.entries(plan.changes)){
    const sheet=workbook.worksheets.add(name);
    for(const [address,value]of Object.entries(cells))sheet.getRange(address).values=[[value]];
  }
  const rows=Object.entries(plan.formulas).flatMap(([sheet,cells])=>Object.entries(cells).map(([address,expression])=>[sheet,address,expression]));
  workbook.worksheets.add('Formula edits').getRange(`A1:C${rows.length+1}`).values=[['Sheet','Cell','Expression'],...rows];
  const sort=workbook.worksheets.add('Sort checks');
  const [sortAddress,sortExpression]=Object.entries(plan.formulas.Items)[0];
  const sortRow=sortAddress.match(/\d+/)[0];
  for(const [index,tier]of ['T0','T1','T2','T3','T4','T5','Unassigned','Not applicable'].entries()){
    const n=index+1;sort.getRange(`C${n}`).values=[[tier]];
    sort.getRange(`AN${n}`).formulas=[['='+sortExpression.replaceAll(`C${sortRow}`,`C${n}`)]];
  }
  const ingredient=workbook.worksheets.add('Ingredient checks');
  const [address,expression]=Object.entries(plan.formulas['Material routes'])[0],row=address.match(/\d+/)[0];
  ingredient.getRange('C1').values=[['Ingredient candidate']];ingredient.getRange('F1').values=[['T5']];
  ingredient.getRange('P1').formulas=[['='+expression.replaceAll(`C${row}`,'C1').replaceAll(`F${row}`,'F1')]];
}
workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:`Review!A1:C${samples.length+1}`,include:'values',tableMaxRows:6,tableMaxCols:3,maxChars:1800})).ndjson);
if(mode!=='before'){
  const expected=[0,1,2,3,4,5,98,99];
  const actual=workbook.worksheets.getItem('Sort checks').getRange('AN1:AN8').values.map(row=>row[0]);
  if(JSON.stringify(actual)!==JSON.stringify(expected))throw Error(`Sort helper regression: ${JSON.stringify(actual)}`);
  if(workbook.worksheets.getItem('Ingredient checks').getRange('P1').values[0][0]!==5)throw Error('T5 ingredient helper did not calculate 5.');
  console.log('Calculated sort keys 0–5, 98, 99 and T5 ingredient number 5.');
}
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
const image=await workbook.render({sheetName:'Review',range:`A1:C${samples.length+1}`,scale:1.5,format:'png'});
await fs.writeFile(`${directory}/${mode}.png`,new Uint8Array(await image.arrayBuffer()));
if(mode!=='before')await(await SpreadsheetFile.exportXlsx(workbook)).save(`${directory}/authored.xlsx`);
