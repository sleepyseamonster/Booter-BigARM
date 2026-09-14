"""Focused Mac desktop-window check; uses existing built SDL, no gameplay."""
from pathlib import Path
import shlex
import subprocess

root = Path(__file__).resolve().parents[2]
build = root / 'build/foundation'
output = root / 'out/windowed-fullscreen-review'
output.mkdir(parents=True, exist_ok=True)
link = shlex.split((build / 'CMakeFiles/engine_workbench.dir/link.txt').read_text())
command = ['c++', '-std=c++20', '-I'+str(root/'Source'),
           '-I'+str(root/'.cache/probe-sources/sdl/include'),
           str(root/'UIUX/Tools/check_windowed_fullscreen.cpp'),
           str(root/'Source/Platform/Window.cpp'), '-o', str(output/'check_window')]
subprocess.run(command + link[link.index('sdl/libSDL3.a'):], cwd=build, check=True)
result = subprocess.run([str(output/'check_window')], capture_output=True, text=True)
(output/'window-check.log').write_text(result.stdout + result.stderr)
print(result.stdout + result.stderr, end='')
result.check_returncode()
