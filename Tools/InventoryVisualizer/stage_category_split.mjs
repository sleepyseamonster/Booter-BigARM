// Sparse Artifact Tool staging avoids re-exporting the large production workbook.
import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const directory=process.argv[2];
if(!directory)throw new Error('Provide the prepared operation directory.');
const plan=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const wb=Workbook.create();
for(const name of new Set(plan.changes.map(c=>c.sheet)))wb.worksheets.add(name);
for(const c of plan.changes.filter(c=>!c.cachedOnly))wb.worksheets.getItem(c.sheet).getRange(c.address).values=[[c.value]];
const review=wb.worksheets.add('Review'),end=plan.items.length+1;
review.getRange(`A1:B${end}`).values=[['Item','New category'],...plan.items.map(i=>[i.name,plan.target])];
review.getRange(`A1:A${end}`).format.columnWidth=45;
review.getRange(`B1:B${end}`).format.columnWidth=28;
review.getRange(`A1:B${end}`).format.rowHeight=25;
wb.recalculate();
await (await SpreadsheetFile.exportXlsx(wb)).save(`${directory}/authored.xlsx`);
const img=await wb.render({sheetName:'Review',range:`A1:B${end}`,scale:1.5,format:'png'});
await fs.writeFile(`${directory}/review.png`,new Uint8Array(await img.arrayBuffer()));
console.log('Authored category split inputs and review.');
