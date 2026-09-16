# Planetary environment and landscape contract

Recorded 2026-09-15 from the user's world clarification. This document translates accepted
environmental facts into engine boundaries. It does not claim that the systems are implemented
or decide final hazard balance, visuals, sound, frequency or lore explanation.

## Accepted world facts

- Natural liquid water has been absent for roughly ten thousand years.
- Windstorms and dust storms occur.
- Electrical storms are violent and dangerous.
- The planet has a mild, persistent low vibration or resonance that can last for weeks or months.
- The resonance is not an ordinary short earthquake caused by familiar Earth-style tectonic
  slip. The planet is shifting, and accumulated resonance can shape the landscape.

## Engine interpretation

Represent the environment as authoritative structured state, not a collection of visual effects.
The simulation owns two scales.

### Persistent planetary state

`PlanetaryEnvironmentState` should eventually contain:

- stable episode ID and environment-recipe version;
- simulation start/end ticks and long-duration envelope;
- regional wind vector, gust severity and dust-loading fields;
- regional electrical potential/storm severity and stable storm-cell IDs;
- resonance amplitude, dominant frequency band, phase/coherence and regional attenuation;
- terrain-forcing revision and the latest coarse evolution checkpoint.

The exact physical cause and numerical ranges remain open. Values use game-world units with
documented conversions. Long episodes advance from the world simulation clock and survive save,
restart and region unload; they cannot depend on a particle system or an audio loop remaining
loaded.

### Local resolved state

Around active streaming anchors, coarse state resolves into bounded local effects:

- wind and dust visibility fields;
- particles, fog/aerial-density changes, lighting and audio parameters;
- electrical strike candidates and local hazard volumes;
- forces on explicitly susceptible loose bodies;
- resonance presentation and stress applied to susceptible terrain/features;
- resulting durable change candidates such as rockfall, granular creep, dune movement, fracture
  growth or a localized strike scar.

These are technical capability categories. Exact damage, materials produced by lightning,
character effects and accepted visual language remain future creative decisions.

## Terrain coupling

The terrain generator stores susceptibility rather than continuously deforming every surface:

- lithology hardness, fracture direction and weathering;
- slope stability, unsupported mass and talus capacity;
- mobile sand/dust depth and aeolian exposure;
- electrical exposure and strike attraction inputs;
- resonance sensitivity, accumulated stress and release threshold.

Historical generation may use long-term environmental forcing to shape fracture fields, talus,
dunes and exposed formations. Runtime state accumulates only bounded, gameplay-relevant change.
When a threshold releases, the engine creates a deterministic event with a stable ID. Accepted
permanent results become sparse world deltas layered over the reproducible base terrain.

The roughly ten-thousand-year dry interval is short geologically. Wind, resonance and storms may
rework surfaces and trigger local failures, but they do not justify regenerating the inherited
canyon network or replacing the base world on every visit.

## Runtime boundaries

- Do not move the entire terrain collider or world transform every frame to represent resonance.
- Do not feed visual camera shake back into physics or authoritative transforms.
- Do not simulate full atmospheric fluid dynamics, Maxwell electromagnetism or plate tectonics.
- Do not generate permanent terrain edits from nondeterministic frame-rate particle collisions.
- Do not advance weeks of missed simulation as unbounded fixed ticks when a save is reopened.

Instead:

- evaluate coarse environment intervals deterministically from stable state and elapsed
  simulation time;
- resolve high-detail effects only near active anchors and within explicit job/resource budgets;
- use analytic or aggregated catch-up for unloaded regions;
- separate continuous presentation signals from discrete physical change events;
- persist stable episode/event IDs, accepted terrain deltas and required accumulation state;
- version the base terrain, environment forcing and delta application together.

## Sensory presentation

The resonance should read as persistent planetary pressure rather than repeated cinematic
earthquake jolts. The engine should provide independent signals for low-frequency audio,
subtle view motion, prop vibration, suspended dust, distant rockfall and controller haptics.
Presentation amplitudes are user-adjustable and never define authoritative physics strength.

Electrical and dust storms consume the shared outdoor-lighting, fog, shadow, particle and audio
contracts. They may alter visibility and navigation risk without requiring the terrain generator
to run every frame.

## AI authoring and diagnostics

Future structured operations should support:

- inspect current/coarse regional environment state;
- preview a storm or resonance episode without accepting permanent changes;
- evaluate susceptible terrain and predicted bounded changes;
- apply or reject generated change events transactionally;
- compare a region before/after a forcing interval;
- validate episode continuity, event identity, resource budgets and saved deltas.

Retained diagnostics should include field visualizations, event timelines, accumulated stress,
storm/resonance revision IDs, changed-area bounds, stage timings and before/after captures.

## Implementation sequence boundary

Terrain T01 records the data/revision seam, T05 supplies dry-process and susceptibility fields,
and T12 connects mobile sediment and environmental forcing to sparse terrain deltas. A later
generic environment-runtime package should own regional storm/resonance evolution and sensory
signals. Terrain consumes that state; terrain does not own the world clock, storm scheduler,
renderer, audio engine or accessibility settings.
