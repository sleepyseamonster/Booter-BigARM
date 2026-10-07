import tempfile
import unittest
from pathlib import Path

from repository_paths import Relocations, contained, find_root


class RepositoryPathsTests(unittest.TestCase):
    def test_root_discovery_survives_tool_depth_changes(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "Packages").mkdir()
            (root / "Packages/manifest.json").write_text("{}")
            (root / "ProjectSettings").mkdir()
            (root / "ProjectSettings/ProjectVersion.txt").write_text("version")
            nested = root / "Tools/Art/Deep/utility.py"
            nested.parent.mkdir(parents=True)
            nested.write_text("")
            self.assertEqual(find_root(nested), root)

    def test_chain_preserves_historical_aliases(self):
        index = Relocations([{"source": "a", "destination": "b"}, {"source": "b", "destination": "c"}])
        self.assertEqual(index.resolve("a"), "c")
        self.assertEqual(index.resolve("unchanged"), "unchanged")

    def test_cycle_and_conflict_rejected(self):
        with self.assertRaises(ValueError):
            Relocations([{"source": "a", "destination": "b"}, {"source": "b", "destination": "a"}])
        with self.assertRaises(ValueError):
            Relocations([{"source": "a", "destination": "b"}, {"source": "a", "destination": "c"}])

    def test_windows_collision_and_escape_rejected(self):
        with self.assertRaises(ValueError):
            Relocations([{"source": "a", "destination": "Target"}, {"source": "b", "destination": "target"}])
        for value in ("../outside", "C:/outside", "C:outside", "/outside", "folder\\file"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                contained(Path.cwd(), value)

    def test_preserved_version_does_not_redirect_live_content(self):
        root = Path.cwd()
        index = Relocations([{"source": "old", "destination": "new"}],
                            [{"source": "old", "sha256": "hash", "destination": "Archive/original"}])
        self.assertEqual(index.path(root, "old"), root / "new")
        self.assertEqual(index.version_path(root, "old", "hash"), root / "Archive/original")


if __name__ == "__main__":
    unittest.main()
