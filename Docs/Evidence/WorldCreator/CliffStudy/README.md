# Cliff section calibration, 2026-09-28

This is a read-only technical survey of the current working-tree production query at seed `24681357`, topology version `13`, and source fingerprint `96ef0728256ce0e5c5524173e588aa7f`. The query/profile files were concurrently uncommitted, so the evidence describes this exact snapshot rather than a durable production baseline. The exporter is `WorldCliffStudyExporter.ExportFromCli`.

- [Report](2026-09-28-report.txt): four 48 m square absolute-coordinate windows, 1,024 owner cells each. They produced 31, 47, 0, and 51 candidates; 34 candidates were near an 18 m chunk boundary. The largest measured rim-to-toe drop was about 6 m.
- [Cross sections](2026-09-28-cross-sections.csv) and [profile graphic](2026-09-28-profile.svg): four separated high-drop candidates, with surface height, slope, strata exposure, material deposit, and proposed toe influence sampled every 0.75 m.

The sample sections already have a usable upper flat, a steep fall, and a lower flat, but mostly read as one smooth 5–6 m slope over several horizontal metres. They do not yet show the reference image's hard caprock break, intermediate layered ledges, or a scree apron. Existing material exposure rises on the slope; the new toe influence is only a proposed field and places no rocks.

All 129 candidates in these windows are broad-ground sections. Their query `DominantFeatureId` is a **per-position geological context ID**, not a shared parent cliff ID. The study now leaves `ParentFeatureId` empty for broad ground and derives each candidate ID from its absolute owner cell. A future continuous face plan must establish a stable larger-scale scarp parent before joining adjacent sections, especially across scan windows and chunk borders. Site, spawn, formation, full route-clearance, geometry, collision, streaming, and visual acceptance remain separate gates.
