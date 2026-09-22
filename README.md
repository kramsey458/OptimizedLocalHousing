# Optimized Local Housing

**Website:** https://timbermods.github.io/OptimizedLocalHousing/ (install guide, troubleshooting, FAQ)

A Timberborn 1.1 mod (built against **1.1.2.4**) that moves adult beavers into the homes that give the
**shortest total commute** between where they live and where they work. Version **1.0.1**.

Everyone can't live in the house nearest their workplace, because beds are limited. The best you can do is
minimize the total travel across the whole colony, and that is exactly what this mod solves, once a day, with
an optimal assignment (the Hungarian algorithm), instead of nudging beavers around one swap at a time.

## Installation

1. Close Timberborn. Extract the release ZIP into `Documents/Timberborn/Mods`. It contains one
   `OptimizedLocalHousing` folder.
2. Launch Timberborn, enable **Optimized Local Housing**, and restart.
3. Load your save (back up important saves first, as with any mod). On a save the mod has not seen before, the
   first pass starts as soon as the game is ticking, then it runs again at the start of every day. One line per
   pass is written to `Player.log` (look for `[OptimizedLocalHousing]`).

Standalone, no dependencies. **Do not run it together with Incremental Housing**: this mod disables itself if
that mod (or another housing-assignment mod it knows about) is enabled. Both multiplayer players must install
the identical version.

## What it does

Each pass:

1. **Capture.** Every housed adult beaver, its home, and its assigned workplace.
2. **Price.** For every workplace, real route costs (`Accessible.FindRoadPath`, so ziplines and stairs count)
   from the 32 nearest homes in its district. Homes in other districts are never queried, because the game's
   route search never leaves a district. Homes farther away are estimated, and any move to one is re-checked
   with a real route before it is allowed.
3. **Solve.** The optimal way to reassign the adults to the beds that adults occupy today. Nobody moves between
   districts, so each district is solved on its own, one after another. In a colony with several districts that
   takes far fewer ticks and far less memory than solving them as one (47 solve ticks instead of 157 for four
   districts of 300 adults), and reaches the same optimum.
4. **Verify.** Every proposed move is re-priced with fresh routes. A move is dropped if the beaver would end up
   unable to reach work, and each cycle of moves must save at least half a route-cost unit in total. The fresh
   costs of homes beyond the 32 nearest are remembered in place of estimates, so a move that was turned down is
   not proposed again every day (anything proposed is still re-priced first). A remembered cost is used for seven
   passes. The seventh prices it again, and keeps it, if one of that workplace's workers lives in the home or if
   there was no route (so a road that comes back is noticed); otherwise it lapses to the estimate.
