# Changelog

All notable changes to this package are documented here. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/); this project uses semantic versioning.

## [1.0.0] - 2026-06-01

Initial release.

### Added
- **Scene Doctor** editor window (**Tools ▸ Scene Doctor**): one-click scan of the
  open scene(s) with a grouped, severity-filtered results list.
- Nine checks across four categories:
  - Scene: missing scripts (with fix), broken references, unassigned references
    (opt-in), AudioListener count, MainCamera tag.
  - UI: missing EventSystem (with fix) and duplicates, UI outside a Canvas.
  - Physics: 2D/3D Rigidbody-vs-collider mismatch.
  - Code: heuristic "expensive calls in Update" scan.
- One-click, undoable fixes where applicable (*Create EventSystem*,
  *Remove missing scripts*).
- **Select** button on every diagnostic with a target (pings the object/script).
- Per-check enable/disable toggles, persisted in `EditorPrefs`.
- **Rescan on save** option.
- Extensible architecture: new rules are auto-discovered via `TypeCache` —
  derive from `SceneCheck`, no registration needed.
- No hard dependency on uGUI / Input System: those types are resolved by
  reflection, so the package compiles and runs with or without them.
