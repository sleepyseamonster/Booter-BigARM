# Batch 6 Pre-Cutover Landscape Baseline — 2026-08-24

## Authority and purpose

The user approved a protective commit of the existing dirty production-landscape files that Batch 6 must replace or adapt. This baseline is a restoration point only. It is not approval of the old scalar terrain authority and it does not make the temporary Fractured Transect proof profile canon.

## Protected file set

| File | SHA-256 before cutover |
| --- | --- |
| `TopDown3DChunkMeshBuilder.cs` | `26e9f559a631f8311946d4e68b7ed8273fb53d7c5319a0be524cf3bec0d8a545` |
| `TopDown3DGeneratedChunk.cs` | `cb8c6ef1ea3d8a538e0024ac8ff9f1b0ec5731d08f787159e36d08b8326d6773` |
| `TopDown3DProceduralWorld.cs` | `0e29534abcb7c945237ba6a425aa267dfb50d275c4575760fa04e1ad9f453dc1` |
| `TopDown3DFarLandscape.cs` | `ee7624c31616c9ed6279f37d4185a23eea5b198b70e8edb485506cd42c52a1e7` |
| `TopDown3DGeologyProfile.cs` | `7bcf02d4f9cbce23da4bdbd031344f8832d746cb9adf9bc59ba5f5642224b4f3` |
| `TopDown3DWorldGenerator.cs` | `426cdc0b59147b3f03420a1f071f0e89c2875a349f76b5569465a4d0f7fc4983` |
| `TopDown3DWorldSurfaceSample.cs` | `06ef125504d1e2cd2077ecf819845e68a34531bf19860bb606ac2a892a7b1e46` |
| `TopDown3DWorldGeneratorTests.cs` | `09e1e555a341b32af13ea29e0f1aa843688a883970d0319d040693c47f527912` |

Their Unity `.meta` files are protected with them where present. The dirty production scene, world-settings asset, other gameplay systems, art, materials, and unrelated documentation remain user-owned and are deliberately excluded.

## Verification state

- Unity 6000.4.0f1 compiled the current dirty worktree successfully.
- `BooterBigArm.Tests.TopDown3DWorldGeneratorTests`: 9 passed, 0 failed (`/tmp/booter-precutover-worldgen.xml`).
- The existing production-scene validator has one pre-existing failure group outside this protected set: the scene is missing the pending packing settings and four BigARM save/cargo components (`/tmp/booter-precutover-validator.xml`).
- An unfiltered 235-test run was stopped after sustained non-terminating CPU work and produced no result receipt. It is not counted as proof.

## Cutover rules

- Batch 6 must replace the scalar macro terrain authority rather than preserve two live terrain authorities.
- Absolute World Creator identity and topology version 2 must drive generated keys.
- The temporary non-canon coordinate and Fractured Transect adapters must remain replaceable and visibly non-canon.
- Reverting the eventual Batch 6 cutover commit must restore this protected baseline without touching unrelated dirty work.
