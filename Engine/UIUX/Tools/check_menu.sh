#!/bin/sh
set -eu
engine_dir=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
output_dir="$engine_dir/out/uiux-menu"
mkdir -p "$output_dir"
"${CXX:-c++}" -std=c++20 -I"$engine_dir/Source" -I"$engine_dir/.cache/probe-sources/imgui" \
  "$engine_dir/UIUX/Tools/check_menu.cpp" "$engine_dir/build/foundation/libengine_imgui.a" \
  -o "$output_dir/check_menu"
"$output_dir/check_menu"
