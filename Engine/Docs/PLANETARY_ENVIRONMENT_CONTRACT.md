# Planetary environment and landscape contract

Recorded 2026-09-15 and corrected after the user's clarification that planetary resonance is
lore, not an ambient gameplay simulation. This document translates accepted environmental facts
into engine boundaries. It does not claim that the systems are implemented or decide final
hazard balance, visuals, sound, frequency or lore explanation.

## Accepted world facts

- Natural liquid water has been absent for roughly ten thousand years.
- Windstorms and dust storms occur.
- Electrical storms are violent and dangerous.
- The setting includes the idea of a mild, long-duration planetary resonance or shifting world.
  This is lore context; the game does not need to vibrate or simulate it continuously.
- If an earthquake appears in the game, it will be an explicitly authored story moment.

## Engine interpretation

Wind, dust and electrical storms may become authoritative structured environment state rather
than a collection of unrelated visual effects. Resonance does not belong in that runtime state.
The engine needs no ambient earthquake subsystem, resonance clock, regional vibration field,
terrain-fatigue accumulator or automatic resonance-driven landscape mutation.

### Persistent storm state

A future `PlanetaryEnvironmentState` may contain:

- stable storm episode/cell IDs and environment-recipe version;
- simulation start/end ticks and intensity envelope;
- regional wind vector, gust severity and dust-loading fields;
- regional electrical potential/storm severity;
- terrain-forcing revision and the latest coarse evolution checkpoint.

Exact numerical ranges remain open. Long storms advance from the world simulation clock and
survive save, restart and region unload; they cannot depend on a particle system or audio loop
remaining loaded.

### Local resolved state

Around active streaming anchors, coarse storm state may resolve into bounded local effects:

- wind and dust visibility fields;
- particles, fog/aerial-density changes, lighting and audio parameters;
- electrical strike candidates and local hazard volumes;
- forces on explicitly susceptible loose bodies;
- resulting durable change candidates such as dune movement or a localized strike scar.

These are technical capability categories. Exact damage, materials produced by lightning,
character effects and accepted visual language remain future creative decisions.

## Terrain coupling

The terrain generator may store storm susceptibility:

- mobile sand/dust depth and aeolian exposure;
- slope stability and talus capacity where storm wind can remobilize loose material;
- electrical exposure and strike-attraction inputs.

Historical geology may use authored fracture fields that aesthetically reflect the setting's
shifting-world lore, but this is ordinary generation data. It does not create runtime resonance
state. Runtime storms accumulate only bounded, gameplay-relevant change. Accepted permanent
results become sparse world deltas layered over reproducible base terrain.

The roughly ten-thousand-year dry interval is short geologically. Storms may rework surfaces,
but they do not justify regenerating the inherited canyon network or replacing the base world on
every visit.

## Scripted earthquake boundary

An earthquake is story content, not a generic recurring simulation. A future scripted sequence
may request bounded presentation and authored physical events:

- camera, audio, haptic, dust and prop-motion envelopes;
- explicitly selected rockfalls, collapses or route changes;
- authored hazard volumes and timing;
- deliberate permanent deltas when the story calls for them.

The sequence owns its start, duration, affected area and accepted results. Camera shake never
defines physics strength. No terrain collider or global world transform is oscillated. No
unscripted earthquake is synthesized from elapsed time or lore state.

## Runtime boundaries

- Do not implement continuous planetary vibration or ambient earthquake gameplay.
- Do not add resonance amplitude, frequency, phase, stress or fatigue to ordinary world saves.
- Do not move the entire terrain collider or world transform to represent an earthquake.
- Do not feed camera shake back into physics or authoritative transforms.
- Do not simulate full atmospheric fluid dynamics, Maxwell electromagnetism or plate tectonics.
- Do not generate permanent terrain edits from nondeterministic frame-rate particle collisions.
- Do not advance missed storm simulation as unbounded fixed ticks when a save is reopened.

Instead:

- evaluate coarse storm intervals deterministically from stable state and elapsed simulation time;
- resolve high-detail storm effects only near active anchors and within explicit budgets;
- use analytic or aggregated catch-up for unloaded regions;
- separate presentation signals from discrete physical change events;
- persist stable storm/event IDs and accepted terrain deltas;
- invoke earthquake presentation and changes only from an authored story sequence.

## AI authoring and diagnostics

Future structured operations may support:

- inspect current/coarse regional storm state;
- preview a storm without accepting permanent changes;
- preview an authored earthquake sequence in an isolated revision;
- apply or reject bounded generated or authored change events transactionally;
- compare a region before/after a forcing interval or scripted sequence;
- validate event identity, affected bounds, resource budgets and saved deltas.

Retained diagnostics should include field visualizations, event timelines, storm revision IDs,
changed-area bounds, stage timings and before/after captures.

## Implementation sequence boundary

Terrain T01 records the storm forcing/revision seam, T05 supplies dry-process fields, and T12
connects mobile sediment and storm forcing to sparse terrain deltas. A later generic environment
package may own regional storm evolution. A future narrative/sequence system owns any earthquake.
Terrain owns neither the world clock, storm scheduler, story timing, renderer, audio engine nor
accessibility settings.
