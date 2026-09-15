# Implement an Engine System

1. Define the engine-owned contract and the deterministic-world implications before writing code.
2. Reuse the selected architecture and dependency records; do not add a second subsystem owner or unverified dependency.
3. Keep runtime code, tests, tools and durable documentation in their canonical `Engine/` homes.
4. Prefer a small end-to-end path over isolated scaffolding. Keep AI authoring and UI-independent operations callable through structured engine interfaces.
5. Add focused technical checks for identity, lifecycle, failure handling, persistence and relevant performance behavior.
6. Record what the check proves and what it does not prove. Visual, Windows and hands-on gameplay acceptance need their own evidence.
