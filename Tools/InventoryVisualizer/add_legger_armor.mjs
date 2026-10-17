// Author only the three new catalogue records; merge preserves the master workbook.
import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const directory=process.argv[2];
if(!directory)throw new Error('Provide the prepared operation directory.');
const plan=JSON.parse(await fs.readFile(`${directory}/plan.json`,'utf8'));
const book=Workbook.create(),sheet=book.worksheets.add('Legger armor');
sheet.getRange('A1:AN4').values=[plan.headers,...plan.records];
await book.recalculate();
await (await SpreadsheetFile.exportXlsx(book)).save(`${directory}/authored.xlsx`);
console.log('Authored three Legger armor records.');
