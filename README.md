# Optimized Local Housing

**Website:** https://timbermods.github.io/OptimizedLocalHousing/ (install guide, troubleshooting, FAQ)

A Timberborn 1.1 mod (built against **1.1.2.4**) that moves adult beavers into the homes that give the
**shortest total commute** between where they live and where they work. Version **1.1.1**.

Everyone can't live in the house nearest their workplace, because beds are limited. The best you can do is
minimize the total travel across the whole colony, and that is exactly what this mod solves, once a day, with
an optimal assignment (the Hungarian algorithm), instead of nudging beavers around one swap at a time.

## Installation

1. Download the `OptimizedLocalHousing-vX.Y.Z.zip` file under **Assets** on the
   [latest release](https://github.com/timbermods/OptimizedLocalHousing/releases/latest) (not "Source code").
2. Close Timberborn. Extract the ZIP into `Documents\Timberborn\Mods`. It contains one
   `OptimizedLocalHousing` folder.
3. Launch Timberborn, enable **Optimized Local Housing** in the Mods menu, and restart.
4. Load your save (back up important saves first, as with any mod). On a save the mod has not seen before, the
   first pass starts as soon as the game is ticking, then it runs again at the start of every day. One line per
   pass is written to `Player.log` (look for `[OptimizedLocalHousing]`).

Standalone, no dependencies. **Do not run it together with Housing Optimize or Commute Balancer.** When a
game loads, this mod checks for their mod IDs (`BobHousingOptimize`, `BobCommuteBalancer`, `housingoptimize`). If one is enabled, it logs a warning and does nothing for that game. In
multiplayer, every player must install the same version of the mod and run the same game version.

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
4. **Verify.** Every proposed move is re-priced with fresh routes. A whole cycle of moves is dropped if it would
   leave a beaver who can reach work today unable to, and each cycle must save at least half a route-cost unit in
   total. The fresh
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
- **No churn.** Each beaver gets a one-route-cost-unit bonus for staying put, so a rearrangement must save at least
  one unit for every beaver it moves. An optimized colony is left alone: a second pass on the same colony changes
  nothing.
- **Disconnected commutes are repaired.** A beaver whose home can no longer reach its workplace is moved to a
  home that can, as long as a bed can be arranged. If none can, it may be moved to another cut-off home when that
  helps others; the pass log's "disconnected commutes repaired" counts only beavers whose new home reaches work.

### Cost and multiplayer

- Work is spread over ticks with budgets per tick: **32 route queries** and about 250,000 solver operations on a
  colony of a few hundred adults. A pass takes roughly a fifth of a game day (147 ticks on a 266-beaver colony),
  never one long stall.
- A larger colony's pass takes larger budgets, set from the colony when the pass starts and saved with it, so
  that it still ends within the day's 512 daytime ticks: at most **128 route queries** and about 1,000,000 solver
  operations per tick (plus the solver row it started last). On test colonies of 1,600 adults in one district a
  pass now takes 480 ticks instead of 1,118 on randomly housed adults, and about 216 instead of 740 once they are
  settled. The route-query budget stops growing at about 770 staffed workplaces, and the solver budget at about
  1,150 adults in one district (about 920 in each of two, or 800 in each of three), so a still larger colony's
  pass takes longer instead (609 ticks on 2,000 randomly housed adults in one district).
- All decisions use integer arithmetic and sorted IDs, and the whole pass state (snapshot, prices, solver rows,
  verification results, and the route costs kept from earlier passes) is saved with the game. Reloading mid-pass,
  or a peer that loads a save taken mid-pass while another peer keeps running, continues exactly where the pass
  was and does the same work on every following tick, so every peer applies the same moves on the same tick.
  There is no wall-clock or frame-time rule anywhere.
- The saved state is a few hundred bytes when idle on a settled colony, and a few KB (about 20 KB at 1,300
  adults) where beavers keep changing jobs. For seven days after a pass that moved many beavers it also holds the
  route costs that pass checked, capped at 1,024 (about 11 KB for 240 adults, about 85 KB at the cap). During a
  pass it grows with the colony: about 70 KB for 240 adults, 100 KB for 350 to 400, 380 KB for 1,300.
  Uninstalling is safe: beavers keep the homes they have.

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

The game logs of the maintainer's own colony, running the code released as 1.0.0, show 16 consecutive passes (numbers 23 to 38) completing without a
single error, warning or rollback, on a colony of about 350 adults, 104 homes and 167 workplaces. Each pass took
about 183 ticks and about 5,350 route queries, and applied between 0 and 4 move cycles. In 10 of the 16 passes a
cycle was skipped because a beaver changed job or home while the pass was running ("stale"). That is expected,
and those beavers are reconsidered in the next pass.

That included a hosted co-op session with a second player running the same mod version: about 11,000 ticks and
15 passes, with no desync reported in the host's log.

### What has not been measured

- Frame-time impact. The per-tick work is bounded (32 route queries on a colony of a few hundred adults, at most
  128 on the largest), but the cost of a real path query has not been timed, nor the larger solver budget that
  large districts get: more than about 730 adults in one district, about 580 in each of two, or about 500 in each
  of three.
- The Iron Teeth faction, and operating systems other than Windows.
- Multiplayer beyond that one hosted session, and the client's side of it. No live two-player session has been
  played on 1.1.0 or 1.1.1 yet.
- BeaverBuddies MultiColony in a live session. Its code was audited against this mod: the mod only
  ticks inside the simulation, draws no random numbers, uses no wall-clock or frame time, never moves a beaver
  between districts (so colonies stay separate), and nothing it touches is patched by BeaverBuddies. Every player
  needs the same version enabled; BeaverBuddies warns at join time when the mod lists differ.

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
queries, and solver operations with one budget across districts), large colonies whose passes end within the
daytime with budgets set from the colony (in lockstep across a reload mid-pass), and the compiled adapter against
the installed game's component blacklist. Omitting the two arguments skips the compiled-adapter check.

