# Radial Menu Polish Audit

Completed 2026-10-06 against the [system design](./RADIAL_MENU_SYSTEM_DESIGN.md) and its interactive preview. The Compass Ring remains the selected design. North opens player inventory, south calls the Legger over, and west selects canister placement. The audit refines presentation and interaction; it does not change gameplay quantities, companion traversal rules, or Unity assets.

## Findings and applied refinements

| Finding | Effect on feel | Refinement applied to preview |
| --- | --- | --- |
| Large rectangular amber blocks sat on circular sectors. | Selection felt like a grid button over a wheel, and emphasized too much surface. | Use a restrained sector tint, an amber perimeter arc, and icon/label emphasis. Keep the center and unselected sectors neutral. |
| Entry labels repeated full titles and long result descriptions. | The wheel felt crowded, especially at narrow widths. | Use Inventory, Call Legger, and Dust Canister on the ring; retain full names in the fixed detail area and accessible labels. Empty east uses a quiet dash. |
| Outcomes appeared in both the handoff panel and detail area. | Duplicate messages competed for attention after activation. | Hide the selection detail area during a handoff. One result panel owns the feedback. |
| Preview buttons and simulated availability controls were always visible. | Debugging controls looked like part of the gameplay menu. | Put them in a collapsed Preview controls disclosure. Gameplay retains only selection, consequence and binding hints. |
| Every selection reconstructed the entry buttons. | Keyboard focus could disappear; repeated DOM/icon work had no visual benefit. | Retain entry and assignment-button nodes, update selection attributes, and replace icon content only when an assignment changes. |
| Pointer selection immediately switched at every angular boundary. | Minor jitter could alternate adjacent sectors. | Apply the specified 5-degree angular hysteresis. Center clear overrides hysteresis. No dwell delay was added. |
| Switching from Customize or Cancel edits silently discarded the draft. | Experimenting with layout could lose work unexpectedly. | Ask Discard changes or Keep editing only when the draft changed. Apply keeps the active page by stable ID. |
| Page deletion occurred immediately. | One mistaken press could remove a configured page from the draft. | Confirm page deletion, retain at least one page, and preserve draft-only semantics until Apply. |
| A host state event could overwrite an open draft. | The editor could change beneath the player. | Ignore incoming preference state while unsaved editing is active; a matching save echo does nothing. Full production storage still needs its version/overwrite policy. |

## Visual hierarchy

The eye should first find the selected direction, then the action name, then its consequence. Use amber for selected geometry and action affordances; use warm neutral text for unselected labels. Avoid assigning a different hue to every sector. A quantity badge belongs only to an item action and remains in a consistent position.

The ring retains all four angular regions. Unavailable items and empty sectors never collapse, move or replace another action. Unavailable selection shows a clear center status and the specific reason below. The readable label remains present, so the player can learn its direction even without the item.

Use one small page name and one compact line of binding hints. In production, prompts show the current device's bindings rather than combined keyboard/controller labels; the preview deliberately shows both to make its available interactions discoverable. Header tabs and Preview controls are demonstration/navigation chrome, not requirements for the gameplay overlay.

Preserve the production design's 420 reference-unit diameter. The browser preview uses a contained responsive ring; it is not a pixel specification for a Unity screenshot. The icon/label hierarchy must still be reviewed at the production camera distance and actual UI scale.

## Responsiveness and controller feel

There is no added input delay, hover dwell, tooltip requirement or animation gate. Opening and hit testing remain immediate. The preview adds only an 80 ms opacity/color transition; reduced-motion preference removes it. Do not animate the selected arc through intermediate sectors or wait for opening animation before accepting a quick selection.

Center return always clears selection. Crossing a boundary must be intentional, but the player should not need exaggerated stick movement. Retain separate pointer and processed-stick thresholds and check the current 5-degree hysteresis on hardware before changing it. No second Input System deadzone processor is added.

Cancel, focus/device loss, explicit confirmation and opener release must retain the system specification's precedence and exactly-once contract. A sleek appearance cannot compensate for an accidental command. The existing browser handoff checks remain green after visual changes.

Keep the customization screen permissive and predictable: choosing an assignment does not execute it; Apply changes the live layout; Cancel retains the old one; unsaved departure gives a clear choice. Native button focus remains visible. Every sector has an accessible label containing direction and full command name, even when its visible label is shortened.

## Verification

The refined preview passed headless Edge checks for default Q hold/release handoffs, cancel suppressing release, missing-canister feedback, separate placement confirmation, draft discard/Apply, page creation/switching, pointer boundary hysteresis, center clear, entry/assignment focus continuity, dirty-draft departure protection, canceled page deletion, and protection from incoming state during editing.

Dark and light layouts were inspected at 320 and 736 pixel widths, with no horizontal overflow. Representative dark desktop and light narrow renders were visually reviewed. The existing check also proves non-overlapping entry targets. The preview remains a simulation: these results do not establish physical-controller comfort, Unity frame timings, authored placement integration, or actual Legger arrival.

Measured text contrast against the sector surface:

| Theme | Primary label | Secondary label |
| --- | --- | --- |
| Dark | 10.59:1 | 6.90:1 |
| Light | 9.85:1 | 5.04:1 |

These opaque surface/token combinations exceed the 4.5:1 target for standard important text in [Xbox Accessibility Guideline 102](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102). This check does not certify every hover, icon, disabled control, blended background or player opacity setting. Production must measure those separately. Hold alternatives and remapping remain governed by [Guideline 107](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107).

Diagnostics remain in ignored `Logs/radial-polish-*` and `Logs/radial-system-preview-check.cjs`. Generic icon rendering uses the conversation host and is not established by the standalone browser capture.

## Remaining acceptance

The refined Compass Ring is ready for the user's next design review. Hardware acceptance should cover fast cardinal selections, accidental diagonal inputs, neutral cancellation, repeated open/cancel, switching prompt devices, and editing a full page without a mouse. Unity acceptance must also prove mode transition safety, live cargo range, reachable callover stopping distance and separate authored-ground canister placement. Global time and movement policies remain as specified; this audit does not introduce slow motion, automatic movement, streaming or procedural generation.
