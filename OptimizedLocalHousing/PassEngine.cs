using System;
using System.Collections.Generic;

namespace OptimizedLocalHousing;

public struct Move { public Guid Adult, From, To, Work; }

// Everything the engine needs from the game. Implemented by HousingService; faked in the tests.
public interface IPassWorld
{
    // Housed adult beavers, their homes and (usable) workplaces, at this instant.
    Snapshot Capture();
    // A fresh route query. False when the home cannot reach the workplace.
    bool TryRoute(Guid home, Guid work, out float cost);
    // Re-validates the cycle against the live game and applies it as a unit. False, with nothing changed,
    // if anyone in it has moved, changed job or died since the snapshot, or a home can no longer take them.
    bool ApplyCycle(Move[] cycle);
}

public sealed class Snapshot
{
    public Guid[] Adults = new Guid[0];         // ascending by ID
    public Guid[] AdultDistrict = new Guid[0];
    public int[] AdultHome = new int[0];         // index into Homes
    public int[] AdultWork = new int[0];         // index into Works, -1 when there is no usable job
    public Guid[] Homes = new Guid[0];           // ascending by ID; only homes with an adult in them
    public Guid[] HomeDistrict = new Guid[0];
    public int[] HomePos = new int[0];           // x, y, z per home
    public Guid[] Works = new Guid[0];           // ascending by ID
    public int[] WorkPos = new int[0];
    public int NearK;                            // candidate homes per workplace row (padding included)
    public int[] Near = new int[0];              // Works.Length * NearK home indices in the workplace's district, nearest
                                                 // first; -1 pads a row when the district has fewer homes than NearK
    public int[] Costs = new int[0];             // fixed-point route cost per Near entry, -1 until queried (padding: Unreachable)
}

public sealed class SnapshotBuilder
{
    private readonly List<(Guid Id, Guid Home, Guid Work, Guid District)> _adults = new List<(Guid, Guid, Guid, Guid)>();
    private readonly HashSet<Guid> _seen = new HashSet<Guid>();
    private readonly Dictionary<Guid, (Guid District, int X, int Y, int Z)> _homes = new Dictionary<Guid, (Guid, int, int, int)>();
    private readonly Dictionary<Guid, (int X, int Y, int Z)> _works = new Dictionary<Guid, (int, int, int)>();
    public void AddHome(Guid id, Guid district, int x, int y, int z) => _homes[id] = (district, x, y, z);
    public void AddWork(Guid id, int x, int y, int z) => _works[id] = (x, y, z);
    public void AddAdult(Guid id, Guid home, Guid work, Guid district) { if (_seen.Add(id)) _adults.Add((id, home, work, district)); }
    public Snapshot Build()
    {
        var adults = new List<(Guid Id, Guid Home, Guid Work, Guid District)>();
        foreach (var a in _adults)
            if (_homes.TryGetValue(a.Home, out var home) && home.District == a.District) adults.Add(a);
        adults.Sort((x, y) => x.Id.CompareTo(y.Id));
        var homeIds = new SortedSet<Guid>(); var workIds = new SortedSet<Guid>();
        foreach (var a in adults) { homeIds.Add(a.Home); if (a.Work != Guid.Empty && _works.ContainsKey(a.Work)) workIds.Add(a.Work); }
        var s = new Snapshot { Homes = new Guid[homeIds.Count], Works = new Guid[workIds.Count] };
        var homeIndex = new Dictionary<Guid, int>(); var workIndex = new Dictionary<Guid, int>();
        s.HomeDistrict = new Guid[s.Homes.Length]; s.HomePos = new int[s.Homes.Length * 3];
        int n = 0;
        foreach (var id in homeIds)
        {
            var h = _homes[id]; s.Homes[n] = id; s.HomeDistrict[n] = h.District;
            s.HomePos[n * 3] = h.X; s.HomePos[n * 3 + 1] = h.Y; s.HomePos[n * 3 + 2] = h.Z; homeIndex[id] = n++;
        }
        s.WorkPos = new int[s.Works.Length * 3]; n = 0;
        foreach (var id in workIds)
        {
            var w = _works[id]; s.Works[n] = id;
            s.WorkPos[n * 3] = w.X; s.WorkPos[n * 3 + 1] = w.Y; s.WorkPos[n * 3 + 2] = w.Z; workIndex[id] = n++;
        }
        s.Adults = new Guid[adults.Count]; s.AdultDistrict = new Guid[adults.Count];
        s.AdultHome = new int[adults.Count]; s.AdultWork = new int[adults.Count];
        for (int i = 0; i < adults.Count; i++)
        {
            s.Adults[i] = adults[i].Id; s.AdultDistrict[i] = adults[i].District; s.AdultHome[i] = homeIndex[adults[i].Home];
            s.AdultWork[i] = adults[i].Work != Guid.Empty && workIndex.TryGetValue(adults[i].Work, out var wi) ? wi : -1;
        }
        return s;
    }
}

