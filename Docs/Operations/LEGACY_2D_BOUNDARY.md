# Historical Unity boundary

Owner: Gottspan. Status: extraction verified on 2026-10-06.

The former Legacy2D and isometric implementations now live in `Archive/Unity/HistoricalProject/`, a separate frozen Unity restore project with its own Assets, pinned Packages and compatible ProjectSettings. Its restore manifest accounts for 786 original input files; eight historical tests passed after import and compilation in a separate project. Do not import this tree into production or maintain it as a second production lane.

Production remains the repository-root project. `Assets/_Project/Scenes/Production/GreaterWasteland.unity` is its sole enabled build scene. The shared default volume profile is in Settings/Profiles with its GUID preserved. UniversalRP uses Renderer3D at index 0; active camera indices resolve it. The generated prototype remains imported but disabled at Scenes/Reference/GeneratedWorld for retained authoring tools. GameplaySetup is the terrain-free rebuild template and contains no generator or save service.

Root production assemblies no longer reference historical runtime/editor assemblies. Historical renderer ordering, scene registration, metadata and compile dependencies were frozen before extraction. See the execution plan and relocation receipts for dependency evidence and rollback mappings. No Player build or gameplay smoke acceptance is implied.
