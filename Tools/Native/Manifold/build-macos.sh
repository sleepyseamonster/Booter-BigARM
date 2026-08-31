#!/usr/bin/env bash
set -euo pipefail

readonly MANIFOLD_VERSION="3.5.2"
readonly MANIFOLD_COMMIT="11235e6b8ebea2dbed8aec4285685aafd3d95667"
readonly SOURCE_SHA256="1e17743a7a0a2c07e9258f5618494c5caa2527063af31b46e4c24947657fe5ef"
readonly ARCHIVE_URL="https://github.com/elalish/manifold/archive/refs/tags/v${MANIFOLD_VERSION}.tar.gz"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../../.." && pwd)"
plugin_dir="${repo_root}/Assets/_Project/Plugins/Manifold/macOS"
unity_lock="${repo_root}/Temp/UnityLockfile"

if [[ -e "${unity_lock}" ]]; then
  echo "Refusing to replace a native Unity plugin while this checkout has an active UnityLockfile." >&2
  echo "Close the editor for this checkout, then run this build again." >&2
  exit 2
fi

build_root="$(mktemp -d /tmp/booter-manifold-build.XXXXXX)"
trap 'rm -rf -- "${build_root}"' EXIT

archive="${build_root}/manifold-v${MANIFOLD_VERSION}.tar.gz"
curl --fail --location --silent --show-error "${ARCHIVE_URL}" --output "${archive}"
actual_source_sha="$(shasum -a 256 "${archive}" | awk '{print $1}')"
if [[ "${actual_source_sha}" != "${SOURCE_SHA256}" ]]; then
  echo "Manifold source checksum mismatch: ${actual_source_sha}" >&2
  exit 3
fi

tar -xzf "${archive}" -C "${build_root}"
source_dir="${build_root}/manifold-${MANIFOLD_VERSION}"
build_dir="${build_root}/build"
install_dir="${build_root}/install"

cmake -S "${source_dir}" -B "${build_dir}" -G "Unix Makefiles" \
  -DCMAKE_BUILD_TYPE=Release \
  '-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64' \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0 \
  -DCMAKE_INSTALL_PREFIX="${install_dir}" \
  -DBUILD_SHARED_LIBS=ON \
  -DMANIFOLD_CBIND=ON \
  -DMANIFOLD_CROSS_SECTION=ON \
  -DMANIFOLD_PAR=OFF \
  -DMANIFOLD_TEST=ON \
  -DMANIFOLD_PYBIND=OFF \
  -DMANIFOLD_JSBIND=OFF \
  -DMANIFOLD_USE_BUILTIN_CLIPPER2=ON

cmake --build "${build_dir}" --parallel
ctest --test-dir "${build_dir}" --output-on-failure
cmake --install "${build_dir}"

mkdir -p "${plugin_dir}"
cp "${install_dir}/lib/libmanifold.${MANIFOLD_VERSION}.dylib" \
  "${plugin_dir}/libmanifold.dylib"
cp "${install_dir}/lib/libmanifoldc.${MANIFOLD_VERSION}.dylib" \
  "${plugin_dir}/libmanifoldc.dylib"

install_name_tool -id @rpath/libmanifold.dylib \
  "${plugin_dir}/libmanifold.dylib"
install_name_tool -id @rpath/libmanifoldc.dylib \
  "${plugin_dir}/libmanifoldc.dylib"
install_name_tool -change @rpath/libmanifold.3.dylib \
  @loader_path/libmanifold.dylib \
  "${plugin_dir}/libmanifoldc.dylib"

echo "Manifold v${MANIFOLD_VERSION} (${MANIFOLD_COMMIT}) installed:"
file "${plugin_dir}/libmanifold.dylib" "${plugin_dir}/libmanifoldc.dylib"
shasum -a 256 "${plugin_dir}/libmanifold.dylib" "${plugin_dir}/libmanifoldc.dylib"
