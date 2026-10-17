"""Regression coverage for repaired links to recycled master rows."""
import unittest
from io import BytesIO
from unittest.mock import patch
from xml.etree import ElementTree as ET
from zipfile import ZipFile
from add_canister_and_iron_salvage import repair_use_links

NS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'


class SalvageUseLinkTests(unittest.TestCase):
    def fixture(self, prefix):
        master = [None] * 40
        master[0] = 'fixture.canister'; master[1] = 'Recovered & matched canister'
        master[3] = 'Salvage'; master[26] = 'Equipment repair'
        sheets = {'Items': [()] * 7 + [tuple(master)],
                  'Item uses': [()] * 5 + [('fixture.canister','Old name','Old group','Primary','Old use','Basis')]}
        declaration = f'xmlns:{prefix[:-1]}="{NS}"' if prefix else f'xmlns="{NS}"'
        cells = ''.join(f'<{prefix}c r="{col}6" s="4" t="str"><{prefix}f>Items!{source}1005</{prefix}f><{prefix}v>Old</{prefix}v></{prefix}c>'
                        for col, source in [('B','B'),('C','D'),('E','AA')])
        raw = f'<{prefix}worksheet {declaration}><{prefix}sheetData><{prefix}row r="6">{cells}</{prefix}row></{prefix}sheetData></{prefix}worksheet>'
        output = BytesIO()
        with ZipFile(output,'w') as z:
            z.writestr('uses.xml',raw);z.writestr('unrelated.txt',b'preserve exactly')
        return output.getvalue(), sheets

    def repair(self, data, sheets, ids):
        with patch('add_canister_and_iron_salvage.parse_sheets',return_value=sheets), \
             patch('add_canister_and_iron_salvage.parts_for',return_value={'Item uses':'uses.xml'}):
            return repair_use_links(data,ids)

    def test_links_resolve_to_actual_master_with_both_namespace_forms(self):
        for prefix in ['', 'x:']:
            with self.subTest(prefix=prefix):
                data,sheets=self.fixture(prefix)
                result,edits=self.repair(data,sheets,{'fixture.canister'})
                self.assertEqual(set(edits),{'B6','C6','E6'})
                with ZipFile(BytesIO(result)) as z:
                    root=ET.fromstring(z.read('uses.xml'))
                    cells={c.attrib['r']:c for c in root.iter(f'{{{NS}}}c')}
                    for address,col,value in [('B6','B','Recovered & matched canister'),('C6','D','Salvage'),('E6','AA','Equipment repair')]:
                        self.assertEqual(cells[address].find(f'{{{NS}}}f').text,f'Items!{col}8')
                        self.assertEqual(cells[address].find(f'{{{NS}}}v').text,value)
                        self.assertEqual(cells[address].attrib['s'],'4')
                    self.assertEqual(z.read('unrelated.txt'),b'preserve exactly')

    def test_unrelated_batch_is_untouched(self):
        data,sheets=self.fixture('x:')
        result,edits=self.repair(data,sheets,{'other.id'})
        self.assertEqual(edits,{})
        with ZipFile(BytesIO(data)) as old,ZipFile(BytesIO(result)) as new:
            self.assertEqual(old.read('uses.xml'),new.read('uses.xml'))

    def test_deleted_master_is_not_recreated_by_relationship_repair(self):
        data,sheets=self.fixture('x:');sheets['Items']=[()] * 8
        result,edits=self.repair(data,sheets,{'fixture.canister'})
        self.assertEqual(edits,{})
        with ZipFile(BytesIO(data)) as old,ZipFile(BytesIO(result)) as new:
            self.assertEqual(old.read('uses.xml'),new.read('uses.xml'))


if __name__ == '__main__': unittest.main()
