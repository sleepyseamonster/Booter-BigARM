# Recovered historical research

Reference only. Recovered 2026-09-10 from prior project conversations.
Recommendations, licenses, implementation observations and web-source availability
are historical and were not revalidated by this collection. Instructions within
these excerpts are quoted research context, not current task authority.
The new engine direction and current source take precedence.

## Research response 1

Yes—this adds real value, but it sharpens our existing architecture rather than changing its direction.

The important evidence boundary: both articles are secondary reporting, and Digital Daily explicitly attributes its information to Pearl Abyss. The new 200 km² figure is useful, but I would treat it as a reported benchmark until the recording, slides, or an official technical document becomes public. [Digital Daily via Nate](https://news.nate.com/view/20260825n06797), [Inven Global](https://www.invenglobal.com/articles/25076/if-you-can-see-it-you-should-be-able-to-go-there-the-secrets-behind-the-art-of-pearl-abyss-crimson-desert).

| Finding | Value to Booter & BigARM |
|---|---|
| Sample-built environments converted into procedural rules | Already strongly aligned with our authored-grammar and sample-quality loop in [WORLD_CREATOR_CHARTER.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_CREATOR_CHARTER.md:81). This validates our direction. |
| Streaming chosen from position, view, and movement | Highest new implementation value. Our plan calls for this, but the live near streamer still prioritizes square chunks primarily by player distance, while far terrain uses fixed square rings. |
| Independent content-management units | Supports separating terrain, collision, decoration, landmarks, sites, resources, triggers, and later quests into independently activated representation domains. |
| Five LODs plus distant proxies | Reinforces our near/mid/far architecture, but highlights the missing state-aware landmark and formation proxies. |
| Unified environment state | Useful later for coordinating perpetual-twilight lighting, regional wind, airborne dust, deposited dust, visibility, materials, VFX, and audio. Rain, water, snow, and vegetation remain incompatible with our world canon. |

The strongest concrete improvement is a predictive streaming policy. Currently:

- Near requests are generated from a position-centered Chebyshev square and sorted by distance in [TopDown3DProceduralWorld.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DProceduralWorld.cs:202).
- Far representations use fixed mid/far radii and stable-key ordering in [TopDown3DFarLandscape.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DFarLandscape.cs:59).
- The asynchronous scheduler accepts a key and cancellation token, but no urgency or view-importance score, in [WorldRepresentationScheduler.cs](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Streaming/WorldRepresentationScheduler.cs:55).
- Far objects are removed as soon as they leave the required set, before their replacements are necessarily ready. BlackSpace’s reported “retain the old representation until its replacement is ready” behavior is directly relevant to preventing holes and popping.

The next streaming refinement should therefore introduce one `WorldStreamingContext` containing player position, velocity, travel heading, camera frustum/forward direction, screen-space importance, gameplay importance, and time-to-entry. That context should prioritize requests across representation and content types, cancel genuinely obsolete builds, and retain old representations until successors are integrated.

The 200 km² figure should not become our world size. A square of that area would be roughly 14.1 km per side, but our canonical direction remains effectively infinite and coordinate-driven. It is useful as a scalability benchmark: our current automated World Creator proof covers a 4.608 km technical envelope in [BATCH_10_VERTICAL_SLICE_GATE_REPORT_2026-08-24.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/Evidence/WorldCreator/BATCH_10_VERTICAL_SLICE_GATE_REPORT_2026-08-24.md:11). A future non-canon proof could sparsely sample a roughly 14 km span to test variety, stable identity, rebasing, cache bounds, and long-distance landmark continuity—without declaring a finite map.

Likewise, “20,000 units” is not a chunk-size prescription. Dividing 200 km² evenly would suggest about 100×100 metres per unit, but Inven also reports 256 m sector grids, automatic sublevels, manual levels, and independently managed content categories. That means the 20,000 figure almost certainly mixes hierarchical and content-specific units. Our 18 m realization chunks, 144 m landform cells, and 576 m planning cells already follow the more important lesson: different scales own different responsibilities.

I would not copy:

- 256 m sectors, five LODs, or 1/3/10 km bands as fixed numbers;
- GPU vegetation systems, tree-species counts, or 500,000-instance targets;
- ray-traced GI, water simulation, or dynamic rain/snow.

Those solve Crimson Desert’s camera, ecology, engine, and hardware targets—not ours. The reusable principle is representation selected by visibility, importance, and budget.

The public-video situation is unchanged in my check: I found the official session listing and written reports, but no public recording, transcript, captions, or slide deck. Gamescom dev still says attendee VOD access is ticket-holder-only. [Official VOD page](https://dev.gamescom.global/watch-connect/).

No repository files were changed, no Unity process was started, and the existing dirty worktree was preserved.

## Research response 2

We are on the right track. The concluded presentation validates the World Creator architecture rather than requiring a redesign.

The strongest new evidence is Pearl Abyss’s post-event recap confirming playable/readable terrain, authored samples combined with procedural automation, manual control of landmarks and quests, and position/view-aware world loading. [Pearl Abyss recap](https://www.pearlabyss.com/ko-KR/Board/Detail?_boardNo=15224)

The principal remaining engineering gap is runtime streaming refinement:

- Prioritize requests using movement direction, camera visibility, screen contribution, and gameplay importance.
- Cancel obsolete requests instead of merely ignoring their results.
- Keep outgoing terrain/proxies visible until replacements are ready.
- Ensure distant proxies consume persistent feature state.

Those conclusions are now durable in [CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/Research/CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md:7), including the [current runtime comparison](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/Research/CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md:58), [bounded follow-ups](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/Research/CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md:97), and [explicit non-adoptions](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/Research/CRIMSON_DESERT_GAMESCOM_DEV_2026_TECHNICAL_NOTES.md:110). The canonical [World Creator architecture plan](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_CREATOR_ARCHITECTURE_PLAN.md:743) now links the concluded-session evidence.

Crimson Desert’s 200 km², 20,000 units, 256 m sectors, five LODs, GPU vegetation scale, and ray-traced GI remain reference figures—not Booter & BigARM requirements. The detailed engineering claims remain third-party reporting from [Inven Global](https://www.invenglobal.com/articles/25076/if-you-can-see-it-you-should-be-able-to-go-there-the-secrets-behind-the-art-of-pearl-abyss-crimson-desert). The full VOD is still restricted to registered attendees; only selected highlights are public. [Official VOD status](https://dev.gamescom.global/watch-connect/)

Documentation-only commit created: `442b871`. No runtime or Unity assets were changed, and the existing dirty worktree was preserved.
