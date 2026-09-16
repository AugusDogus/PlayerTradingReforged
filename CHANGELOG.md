# 1.0.0 (unreleased)

- Introduce Player Trading Reforged as an independent mod with its own plugin ID and fresh configuration.

- Update for Valheim 1.0.12 and TextMeshPro while retaining the original trading layout and controls.
- Bind requests and messages to a specific peer and trade session. Require matching offer revisions and a prepare/commit handshake.
- Reject malformed inventories and offers containing unavailable items.
- Preserve interrupted offers and inventory overflow in character recovery data. Resolve uncertain completion using the other character's saved decision.
- Cancel on death, teleport, excessive distance, logout, or connection timeout.
- Fix duplicate UI objects, read-only previews, early initialization, HUD restoration, and stale callbacks across world changes.
- Use current vanilla drag/drop and equipment handling instead of a copied 2022 implementation.
- Replace bundled ServerSync and fastJSON with local BepInEx settings and Unity JSON serialization.
- Adopt ValheimModTemplate layout, cross-platform builds, packaging, checks, and release tooling.

Original code and UI by [projjm](https://github.com/projjm/Valheim-Player-Trading).
