# Legacy URP 2D Standard

This is the maintenance baseline for the isolated legacy 2D prototype. TopDown3D is the primary production product; these rules do not govern new production systems.

## Baseline

- Keep the legacy 2D Renderer asset at `Assets/_Project/Legacy2D/Settings/Rendering/URP/Renderer2D.asset`.
- Keep legacy cameras explicitly assigned to renderer index 0.
- Keep the production 3D renderer at index 1 as the project default.

Unity's 2D Renderer setup is documented in the official URP 2D manual. The preserved legacy lane follows that pattern. See:
- [Set up the 2D Renderer asset in URP](https://docs.unity3d.com/kr/6000.0/Manual/urp/Setup.html)
- [2D lighting in URP](https://docs.unity3d.com/kr/6000.0/Manual/urp/2d-index.html)

## Sorting Standard

- Use `Sorting Layer` and `Order in Layer` as the primary render-order controls for sprites.
- Use `Sorting Group` on multi-sprite prefabs and complex characters so child renderers stay together.
- Prefer explicit sorting layers over relying on tie-break behavior.
- Keep prefab render order predictable across instances.

Unity's sorting docs state that `Sorting Layer` and `Order in Layer` are the main 2D sorting controls, and that `Sorting Group` is for grouping renderers with a shared root. See:
- [Use sorting groups](https://docs.unity3d.com/kr/6000.0/Manual/sprite/sorting-group/use-sorting-groups.html)
- [Sorting group reference](https://docs.unity3d.com/kr/6000.0/Manual/sprite/sorting-group/sorting-group-reference.html)
- [2D Sorting](https://docs.unity3d.com/ja/2022.3/Manual/2DSorting.html)

## Camera And Readability

- Keep the top-down camera setup simple and readable.
- Treat sorting layers and sorting groups as the primary way to express depth and overlap.
- Use camera sorting settings only when a specific visual need requires it.
- Keep the render path compatible with pixel-perfect presentation on a 32px art scale.
- Favor crisp sprite edges over subpixel softness in the base look.

This is partly an inference from Unity's sorting behavior: in 2D, renderer order is driven first by sorting layer and order, then by camera distance and other tie-breakers. For a top-down game, explicit sorting rules are safer than depending on distance alone.

## Lighting Standard

- Use 2D lights intentionally and sparingly.
- Prefer lighting that supports gameplay readability instead of filling every scene with lights.
- Use Shadow Caster 2D and sprite-lit materials only where they improve the scene.

Unity's 2D lighting documentation covers light types, sprite preparation, tilemap lighting, shadow casting, and optimization. See:
- [2D lighting in URP](https://docs.unity3d.com/kr/6000.0/Manual/urp/2d-index.html)
- [Prepare and upgrade sprites for 2D lighting in URP](https://docs.unity3d.com/kr/6000.0/Manual/urp/PrepShader.html)
- [Create shadows with Shadow Caster 2D in URP](https://docs.unity3d.com/ja/current/Manual/urp/2DShadows.html)

## Practical Rule Set

1. Use URP 2D only inside the isolated legacy lane.
2. Use sorting layers and sorting groups before special camera logic.
3. Keep lighting purposeful and readable.
4. Keep legacy render assets inside `Assets/_Project/Legacy2D/Settings/Rendering/URP/`.
5. Keep post-processing restrained enough that the pixel art remains sharp.
