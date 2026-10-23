#ifndef BOOTER_WORLD_ANTI_TILING_INCLUDED
#define BOOTER_WORLD_ANTI_TILING_INCLUDED

// URP Core.hlsl must be included first. Fragment-stage only: derivatives are
// captured before any cell-dependent sampling. Units are absolute world metres.
// Independently implemented triangular lattice blending; no vendor code/assets.
struct BATileTap
{
    float2 uv;
    float2 dx;
    float2 dy;
    float2 rotation; // cosine, sine: texture-space rotation
};

struct BAAntiTileContext
{
    BATileTap a, b, c;
    float3 weights;
};

float3 BAAntiTileHash(float2 cell, float seed)
{
    // Bounded arithmetic hash, with no time, object ID or terrain-local input.
    float3 p = frac(float3(cell.x, cell.y, cell.x) * float3(.1031, .1030, .0973)
        + frac(seed * .00137));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx);
}

float2 BARotate(float2 v, float2 rotation)
{
    return float2(rotation.x * v.x - rotation.y * v.y,
                  rotation.y * v.x + rotation.x * v.y);
}

BATileTap BABuildTileTap(float2 cell, float seed, float2 uv, float2 dx,
    float2 dy, float rotationRadians, float scaleVariation)
{
    float3 h = BAAntiTileHash(cell, seed);
    float angle = (h.z * 2 - 1) * clamp(rotationRadians, 0, 3.14159265);
    BATileTap tap;
    sincos(angle, tap.rotation.y, tap.rotation.x);
    float scale = 1 + (h.x * 2 - 1) * clamp(scaleVariation, 0, .5);
    tap.uv = BARotate(uv, tap.rotation) * scale + h.xy * 17;
    tap.dx = BARotate(dx, tap.rotation) * scale;
    tap.dy = BARotate(dy, tap.rotation) * scale;
    return tap;
}

BAAntiTileContext BABuildAntiTileContext(float2 worldMetres, float metresPerTile,
    float cellMetres, float seed, float rotationRadians, float scaleVariation,
    float blendSharpness)
{
    float2 uv = worldMetres / max(metresPerTile, .01);
    float2 dx = ddx(uv), dy = ddy(uv);
    float2 p = worldMetres / max(cellMetres, .01);
    // Equilateral triangles: skew XZ onto a square integer lattice.
    float2 skew = float2(p.x - p.y * .577350269, p.y * 1.154700538);
    float2 cell = floor(skew), f = frac(skew);
    float2 a, b, c;
    float3 w;
    if (f.x + f.y <= 1)
    {
        a = cell; b = cell + float2(1, 0); c = cell + float2(0, 1);
        w = float3(1 - f.x - f.y, f.x, f.y);
    }
    else
    {
        a = cell + 1; b = cell + float2(0, 1); c = cell + float2(1, 0);
        w = float3(f.x + f.y - 1, 1 - f.x, 1 - f.y);
    }
    w = pow(saturate(w), clamp(blendSharpness, 1, 8));
    BAAntiTileContext ctx;
    ctx.weights = w / max(dot(w, 1.0.xxx), 1e-6);
    ctx.a = BABuildTileTap(a, seed, uv, dx, dy, rotationRadians, scaleVariation);
    ctx.b = BABuildTileTap(b, seed, uv, dx, dy, rotationRadians, scaleVariation);
    ctx.c = BABuildTileTap(c, seed, uv, dx, dy, rotationRadians, scaleVariation);
    return ctx;
}

// Reuse ONE context for all maps of a surface. Height can be read from any
// selected channel of this same sampler; this function adds no displacement.
float4 BASampleAntiTiled(TEXTURE2D_PARAM(map, mapSampler), BAAntiTileContext ctx)
{
    return SAMPLE_TEXTURE2D_GRAD(map, mapSampler, ctx.a.uv, ctx.a.dx, ctx.a.dy) * ctx.weights.x
         + SAMPLE_TEXTURE2D_GRAD(map, mapSampler, ctx.b.uv, ctx.b.dx, ctx.b.dy) * ctx.weights.y
         + SAMPLE_TEXTURE2D_GRAD(map, mapSampler, ctx.c.uv, ctx.c.dx, ctx.c.dy) * ctx.weights.z;
}

float3 BAReorientTileNormal(float3 n, float2 rotation)
{
    return float3(BARotate(n.xy, rotation * float2(1, -1)), n.z);
}

// Planar XZ consumer basis. A vertical face parallel to world X has no projected
// X tangent, so choose projected Z instead of normalizing a zero vector. This
// keeps shading finite; it does not remove planar texture stretching on cliffs.
float3 BAPlanarNormalToWorld(float3 normalTS, float3 geometricNormalWS)
{
    float3 n = normalize(geometricNormalWS);
    float3 t = float3(1, 0, 0) - n * n.x;
    if (dot(t, t) < 1e-6)
        t = float3(0, 0, 1) - n * n.z;
    t = normalize(t);
    float3 b = cross(t, n);
    return normalize(t * normalTS.x + b * normalTS.y + n * normalTS.z);
}

// Normal textures must use Unity's Normal Map importer. The returned normal is
// in the ORIGINAL planar tangent basis, not any individual rotated tile basis.
float3 BASampleAntiTiledNormal(TEXTURE2D_PARAM(map, mapSampler),
    BAAntiTileContext ctx, float strength)
{
    float3 a = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map, mapSampler,
        ctx.a.uv, ctx.a.dx, ctx.a.dy), strength);
    float3 b = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map, mapSampler,
        ctx.b.uv, ctx.b.dx, ctx.b.dy), strength);
    float3 c = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map, mapSampler,
        ctx.c.uv, ctx.c.dx, ctx.c.dy), strength);
    return normalize(BAReorientTileNormal(a, ctx.a.rotation) * ctx.weights.x
        + BAReorientTileNormal(b, ctx.b.rotation) * ctx.weights.y
        + BAReorientTileNormal(c, ctx.c.rotation) * ctx.weights.z);
}

#endif
