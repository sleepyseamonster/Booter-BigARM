// Scoped Artifact Tool authoring avoids re-exporting the linked master workbook.
import fs from 'node:fs/promises';
import {Workbook, SpreadsheetFile} from '@oai/artifact-tool';
const directory=process.argv[2];
const mode=process.argv[3] ?? 'after';
const plan=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const samples=JSON.parse(await fs.readFile(`${directory}/preview.json`,'utf8'));
const wb=Workbook.create();
const sample=wb.worksheets.add('Sample');
for(const [i,entry] of samples.entries()){
  for(const [j,cell] of entry.cells.entries()){
    const target=sample.getCell(i,j);
    target.values=[[mode==='after' ? [entry.name,entry.tier,entry.rarity][j] : cell.value]];
    target.format.columnWidth=cell.width;
    target.format.rowHeight=cell.height;
    target.format.font={name:cell.font,size:cell.size,bold:cell.bold};
    target.format.wrapText=cell.wrap;
  }
}
if(mode==='after'){
  for(const [name,cells] of Object.entries(plan.changes)){
    const sheet=wb.worksheets.add(name);
    for(const [address,value] of Object.entries(cells))sheet.getRange(address).values=[[value]];
  }
}
wb.recalculate();
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:20},maxChars:1000});
console.log(errors.ndjson);
const rendered=await wb.render({sheetName:'Sample',range:`A1:C${samples.length}`,scale:1.5,format:'png'});
await fs.writeFile(`${directory}/${mode}.png`,new Uint8Array(await rendered.arrayBuffer()));
if(mode==='after')await(await SpreadsheetFile.exportXlsx(wb)).save(`${directory}/authored.xlsx`);
console.log(`Rendered ${mode} mineral sample${mode==='after'?' and authored exact cell inputs':''}.`);
