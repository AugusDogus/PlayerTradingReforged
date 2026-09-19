# Verification

Run the automated checks with the installed game's assemblies:

```sh
bun install --frozen-lockfile
bun run typecheck
bun test tests/
dotnet build src/PlayerTradingReforged/PlayerTradingReforged.csproj -c Release -t:Package \
  -p:GameDir="/path/to/Valheim" -p:BepInExDir="/path/to/profile/BepInEx"
MANAGED_DIR="/path/to/Valheim/valheim_Data/Managed" \
BEPINEX_DIR="/path/to/profile/BepInEx" bash scripts/check.sh
```

The compatibility checker resolves Harmony targets and injected arguments, plus compiled game methods and fields, against real assemblies. This cannot verify Unity object hierarchies, input, rendering, or actual network delivery.

## Upstream regression reports

- [#3: Item lost when walking away](https://github.com/projjm/Valheim-Player-Trading/issues/3).
- [#4: Items disappear on cancellation or with full bags](https://github.com/projjm/Valheim-Player-Trading/issues/4).

`TradeRecoveryTests` exercises both reports, including walking away while bags are full, against the actual trade/recovery code with small inventory and Unity test doubles. The dedicated-server scenarios still require the manual checks below.

## Two-client manual checks

Use disposable characters with counted items, matching 1.0.12 installations, and the same mod build. Test both a vanilla dedicated server and a player-hosted world.

1. Request and accept trades with E and controller Use. Verify requests expire, distant players cannot trade, typing does not request a trade, and the modifier setting changes immediately.
2. Compare the two 6-by-4 windows, button positions, labels, colors, tooltips, and inventory weights with the original UI. Try different resolutions and GUI scales.
3. Drag items, split stacks, quick move in both directions, and Take All. Confirm preview items cannot be moved, used, equipped, dropped, or selected through controller/touch callbacks.
4. Accept one offer. Confirm Change Trade withdraws acceptance, and editing either offer resets both acceptances. Change or cancel immediately as the other player accepts. Repeat with latency.
5. Exchange multiple stacks, upgraded equipment, named/crafted items, and items with custom data. Count both inventories before and after. Test full inventory, partially filled stacks, different qualities, and items from missing mods.
6. Cancel by button, Escape, walking away, teleporting, death, disconnect, and returning to the menu. Verify each offered item returns exactly once, including full-inventory overflow. On death, check the gravestone and recovery after respawn.
7. Disconnect during final confirmation. Confirm the prepared player's offer is held, new trades are blocked, and reconnecting the same characters resolves the saved decision exactly once.
8. Save during an open trade, quit, then reload. Verify escrow recovery. Hard-kill separately near commit to characterize the documented save-consistency limitation; do not use valuable characters for this test.
9. Enter F11 placement mode, drag every element, close and reopen it, and restart. Verify offsets persist and items cannot enter placement-only windows. Check X/B, tab switching, and cancel on a controller.
10. Open an ordinary chest and crafting panel after a trade, return to the menu, join another world, and trade again. Verify normal layout, controls, animation speed, and no duplicate windows or callbacks.
11. With both players on 1.1.1, check the partner inventory in the upper-right grid panel, including all eight vanilla columns, equipped gear, tooltips, stack sizes, quality, and weight. Move items between your inventory and offer; have your partner equip, consume, repair, and pick up items. Verify previews update within about half a second and cannot be moved, used, equipped, or dropped with mouse, touch, or controller input.
12. Test an expanded main inventory, scrolling extra rows, UI scaling, and F11 repositioning. The preview should not overlap the offer windows at the default layout. Check unknown custom items and oversized inventories show Unavailable without cancelling a valid trade. Close and reopen a trade with a different player and verify no previous inventory remains visible. Separate backpack/equipment mod inventories are not shared.

The installed 1.0.12 inventory prefab was also inspected from its local asset bundle. The container, grid root, background, weight label, Take All button, and controller hint paths were confirmed. Its container is nested under the player inventory, so trades temporarily reparent it to the full inventory canvas and restore it afterward. This static check does not replace the manual rendering and input checks above.

These manual checks have not been run by the automated build.

13. Hover every occupied slot in both preview windows while neither player changes items. Check tooltip titles and descriptions update, including weapons, armor, food, and quest items.
14. Check both projected weights after adding, splitting, and removing offers. Offered items are already outside the bags, so each projection adds incoming items to the current bag weight. Compare against actual bag weights after completing the trade. An unavailable partner snapshot must show `?`.
15. Verify the four panels and footer fit at 1080p, 720p, ultrawide, and minimum/maximum GUI scale. Close trading and F11 placement mode, then open a chest and confirm the personal inventory and container restore their original position and size.
