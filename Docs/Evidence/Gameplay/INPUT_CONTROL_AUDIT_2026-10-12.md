# Production input and control audit — October 12, 2026

**October 13 supersession:** the author removed gamepad Square/West cargo opening. Square now performs an ordinary world interaction on press; Triangle/North continues to open inventory. The retained findings below describe the earlier tap/hold repair. The control map reflects the current gamepad behavior; the keyboard E pair remains at the shipped 0.35-second default, with its existing customization.

Requested by the author after Legger inventory interfered with ironstone mining and canister pickup. Scope: the production TopDown3D router, input asset, interaction/action controllers, inventory, radial, system menu, camp, movement/camera and algae consumers. Root AGENTS and the input standard control the work. Existing dirty production terrain, materials and terrain receipts remain outside this task.

## Findings and corrections

1. **Cargo stole the interaction target.** `TopDown3DInteractionController` included Legger cargo in its nearest-target scan. `TopDown3DPlayerActionController` opened cargo from the same command used to mine and pick up canisters. Cargo is now excluded from that scan. Separate `Gameplay/OpenCargo` holds West/E for 0.45 seconds; ordinary `Gameplay/Interact` taps the same controls and performs on release. Holding cannot both start a world action and open inventory.
2. **Cargo opening needed an independent physical gate.** The hold command checks the existing same-scene, access-range and line-of-sight authority, independently of facing/current world target. Mining, cargo waiting, pickup and placement block opening. The UI rechecks access before opening. This never recalls or teleports a distant companion.
3. **Held openers crossed into inventory commands.** Inventory now records physically held Submit, Click, QuickTransfer and panel controls when opening/resuming. Those controls are ignored until release. A fresh West press after opening can quick-transfer normally. The West/E cargo prompt appears alongside the world tap prompt when cargo is reachable.
4. **D-pad diagonals could deploy while algae rejected them.** Deployment now joins all three algae commands in end-of-update arbitration. Diagonals, simultaneous dose/deploy inputs and modal transitions reject the whole request. All four require release before another command.
5. **Keyboard navigation could immediately move Booter after closing inventory.** Keyboard movement now receives the same neutral requirement as the gamepad when returning from inventory, menus or camp.
6. **Retained actions imply mechanics that do not currently exist.** Jump (South/Space), Attack (mouse/Enter), Scout (F2), danger ping (right-stick click/F3), Previous/Next (1/2), Gameplay/OpenInventory and System Save/Load/Rebuild/NextSeed have no active production consumers. F5–F7 overlap algae only with dormant debug actions. They were preserved to avoid changing unrelated action contracts; these are not advertised as working production controls. Implementing combat, jumping, scouting or debug saves is outside this repair.

## Active control map

Binding text is illustrative; runtime prompts resolve the connected device and effective action binding.

| Context/action | Gamepad | Keyboard/mouse |
|---|---|---|
| Move | Left stick | WASD / arrows |
| Sprint; automatic traversal | Right shoulder | Left Shift |
| Camera orbit | Right stick | Mouse motion |
| Camera look-ahead | Hold left trigger with right stick | No separate modifier bound |
| World interaction: mine, canister, camp, cache | Press Square / West | Tap E |
| Nearby Legger cargo (idle) | Triangle / North inventory pane; no Square hold | Hold E, 0.35 seconds by default |
| Personal inventory | North | Tab |
| Call Legger over | Radial command | F1 or radial command |
| Canister preview / confirm placement | D-pad down | G |
| Canister pickup / cancel current item action or preview | East | C; Backspace also cancels |
| Drink / eat / treat with algae | D-pad left / right / up | F5 / F6 / F7 |
| Radial open | Left shoulder | Q |
| Radial selection | Right stick / D-pad | Mouse / arrows |
| Radial cycle entries | West / North | Right / left bracket |
| Radial next page | Right shoulder | PageDown; PageUp for previous |
| Radial confirm / cancel | South / East | Enter or click / Escape |
| System menu | Start | Escape |
| UI navigate / submit / back | Left stick or D-pad / South / East | WASD or arrows / Submit / Escape; mouse |
| Inventory panel / quick transfer | Shoulders / West | Q–E / R |
| Camp placement move / orbit | Left / right sticks | WASD / mouse |
| Camp confirm / cancel | South / East | Enter / Escape |
| Camp rotate / face sun | D-pad left–right / up | Z–C / R |

## Existing safeguards and remaining limitations

