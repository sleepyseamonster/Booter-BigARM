"""Additive terrain integration must preserve original scene serialization."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'Tools/Art/Blender/death_valley'))
from preserve_expansion_scene import preserve,split_documents


class ScenePreservationTests(unittest.TestCase):
    def setUp(self):
        self.before='%YAML 1.1\n--- !u!4 &1\nTransform:\n  m_Children:\n  - {fileID: 10}\n  m_Father: {fileID: 0}\n--- !u!20 &3\nCamera:\n  far clip plane: 8000\n'
        changed=self.before.replace('8000','1300').replace('  m_Father:',
            '  - {fileID: 1000}\n  - {fileID: 1001}\n  - {fileID: 1002}\n  m_Father:')
        self.after=changed+''.join(f'--- !u!4 &{i}\nTransform:\n  m_Father: {{fileID: 1}}\n' for i in range(1000,4846))

    def test_original_camera_is_exact_and_new_documents_are_preserved(self):
        result=preserve(self.before,self.after,'1')
        _,old=split_documents(self.before);_,current=split_documents(self.after);_,actual=split_documents(result)
        self.assertEqual(actual['3'],old['3'])
        self.assertEqual(len(actual),3848)
        for key in current.keys()-old.keys():self.assertEqual(actual[key],current[key])

    def test_removed_original_or_replaced_child_is_rejected(self):
        broken=self.after.replace('--- !u!20 &3\nCamera:\n  far clip plane: 1300\n','')
        with self.assertRaises(ValueError):preserve(self.before,broken,'1')
        with self.assertRaises(ValueError):preserve(self.before,self.after.replace('fileID: 10}','fileID: 11}'),'1')

    def test_missing_new_section_or_duplicate_document_is_rejected(self):
        with self.assertRaises(ValueError):preserve(self.before,self.after.replace('  - {fileID: 1002}\n',''),'1')
        with self.assertRaises(ValueError):preserve(self.before,self.after+'--- !u!4 &1000\nTransform:\n','1')


if __name__=='__main__':unittest.main()
