# Native single-rock generator v7

These ten editable recipes use the original Unity single-rock planning rules in
native C++. Start with `01-Golden-Rock.json`; its empty `volumes` array generates
the approved composition from seed 2126351350. `02-New-Golden-Rocks.json` uses
seeded dimensions, and the remaining recipes expose automatic or explicit
silhouette families. The JSON filenames are the library labels.

`single.width` and `single.body_length` are meters before the resting rotation.
`single.random_dimensions` selects the original bell-shaped 0.3–1.2 m size
distribution. `single.dark_auto_profile` selects the original dark-desert subset
when profile is Auto. Explicit profiles are Boulder (1), Slab (2), Angular Chunk
(3), Split Lobe (4), Shard (5), Fractured Boulder (6), Blocky Monolith (7), and
Broken Slab (8).

Empty `volumes` regenerate masses, source seeds, cuts, orientation and burial from
the recipe seed. Explicit volumes override that plan for authored edits. The
generator uses a temporary signed-distance grid to create an ordinary mesh.
These are single-rock recipes; formation and streamed placement remain separate.

See [implementation and proof](../../Docs/SINGLE_ROCK_PARITY.md) and the
[reference comparison runner](../../Tests/ReferenceRock/README.md).