Gameplay/UI/radial/camp maps remain centrally separated. Menu Escape is resolved using the mode at the start of the input update. Menu and camp have existing release protection; radial requires neutral selection, cancels on focus/device loss and prevents camera-stick carryover. Camera keeps mouse pixels separate from stick rate and does not stack binding-level stick deadzones. East/C already cancels mining, awaiting cargo and placement; no separate gamepad cancel action was missing.

Runtime rebinding with persisted overrides remains unimplemented despite being an architectural baseline. Gamepad radial paging cycles forward only; keyboard also has backward paging. No sensitivity, acceleration, traversal, mining timing or companion motion was retuned based on source inspection alone.

This changes input dispatch and transient UI guards only. Deterministic world identity, authored constraints, object/resource IDs, generated-object identity and persisted runtime deltas remain owned by existing systems. Greater Wasteland terrain stays fixed and always loaded; no terrain generation, streaming, chunk unload/reload or save schema is introduced.

## Verification

Unity 6000.4.0f1 imported and compiled the refreshed source in the stopped, isolated `D:/BooterBigArmValidation/Inventory20261011-01` candidate. Its prior Scripts, Editor tests and Input files were preserved under `Logs/input-audit-20261012-prechange` before refresh. Package manifest hashes matched the source checkout. The production Editor was not focused, saved, rebuilt or placed in Play Mode by this task.

**101/101 focused EditMode tests passed**, zero failed/skipped: Interaction 9, InventoryLoopUi 12, AlgaeInput 11, CampInput 5, Radial 12, InventoryUi 4, Menu 16, PlayerLocomotion 18, SmartTraversal 7, MicroDustHarvester 7. Diagnostics: candidate `Logs/input-audit-20261012.log` and `Logs/input-audit-20261012.xml`.

Added proof covers keyboard/gamepad tap versus timed hold, cargo focus with no opener transfer, fresh transfer after release, distant/blocked access, Legger exclusion while targeting ironstone, keyboard neutral return, and D-pad deploy/diagonal/modal conflicts. Existing mining/resource transaction and dust-capacity tests continue to pass.

This is input/structure/transaction proof. No gameplay smoke test or controller feel test was run. Hands-on acceptance remains author-owned: check the 0.45-second threshold, mining beside Legger, canister pickup with him present, cancellation, and transitions among cargo, inventory, radial and menus.

## Square cargo shortcut removal — October 13, 2026

The author requested removal of Square long-press cargo opening because it still interfered with world interaction, with inventory already accessible on Triangle. Gamepad `Interact` now uses the normal button press. The existing Tap interaction is confined to its keyboard binding so the keyboard E tap/hold behavior remains available. Gamepad cargo bindings are disabled when the input router initializes and after control preferences apply; a device guard also prevents a manually restored gamepad binding from emitting a cargo-open command. This preserves existing action/binding IDs and preference-file compatibility while preventing legacy paired overrides or a gamepad interaction rebind from reviving the shortcut.

The gamepad HUD no longer advertises the cargo hold. The Legger callover feedback now directs players to the existing inventory opener, and the controls hold-time label explicitly describes the keyboard gesture. Triangle inventory opening, the cargo pane, physical cargo-access checks, inventory transfer commands, world-target selection and keyboard cargo opening retain their existing owners. No world identity, terrain, streaming, authored placement or persisted gameplay-delta contract changes.

Unity 6000.4.0f1 imported and compiled the complete candidate in the main checkout. Its input-asset structural check passed directly through the background CLI. All **120/120 focused EditMode tests passed**, zero failures/skips, in the task-owned isolated dependency candidate `D:\BooterBigArmValidation\CargoSquare20261013-01`: `TopDown3DInventoryLoopUiTests`, `TopDown3DInteractionTests`, `TopDown3DInteractionFeedbackHudTests`, `TopDown3DControlPreferencesTests`, `TopDown3DInventoryUiTests` and `TopDown3DMenuTests`. Regression cases prove held Square interacts exactly once without opening cargo, release does not interact again, Triangle still opens inventory, gamepad HUD hints exclude cargo hold, and legacy paired/rebound gamepad preferences cannot restore cargo opening. Existing keyboard timing, access, inventory and menu checks also pass.

The first reduced candidate lacked the existing menu prefab, gather animation and GameplaySetup dependencies; those assets were copied from the source checkout without alteration. The next run passed 119/120, with one menu assertion still expecting the retired gamepad cargo rebind; that assertion now requires cargo to remain disabled. The final full selected run passed 120/120. Current diagnostics remain under ignored `Logs/cargo-square-sealed-tests.log` and `Logs/cargo-square-sealed-tests.xml`. Action-map, action and binding GUIDs are unchanged. No gameplay smoke test, scene save, app focus, package change or external publication was performed; CampLoopTesting remains the open scene.
