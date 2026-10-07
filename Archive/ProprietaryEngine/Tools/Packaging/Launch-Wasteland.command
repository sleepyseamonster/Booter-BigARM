#!/bin/sh
set -eu
package_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
mkdir -p "$package_dir/UserData"
set -- --terrain "$package_dir/bin/Assets/Recipes/terrain.json" --stream-rock "$package_dir/bin/Assets/Recipes/stream-rock.json" --terrain-preview --world-profile "$package_dir/UserData/World" --save-inspection "$package_dir/UserData/inspection.json" "$@"
if [ -f "$package_dir/UserData/inspection.json" ]; then
    set -- --inspection "$package_dir/UserData/inspection.json" "$@"
else
    set -- --inspection "$package_dir/bin/Assets/inspection.json" "$@"
fi
cd "$package_dir/UserData"
exec "$package_dir/bin/engine_workbench" "$@"
