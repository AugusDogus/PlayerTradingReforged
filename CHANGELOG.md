# 1.2.2 (unreleased)

- Show the partner’s offered items greyed out in their original inventory slots.
- Share available items and reservations in one preview update, including partial stacks and reservation tooltips.
- Keep the synchronized weight totals and trade ownership unchanged.

# 1.2.1

- Put Your Inventory inside the same framed header as the partner inventory.
- Send current, in-bag, and maximum weights together from the owner, preventing brief double counting when offer and inventory updates arrive separately.
- Restore count/stack-limit labels on reserved and partially offered stacks, with reservation details in tooltips.

# 1.2.0

- Add a Your Inventory heading and muted teal highlights for the partner’s equipped items.
- Hide the armor badge during trading and place Cancel on the left, Accept on the right.
- Keep current weight steady when items move into or out of the offer; update the projected weight instead.
- Keep offered items visible in their original inventory slots with the vanilla dragged-item grey tint. Partial stacks show available and reserved counts.
- Reserve those slots against pickups and drops, and release them when offers return or the trade closes. Keep the existing escrow recovery behavior.

# 1.1.2

- Show current and projected weights with carrying capacity inside native-style inventory weight badges.
- Share carrying capacity changes, including equipment bonuses, during trades.
- Use the partner’s character name in their inventory title.

# 1.1.1

- Align personal and partner inventories above the two offer windows, with trade buttons below.
- Restore the personal inventory position when trading closes.
- Fix empty preview tooltips when hovering items without an inventory update.
- Show both players' projected inventory weights after the trade, keeping offered-item weights visible.
- Remove “Read Only” from the partner inventory title.

# 1.1.0

- Show a read-only view of the trading partner's main inventory, including equipped items, in the crafting area.
- Refresh inventory previews while a trade is open without changing offers or acceptance. Clear the preview when the trade closes.
- Allow repositioning the inventory preview with F11. Both players need 1.1.0 or newer for previews.

# 1.0.0

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
