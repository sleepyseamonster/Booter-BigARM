"""Preserve unrelated saved documents and reject malformed native appends."""
import unittest
from preserve_north_row4_scene import preserve
from preserve_expansion_scene import split_documents

def fixture():
    original='%YAML 1.1\n--- !u!4 &1259644692\nTransform:\n  m_Children:\n  - {fileID: 10}\n  m_Name: original\n--- !u!1 &10\nGameObject:\n  m_Name: Retained\n'
    current=original.replace('  m_Name: Retained','  m_Name: IncidentalSerializerDrift').replace('  - {fileID: 10}\n','  - {fileID: 10}\n  - {fileID: 20}\n')
    added=''.join(f'--- !u!{218 if i<256 else 154 if i<512 else 4} &{20+i}\nObject:\n  m_Name: New\n' for i in range(1314))
    return original,current+added

class ScenePreservationTests(unittest.TestCase):
    def test_restores_retained_documents_without_losing_native_append(self):
        before,current=fixture();result=preserve(before,current)
        _,old=split_documents(before);_,new=split_documents(result)
        self.assertEqual(new['10'],old['10'])
        self.assertEqual(len(set(new)-set(old)),1314)
        self.assertIn('  - {fileID: 20}\n',new['1259644692'])
        self.assertNotIn('IncidentalSerializerDrift',result)

    def test_rejects_missing_collider_or_retained_document(self):
        before,current=fixture()
        for bad in [current.replace('--- !u!154 &276','--- !u!4 &276'),current.replace('--- !u!1 &10\nGameObject:\n  m_Name: IncidentalSerializerDrift\n','')]:
            with self.assertRaises(ValueError):preserve(before,bad)

    def test_rejects_parent_reordering_and_unknown_child(self):
        before,current=fixture()
        for bad in [current.replace('  - {fileID: 10}\n  - {fileID: 20}\n','  - {fileID: 20}\n  - {fileID: 10}\n'),current.replace('  - {fileID: 20}\n','  - {fileID: 999999}\n')]:
            with self.assertRaises(ValueError):preserve(before,bad)

if __name__=='__main__':unittest.main()
