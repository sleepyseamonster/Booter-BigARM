# Fitted slide and roll controls — 2026-10-14

Sliding and selected roll landing are enabled through the existing player and station controls. Sprint forward and press the crouch command to slide. Select Falling To Roll in the palette before a jump or drop to use the physical contact handoff. Cancellation completes the active roll; blocked recovery retains posture or hands off to crouch. Existing pause, ownership, reset and disposal rules continue to apply.

Six new private equipped derivatives account for body and backpack contact without changing the original downloads or earlier derivatives. The backpack is a real contact surface during the roll. The measured equipped low envelope selects a 0.76117m longitudinal capsule radius, keeping the original foot plane. The authored AP03 roof remains unchanged.

The driver's readable runtime cloak copy now bends to a bounded ground plane through inverse weighted skinning. It preserves the rig's bones, original assets, both chest/hips influences and one Animator output. It supports one, two and four skin weights, restores original mesh positions/normals/bounds after release and retains the constraint through outgoing pose blends. Body/backpack projection keeps its existing 18cm limit; garment displacement has a separate 60cm bound.

The final 108 dense envelopes include source phases, ordinary boundaries, sprint/landing entries and crouch handoffs. The reduced contact cache contains 126 sampled vertices. It matches the full critical-body cache on this grid; CPU skinning agrees within one micrometre. Body contact clears the reference plane within 0.001mm, and the garment keeps approximately 10mm of floor clearance. Maximum body correction is below 18cm and maximum garment displacement below 60cm. These are deterministic geometry checks, not appearance or feel acceptance.

All 143 focused Edit Mode checks pass without failures or skips. Controller fixtures cover both original and equipped collider profiles, plus pause, cancellation, support, reset/retry, effects, handoff and disposal. Player and station fixtures now assert slide/roll readiness. The isolated candidate includes the previously sealed committed runtime/riding/workstation boundary and its saved-scene snapshot; later unrelated source/scene changes remain outside this proof.

The PHY1LowMotionQualified private seal contains 127 asset/metadata pairs: all 119 prior assets, six new equipped clips and the fitted profile/cache. The player binding selects the fitted profile. Original FBX bytes, importer metadata, old derivatives/profile/cache and GUIDs remain exact.

The lab uses authored identity and nine resident native terrains; this fitting adds no generation, streaming or persisted deltas. No author-scene save, terrain edit, package change, Play Mode, gameplay smoke test or publication occurred. Author appearance/feel review, the remaining PHY1 locomotion gaps and other requested interaction families remain in the active goal.
