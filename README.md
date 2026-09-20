<p align="center">
  <img src="package/banner.png" alt="Player Trading Reforged: player trading for Valheim" width="900">
</p>

Trade directly with other players through familiar inventory windows. An independent rewrite of [projjm's Player Trading](https://github.com/projjm/Valheim-Player-Trading) for Valheim **1.0.12**.

Interact with a nearby player to request a trade. They interact with you to accept. Place items in **You Will Give**, review **You Will Receive**, and press **Accept Trade**. Both players must accept the same offers. **Change Trade** withdraws acceptance; editing an offer resets both players' acceptance; **Cancel Trade** returns your items.

- The original two-panel layout, green acceptance indicators, stack splitting, quick move, and controller buttons remain.
- Offered items stay visible as grey reservations in **Your Inventory**. Stack labels use the usual count/limit format. Partial stacks show the usable count; tooltips show the reserved quantity. Return items from the offer window to make them usable again.
- Current weight includes your offered items, so moving items into the offer changes only the after-trade weight.
- Personal and partner inventories sit above the offer windows in an aligned grid, with trade buttons below and current/projected weights in matching inventory badges. Your normal inventory position returns when trading closes.
- A separate panel titled with your partner’s character name shows the other player's inventory during a trade, including equipped items highlighted in muted teal. It updates as items change and is read-only. Items only change ownership through the offer windows after both players accept.
- Press **F11** to reposition windows and buttons. Positions and the optional trade modifier key remain configurable.
- Both trading players need this version. Other players and the server do not need the mod.
- Custom items require matching item mods on both clients. Unknown or malformed offers cancel instead of silently dropping items.

Both players need **1.1.0 or newer** to share inventory previews. The preview uses the main player inventory; separate equipment or backpack inventories from other mods are not included. If the preview cannot be read because item mods differ or an inventory exceeds the supported 16 columns or 32 rows, the panel displays **Unavailable** and trading remains possible. Inventory sharing stops when the trade closes. Both players need **1.2.1 or newer** for synchronized current and after-trade weights. Weight badges show current and after-trade weight against the current carrying limit; an unknown limit displays `?`.

## Installation

Install **BepInExPack_Valheim 5.4.2350** in your mod profile, then import the local package ZIP through r2modman, or place `PlayerTradingReforged.dll` in `BepInEx/plugins/PlayerTradingReforged/`.

This is an independent mod with fresh settings. It does not read or migrate the original Player Trading configuration or translations. Settings use `augusdogus.mods.PlayerTradingReforged.cfg`; translations use `augusdogus.mods.PlayerTradingReforged.strings.json`. Both traders need Player Trading Reforged. Jötunn is not required.

## Interrupted trades

Cancellation returns the offered items. If your inventory filled up meanwhile, remaining items stay in the character's trade recovery data and return when space becomes available. New trades are blocked until recovery finishes.

If the connection fails during final confirmation, the trade waits for the other player's decision instead of refunding items that may already have been exchanged. Reconnect both characters in the same world and bring them near one another to resolve it. Keep this mod and the relevant item mods installed until recovery completes.

Recovery data is included in normal character saves. This is a client-side exchange, not a server-authoritative transaction: process crashes, restoring an older save, or modified clients can still break consistency between character saves. It is not an anti-cheat system or a guarantee against hard-crash item loss/duplication.

## Build and verification

```sh
bun install --frozen-lockfile
dotnet build src/PlayerTradingReforged/PlayerTradingReforged.csproj -c Release -t:Package \
  -p:GameDir="/path/to/Valheim" \
  -p:BepInExDir="/path/to/profile/BepInEx"
```

Requires .NET SDK 8 and Bun 1.4.1+. Output: `artifacts/PlayerTradingReforged-1.2.1.zip`. Building never installs or publishes the mod.

The current build is checked against the installed 1.0.12 assemblies, with automated protocol, capacity, packaging, and Harmony compatibility checks. **Two-client gameplay and visual/controller checks are still required** before calling this release tested in-game. See the [test checklist](docs/TESTING.md) and [development instructions](docs/DEVELOPMENT.md).

## Credits

Original Player Trading code and interface by **projjm**. Player Trading Reforged has its own icon and banner. Repository structure and release tooling follow [ValheimModTemplate](https://github.com/AugusDogus/ValheimModTemplate).

[Original video preview](https://www.youtube.com/watch?v=jc0tMuEjXbM)
