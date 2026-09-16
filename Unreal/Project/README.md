# Unreal Host Project

This directory will contain the tracked `.uproject`, `Config/`, `Content/`, `Source/` and project/plugin integration after the Unreal version and toolchain are verified.

Host code may depend on `../PortableCore/`. PortableCore must never depend on this directory or Unreal types. Generated `Binaries/`, `DerivedDataCache/`, `Intermediate/` and `Saved/` directories are ignored.

No placeholder `.uproject` is committed because its engine association, modules and plugin descriptors must match the selected Unreal version.
