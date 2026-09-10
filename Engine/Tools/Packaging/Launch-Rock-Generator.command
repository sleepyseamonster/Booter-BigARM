#!/bin/sh
set -eu
package_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
mkdir -p "$package_dir/UserData"
if [ ! -e "$package_dir/UserData/rock.json" ]; then
    cp -n "$package_dir/bin/Assets/Recipes/rock.json" "$package_dir/UserData/rock.json"
fi
set -- --rock "$package_dir/UserData/rock.json" --save-inspection "$package_dir/UserData/inspection.json" "$@"
if [ -f "$package_dir/UserData/inspection.json" ]; then
    set -- --inspection "$package_dir/UserData/inspection.json" "$@"
fi
if [ -f "$package_dir/bin/Assets/Models/Calibration/model.json" ]; then
    set -- --model "$package_dir/bin/Assets/Models/Calibration/model.json" "$@"
fi
cd "$package_dir/UserData"
exec "$package_dir/bin/engine_workbench" "$@"