5. **Apply.** Moves are applied as whole cycles (A takes B's bed, B takes C's, C takes A's), in one game tick.

### Rules it keeps

- **Every home keeps exactly the same number of adults.** So homes never overfill, breeding capacity and
  newborn beds are untouched, and vacant beds are left for the game to fill.
- **Children never move.** Neither do beavers in paused, blocked or otherwise unusable homes, and nobody is
  moved into one. Nobody crosses districts. Beavers are never made homeless.
- **Unemployed adults** don't care where they live, so they give up good beds to people who commute.
- **No churn.** A beaver prefers to stay put unless moving saves at least one route-cost unit, so an optimized
  colony is left alone: a second pass on the same colony changes nothing.
- **Disconnected commutes are repaired.** A beaver whose home can no longer reach its workplace is moved to a
  home that can, as long as a bed can be arranged.

### Cost and multiplayer

- Work is spread over ticks with fixed budgets: **at most 32 route queries per tick**, and at most about
  250,000 solver operations per tick. A pass takes roughly a fifth of a game day (147 ticks on a 266-beaver
  colony), never one long stall.
- All decisions use integer arithmetic and sorted IDs, and the whole pass state (snapshot, prices, solver rows,
  verification results, and the route costs kept from earlier passes) is saved with the game. Reloading mid-pass,
  or a peer that loads a save taken mid-pass while another peer keeps running, continues exactly where the pass
  was and does the same work on every following tick, so every peer applies the same moves on the same tick.
  There is no wall-clock or frame-time rule anywhere.
- The saved state is a few hundred bytes when idle on a settled colony, and a few KB (about 20 KB at 1,300
  adults) where beavers keep changing jobs. For seven days after a pass that moved many beavers it also holds the
  route costs that pass checked, capped at 1,024 (about 11 KB for 240 adults, about 85 KB at the cap). During a
  pass it grows with the colony: about 70 KB for 240 adults, 100 KB for 350 to 400, 380 KB for 1,300.
  Uninstalling is safe: beavers just keep the homes they have.

## Results

### Replay of a real colony

Replaying a real 266-beaver colony (222 employed adults, 89 homes) through the actual pass engine, with
straight-line distance standing in for route cost:

| | Average commute |
|---|---|
| Before | 53.1 |
| After one pass | **24.1** |
| True optimum (brute-force-verified solver) | 24.0 |

The pass took 147 ticks, 4,388 route queries and about 22 ms of total CPU, rehomed 182 beavers in 20 cycles,
and a second pass changed nothing.

### In the live game

The game logs of the maintainer's own colony show 16 consecutive passes (numbers 23 to 38) completing without a
single error, warning or rollback, on a colony of about 350 adults, 104 homes and 167 workplaces. Each pass took
about 183 ticks and about 5,350 route queries, and applied between 0 and 4 move cycles. In 10 of the 16 passes a
cycle was skipped because a beaver changed job or home while the pass was running ("stale"). That is expected,
and those beavers are simply reconsidered in the next pass.

That included a hosted co-op session with a second player running the same mod version: about 11,000 ticks and
15 passes, with no desync reported in the host's log.

### What has not been measured

- Frame-time impact. The per-tick work is bounded (32 route queries), but the cost of a real path query has not
  been timed.
- The Iron Teeth faction, and operating systems other than Windows.
- Multiplayer beyond that one hosted session, and the client's side of it.
- BeaverBuddies MultiColony (1.4.0-alpha21) in a live session. Its code was audited against this mod: the mod only
  ticks inside the simulation, draws no random numbers, uses no wall-clock or frame time, never moves a beaver
  between districts (so colonies stay separate), and nothing it touches is patched by BeaverBuddies. Both players
  need the same version enabled; BeaverBuddies warns at join time when the mod lists differ.

## Limits

- Only beds already occupied by adults are used, so vacant beds are not filled by this mod (the game still fills
  them normally).
- The first pass on a badly housed colony moves most adults at once, so parents can end up living away from
  their children. Later passes move only a few.
- If a beaver changes home or job while a pass is running, the move cycle it belongs to is skipped and retried
  in the next day's pass.
- Home candidates are the 32 nearest by block distance in each workplace's district. A home much farther by
  straight line but cheaper by zipline could be missed.
- Homes must have a single access and a valid route, like the game's own assigner requires.
- No settings and no UI. The tuning constants (`NearHomes`, `QueriesPerTick`, `StayBonus`) are in the source.

## Build and test

Requires .NET SDK 8. Building the mod also needs a local Timberborn installation; no game DLLs are redistributed.
The tests compile only the game-independent engine and restore Newtonsoft.Json from nuget.org (13.0.4, the same
release the game ships), so they run without the game. CI (`.github/workflows/tests.yml`) runs them that way on
every push to main and every pull request, without the two arguments below: the compiled-adapter check needs the
built mod and the game, so it runs only locally.

```powershell
dotnet build OptimizedLocalHousing/OptimizedLocalHousing.csproj -c Release -p:GameManaged="C:\path\Timberborn_Data\Managed"
dotnet run --project OptimizedLocalHousing.Tests -c Release -- OptimizedLocalHousing/bin/Release/netstandard2.1/OptimizedLocalHousing.dll "C:\path\Timberborn_Data\Managed"
./package.ps1
```

The tests cover the solver against brute force, pause/resume at every row, cycle splitting, optimality on
random colonies with one to three districts, route queries that stay inside a district, each district solved on
its own (the same homes as a colony of that district alone, in no more ticks), the safety rules above,
stale-world handling, route costs carried from one pass to the next and priced again when they come due,
determinism between peers, save/reload at every tick of three passes (the first, the next, and the one where the
first pass's remembered costs come due; with one district and with several), per-tick work bounds (route
queries, and solver operations with one budget across districts), and the compiled adapter against the installed
game's component blacklist. Omitting the two arguments skips the compiled-adapter check.

## Uninstall

Disable the mod and restart. Beavers keep their current homes.

## Changelog

- **1.0.1**: a peer that loads a save taken mid-pass now keeps step, tick for tick, with a peer that never reloaded
  (the rebuild after a load was charged to that tick's solver budget, so the pass could end a tick later on one
  computer). A test covers it. No change in what the mod decides.
- **1.0.0**: first stable release.

## License

MIT.
