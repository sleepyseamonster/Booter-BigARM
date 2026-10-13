# Camp Loop motion gaps and acquisition priorities

Date: 2026-10-13. Revision 2 companion to the [master plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). These are motion roles required by behavior, not claims that a particular Mixamo title is available. Audit only: no additional animation was downloaded or authored.

P0 means a prerequisite for its named family/quality gate, not that every P0 blocks initial body preview. P1 closes requested or complete behavior. P2 is later enhancement. Existing/derived solutions qualify only after Avatar, contact, transition and physical review. The [machine backlog](./AnimationPlayground20261013/animation-gap-backlog.json) carries IDs, source options, stage and completion state.

## Immediate family gaps

| ID | Priority | Role and why it is needed | Proposed source or closure | Gate |
| --- | --- | --- | --- | --- |
| MR01 | P1 | Upright backward walk | Acquire/author an unarmed loop; absent from the inspected active character collection | PHY1 full facing-independent movement |
| MR02 | P1 | Upright backward run | Acquire/author; do not reverse forward running as the default | PHY1 full facing-independent movement |
| MR03 | P1 | Standing turn-in-place left/right | Acquire/author; moving pivots do not cover idle foot rotation | PHY1 stationary facing changes |
| MR04 | P0 | Starts/stops/pivots and directional roles | Retarget/recalibrate the existing eight profile roles before seeking replacements | ART2 twelve-role profile |
| MR05 | P0 | Gait contact/cycle origin | Measure and derive two-cycle/phase-rotated sources if needed; R-L-R-L order, no invented contacts | ART2 profile/contact |
| MR06 | P0 | Hold object idle | Derive/author a compatible held pose from the reviewed pickup/examine context | PHY2 pickup completion |
| MR07 | P1 | Carry walk/turn and grip | Acquire/author; holding through ordinary arm-swing locomotion is not a carry set | PHY2 moving carry |
| MR08 | P1 | Place object down | Adapt/author a contact/release sequence; safe instant drop remains a safety command, not this animation | PHY2 place behavior |
| MR09 | P0 | Chair stand-up | Acquire/author compatible with downloaded sit/idle | PHY4 complete chair cycle |
| MR10 | P0 | Ground sit-down and stand-up | Adapt/author; floor idle and back/stomach get-up do not automatically match | PHY4 complete ground rest |
| MR11 | P0 | Lie down, wake/rise and bed contact | Qualify existing camp transitions or author for side-sleep source; preserve camp/equipment ownership | PHY4 complete sleep cycle |
| MR12 | P0 | Cover exit | Compose/review crouch backward movement and crouch-to-stand after clearance; acquire/author if contact fails | PHY3 complete cover cycle |
| MR13 | P1 | Ladder descent and bottom exit | Acquire/author dedicated sequence; ascent station uses a return ramp meanwhile | PHY3 bidirectional ladder |
| MR14 | P0 | Takeoff, airborne and touchdown roles | Segment current standing/running/unarmed jumps and existing fall/land sources against real physics | PHY1 complete jump |
| MR15 | P1 | Hard/roll landing recovery | Review Falling To Roll/Landing and get-up compatibility; derive/author missing recovery | PHY1 landing variants |
| MR16 | P0 | Ladder abort/recovery | Behavior and anchor/pose review first; fall/landing or bottom exit may supply roles | PHY3 safe interruptions |
| MR17 | P0 | Unarmed crouched death | Acquire/author; current Crouch Death reference has weapon-style posture and stays excluded | PHY5 crouched fatal state |
| MR18 | P1 | Crouched hurt | Qualify a posture-preserving masked reaction or author; standing full-body hit is not automatically suitable | PHY5 crouched reactions |
| MR19 | P0 | Airborne/traversal damage exit | Define gravity/motion-safe policy using fall/landing where suitable; qualify extra poses if needed | PHY5 fatal/large-hit transitions |
| MR20 | P1 | Seated/sleeping interruption or fatal pose | Qualify exit/current-pose terminal treatment or author, with collision/health policy | PHY4/PHY5 posture coverage |
| MR21 | P0 | Knockdown/death to back/stomach get-up | Pair exact final floor posture with existing get-up; author a bridge/knockdown if needed | PHY5 recovery compatibility |
| MR22 | P0 | Mining grip, repeated swing/impact/return | Adapt the one-handed downward reference to a blunt mining tool or author true mining; no finished mining claim from the source name | PHY4 mining |
| MR23 | P1 | Mount and dismount | Requested: acquire/author for a static mechanical saddle/seat fixture; no moving vehicle system needed initially | EXT1 requested mounting |
| MR24 | P1 | Standing wall-hug or crowd-squeeze alternative | Requested: qualify one suitable complete alternative; crouched cover and standard walk alone do not fulfill it | EXT1 requested corridor motion |
| MR30 | P1 | Crouch/cover travel starts/stops/turns | Compose/review current loops/enter/exit; derive/author if blends visibly pop | PHY1/PHY3 transition quality |
| MR33 | P0 | Prop and ladder hand grips | Fuller target rig plus authored poses if required; absent fingers cannot be added by IK alone | ART1 and hand-contact families |

Full directional movement and moving carry are capability gates. Initial lateral strafe and stationary hold/drop can be tested with clearly limited controls while those roles remain open. They must not silently animate unsupported directions or carry poses. Required seat/sleep/mining/posture sequences block their family completion, not unrelated accepted stations.

## Additional useful animations

| ID | Priority | Candidate role | Why it may be useful |
| --- | --- | --- | --- |
| MR25 | P2 | Unarmed lean/peek left/right | Extends measured cover/visibility experiments after basic cover works. |
| MR26 | P2 | Unarmed block/parry | Adds defensive comparison after strike/hit ownership and timing pass. |
| MR27 | P2 | Drink algae, eat and self-treatment | Existing gameplay commands could gain cosmetic presentation. Existing item/vitals transaction stays authoritative; animation does not dose again or change timing silently. |
| MR28 | P2 | Look around/environment inspection and relaxed idle variants | Tests grounded idle variety and attention without requiring NPC/quest systems. |
| MR29 | P2 | Heavy lift/carry | Existing Lifting can be evaluated; heavier carry needs its own posture/motion set. |
| MR31 | P2 | Stumble/trip/nonfatal knockdown | Useful for collision/damage recovery variety after safe physical recovery exists. |
| MR32 | P2 | Left/forward dodge | Mirror the right source only if symmetry/contact/path review passes; forward dodge needs a suitable source. |

No weapon animations are needed. The first expansion should complete missing sequences and control directions, then add polish. A library of extra isolated clips cannot substitute for shared motion ownership, contact, release and recovery systems.

## Acquisition and acceptance

For each role record: downloaded/project source or custom method, license/provenance, target rig/posture, full sequence/markers, active motion owner, station, source hash and readiness. Search for unarmed variants, inspect poses rather than names, and preserve restricted/rejected sources privately. Use current source clips and existing project assets when compatible; do not purchase a pack or change accounts as an implicit audit step.

Rig/contact approval precedes deriving dozens of grips. Source/target Avatar distinction, calibrated fixture and phase normalization precede profile replacement. A role closes only when it is imported/bound, tested physically in its family, and accepted for appearance/contact. Requested MR23/MR24 remain in Milestone B; absence from Mixamo does not erase the request. Optional P2 roles remain separate from that completion condition.
