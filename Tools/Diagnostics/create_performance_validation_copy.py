"""Create a new, small non-rendering test project; never overwrite a retained checkout."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


def git(*args):
    return subprocess.check_output(["git", *args], cwd=Path(__file__).resolve().parents[2])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--destination", required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    destination = Path(args.destination).resolve()
    allowed = Path("D:/BooterBigArmValidation").resolve()
    if destination.parent != allowed or destination.exists():
        raise SystemExit("Use a new immediate child of D:/BooterBigArmValidation; existing copies are preserved.")
    task_files = {
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainSurfaceIndex.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainRenderWindow.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/GreaterWastelandTerrainRendering.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainCameraCoverage.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationIdentity.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationState.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationPlanner.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationPresentation.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationScheduler.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainPopulationSectorStudy.cs",
        "Assets/_Project/Scripts/Editor/Automation/FixedTerrainPopulationSectorBuilder.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySnapshot.cs",
        "Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandCheckpointWriter.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainDressingTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainRenderWindowTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainCameraCoverageTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationIdentityTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationStateTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationPlannerTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationPresentationTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationSchedulerTests.cs",
        "Assets/_Project/Tests/Editor/FixedTerrainPopulationSectorStudyTests.cs",
        "Assets/_Project/Tests/Editor/TopDown3DRouteRetryTests.cs",
        "Assets/_Project/Tests/Editor/GreaterWastelandCheckpointWriterTests.cs",
        "Assets/_Project/Tests/Editor/GreaterWastelandInventoryPersistenceTests.cs",
    }
    task_files |= {name + ".meta" for name in tuple(task_files)}
    sector_receipt = root / "Docs/Evidence/Performance/population-sector-native-inputs-2026-10-12.json"
    sector_files = set()
    if sector_receipt.exists():
        inputs = json.loads(sector_receipt.read_text(encoding="utf-8"))
        for item in inputs["asset_files"]:
            if hashlib.sha256((root / item["path"]).read_bytes()).hexdigest() != item["sha256"]:
                raise SystemExit("Reference-sector native inputs changed; refresh the study receipt before validation.")
            sector_files.add(item["path"])
        task_files.add(str(sector_receipt.relative_to(root)).replace("\\", "/"))
        sector_files |= {"Assets/_Project/Scenes/Reference/Performance/GreaterWastelandPopulationSector20261012.unity",
                         "Assets/_Project/Scenes/Reference/Performance/GreaterWastelandPopulationSector20261012.unity.meta",
                         "Assets/_Project/Scenes/Reference.meta", "Assets/_Project/Scenes/Reference/Performance.meta"}
    names = set(git("ls-files", "-z", "Assets", "Packages", "ProjectSettings").decode().split("\0")) | task_files | sector_files
    dirty = set(git("diff", "--name-only", "HEAD").decode().splitlines())
    manifest = []
    destination.mkdir()
    for name in sorted(names):
        if not name or (name.startswith("Assets/_Project/Art/Terrain") or name.startswith("Assets/_Project/Scenes/")) and name not in sector_files:
            continue
        if ".blend" in name.lower() or name.startswith("Assets/_Project/Legacy2D/"):
            continue
        source = root / name
        if not source.is_file():
            continue
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        if name.endswith(".meta") and source.with_suffix("").is_dir():
            target.with_suffix("").mkdir(exist_ok=True)
        committed = name in dirty and name not in task_files
        if committed:
            target.write_bytes(git("show", "HEAD:" + name))
        else:
            shutil.copyfile(source, target)
        manifest.append({"path": name, "bytes": target.stat().st_size,
                         "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                         "source": "HEAD" if committed else "worktree"})
    receipt = {"source": str(root), "destination": str(destination), "head": git("rev-parse", "HEAD").decode().strip(),
               "purpose": "Focused non-rendering EditMode validation; native terrain/scenes excluded. Not a Player/performance baseline.",
               "files": manifest, "total_bytes": sum(item["bytes"] for item in manifest)}
    (destination / "Logs").mkdir()
    (destination / "Logs/performance-validation-copy.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps({"destination": str(destination), "files": len(manifest), "bytes": receipt["total_bytes"]}))


if __name__ == "__main__":
    main()
