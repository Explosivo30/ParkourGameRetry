# Changelog

All notable changes to EZ Scene Collab are documented here.

## [0.5.0] - 2026-07-22

### Added

- New product name: **EZ Scene Collab**.
- Complete offline English and Spanish manuals.
- LAN, Tailscale, Hamachi, and port-forwarding tutorials.
- Host, client, lock, conflict, and reconnection workflows.
- Compatibility matrix and exact limitations.
- Update, uninstall, privacy, support, and troubleshooting guides.
- Asset Store listing draft and publisher checklist.
- Demo scene with primitives, hierarchy tests, a prefab, materials, a light, colliders, an inactive object, and test stations.
- Optional command for creating a real ProBuilder test object when ProBuilder 6.x is installed.

### Changed

- English remains the default language for new users.
- Visible menu and window names now use EZ Scene Collab.
- Package display version updated to 0.5.0.

### Known limitations

- Windows is the only tested operating system.
- Networking is direct, unencrypted TCP without built-in authentication.
- Asset files are not transferred.
- Built-in Render Pipeline and HDRP validation are still pending.

## [0.4.0] - 2026-07-14

### Added

- Optional ProBuilder 6.x integration with automatic detection.
- English and Spanish UI with English fallback.
- Authoritative locks and colored overlays for objects locked by other users.
- Component, hierarchy, prefab-reference, generated-mesh, and Inspector-state synchronization.
- Incremental recovery after disconnection with full-snapshot fallback.
- Asset GUID, path, and dependency-hash warnings.
- Unity JSON serialization without a Newtonsoft dependency.

## [0.3.0 and earlier]

- Initial direct-TCP host/client collaboration prototype.
