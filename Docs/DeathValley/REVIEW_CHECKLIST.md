# Greater Wasteland baseline review

Use this review before integrating adjoining terrain. The Player is a baseline package, not a performance acceptance result. Hands-on review belongs to the user; this task does not launch or navigate gameplay automatically.

## Coverage review

Open [the atlas](./coverage_atlas.html). Confirm that the existing 4.096 km Badwater terrain is the intended starting area and compare the west, north and east adjoining proposals. West is the current first-batch recommendation because its source coverage and shared border have been verified. The proposed west batch is basin-floor terrain; choose another direction if ridges or canyon approaches are the immediate priority.

The full regional rectangle includes land outside the official park. Decide the eventual playable extent from geography and desired exploration rather than using that source rectangle as an accidental game boundary.

## Player comparison setup

Use the successful Windows Development Player recorded in [verification evidence](./VERIFICATION.md), at the proposed 1920 × 1080 window and unchanged full-content rendering. Keep the same actual quality tier, resolution and camera poses for later comparisons. Record the quality tier instead of assuming that the serialized Editor selection is the Player selection. Do not opt into the reduced stress profile: it is not representative of the intended view.

Close unrelated heavy work when taking repeatable measurements. Record a cold launch and a repeat launch separately. The current comparison machine and provisional frame targets are recorded in [windows_baseline_configuration.json](./windows_baseline_configuration.json); memory and startup acceptance thresholds remain unset until measured.

## Views and traversal

| Check | Record |
| --- | --- |
| Basin floor and eastern mountain horizon | Same-view capture, camera pose, frame-time distribution and visible terrain detail |
| Ordinary chunk boundary | Any crack, height discontinuity, falling or collision interruption |
| Four high-detail chunks and their perimeter | Close surface readability and continuity with surrounding terrain |
| Companion movement on an intended route | Physical traversal, blocked slopes and any false route or missing ground |
| Selected slope or mountain approach | Whether the terrain is usable for the intended route; steep inaccessible terrain is not automatically a defect |
| Startup and memory | Cold/repeat launch times and native process/terrain/texture memory |

Existing performance telemetry can support frame and timing observations, but it does not replace native-memory or GPU profiling. A visible symptom, source-height check and frame-rate measurement are different findings; record them separately. Geographic color is provisional, so a ground-art issue should not be confused with missing elevation or broken collision.

## Return to implementation

Provide the chosen first expansion direction and any baseline defects or performance measurements. Fix blocking coordinate, terrain or collision problems first. If the baseline is sound, prepare one adjoining batch and compare it against the recorded views. Defer art polish that does not block integration, and introduce collision/asset loading only for measured cost or a justified larger coverage requirement.

Greater Wasteland remains the primary scene through the coordinated promotion task. This terrain expansion adds no procedural generation. A future procedural design and its generated-content/save integration require their own approved work.
