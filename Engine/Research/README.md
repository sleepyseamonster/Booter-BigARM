# Research and Evidence Index

The [foundation survey](../Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md) remains the overall technology comparison. Focused work here answers particular open decisions rather than replacing it with another broad survey.

The cohort first pinned for EXP-001 is now also used by the [application foundation](../Docs/FOUNDATION_RESULT.md). The historical lock filename and content are retained so experiment hashes stay meaningful. The dependency inventory records the current selection scope.

- [Stack audit](./STACK_AUDIT.md): requirements, integration findings and remaining proof.
- [Texture-system research](./TEXTURE_SYSTEM_RESEARCH.md): asset/channel audit, tested mip/compiler limitations, runtime format and ownership recommendations.
- [TEXTURE-001 offline experiment](./Experiments/TEXTURE-001/README.md): reproducible numerical evidence using the pinned texture tools.
- [Dependency inventory](./dependencies.json): proposed versus experiment-only components.
- [Experiment source lock](./probe-lock.json): immutable revisions and archive checksums.
- [License inventory](./license-manifest.json): collected primary notices, with limited scope.
- [EXP-001 reproduction](./Experiments/README.md).
- [Current status and result links](../Docs/STATUS.md).

License files under `Licenses/` are exact primary component notices collected from the pinned sources. They do not constitute a complete transitive license review or approval to redistribute every bundled asset/tool in those source archives. Complete the actual linked/package inventory before shipping anything.

Upstream source archives and extracted code live in the ignored cache. The lock, checksums, instructions, meaningful logs and conclusions are durable in Git. Keeping dependency caches out of Git is intentional; a future build may need network access to fetch the pinned archives again.
