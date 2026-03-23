# Changelog

## [0.1.0] - 2026-03-23

### Added
- Inspector Reference Check: validates `[SerializeField]` private fields are assigned
- Blocks Play mode and Build when missing references are found
- `[AllowEmpty]` attribute to exclude fields from validation
- Prefab validation with incremental cache via AssetPostprocessor
- Scene validation on Play mode entry and Build
- Project Settings UI under `Project/Integrity`
- Settings: toggle validation, block play, block build independently