public sealed class PassState
{
    public const int CurrentVersion = 2;         // 2: Near rows hold only the workplace's district, padded with -1
    public int Version = CurrentVersion;
    public bool Requested = true;                // start a pass at the next opportunity
    public int Stage;                            // 0 idle, 1 route costs, 2 assignment, 3 verification
    public Snapshot Snap;
    public int Cursor;                           // stage 1: next cost entry; stage 3: next move to verify
    public int Row = 1;                          // stage 2: next row of the assignment
    public long[] U, V; public int[] P;          // stage 2 solver state
    public int[] VerifyCurrent, VerifyTarget;    // stage 3: fresh route costs per move
    public long Queries, Ticks;                  // of the running pass
    public long Passes, MovedAdults, AppliedCycles, RejectedCycles, StaleCycles;   // lifetime
}

public sealed class PassReport
{
    public int Adults, Homes, Works, Moves, Applied, Rejected, Stale, Recovered;
    public long Queries, Ticks;
    public double RouteCostSaved;
}

// One pass = capture the colony, price each workplace's nearest homes in its district, solve the optimal assignment,
// re-check every proposed move against fresh routes, then apply whole cycles of moves. Each stage is spread
// over ticks with fixed operation budgets, and the entire state is serializable, so a pass resumes identically
// after a save/load and on every multiplayer peer.
public sealed class PassEngine
{
    public const int NearHomes = 32, QueriesPerTick = 32;
    public const long SolveOpsPerTick = 250_000;
    private readonly IPassWorld _world;
    public PassState State { get; private set; }
    public PassReport LastReport { get; private set; }
    public event Action<PassReport> Reported;
    public event Action<Exception> Faulted;
    private long[,] _matrix; private int _built;   // rebuilt from the snapshot after a load; never saved
    private List<int[]> _moves;                     // [adult index, from home, to home]

    public PassEngine(IPassWorld world, PassState state = null)
    {
        _world = world; State = state ?? new PassState();
        if (State.Version != PassState.CurrentVersion) State = new PassState();
    }
    public bool Busy => State.Stage != 0;
    public void RequestPass() => State.Requested = true;

    public void Tick()
    {
        try { TickCore(); }
        catch (Exception exception)
        {
            State = new PassState { Requested = false, Passes = State.Passes }; Forget();
            Faulted?.Invoke(exception);
        }
    }
    private void TickCore()
    {
        switch (State.Stage)
        {
            case 0: if (State.Requested) Begin(); break;
            case 1: State.Ticks++; PriceStep(); break;
            case 2: State.Ticks++; SolveStep(); break;
            case 3: State.Ticks++; VerifyStep(); break;
            default: throw new InvalidOperationException("Unknown pass stage " + State.Stage);
        }
    }
    private void Forget() { _matrix = null; _built = 0; _moves = null; _known = null; _far = null; }

