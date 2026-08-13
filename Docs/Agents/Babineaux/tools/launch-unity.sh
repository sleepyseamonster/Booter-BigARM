#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd "$script_dir/../../../.." && pwd)"
project_name="$(basename "$project_root")"
project_version_file="$project_root/ProjectSettings/ProjectVersion.txt"
primary_scene="$project_root/Assets/_Project/Scenes/PrototypeScene.unity"
mode="launch"

if [[ "${1:-}" == "--status" ]]; then
  mode="status"
elif [[ $# -gt 0 ]]; then
  echo "Usage: $0 [--status]" >&2
  exit 64
fi

if [[ ! -f "$project_version_file" ]]; then
  echo "ERROR: Missing Unity project version file: $project_version_file" >&2
  exit 1
fi

project_version="$(awk '/m_EditorVersion:/{print $2; exit}' "$project_version_file")"
unity_app="/Applications/Unity/Hub/Editor/$project_version/Unity.app"
unity_binary="$unity_app/Contents/MacOS/Unity"

if [[ -z "$project_version" || ! -x "$unity_binary" ]]; then
  echo "ERROR: Unity $project_version is not installed at $unity_binary" >&2
  exit 1
fi

find_project_pid() {
  local candidate_pid
  local command_line

  while IFS= read -r candidate_pid; do
    [[ -n "$candidate_pid" ]] || continue
    command_line="$(ps -ww -p "$candidate_pid" -o command= 2>/dev/null || true)"
    if [[ "$command_line" == *"-projectPath $project_root"* ]]; then
      echo "$candidate_pid"
      return 0
    fi
  done < <(pgrep -f "$unity_binary" || true)

  return 1
}

window_names_for_pid() {
  local editor_pid="$1"

  osascript \
    -e 'tell application "System Events"' \
    -e "set unityProcess to first process whose unix id is $editor_pid" \
    -e 'return name of every window of unityProcess' \
    -e 'end tell' 2>/dev/null || true
}

focus_editor() {
  local editor_pid="$1"

  osascript \
    -e 'tell application "System Events"' \
    -e "set unityProcess to first process whose unix id is $editor_pid" \
    -e 'set frontmost of unityProcess to true' \
    -e 'end tell'
}

open_primary_scene() {
  local editor_pid="$1"

  osascript \
    -e 'tell application "System Events"' \
    -e "set unityProcess to first process whose unix id is $editor_pid" \
    -e 'set frontmost of unityProcess to true' \
    -e 'delay 0.5' \
    -e 'keystroke "o" using command down' \
    -e 'delay 1.5' \
    -e 'keystroke "g" using {command down, shift down}' \
    -e 'delay 0.8' \
    -e "keystroke \"$primary_scene\"" \
    -e 'delay 0.5' \
    -e 'key code 36' \
    -e 'delay 1.0' \
    -e 'key code 36' \
    -e 'end tell'
}

editor_pid="$(find_project_pid || true)"

if [[ "$mode" == "status" ]]; then
  if [[ -z "$editor_pid" ]]; then
    echo "STOPPED: $project_name is not open in Unity $project_version"
    exit 1
  fi

  window_names="$(window_names_for_pid "$editor_pid")"
  echo "RUNNING: pid=$editor_pid version=$project_version window=$window_names"
  [[ "$window_names" == *"$project_name"* ]]
  exit
fi

if [[ -z "$editor_pid" ]]; then
  existing_unity_pids="$(pgrep -f "$unity_binary" || true)"

  if [[ -n "$existing_unity_pids" ]]; then
    visible_unity_window=false
    while IFS= read -r existing_pid; do
      [[ -n "$existing_pid" ]] || continue
      if [[ -n "$(window_names_for_pid "$existing_pid")" ]]; then
        visible_unity_window=true
        break
      fi
    done <<< "$existing_unity_pids"

    if [[ "$visible_unity_window" != true ]]; then
      echo "ERROR: Unity has a windowless process. Close that stale process before launching $project_name." >&2
      exit 1
    fi

    /usr/bin/open -na "$unity_app" --args -projectPath "$project_root"
  else
    /usr/bin/open -a "$unity_app" --args -projectPath "$project_root"
  fi

  for _ in {1..45}; do
    editor_pid="$(find_project_pid || true)"
    [[ -n "$editor_pid" ]] && break
    sleep 1
  done

  if [[ -z "$editor_pid" ]]; then
    echo "ERROR: Unity did not start $project_name within 45 seconds." >&2
    exit 1
  fi
fi

window_names=""
for _ in {1..45}; do
  window_names="$(window_names_for_pid "$editor_pid")"
  [[ "$window_names" == *"$project_name"* ]] && break
  sleep 1
done

if [[ "$window_names" != *"$project_name"* ]]; then
  echo "ERROR: Unity pid $editor_pid has no $project_name editor window." >&2
  exit 1
fi

if [[ "$window_names" != *"PrototypeScene"* ]]; then
  if [[ "$window_names" == *"Untitled - $project_name"* ]]; then
    open_primary_scene "$editor_pid"

    for _ in {1..20}; do
      window_names="$(window_names_for_pid "$editor_pid")"
      [[ "$window_names" == *"PrototypeScene"* ]] && break
      sleep 1
    done
  fi
fi

focus_editor "$editor_pid"

if [[ "$window_names" == *"PrototypeScene"* ]]; then
  echo "READY: pid=$editor_pid version=$project_version scene=PrototypeScene"
else
  echo "READY: pid=$editor_pid version=$project_version window=$window_names"
  echo "NOTE: The editor is open, but PrototypeScene was not confirmed." >&2
fi
