# Held prop foundation and contact draft — October 14

Pickup/hold/drop/throw preparation now extends the existing registered prop owner. AP05's physical actions remain unavailable until the pose/contact family is qualified.

The prop reserves one lease, aligns its authored grip to a hand anchor without parenting or scaling its Rigidbody through the skeleton, and follows the evaluated hand pose. Attach and release acknowledge effect-key replays without applying an effect twice. Both the captured prop epoch and session epoch must remain current. Release restores authored Rigidbody/collider settings and supplies velocity once.

A separate collision guard preserves the original ignore-pair state and protects a released prop from the actor until their collider bounds separate. It survives cancellation, prop-component disable and session unbinding. It only owns collision pairs. Lost hand/owner cleanup releases the body; ordered reset restores its authored pose and clears the guard. Rebinding preserves the original reset snapshot and invalidates previous prop captures.

## Verification

[The exact source manifest](source-manifest.json), [test results](tests.xml) and [receipt](receipt.json) seal 171 passing focused Edit Mode checks, with no failures or skips. Fourteen new prop checks cover grip following, scale isolation, duplicate effects, stale captures, foreign/dead owners, invalid release velocity, hand loss, collision separation, existing ignore pairs, cancellation, disable, reset and rebind. Existing action clock, target, player, station, crouch, airborne and low-motion regressions also pass.

The [contact candidate summary](source-contact-candidates.json) records nine canonical clips and 3,298 poses sampled at approximately 90Hz on the accepted target/calibration. It contains source/import hashes and extrema, rather than complete motion timelines. The complete timelines are sealed locally at D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\ContactEvidence\PHY2PropSourceDraft3298. The existing 127-pair PHY1LowMotionQualified source seal remains byte-identical.

The candidate compiles the tested task files against the previously sealed shared runtime and isolated saved-scene fixture. Its scene differs from the current author scene; both hashes are recorded. This batch does not claim qualification of concurrent scene/rendering changes. The author scene, station prefabs, private player bindings, package versions and settings were not written.

## Qualification still required

Hand extrema and peak speed propose contact ranges; they do not accept a grip or release marker. The physical controller must evaluate its owned pose at the effect boundary, align real prop/contact fixtures, reserve and release once, and retain a qualified hold pose. Place-down requires its own transition; moving carry stays unavailable until carry locomotion is qualified.

Raw sole penetration ranges from approximately 26mm to 84mm for the pickup/throw/button sources. PushStop reaches approximately 221mm below the calibrated floor, exceeding the existing 180mm projection ceiling. Its pelvis/root gauge, planted feet and transition need fitting before push enablement. Prop/body/equipment obstruction, throwing trajectories and self-collision recovery also need paired rigged qualification. No PHY2 physical action is enabled by this receipt.

## World and session boundaries

These are authored laboratory props with stable AP05/AP06 identities and session epochs. No generated-object identity, random seed or terrain generation is introduced. Camp Loop's native terrain stays always loaded; terrain/chunk streaming is not applicable. Station parents, grips and fixture bounds constrain the work. Session teardown releases leases and scene unload destroys scene-owned objects/guards. The changes are transient laboratory state; persisted world deltas and production save integration are not applicable to this isolated test system.

No Play Mode, gameplay smoke test, foreground activation, player build or external publication was performed. Appearance and interaction feel remain author-owned acceptance.
