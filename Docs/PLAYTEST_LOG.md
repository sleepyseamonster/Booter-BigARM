# Playtest Log

This log turns game-design questions into repeatable observations. Record what happened; do not retrofit the result to match the intended design.

## Session Template

### YYYY-MM-DD — Question Or Slice

- **Build / commit:**
- **Unity version and scene:**
- **Design question:**
- **Setup and duration:**
- **Player / observer:**
- **Expected signal:**
- **Observed behavior:**
- **Friction, confusion, or failure:**
- **What worked:**
- **Evidence:** notes / screenshot / capture / telemetry path
- **Interpretation:**
- **Decision:** none / link to `DECISION_LOG.md`
- **Next experiment:**

## Rules

- Change one major variable at a time when practical.
- Separate observation from interpretation.
- Tie results to an exact commit or build and the scene used.
- A single session can reveal a problem but rarely proves a broad balance claim.
- Promote stable conclusions into the appropriate canon, roadmap, or implementation standard.
- Hands-on smoke testing is user-owned. Codex agents do not create or run smoke tests unless the user explicitly requests them.

### 2026-08-13 — Perspective Foundation Review

- **Build / commit:** `828d52f`
- **Unity version and scene:** Unity `6000.4.0f1`, `TopDown3DPrototype`
- **Design question:** Is the initial perspective top-down 3D direction sound enough to continue?
- **Player / observer:** user
- **Observed behavior:** The user reported that everything looked good and approved continuing the planned sequence.
- **Evidence:** user hands-on review in the project task
- **Interpretation:** The separate 3D foundation is accepted as the working direction; this does not establish physical-controller feel or extended traversal reliability.
- **Decision:** continue with traversal hardening and conventional right-stick camera rotation
- **Next experiment:** user checks camera orbit and movement while crossing multiple chunk boundaries on a physical gamepad

### 2026-08-13 — Landscape Framing Review

- **Unity version and scene:** Unity `6000.4.0f1`, `TopDown3DPrototype`
- **Design question:** Does the camera show enough of the surrounding landscape?
- **Player / observer:** user
- **Observed behavior:** The camera felt too close to Booter and did not show enough of the generated landscape.
- **Evidence:** user hands-on review in the project task
- **Interpretation:** Pull the perspective camera back modestly while expanding streamed terrain far enough to preserve every supported orbit angle.
- **Decision:** increase the camera distance from 16 to 18 world units and the steady streaming radius from four to five chunks; keep the immediate two-chunk ring and frame budget unchanged.
- **Next experiment:** user checks character readability, landscape visibility, and empty-space coverage at the shallowest pitch while orbiting through a full turn.

### 2026-08-13 — Expanded Landscape Framing

- **Unity version and scene:** Unity `6000.4.0f1`, `TopDown3DPrototype`
- **Design question:** Is the initial 18-unit pullback visually substantial enough?
- **Player / observer:** user
- **Observed behavior:** The user did not perceive enough change and requested a camera distance of 25 world units.
- **Evidence:** user hands-on review in the project task
- **Decision:** set the camera distance to 25 and expand the steady streaming radius to seven chunks without changing pitch, FOV, the immediate ring, or the per-frame build budget.
- **Next experiment:** reopen the scene from disk, then check landscape visibility, character readability, and empty-space coverage through a full orbit.