    private void Begin()
    {
        Forget();
        State.Requested = false; State.Queries = 0; State.Ticks = 0;
        var snap = _world.Capture();
        if (snap.Adults.Length < 2 || snap.Works.Length == 0) { Finish(snap, new List<int[]>(), null, null); return; }
        // The game's road routes never leave a district, so a workplace only ranks homes in its own district: the one
        // its workers live in (a job in another district does not count), taken from the lowest-ID worker.
        var workDistrict = new Guid[snap.Works.Length]; var seen = new bool[snap.Works.Length];
        for (int i = 0; i < snap.Adults.Length; i++)
        {
            int w = snap.AdultWork[i];
            if (w >= 0 && !seen[w]) { workDistrict[w] = snap.AdultDistrict[i]; seen[w] = true; }
        }
        int k = Math.Min(NearHomes, snap.Homes.Length);
        snap.NearK = k; snap.Near = new int[snap.Works.Length * k]; snap.Costs = new int[snap.Works.Length * k];
        var order = new List<int>(snap.Homes.Length); var distance = new int[snap.Homes.Length];
        for (int w = 0; w < snap.Works.Length; w++)
        {
            order.Clear();
            for (int h = 0; h < snap.Homes.Length; h++)
                if (snap.HomeDistrict[h] == workDistrict[w]) { order.Add(h); distance[h] = Geometry.Distance(snap.HomePos, h, snap.WorkPos, w); }
            order.Sort((a, b) => distance[a] != distance[b] ? distance[a].CompareTo(distance[b]) : a.CompareTo(b));
            // Rows keep a fixed stride of k; a district with fewer homes pads its row with -1, never queried.
            for (int i = 0; i < k; i++)
            {
                bool real = i < order.Count;
                snap.Near[w * k + i] = real ? order[i] : -1; snap.Costs[w * k + i] = real ? -1 : Cost.Unreachable;
            }
        }
        State.Snap = snap; State.Stage = 1; State.Cursor = 0;
    }

    private void PriceStep()
    {
        var s = State.Snap; int k = s.NearK;
        for (int q = 0; q < QueriesPerTick && State.Cursor < s.Costs.Length; State.Cursor++)
        {
            int at = State.Cursor;
            if (s.Near[at] < 0) continue;   // padding: already Unreachable, and costs no query
            s.Costs[at] = _world.TryRoute(s.Homes[s.Near[at]], s.Works[at / k], out var route) ? Cost.Fixed(route) : Cost.Unreachable;
            State.Queries++; q++;
        }
        if (State.Cursor < s.Costs.Length) return;
        int n = s.Adults.Length;
        State.U = new long[n + 1]; State.V = new long[n + 1]; State.P = new int[n + 1]; State.Row = 1; State.Stage = 2;
    }

    private void SolveStep()
    {
        var s = State.Snap; int n = s.Adults.Length;
        if (_matrix == null) { _matrix = new long[n, n]; _built = 0; }
        // After a load the rows solved so far are rebuilt first; they are needed by the rows still to come. That work
        // is not charged to this tick's budget: how many rows a tick solves must depend on the saved state alone, so a
        // peer that loaded a save taken mid-pass keeps step, tick for tick, with a peer that did not.
        while (_built < State.Row - 1) BuildRow(_built++);
        if (!Hungarian.Step(_matrix, n, State.U, State.V, State.P, ref State.Row, SolveOpsPerTick, row => { BuildRow(row); _built = row + 1; return n; })) return;
        _moves = null; EnsureMoves();
        State.VerifyCurrent = new int[_moves.Count]; State.VerifyTarget = new int[_moves.Count];
        State.Cursor = 0; State.Stage = 3;
    }

    // Row i of the cost matrix: what each existing bed would cost adult i.
    private Dictionary<int, int>[] _known; private long[] _far;
    private void BuildRow(int i)
    {
        var s = State.Snap; int n = s.Adults.Length, k = s.NearK;
        if (_known == null)
        {
            _known = new Dictionary<int, int>[s.Works.Length]; _far = new long[s.Works.Length];
            for (int w = 0; w < s.Works.Length; w++)
            {
                // Unpriced homes are estimated beyond the worst reachable priced one. If none could reach the workplace,
                // it is likely cut off, so unpriced homes are assumed no better than unreachable.
                var d = new Dictionary<int, int>(k); int worst = -1;
                for (int e = 0; e < k; e++)
                {
                    int home = s.Near[w * k + e], c = s.Costs[w * k + e];
                    if (home < 0) continue;
                    d[home] = c; if (c < Cost.Unreachable && c > worst) worst = c;
                }
                _known[w] = d; _far[w] = (long)(worst < 0 ? Cost.Unreachable : worst) + Cost.FarMargin;
            }
        }
        int own = s.AdultHome[i], work = s.AdultWork[i];
        for (int j = 0; j < n; j++)
        {
            int bed = s.AdultHome[j];   // bed j is the one adult j lives in today; beds in one home are interchangeable
            if (s.AdultDistrict[i] != s.HomeDistrict[bed]) { _matrix[i, j] = Cost.Forbidden; continue; }
            long c = 0;
            if (work >= 0)
                c = _known[work].TryGetValue(bed, out var known) ? known
                    : _far[work] + (long)Cost.Scale * Geometry.Distance(s.HomePos, bed, s.WorkPos, work);
            if (bed == own) c -= Cost.StayBonus;
            _matrix[i, j] = c;
        }
    }