## Uninstall

Disable the mod and restart. Beavers keep their current homes.

## Changelog

- **1.1.1**: maintenance release. The conflict check now covers only Housing Optimize and Commute Balancer.
  How passes work, and what is saved, are unchanged from 1.1.0. As always, every co-op player installs the same
  version.
- **1.1.0**: better homes near district borders, fewer repeated rejections, and shorter passes on large colonies.
  All co-op players must update together.
  - A workplace now prices only the homes in its own district. The game's route search never leaves a district,
    so those queries always failed (about 10-20% of all queries in two-district colonies).
  - Route costs found while re-checking moves to far homes are remembered in the save, so a move that was turned
    down is not proposed and rejected again every day. After seven passes a remembered cost is checked again if a
    worker still lives in that home or there was no route, so a road that comes back is noticed.
  - Each district is solved on its own: the same result in a third to a half of the solve ticks, and far less
    memory, in colonies with several districts.
  - A large colony's pass takes larger per-tick budgets, set from the colony when the pass starts, so it ends
    within the day's daytime (480 ticks instead of 1,118 for 1,600 adults in one district). Colonies of a few
    hundred adults keep the same budgets as before.
  - The pass log's "disconnected commutes repaired" no longer counts a beaver that was moved but still can't reach
    its workplace.
  - A pass that was running when an older save was made starts over once after the update. Idle saves keep their
    schedule, and the log's pass count carries on.
  - Development: the tests build without the game and run on GitHub Actions.
- **1.0.1**: a peer that loads a save taken mid-pass now keeps step, tick for tick, with a peer that never reloaded
  (the rebuild after a load was charged to that tick's solver budget, so the pass could end a tick later on one
  computer). A test covers it. No change in what the mod decides.
- **1.0.0**: first stable release.

## License

MIT.
