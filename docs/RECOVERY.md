# Interrupted trades

Cancellation returns the offered items. If your inventory filled up meanwhile, remaining items stay in the character's trade recovery data and return when space becomes available. New trades are blocked until recovery finishes.

If the connection fails during final confirmation, the trade waits for the other player's decision instead of refunding items that may already have been exchanged. Reconnect both characters in the same world and bring them near one another to resolve it. Keep this mod and the relevant item mods installed until recovery completes.

Recovery data is included in normal character saves. This is a client-side exchange, not a server-authoritative transaction: process crashes, restoring an older save, or modified clients can still break consistency between character saves. It is not an anti-cheat system or a guarantee against hard-crash item loss/duplication.

