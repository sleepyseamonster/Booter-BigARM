#!/usr/bin/env bash

set -uo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../../.." && pwd)"
failures=0
warnings=0

pass() {
  echo "PASS  $1"
}

warn() {
  echo "WARN  $1"
  warnings=$((warnings + 1))
}

fail() {
  echo "FAIL  $1"
  failures=$((failures + 1))
}

cd "$repo_root" || exit 1

echo "Booter & BigARM repo health"
echo "Root: $repo_root"

required_files=(
  "AGENTS.md"
  "Docs/WORLD_BASIS.md"
  "Docs/ROADMAP.md"
  "Docs/PROJECT_BASELINE.md"
  "Docs/UNITY_AUTOMATION.md"
  "Docs/Agents/Gottspan/README.md"
  "Packages/manifest.json"
  "Packages/packages-lock.json"
  "ProjectSettings/ProjectVersion.txt"
  "ProjectSettings/EditorBuildSettings.asset"
  "Assets/_Project/Scripts/Runtime/BooterBigArm.Runtime.asmdef"
  "Assets/_Project/Scripts/Editor/BooterBigArm.Editor.asmdef"
)

missing_required=0
for required_file in "${required_files[@]}"; do
  if [[ ! -e "$required_file" ]]; then
    echo "      missing: $required_file"
    missing_required=$((missing_required + 1))
  fi
done

if (( missing_required == 0 )); then
  pass "Required manager and Unity files exist"
else
  fail "$missing_required required file(s) are missing"
fi

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  branch_name="$(git branch --show-current)"
  pass "Git worktree detected on branch '${branch_name:-DETACHED}'"

  if [[ -n "$(git status --porcelain)" ]]; then
    warn "Worktree is dirty; classify ownership before editing or staging"
  else
    pass "Worktree is clean"
  fi

  if git diff --check && git diff --cached --check; then
    pass "Tracked diffs contain no whitespace errors"
  else
    fail "Whitespace errors found in tracked diffs"
  fi

  tracked_generated="$(git ls-files 'Library/**' 'Temp/**' 'Logs/**' 'UserSettings/**' 'Build/**' 'Builds/**')"
  if [[ -z "$tracked_generated" ]]; then
    pass "No generated Unity or build-output directories are tracked"
  else
    echo "$tracked_generated"
    fail "Generated Unity or build-output files are tracked"
  fi
else
  fail "Repository root is not a Git worktree"
fi

meta_problems=0
while IFS= read -r asset_path; do
  if [[ ! -e "$asset_path.meta" ]]; then
    echo "      missing meta: $asset_path"
    meta_problems=$((meta_problems + 1))
  fi
done < <(find Assets -mindepth 1 ! -name '*.meta' ! -name '.DS_Store' -print | sort)

while IFS= read -r meta_path; do
  asset_path_without_meta="${meta_path%.meta}"
  if [[ ! -e "$asset_path_without_meta" ]]; then
    echo "      orphan meta: $meta_path"
    meta_problems=$((meta_problems + 1))
  fi
done < <(find Assets -name '*.meta' -print | sort)

if (( meta_problems == 0 )); then
  pass "Unity assets and .meta files are paired"
else
  fail "$meta_problems Unity asset/.meta pairing problem(s) found"
fi

finder_noise="$(find Assets -name '.DS_Store' -print)"
if [[ -n "$finder_noise" ]]; then
  warn "Finder metadata exists under Assets (ignored by Git)"
else
  pass "No Finder metadata exists under Assets"
fi

project_version="$(awk '/m_EditorVersion:/{print $2; exit}' ProjectSettings/ProjectVersion.txt)"
if [[ -n "$project_version" ]]; then
  pass "Unity project version is $project_version"
else
  fail "Could not read the Unity project version"
fi

unity_binary="/Applications/Unity/Hub/Editor/$project_version/Unity.app/Contents/MacOS/Unity"
if [[ -x "$unity_binary" ]]; then
  pass "Matching Unity editor is installed"
else
  warn "Matching Unity editor was not found at $unity_binary"
fi

if [[ -e "Temp/UnityLockfile" ]]; then
  warn "Unity lockfile is present; do not launch batchmode against the open project"
else
  pass "No Unity project lockfile is present"
fi

echo "Summary: $failures failure(s), $warnings warning(s)"
exit "$failures"
