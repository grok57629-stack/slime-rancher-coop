# SRMP Revival — bug status

**Audit actualisé au 25 septembre 2026 :** build 0.0.0.1652, compilation SRML réussie et 160 contrôles automatisés réussis (60 régressions, 90 assembly/Harmony, 10 installateur), détaillés dans [AUDIT_STABILITY.md](AUDIT_STABILITY.md). Le build 1652 est installé dans la copie portable. Les effets sur les FPS et les essais multijoueur restent à valider en jeu.

This document separates the old build-1584 list from what can actually be established in the current source. The repository contains substantial fixes made after that list was written, so a historical item is not automatically still broken.

| Area | Historical report | Revival status |
|---|---|---|
| Multiplayer UI | Window missing below 1920×1080 | Fixed in later history; regression-test only |
| Exchange rewards | Simultaneous inputs could skip rewards | Fixed in later history; regression-test only |
| Exchange chest | Could disappear without rewards | Fixed in later history; regression-test only |
| World collision | Player could fall through world | Fixed in later history; regression-test only |
| Plort collector | Effect plays but item is not pulled | **Hardened in revival:** host-authority fallback during incomplete region init + non-owner StartCollection suppression; needs two-client verification |
| Nutcracker | Wrong quantity / invalid baby output | Dedicated current patch exists; exact-output test still required |
| Slime emotion | Slime may look angry only to remote player | Actor emotion/feral patches exist; remote-state test required |
| Gadget stutter | Placement/removal causes hitching | Actor transform traffic is now bundled into bounded batches; gadget placement still requires real frame-time profiling and may have a separate cause |
| First-run state | First use may break slime eating until restart | **Partially hardened:** packet guard can no longer stay stuck after early return/exception; fresh-profile test required |
| DLC reconnect | DLC state incorrect after leave/rejoin | Live host/client leave/rejoin test required |
| DLC initialization | False missing-DLC prompt before Manage DLCs | Fresh-launch entitlement test required |
| Gordos | Rewards sometimes missing | Gordo eat/snare networking exists; reward replication test required |
| Slime plorts | Slime occasionally fails to produce plorts | Eating/reproduction networking exists; replication test required |
| Drones | Drone can become stuck | Multiple drone/station/program patches exist; long two-client soak required |
| Chat | Remote player can receive empty chat | **Hardened in revival:** null/blank display rejected, Return-key focus race fixed, wrapping/render-state bugs fixed; two-client soak required |
| Player upgrades | Upgrade not applied to every player | Player-state packets exist; host/client upgrade matrix required |
| Packet state guard | Network handling can leave `Globals.HandlePacket` true | **Fixed in revival** with direct `try/finally` scope restoration in client and server handlers, including nested calls |
| Player save identity | Client supplied the GUID used to read/write host-side position saves | **Fixed in build 1633:** host derives the save identity from the authenticated EOS Product User ID |
| EOS notification cleanup | Auth-expiration handle was removed through the login-status API | **Fixed in build 1635, included in 1637:** each callback uses its matching EOS removal API; compiled IL regression check added |
| Installation | Old rollback DLL under `SRML\Mods` was recursively loaded as a duplicate SRMP mod | **Fixed in build 1637:** backups live under `SRML\Backups\SRMP`; legacy backups are migrated and uninstall restores outside `Mods` |
| AutoHost | World kept advancing with no connected players; failed startup could leave an unusable process; operator rights trusted spoofable player names | **Fixed in build 1633:** idle world pause with network tick preserved, startup failures exit for supervisor retry, operator allowlist uses authenticated EOS Product User IDs |
| Remote biome authority | Spawn stopped when the host was outside the biome | **Fixed in build 1641:** server tracks player presence and hands biome ownership to a connected player actually in that region; needs multi-client verification |
| Dedicated server self-join | Host could not join its own server; malformed roster data disconnected the attempt | **Diagnosed and guarded in build 1641:** server and game used the same EOS Product User ID. The client now explains this and aborts before authentication; joining still requires a different EOS identity |
| Actor replication load | Each moving actor generated an individual position packet up to 10 times per second | **Reduced in build 1642:** positions coalesce by actor and go in unreliable packets of at most 20; needs in-game frame-time and movement verification |
| Remote player T-pose | Animation packet queue was consumed on the server before relay; early coroutine packets could also arrive before remote Animator parameters existed, and the first state hash was omitted | **Fixed in build 1650:** relay an independent packet copy, guard Animator initialization and send the initial state; test with two live players |
| EOS invalid identity recovery | Reauthentication returned early after the login-status callback had already cleared the Product User ID | **Fixed in build 1650:** restart Connect login when the device is ready even if the old identity is already null; EOS account recovery still needs a real reconnect test |
| Silo snapshot decode | Silo emotion decode indexed ammo slots by emotion index instead of slot index | **Fixed in build 1650:** regression test covers an empty slot before an emotion-bearing slot |
| Gadget fashion IDs | Snare/drone fashion values were read as signed 16-bit although written as unsigned | **Fixed in build 1650:** round-trip test covers value 40000 |
| Malformed drone program list | Peer-controlled count was used directly to allocate an array | **Fixed in build 1650:** count bounded and malformed-count regression test added |
| Remote biome starting actors | Client suppressed every scene-start actor, even in a biome whose region authority had been granted to that client | **Fixed in build 1650:** only clients without local region authority suppress these registrations; `/netstate` now exposes spawner/owner state |
| Actor owner disagreement | Logs show clients requesting deletion of actors which the host still assigns to another owner; rejected deletes did not restore the client's missing/stale copy | **Fixed in build 1650:** server returns an authoritative actor snapshot on rejected deletion, and clients reconcile existing snapshots as well as create missing actors |
| FPS / slime food-search exceptions | `FindConsumable` passed a destroyed Unity object to `Identifiable.IsEdible`, throwing repeatedly in `StalkConsumable.Relevancy` / actor fixed updates; the supplied excerpt contains 129 repeats | **Fixed in build 1651:** Harmony treats null/destroyed candidates as inedible so the search continues. Regression guard test passes; real frame-time and host/client soak test still required |
| Gold slime in food-search trace | `slimeGold(Clone)` hits `AweTowardsLargos → FindConsumable → Identifiable.IsEdible`; the log shows the same stale consumable reference as the other slime errors, not a gold-specific food rule | **Covered by build 1651:** the global edible check safely rejects destroyed/null Unity objects; reproduce with a gold slime in a live game to confirm |
| Actor destruction packet abort | A failed optional destroy effect threw before the real despawn and registry cleanup, leaving an actor replica behind; 14 such failures appear in the supplied pre-1651 log | **Fixed in build 1652:** host and client use shared guarded cleanup, continue despawning after FX failure, and always clear the actor registry; Unity teardown still needs multiplayer validation |

## Verification rule

Timing, replication, save/rejoin, remote-client and DLC behavior are only promoted to **verified fixed** after passing the two-client checklist in `TESTING.md`. Static source inspection alone is not enough to make that claim safely.
