# Crouch facing, grounding and author-scene installation — 2026-10-14

Seven new private derivatives preserve each crouch source's authored orientation, bake its vertical body motion and discard horizontal root motion. The original FBX bytes and importer metadata remain unchanged. Comparing 21 policies showed that body-based orientation made these strides diagonal; changing the whole character's rotation would have broken its standing walk. The policy distinction follows [Unity's root-motion documentation](https://docs.unity3d.com/6000.4/Documentation/Manual/RootMotion.html).

The contact draft uses deformed sole height and foot movement opposing the intended travel axis. Contact fragments across the loop seam merge into one plant. All four directional maps alternate R-L-R-L, with measured nominal speeds approximately 1.37m/s forward, 1.00m/s backward, 1.22m/s left and 1.08m/s right. Source limb preservation is checked in the hip frame because authored root orientation intentionally differs from the earlier projection.

Individual poses fitted the floor, but actual runtime blends penetrated it by up to about 11cm. The private correction surface has 257 curves: 32 direction samples by eight movement weights, plus idle. Each curve samples 129 semantic phases and takes the envelope across four idle phases. The existing pose driver evaluates four neighbouring curves under its single Animator output. It does not add a competing bone or physics writer.

The final intermediate-grid check contains 259 finite pose envelopes. Minimum sole height is -0.910302m against the -0.91m reference plane: 0.3mm below it. Maximum crouch body height is 1.530045m, and protected torso/head vertices retain at least 35.9mm of capsule clearance. Exact semantic plant samples in the runtime graph stay within 11.5mm of the floor. Full poses and correction curves remain private; published reports contain only aggregates and restoration identities.

The measured capsule is now 1.57m high and 0.40m in radius, preserving the original standing foot plane. AP03's roof underside is 1.625m. Radius expansion requires clearance before entry; blocked standing and failed reset retain the safe crouch pose. Sustained pose/contact time follows measured Rigidbody displacement, preventing commanded velocity against a wall from producing footsteps.

The 97-pair PHY1CrouchGroundingDraft seal includes all previous dependencies and seven additional derivatives. The former seven-role draft and its profile remain preserved. The active profile keeps contactsQualified and groundingQualified false while transition boundaries, interruption/disposal behavior, equipment and terrain contact acceptance remain unfinished. These flags prevent physical input from treating the numerical fixture as full family acceptance.

## Author scene

The author editor process was confirmed absent and the project lock was free. The saved scene still matched the sealed source hash; its older editor recovery file predates that save and was separately preserved. A guarded background installer on repository main installed all twelve prepared station areas.

The scene validator passes: nine native terrains, twelve stations, five station bodies, 364 station renderers, zero station skins, twelve original Ironstone nodes and one cache. All station returns have support and clearance. Serialization added 674 records and changed only the session transform, its station/return arrays and the camera transform's Y compensation of -0.33m. Existing Booter, Legger and terrain records are unchanged; no records were removed. TerrainData identities and bytes match the sealed recipe.

See the aggregate reports, private bundle manifest, focused test XML and receipt.json for exact proof. No Play Mode, hands-on smoke test, package change, production scene/terrain edit, source publication or external write occurred. Station physical targets remain gated by their unfinished families. The full implementation goal remains active.
