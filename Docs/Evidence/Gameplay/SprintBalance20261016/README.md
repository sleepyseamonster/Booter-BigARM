# Camp Loop sprint balance — October 16, 2026

The author requested slightly faster sprint movement and slightly slower animation. Full gamepad sprint is now **6.0 → 6.15 m/s (+2.5%)**, with **2.5% slower cadence**, installed in the existing main CampLoopTesting native-ground setup.

## Calibration and scope

The distance-driven clock uses an effective sprint cycle of **5.483365 m**, previously 5.215884 m: `oldCycle * (6.15 / 6) / 0.975`. This is a deliberate stride calibration, not a new geometric measurement. Native clip curves, source GUID, contact phases and signed height maps remain unchanged. The lower four gait calibrations remain byte-for-byte intact at 1.4, 3, 4 and 5 m/s; normal gamepad fast run and keyboard pacing retain their existing bindings.

The forward LT limit and native continuation air limit both rise to 6.15 m/s so full sprint is available while facing is locked and held steering does not bleed sprint-jump speed toward the old 6 m/s cap. Validators allow only the finite 6–6.15 m/s interval, retaining compatibility with the previous profiles. Lateral/backward caps remain 5/4 m/s, including their existing continuous diagonal blend. Jump gravity remains 1.6× with the same impulse, landing policy and timing. No global Animator speed changes are applied.

Only transient actor motion on fixed, always-loaded terrain changes. World identity, streaming/unload, stable generated-object identity and persisted deltas are not applicable because this tuning creates or changes none. Authored terrain and collision/slope constraints are preserved. Greater Wasteland production promotion remains separate from this CampLoopTesting follow-up.

## Verification and installation

[Checks and installation](checks-and-install.json) record **93 passing focused EditMode tests**: expanded forward 8, directional 13, continuation 28, airborne 37 and native ground 7. Existing checks now include 6.15 m/s alongside the preceding speeds. They verify continuous source-rate-bounded gait blends, distance/contact clocks, gamepad pace routing, LT direction limits, jump momentum/run-out and equipped single-output poses at one, two and four bone influences. The native family sweep covers **23,040 poses** on five main-camp terrain sites, four facings and eight headings, plus **840 grounded action poses**. Existing clearance tolerances are retained.

The retained validation scene has an older lab/lighting/hierarchy layout. A final [input-contract audit](candidate-input-contract.json) verified all 18 native terrain/collider records, nine composed terrain world transforms, nine TerrainData asset/meta pairs and 161 other protected inputs against the current checkout. A read-only check of the live author scene matched all nine active native colliders to those qualified transforms and confirmed clean TerrainData. Greater Wasteland was not loaded by these checks.

The loaded author code also accepted 6 and 6.15 m/s while rejecting 5.99, 6.16, NaN and infinity for both calibrated limits. Installation verified three exact private profile payloads, unchanged paired metadata, eight sealed task sources and **163 unchanged protected files**. The same eight scene root IDs and dirty scene state were retained; no scene save or reload occurred. The private profile diff consists of four scalar changes. [Calibration values and hashes](calibration.json) make that local-only change reviewable.

Tests and imports ran in the existing Editor or a marked background validation project, preserving the author's foreground app. No Play Mode, gameplay smoke, actual control driving or physics trajectory simulation ran. Offline checks establish source/pose and command contracts; they do not accept real-time balance, perceived foot sliding or physical traversal. The author should compare held RB sprint, the blend from fast run, LT forward/diagonal movement and a sprint jump in the main camp area.

## Recovery

Immutable starting profile pairs and source files, qualified profile pairs, exact calibration diff, full test responses and guarded install/rollback commands are retained locally under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/SprintBalance20261016/`. Animation payloads remain private and ignored by Git. Restoring the three `before/.../PrivateBindings/*.asset` payloads with targeted stable-EditMode imports restores the previous 6 m/s sprint/caps and cadence; preserve their existing metadata and the author's scene. Do not restore an entire candidate scene or overwrite unrelated work. The previous 6 m/s profiles remain valid with this code.

## Local commit boundary

During closeout, concurrent inventory work briefly included this pass's staged files in `645b1472c16fd88c5a5c9d32979c87096169ed90`; that concurrent task subsequently returned the branch to its preceding checkpoint. This pass then committed only its own source/test/evidence paths, preserving the inventory entries in the shared index. Task ownership is the three runtime guards, five test files and four files in this evidence directory. No history rewrite or push was performed by this tuning task.
