import fs from 'node:fs/promises';
import {Workbook, SpreadsheetFile} from '@oai/artifact-tool';
const directory = process.argv[2];
if (!directory) throw new Error('Provide the prepared operation directory.');
const {spec} = JSON.parse(await fs.readFile(`${directory}/plan.json`, 'utf8'));
const choices = JSON.parse(await fs.readFile(`${directory}/material-choices.json`, 'utf8'));
const wb = Workbook.create(), end = spec.items.length + 1;
wb.worksheets.add('Catalog inputs').getRange(`A1:G${end}`).values = [
  ['ID', 'Name', 'Purpose', 'Icon direction', 'Material', 'Use', 'Specific fields'],
  ...spec.items.map(i => [i.id, i.name, i.purpose, i.icon, i.material, i.use, JSON.stringify(i.fields || {})])
];
wb.worksheets.add('Material choices').getRange(`A1:A${choices.length+1}`).values = [['Material family'], ...choices.map(c => [c])];
const review = wb.worksheets.add('Review');
review.getRange(`A1:C${end}`).values = [['Canyon Crew piece', 'Category', 'Material'], ...spec.items.map(i => [i.name, spec.defaults.Category, i.material])];
review.getRange(`A1:A${end}`).format.columnWidth = 42;
review.getRange(`B1:C${end}`).format.columnWidth = 20;
review.getRange(`A1:C${end}`).format.rowHeight = 25;
wb.recalculate();
await (await SpreadsheetFile.exportXlsx(wb)).save(`${directory}/authored.xlsx`);
const img = await wb.render({sheetName: 'Review', range: `A1:C${end}`, scale: 1.5, format: 'png'});
await fs.writeFile(`${directory}/review.png`, new Uint8Array(await img.arrayBuffer()));
console.log('Authored five Canyon Crew pieces and the complete material choices.');
