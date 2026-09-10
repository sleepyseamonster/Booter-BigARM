# Engine texture recipes

[library.json](./library.json) declares semantic imports for the 38 preserved surface textures and three diagnostic PNGs. The diagnostic images contain exact color quadrants, normal directions and packed channel values; they are technical data rather than game art.

[The pipeline result](../../Docs/TEXTURE_PIPELINE_RESULT.md) defines filtering, channels, runtime validation, ownership and evidence. Cooked files belong in ignored output directories and are rebuilt from these recipes. Source files remain immutable. PBR material bindings and the transferred Unity roughness interpretation are separate P05 work.
