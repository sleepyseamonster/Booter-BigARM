# Portable Game and World Core

This directory is reserved for standalone C++ code that owns engine-neutral Booter & BigARM truth. It will build and test independently from Unreal and will be linked or consumed by an Unreal plugin through a narrow adapter.

Initial migration candidates are deterministic world identity, terrain manifests and semantic fields, authored constraints, geological/rock recipes, procedural validation and stable persisted deltas. Existing code is not copied here until the migration audit confirms its ownership, dependencies and tests.