    // Moves implied by the assignment: [adult, from home, to home], in adult order.
    private void EnsureMoves()
    {
        if (_moves != null) return;
        var s = State.Snap; int n = s.Adults.Length;
        var bedOf = new int[n];
        for (int j = 1; j <= n; j++) bedOf[State.P[j] - 1] = j - 1;
        _moves = new List<int[]>();
        for (int i = 0; i < n; i++)
        {
            int to = s.AdultHome[bedOf[i]];
            if (to != s.AdultHome[i]) _moves.Add(new[] { i, s.AdultHome[i], to });
        }
    }

    private void VerifyStep()
    {
        EnsureMoves();
        var s = State.Snap;
        for (int q = 0; q < QueriesPerTick / 2 && State.Cursor < _moves.Count; q++, State.Cursor++)
        {
            var m = _moves[State.Cursor]; int work = s.AdultWork[m[0]];
            if (work < 0) { State.VerifyCurrent[State.Cursor] = 0; State.VerifyTarget[State.Cursor] = 0; continue; }
            State.VerifyCurrent[State.Cursor] = Price(s.Homes[m[1]], s.Works[work]);
            State.VerifyTarget[State.Cursor] = Price(s.Homes[m[2]], s.Works[work]);
        }
        if (State.Cursor < _moves.Count) return;
        var edgesFrom = new int[_moves.Count]; var edgesTo = new int[_moves.Count];
        for (int e = 0; e < _moves.Count; e++) { edgesFrom[e] = _moves[e][1]; edgesTo[e] = _moves[e][2]; }
        Finish(s, Cycles.Split(s.Homes.Length, edgesFrom, edgesTo), State.VerifyCurrent, State.VerifyTarget);
    }
    private int Price(Guid home, Guid work) { State.Queries++; return _world.TryRoute(home, work, out var route) ? Cost.Fixed(route) : Cost.Unreachable; }

    private void Finish(Snapshot s, List<int[]> cycles, int[] current, int[] target)
    {
        var report = new PassReport { Adults = s.Adults.Length, Homes = s.Homes.Length, Works = s.Works.Length,
            Moves = _moves?.Count ?? 0, Queries = State.Queries, Ticks = State.Ticks };
        var accepted = new List<(int[] Edges, long Gain)>();
        foreach (var cycle in cycles)
        {
            long gain = 0; bool ok = true;
            foreach (int e in cycle)
            {
                // A mover must not end up unreachable unless it already was.
                if (target[e] >= Cost.Unreachable && current[e] < Cost.Unreachable) ok = false;
                gain += (long)current[e] - target[e];
            }
            if (ok && gain >= Cost.MinimumGain) accepted.Add((cycle, gain)); else report.Rejected++;
        }
        accepted.Sort((x, y) => x.Gain != y.Gain ? y.Gain.CompareTo(x.Gain) : x.Edges[0].CompareTo(y.Edges[0]));
        foreach (var (edges, gain) in accepted)
        {
            var moves = new Move[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                var m = _moves[edges[i]]; int work = s.AdultWork[m[0]];
                moves[i] = new Move { Adult = s.Adults[m[0]], From = s.Homes[m[1]], To = s.Homes[m[2]], Work = work < 0 ? Guid.Empty : s.Works[work] };
            }
            if (_world.ApplyCycle(moves))
            {
                report.Applied++;
                foreach (int e in edges)
                {
                    if (current[e] >= Cost.Unreachable) report.Recovered++;
                    else report.RouteCostSaved += (double)((long)current[e] - target[e]) / Cost.Scale;
                    State.MovedAdults++;
                }
            }
            else report.Stale++;
        }
        State.AppliedCycles += report.Applied; State.RejectedCycles += report.Rejected; State.StaleCycles += report.Stale;
        State.Passes++;
        State.Snap = null; State.U = null; State.V = null; State.P = null; State.VerifyCurrent = null; State.VerifyTarget = null;
        State.Stage = 0; State.Cursor = 0; State.Row = 1; Forget();
        LastReport = report; Reported?.Invoke(report);
    }
}
