#pragma once
#include <array>
#include <vector>

namespace engine {
struct FixtureVertex { float x, y, z, nx, ny, nz; };
using FixtureMesh = std::vector<FixtureVertex>;
using Matrix4 = std::array<float, 16>;

// Column-major affine matrices, column vectors, right-handed coordinates.
// Throws for nonfinite, nonaffine, or singular/near-collinear transforms.
Matrix4 normalMatrix(const Matrix4& model);
FixtureMesh fixtureCube();
FixtureMesh fixtureSlopedSolid();
FixtureMesh fixtureSphere();
FixtureMesh fixtureCapsule();
// Independent flat-normal reference: recompute normals from transformed edges.
FixtureMesh bakeFlatReference(const FixtureMesh& mesh, const Matrix4& model);
}
