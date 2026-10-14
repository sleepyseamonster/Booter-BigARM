# Lab input and palette evidence

Date: 2026-10-13. FND3 follows the [foundation batch](../Foundations/README.md). The implementation goal remains active.

`lab-input-palette-final-tests.xml` and its log under `D:\BooterBigArmValidation\AnimationPlayground20261013\Logs\` report 74 passing focused EditMode cases: owned input identity/override copying, coactive conflict scope, held-jump modal return, East cancel/pickup priority, preference inheritance/file preservation, authored catalog/palette navigation, and existing Controls/menu regressions. Earlier runs found one diagnostic wording mismatch, corrected so the conflicting action is named. Tests never replaced the author's loaded scene.

The public catalog contains 71 motion recipes, including three restricted references; the palette offers 68 candidates. The panel is authored as a prefab and registered under the existing menu view, EventSystem/module and modal owner. It pages eight choices, exposes readiness, and requires a fresh gameplay request after selecting. A separate no-effect preview request is disabled until its clip qualifies. No physical action is implemented by the panel itself.

Only the session catalog/palette reference fields change in CampLoopTesting. Production/null configuration retains legacy bindings, UI and preference policy. Each runtime clone preserves shipped IDs and gives new bindings deterministic IDs. Physical commands disarm across focus/device/modal transitions; cancellation suppresses idle pickup when an owned lab action exists. Lab preferences use their own directory, preserve existing data, and may read compatible production choices without modifying the production file.

This slice proves input, UI and preference contracts. Rig/contact qualification, motion imports, physical action ownership and scene stations remain later gates. Source FBXs and their metadata remain private and unimported at this checkpoint. Hands-on appearance/feel acceptance is pending.
