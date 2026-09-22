using Newtonsoft.Json;
using OptimizedLocalHousing;

static class Program
{
    static int passed;
    static Guid G(int n) => new Guid(n, 0, 0, new byte[8]);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }

    // ---- a fake game --------------------------------------------------------------------------------------------
    sealed class Person { public Guid Id, Home, Work, District; public bool Adult = true; }
    sealed class Home { public int Capacity, X, Y, Z; public Guid District = G(9000); public bool Usable = true; }
    sealed class Fake : IPassWorld
    {
        public Dictionary<Guid, Person> People = new(); public Dictionary<Guid, Home> Homes = new();
        public Dictionary<Guid, (int X, int Y, int Z)> Works = new(); public HashSet<(Guid, Guid)> Blocked = new();
        public Dictionary<Guid, Guid> WorkDistrict = new();   // a workplace not listed here is in G(9000), like a home
        public bool Reverse; public int Calls, CrossCalls; public List<string> Applied = new(); public bool ThrowOnApply;
        public Snapshot Capture()
        {
            var b = new SnapshotBuilder(); var people = Reverse ? People.Values.Reverse() : People.Values;
            foreach (var p in people)
            {
                if (!p.Adult || !Homes.TryGetValue(p.Home, out var h) || !h.Usable) continue;
                b.AddHome(p.Home, h.District, h.X, h.Y, h.Z);
                var work = Job(p); if (work != Guid.Empty) b.AddWork(work, Works[work].X, Works[work].Y, Works[work].Z);
                b.AddAdult(p.Id, p.Home, work, p.District);
            }
            return b.Build();
        }
        public Guid DistrictOf(Guid work) => WorkDistrict.TryGetValue(work, out var d) ? d : G(9000);
        // Like the game, a workplace in another district is no job at all.
        public Guid Job(Person p) => p.Work != Guid.Empty && Works.ContainsKey(p.Work) && DistrictOf(p.Work) == p.District ? p.Work : Guid.Empty;
        public float Route(Guid home, Guid work)
        { var h = Homes[home]; var w = Works[work]; return Math.Abs(h.X - w.X) + Math.Abs(h.Y - w.Y) + 2 * Math.Abs(h.Z - w.Z) + 5; }
        public bool TryRoute(Guid home, Guid work, out float cost)
        {
            Calls++; cost = 0;
            if (!Homes.ContainsKey(home) || !Works.ContainsKey(work) || Blocked.Contains((home, work))) return false;
            // The game's road pathfinding never leaves the start's district, so there is no route to another district.
            if (Homes[home].District != DistrictOf(work)) { CrossCalls++; return false; }
            cost = Route(home, work); return true;
        }
        public bool ApplyCycle(Move[] cycle)
        {
            if (ThrowOnApply) throw new InvalidOperationException("boom");
            var delta = new Dictionary<Guid, int>();
            foreach (var m in cycle)
            {
                if (!People.TryGetValue(m.Adult, out var p) || !p.Adult || p.Home != m.From || Job(p) != m.Work || !Homes[m.To].Usable) return false;
                delta[m.From] = delta.GetValueOrDefault(m.From) - 1; delta[m.To] = delta.GetValueOrDefault(m.To) + 1;
            }
            foreach (var d in delta) if (People.Values.Count(p => p.Home == d.Key) + d.Value > Homes[d.Key].Capacity) return false;
            foreach (var m in cycle) People[m.Adult].Home = m.To;
            Applied.Add(string.Join(",", cycle.Select(m => $"{m.Adult.ToString()[..4]}>{m.To.ToString()[..4]}")));
            return true;
        }
        public double Total() => People.Values.Where(p => p.Adult && Job(p) != Guid.Empty).Sum(p => Route(p.Home, p.Work));
        public double Objective() => People.Values.Where(p => p.Adult).Sum(p => Job(p) == Guid.Empty ? 0 : Cost.Fixed(Route(p.Home, p.Work))); // fixed-point units
        public Dictionary<Guid, int> AdultCounts() => People.Values.Where(p => p.Adult).GroupBy(p => p.Home).ToDictionary(g => g.Key, g => g.Count());
        public string Fingerprint() => string.Join(";", People.Values.OrderBy(p => p.Id).Select(p => p.Id.ToString()[..8] + ">" + p.Home.ToString()[..8]));
        public Fake Copy()
        {
            var f = new Fake { Reverse = Reverse };
            foreach (var p in People) f.People[p.Key] = new Person { Id = p.Value.Id, Home = p.Value.Home, Work = p.Value.Work, District = p.Value.District, Adult = p.Value.Adult };
            foreach (var h in Homes) f.Homes[h.Key] = new Home { Capacity = h.Value.Capacity, X = h.Value.X, Y = h.Value.Y, Z = h.Value.Z, District = h.Value.District, Usable = h.Value.Usable };
            foreach (var w in Works) f.Works[w.Key] = w.Value; f.Blocked = new HashSet<(Guid, Guid)>(Blocked);
            f.WorkDistrict = new Dictionary<Guid, Guid>(WorkDistrict); return f;
        }
        // One district on its own: its adults, homes and workplaces.
        public Fake Only(Guid district)
        {
            var f = Copy();
            foreach (var p in People.Values) if (p.District != district) f.People.Remove(p.Id);
            foreach (var h in Homes) if (h.Value.District != district) f.Homes.Remove(h.Key);
            foreach (var w in Works.Keys) if (DistrictOf(w) != district) f.Works.Remove(w);
            return f;
        }
    }

    // Random colony: every home is filled by adults (plus a few children who never move); some adults are unemployed.
    // Districts lie side by side along x, 60 blocks wide each.
    static Fake Colony(int seed, int homes, int adults, int works, int children = 0, int districts = 1)
    {
        var rng = new Random(seed); var f = new Fake();
        for (int h = 0; h < homes; h++) f.Homes[G(100 + h)] = new Home { X = h % districts * 60 + rng.Next(0, 60), Y = rng.Next(0, 60), Z = rng.Next(0, 3), District = G(9000 + h % districts) };
        for (int w = 0; w < works; w++) { f.Works[G(5000 + w)] = (w % districts * 60 + rng.Next(0, 60), rng.Next(0, 60), rng.Next(0, 3)); f.WorkDistrict[G(5000 + w)] = G(9000 + w % districts); }
        var keys = f.Homes.Keys.OrderBy(k => k).ToList();
        Populate(f, rng, adults);
        for (int c = 0; c < children; c++) { var home = keys[rng.Next(keys.Count)]; f.People[G(3000 + c)] = new Person { Id = G(3000 + c), Home = home, District = f.Homes[home].District, Adult = false }; }
        foreach (var h in keys) f.Homes[h].Capacity = f.People.Values.Count(p => p.Home == h);
        return f;
    }
    // Fills the homes in turn with adults; each works in its own district, as the game requires, or 1 in 8 has no job.
    static void Populate(Fake f, Random rng, int adults)
    {
        var keys = f.Homes.Keys.OrderBy(k => k).ToList();
        for (int a = 0; a < adults; a++)
        {
            var home = keys[a % keys.Count]; var district = f.Homes[home].District;
            var jobs = f.Works.Keys.Where(w => f.DistrictOf(w) == district).OrderBy(w => w).ToList();
            f.People[G(10 + a)] = new Person { Id = G(10 + a), Home = home, District = district,
                Work = rng.Next(0, 8) == 0 || jobs.Count == 0 ? Guid.Empty : jobs[rng.Next(0, jobs.Count)] };
        }
    }
    // A compact small district right beside the big district's workplaces: the homes nearest those workplaces are
    // in the small district, where no road from them leads.
    static Fake BorderColony(int seed)
    {
        var rng = new Random(seed); var f = new Fake(); Guid small = G(9001), big = G(9000);
        int smallHomes = rng.Next(10, 31), bigHomes = rng.Next(40, 90), works = rng.Next(6, 25), adults = rng.Next(150, 300), id = 100;
        for (int h = 0; h < smallHomes; h++) f.Homes[G(id++)] = new Home { District = small, X = rng.Next(0, 6), Y = rng.Next(0, 6) };
        for (int h = 0; h < bigHomes; h++) f.Homes[G(id++)] = new Home { District = big, X = rng.Next(7, 90), Y = rng.Next(0, 40), Z = rng.Next(0, 2) };
        for (int w = 0; w < works; w++)
        {
            f.Works[G(5000 + w)] = (w < 2 ? rng.Next(0, 6) : rng.Next(0, 3) == 0 ? rng.Next(7, 12) : rng.Next(7, 90), rng.Next(0, 40), 0);
            f.WorkDistrict[G(5000 + w)] = w < 2 ? small : big;
        }
        Populate(f, rng, adults);
        foreach (var h in f.Homes) h.Value.Capacity = f.People.Values.Count(p => p.Home == h.Key);
        return f;
    }
    // District 9000: 30 homes and one workplace; district 9001: 2 homes and a workplace with a higher ID. Rows hold 32
    // homes, so pricing makes 30 + 2 real queries, exactly one tick's budget, then spends a tick only skipping padding.
    static Fake PaddingTail()
    {
        var rng = new Random(7); var f = new Fake();
        for (int h = 0; h < 32; h++)
            f.Homes[G(100 + h)] = new Home { X = h < 30 ? rng.Next(0, 60) : rng.Next(60, 70), Y = rng.Next(0, 60), District = G(h < 30 ? 9000 : 9001) };
        f.Works[G(5000)] = (30, 30, 0); f.Works[G(5001)] = (65, 30, 0); f.WorkDistrict[G(5001)] = G(9001);
        Populate(f, rng, 70);
        foreach (var h in f.Homes) h.Value.Capacity = f.People.Values.Count(p => p.Home == h.Key);
        return f;
    }
    // Per district d, 100 blocks apart: a workplace G(500 + d) at X=0 prices its 32 nearest homes (X=1), each holding one
    // of its workers. Adult G(10 + 2d) works there and lives at X=50; adult G(11 + 2d), unemployed, lives at X=40.
    // Neither home is priced, so X=40 looks 10 units closer; with blocked, no route leads from it to the workplace, and
    // the swap the solver proposes is turned down by its route check.
    static Fake FarSwap(int districts = 1, bool blocked = true)
    {
        var f = new Fake();
        for (int d = 0; d < districts; d++)
        {
            int x = 100 * d; Guid district = G(9000 + d), work = G(500 + d);
            f.Works[work] = (x, 0, 0); f.WorkDistrict[work] = district;
            for (int i = 0; i < 32; i++)
            {
                f.Homes[G(1100 + 50 * d + i)] = new Home { Capacity = 1, X = x + 1, District = district };
                f.People[G(2100 + 50 * d + i)] = new Person { Id = G(2100 + 50 * d + i), Home = G(1100 + 50 * d + i), Work = work, District = district };
            }
            f.Homes[G(200 + 2 * d)] = new Home { Capacity = 1, X = x + 50, District = district };
            f.Homes[G(201 + 2 * d)] = new Home { Capacity = 1, X = x + 40, District = district };
            f.People[G(10 + 2 * d)] = new Person { Id = G(10 + 2 * d), Home = G(200 + 2 * d), Work = work, District = district };
            f.People[G(11 + 2 * d)] = new Person { Id = G(11 + 2 * d), Home = G(201 + 2 * d), District = district };
            if (blocked) f.Blocked.Add((G(201 + 2 * d), work));
        }
        return f;
    }
    static PassEngine Run(IPassWorld f, PassState state = null, int limit = 5000)
    {
        var e = new PassEngine(f, state); e.RequestPass();
        for (int t = 0; t < limit; t++) { e.Tick(); if (!e.Busy && !e.State.Requested) return e; }
        throw new Exception("Pass did not finish");
    }
    // What a save and load hands the next engine: only what the saved state holds.
    static PassState Reload(PassState state) => JsonConvert.DeserializeObject<PassState>(JsonConvert.SerializeObject(state));
    // Costs verified by earlier passes that the running pass takes instead of an estimate: pairs in its snapshot that
    // are not in the workplace's candidate row.
    static int LearnedInUse(PassState state)
    {
        var s = state.Snap; int used = 0;
        for (int e = 0; e < state.LearnedCost.Length; e++)
        {
            int w = Array.IndexOf(s.Works, state.LearnedWork[e]), h = Array.IndexOf(s.Homes, state.LearnedHome[e]);
            if (w >= 0 && h >= 0 && !Enumerable.Range(0, s.NearK).Any(i => s.Near[w * s.NearK + i] == h)) used++;
        }
        return used;
    }
    // Best objective any arrangement of the adults into the existing beds can reach (route cost minus stay bonuses).
    static long Best(Fake f)
    {
        var adults = f.People.Values.Where(p => p.Adult).OrderBy(p => p.Id).ToList(); int n = adults.Count;
        var beds = adults.Select(a => a.Home).ToList(); long best = long.MaxValue; var perm = Enumerable.Range(0, n).ToArray();
        long Cost1(Person a, Guid bed) => (f.Job(a) != Guid.Empty ? Cost.Fixed(f.Route(bed, a.Work)) : 0) - (bed == a.Home ? Cost.StayBonus : 0);
        void Go(int k, long sum)
        {
            if (k == n) { best = Math.Min(best, sum); return; }
            for (int i = k; i < n; i++)
            {
                if (f.Homes[beds[perm[i]]].District != adults[k].District) continue;   // nobody crosses districts
                (perm[k], perm[i]) = (perm[i], perm[k]);
                Go(k + 1, sum + Cost1(adults[k], beds[perm[k]]));
                (perm[k], perm[i]) = (perm[i], perm[k]);
            }
        }
        Go(0, 0); return best;
    }
    static long Achieved(Fake f) => f.People.Values.Where(p => p.Adult).Sum(p => (f.Job(p) != Guid.Empty ? Cost.Fixed(f.Route(p.Home, p.Work)) : 0) - 0L)
        - f.People.Values.Where(p => p.Adult).Count(p => p.Home == StartHome[p.Id]) * (long)Cost.StayBonus;
    static Dictionary<Guid, Guid> StartHome = new();

    static int Main(string[] args)
    {
        try { Run(args); return 0; }
        catch (Exception exception) { Console.Error.WriteLine("TEST FAILURE: " + exception); return 1; }
    }

    static void Run(string[] args)
    {
        Test("Hungarian solver matches brute force, including ties and forbidden entries", () => {
            var rng = new Random(11);
            for (int trial = 0; trial < 300; trial++)
            {
                int n = rng.Next(1, 8); var c = new long[n, n];
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) c[i, j] = rng.Next(0, 4) == 0 ? Cost.Forbidden : rng.Next(0, 6) * (rng.Next(0, 2) == 0 ? 1 : 1000) - 40;
                var u = new long[n + 1]; var v = new long[n + 1]; var p = new int[n + 1]; int row = 1;
                Check(Hungarian.Step(c, n, u, v, p, ref row, long.MaxValue, _ => 0), "Did not finish");
                long got = 0; for (int j = 1; j <= n; j++) got += c[p[j] - 1, j - 1];
                long best = long.MaxValue; var perm = Enumerable.Range(0, n).ToArray();
                void Go(int k, long s) { if (k == n) { best = Math.Min(best, s); return; } for (int i = k; i < n; i++) { (perm[k], perm[i]) = (perm[i], perm[k]); Go(k + 1, s + c[k, perm[k]]); (perm[k], perm[i]) = (perm[i], perm[k]); } }
                Go(0, 0); Check(got == best, $"Suboptimal: {got} vs {best}");
            }
        });
        Test("Hungarian can be paused after any row and resumed with identical results", () => {
            var rng = new Random(5); int n = 40; var c = new long[n, n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) c[i, j] = rng.Next(0, 50);
            var u1 = new long[n + 1]; var v1 = new long[n + 1]; var p1 = new int[n + 1]; int r1 = 1; Hungarian.Step(c, n, u1, v1, p1, ref r1, long.MaxValue, _ => 0);
            var u2 = new long[n + 1]; var v2 = new long[n + 1]; var p2 = new int[n + 1]; int r2 = 1; int calls = 0;
            while (!Hungarian.Step(c, n, u2, v2, p2, ref r2, 1, _ => 0)) calls++;
            Check(calls >= n - 1 && p1.SequenceEqual(p2) && u1.SequenceEqual(u2) && v1.SequenceEqual(v2), "Paused solve differed");
        });
        Test("cycle splitting: simple, complete, deterministic and applicable one at a time", () => {
            var rng = new Random(3);
            for (int trial = 0; trial < 200; trial++)
            {
                int nodes = rng.Next(2, 9), edges = rng.Next(1, 25); var from = new List<int>(); var to = new List<int>();
                // Build a balanced multigraph out of random cycles.
                while (from.Count < edges) { int len = rng.Next(2, 5); var ring = Enumerable.Range(0, nodes).OrderBy(_ => rng.Next()).Take(Math.Min(len, nodes)).ToList(); for (int i = 0; i < ring.Count; i++) { from.Add(ring[i]); to.Add(ring[(i + 1) % ring.Count]); } }
                var a = Cycles.Split(nodes, from.ToArray(), to.ToArray()); var b = Cycles.Split(nodes, from.ToArray(), to.ToArray());
                Check(a.Count == b.Count && a.Zip(b).All(x => x.First.SequenceEqual(x.Second)), "Not deterministic");
                Check(a.SelectMany(x => x).OrderBy(x => x).SequenceEqual(Enumerable.Range(0, from.Count)), "Edges lost or repeated");
                foreach (var cycle in a)
                {
                    Check(cycle.Select(e => from[e]).Distinct().Count() == cycle.Length, "Cycle is not simple");
                    for (int i = 0; i < cycle.Length; i++) Check(to[cycle[i]] == from[cycle[(i + 1) % cycle.Length]], "Cycle does not close");
                }
            }
        });
        Test("unbalanced moves are refused rather than looped on", () => {
            bool threw = false; try { Cycles.Split(3, new[] { 0 }, new[] { 1 }); } catch (InvalidOperationException) { threw = true; }
            Check(threw, "Unbalanced graph accepted");
        });

        Test("the pass reaches the true optimum (brute force) on random small colonies", () => {
            for (int seed = 0; seed < 100; seed++)
            {
                var f = Colony(seed, homes: 3 + seed % 3, adults: 6 + seed % 3, works: 3, children: seed % 3, districts: seed < 60 ? 1 : 2);
                StartHome = f.People.ToDictionary(p => p.Key, p => p.Value.Home);
                long best = Best(f); var counts = f.AdultCounts(); var kids = f.People.Values.Where(p => !p.Adult).ToDictionary(p => p.Id, p => p.Home);
                var e = Run(f);
                Check(Achieved(f) == best, $"seed {seed}: objective {Achieved(f)} but optimum is {best}");
                Check(f.AdultCounts().OrderBy(x => x.Key).SequenceEqual(counts.OrderBy(x => x.Key)), "A home's adult count changed");
                Check(kids.All(k => f.People[k.Key].Home == k.Value), "A child moved");
            }
        });
        Test("a badly housed colony gets much shorter commutes and no home changes its head count", () => {
            var f = Colony(1, homes: 90, adults: 240, works: 120, children: 20); double before = f.Total(); var counts = f.AdultCounts();
            var e = Run(f);
            Check(f.Total() < before * 0.6, $"Commute only {before:F0} -> {f.Total():F0}");
            Check(f.AdultCounts().OrderBy(x => x.Key).SequenceEqual(counts.OrderBy(x => x.Key)), "Adult counts changed");
            foreach (var h in f.Homes) Check(f.People.Values.Count(p => p.Home == h.Key) <= h.Value.Capacity, "Home over capacity");
            Console.WriteLine($"   240 adults, 90 homes: commute {before:F0} -> {f.Total():F0} in {e.LastReport.Ticks} ticks, {e.LastReport.Queries} route queries, {e.LastReport.Applied} cycles");
        });
        Test("a second pass on an optimized colony changes nothing", () => {
            var f = Colony(2, homes: 40, adults: 100, works: 50); var e = Run(f); var fp = f.Fingerprint(); int applied = f.Applied.Count;
            Run(f, Reload(e.State)); Check(f.Fingerprint() == fp && f.Applied.Count == applied, "Optimized colony was reshuffled");
        });
        Test("small savings below the stay bonus do not move anyone", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 11 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 10 };
            f.Works[G(500)] = (0, 0, 0);
            f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(2), District = G(9000) };   // unemployed
            Run(f); Check(f.Applied.Count == 0, "Moved for a 1 unit saving");
            f.Homes[G(2)].X = 4; Run(f); Check(f.People[G(10)].Home == G(2) && f.People[G(11)].Home == G(1), "Did not take a 7 unit saving");
        });
        Test("unemployed adults are shuffled out of the way for workers", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 0 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 50 };
            f.Works[G(500)] = (1, 0, 0);
            f.People[G(10)] = new Person { Id = G(10), Home = G(2), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(1), District = G(9000) };
            Run(f); Check(f.People[G(10)].Home == G(1) && f.People[G(11)].Home == G(2), "Idle adult kept the best bed");
        });

        Test("input enumeration order does not change the outcome", () => {
            foreach (int districts in new[] { 1, 3 })
            {
                var a = Colony(7, 60, 150, 70, 10, districts); var b = a.Copy(); b.Reverse = true;
                var ea = Run(a); var eb = Run(b);
                Check(a.Fingerprint() == b.Fingerprint() && string.Join("|", a.Applied) == string.Join("|", b.Applied), "Outcome depends on order");
                Check(JsonConvert.SerializeObject(ea.State) == JsonConvert.SerializeObject(eb.State), "State depends on order");
            }
        });
        Test("two peers ticking in lockstep have identical serialized state at every tick of two passes", () => {
            // Two districts of 45 homes: the second pass takes costs the first one verified outside the candidate rows.
            foreach (var (name, start) in new[] { ("1 district", Colony(8, 50, 130, 60, 8)), ("3 districts", Colony(8, 50, 130, 60, 8, 3)), ("2 districts of 45 homes", Colony(8, 90, 230, 80, 8, 2)) })
            {
                var a = start.Copy(); var b = start.Copy(); var ea = new PassEngine(a); var eb = new PassEngine(b); int passes = 1, used = 0;
                for (int t = 0; t < 1200 && (ea.Busy || ea.State.Requested || passes < 2); t++)
                {
                    if (!ea.Busy && !ea.State.Requested) { ea.RequestPass(); eb.RequestPass(); passes++; }   // the next day starts on both
                    ea.Tick(); eb.Tick(); Check(JsonConvert.SerializeObject(ea.State) == JsonConvert.SerializeObject(eb.State), $"{name}: peers diverged at tick {t}");
                    if (passes == 2 && ea.State.Stage == 1) used = LearnedInUse(ea.State);
                }
                Check(!ea.Busy && passes == 2 && a.Fingerprint() == b.Fingerprint(), $"{name}: peers housed differently");
                Check(name != "2 districts of 45 homes" || used > 0, $"{name}: the second pass used no cost the first one verified");
            }
        });
        Test("saving and reloading at any tick of two passes gives exactly the uninterrupted result", () => {
            // Three districts of 15 homes: padded candidate rows. PaddingTail: pricing ends with a tick that only skips padding.
            // Two districts of 45 homes: the second pass takes costs the first one verified outside the candidate rows.
            // A rejected swap in each of two districts: those costs decide what the second pass does.
            foreach (var (name, colony) in new[] { ("1 district", Colony(9, 45, 110, 55, 6)), ("3 districts", Colony(9, 45, 110, 55, 6, 3)), ("2 districts of 45 homes", Colony(9, 90, 220, 90, 6, 2)),
                ("2 districts, a rejected swap each", FarSwap(2)), ("padding tail", PaddingTail()) })
            {
                var start = colony; PassState saved = null; bool skipOnly = false; int used = 0;
                for (int pass = 1; pass <= 2; pass++)
                {
                    // Each pass starts from the world and the saved state the previous one left.
                    var whole = start.Copy(); var ew = Run(whole, saved == null ? null : Reload(saved));
                    string reference = JsonConvert.SerializeObject(ew.State); int ticks = (int)ew.LastReport.Ticks;
                    var live = start.Copy(); var el = new PassEngine(live, saved == null ? null : Reload(saved)); el.RequestPass();
                    for (int t = 0; t < ticks + 2; t++)
                    {
                        var resumedWorld = start.Copy();   // nothing is applied before the last tick, so the world is unchanged mid-pass
                        if (el.Busy && live.Applied.Count == 0)
                        {
                            var restored = new PassEngine(resumedWorld, Reload(el.State));
                            for (int k = 0; k < 5000 && (restored.Busy || restored.State.Requested); k++) restored.Tick();
                            Check(resumedWorld.Fingerprint() == whole.Fingerprint(), $"{name}: resume at tick {t} of pass {pass} housed differently");
                            Check(JsonConvert.SerializeObject(restored.State) == reference, $"{name}: resume at tick {t} of pass {pass} ended in a different state");
                        }
                        int calls = live.Calls, stage = el.State.Stage; el.Tick();
                        if (stage == 1 && el.State.Stage == 2 && live.Calls == calls) skipOnly = true;
                        if (pass == 2 && el.State.Stage == 1) used = LearnedInUse(el.State);
                    }
                    start = whole; saved = ew.State;
                }
                Check(name != "padding tail" || skipOnly, "PaddingTail did not end pricing with a tick that only skips padding");
                Check(!name.StartsWith("2 districts") || used > 0, $"{name}: the second pass used no cost the first one verified");
            }
        });
        Test("a peer that reloaded mid-pass stays in lockstep, tick for tick, with one that did not", () => {
            // A multiplayer peer may load a save taken mid-pass (a rehost, a rejoin) while another peer never
            // reloaded. From then on both must do the same work each tick: a pass that finished one tick later on one
            // computer would move beavers at a different tick there, which is a desync.
            // Two passes: the second starts from costs the first one verified, which the saved state must carry. In the
            // rejected-swap colony they decide what the second pass does, so a peer that lost them would not keep step.
            string Position(PassEngine e) => $"stage {e.State.Stage} row {e.State.Row} cursor {e.State.Cursor} ticks {e.State.Ticks} queries {e.State.Queries}";
            // Two districts of 50 homes: learned costs outside the rows; four of 25: padded rows.
            foreach (var (name, start, every) in new[] { ("1 district", Colony(19, 100, 300, 130, 10), 5), ("2 districts", Colony(19, 100, 300, 130, 10, 2), 5),
                ("4 districts", Colony(19, 100, 300, 130, 10, 4), 5), ("2 districts, a rejected swap each", FarSwap(2), 1) })
            {
                var live = start.Copy(); var el = new PassEngine(live);
                var restored = new List<(int At, PassEngine Engine, Fake World)>(); int t = 0, used = 0; var ticks = new List<long>(); var rejected = new List<int>();
                for (int pass = 1; pass <= 2; pass++)
                {
                    el.RequestPass(); foreach (var (_, engine, _) in restored) engine.RequestPass();   // the day starts on every peer at once
                    for (; t < 5000 && (el.Busy || el.State.Requested); t++)
                    {
                        if (el.Busy && (el.State.Stage == 2 || t % every == 0))
                        {
                            var world = live.Copy();   // nothing is applied before a pass's last tick
                            restored.Add((t, new PassEngine(world, Reload(el.State)), world));
                        }
                        el.Tick();
                        if (pass == 2 && el.State.Stage == 1) used = LearnedInUse(el.State);
                        foreach (var (at, engine, _) in restored)
                        {
                            engine.Tick();
                            Check(Position(engine) == Position(el), $"{name}: a peer that reloaded at tick {at} is at {Position(engine)} while the other is at {Position(el)} after tick {t}");
                        }
                    }
                    string reference = JsonConvert.SerializeObject(el.State);
                    foreach (var (at, engine, world) in restored)
                        Check(JsonConvert.SerializeObject(engine.State) == reference && world.Fingerprint() == live.Fingerprint(), $"{name}: a peer that reloaded at tick {at} ended pass {pass} differently");
                    ticks.Add(el.LastReport.Ticks); rejected.Add(el.LastReport.Rejected);
                }
                Check(restored.Count > (every == 1 ? 5 : 40), $"{name}: too few reload points exercised: {restored.Count}");
                Check(!name.StartsWith("2 districts") || used > 0, $"{name}: the second pass used no cost the first one verified");
                Check(!name.Contains("rejected swap") || rejected.SequenceEqual(new[] { 2, 0 }), $"{name}: cycles rejected per pass {string.Join(", ", rejected)}, expected 2, 0");
                Console.WriteLine($"   {name}: {restored.Count} reload points, each in lockstep to the end of two passes ({string.Join(" + ", ticks)} ticks)");
            }
        });

        Test("work is bounded per tick: route queries stay within budget for a large colony", () => {
            foreach (int districts in new[] { 1, 6 })   // six districts of 25 homes: padded candidate rows
            {
                var f = Colony(4, 150, 400, 120, 30, districts); var e = new PassEngine(f); e.RequestPass(); int worst = 0, ticks = 0;
                do { f.Calls = 0; e.Tick(); worst = Math.Max(worst, f.Calls); ticks++; } while ((e.Busy || e.State.Requested) && ticks < 3000);
                Check(!e.Busy && ticks < 3000, "Did not finish");
                Check(worst <= PassEngine.QueriesPerTick, $"{districts} district(s): {worst} route queries in one tick");
                Console.WriteLine($"   400 adults, 150 homes, 120 workplaces, {districts} district(s): {ticks} ticks, {e.LastReport.Queries} queries, worst tick {worst}");
            }
        });
        Test("far homes are never chosen without a fresh route check", () => {
            var f = Colony(5, 120, 200, 40); var e = new PassEngine(f); e.RequestPass(); var checkedPairs = new HashSet<(Guid, Guid)>(); var before = f.People.ToDictionary(p => p.Key, p => p.Value.Home);
            var probe = new Probe(f, checkedPairs); e = new PassEngine(probe); e.RequestPass();
            for (int t = 0; t < 4000 && (e.Busy || e.State.Requested); t++) { probe.Recording = e.State.Stage == 3; e.Tick(); }
            foreach (var p in f.People.Values.Where(p => p.Adult && p.Home != before[p.Id] && f.Works.ContainsKey(p.Work)))
                Check(checkedPairs.Contains((p.Home, p.Work)), "Moved to an unchecked home");
        });

        Test("a beaver whose route is cut is moved to a home that can reach work", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 3 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 4 };
            f.Works[G(500)] = (0, 0, 0); f.Blocked.Add((G(1), G(500)));
            f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(2), District = G(9000) };
            var e = Run(f); Check(f.People[G(10)].Home == G(2) && e.LastReport.Recovered == 1, "Disconnected commute not repaired");
        });
        Test("a beaver who moves but stays cut off is not counted as a repaired commute", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 0 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 50 };
            f.Works[G(500)] = (25, 0, 0); f.Works[G(501)] = (0, 0, 0);
            f.Blocked.Add((G(1), G(500))); f.Blocked.Add((G(2), G(500)));   // adult 10's workplace is out of reach from every home
            f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(2), Work = G(501), District = G(9000) };
            var e = Run(f);
            Check(f.People[G(11)].Home == G(1) && e.LastReport.RouteCostSaved == 50, "Adult 11's 50-unit saving was not made");
            Check(e.LastReport.Recovered == 0, $"{e.LastReport.Recovered} disconnected commute(s) reported repaired, but adult 10 is still cut off");
        });
        Test("a move is dropped when its route disappears after pricing", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 40 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 1 };
            f.Works[G(500)] = (0, 0, 0);
            f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(2), District = G(9000) };
            var e = new PassEngine(f); e.RequestPass();
            while (e.State.Stage < 2) e.Tick();
            f.Blocked.Add((G(2), G(500)));   // a wall goes up while the solver is running
            for (int t = 0; t < 100 && (e.Busy || e.State.Requested); t++) e.Tick();
            Check(f.Applied.Count == 0 && f.People[G(10)].Home == G(1) && e.LastReport.Rejected == 1, "Moved into an unreachable home");
        });
        Test("a cycle is rejected if anyone in it would end unreachable, even when another member gains far more", () => {
            var f = new Fake(); f.Homes[G(1)] = new Home { Capacity = 1, X = 1000 }; f.Homes[G(2)] = new Home { Capacity = 1, X = 1 };
            f.Works[G(500)] = (0, 0, 0); f.Works[G(501)] = (20, 0, 0); f.Blocked.Add((G(1), G(500)));   // adult 10 is cut off today
            f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
            f.People[G(11)] = new Person { Id = G(11), Home = G(2), Work = G(501), District = G(9000) };
            var e = new PassEngine(f); e.RequestPass();
            while (e.State.Stage < 2) e.Tick();
            f.Blocked.Add((G(1), G(501)));   // ...and the swap partner would lose their route once the wall goes up
            for (int t = 0; t < 100 && (e.Busy || e.State.Requested); t++) e.Tick();
            Check(f.Applied.Count == 0 && e.LastReport.Rejected == 1, "Accepted a cycle that strands someone");
        });
        Test("workers of a workplace no home can reach are not sent after unpriced homes", () => {
            // 33 one-bed homes in a row: the workplace at X=0 prices the 32 nearest and none can reach it, so the 33rd,
            // unpriced, must not look like a way out. Moving there would only push the neighbour away from their job.
            var f = new Fake(); f.Works[G(500)] = (0, 0, 0); f.Works[G(501)] = (40, 0, 0);
            for (int i = 0; i <= 32; i++)
            {
                f.Homes[G(100 + i)] = new Home { Capacity = 1, X = i }; f.Blocked.Add((G(100 + i), G(500)));
                f.People[G(10 + i)] = new Person { Id = G(10 + i), Home = G(100 + i), District = G(9000) };
            }
            f.People[G(10)].Work = G(500); f.People[G(42)].Work = G(501);   // cut off at X=0; commutes from X=32
            var e = Run(f);
            Check(e.LastReport.Moves == 0 && e.LastReport.Rejected == 0, $"{e.LastReport.Moves} moves proposed, {e.LastReport.Rejected} cycle(s) rejected");
        });
        Test("one home that can't reach a workplace does not make its unpriced homes look unreachable", () => {
            // 33 one-bed homes in a row and two workplaces at X=0, each pricing the 32 nearest homes. Workplace 500's
            // worker lives in the unpriced home at X=32, and one of its priced homes can't reach it. Everyone else works
            // at 501, so every arrangement costs the same: moving anyone gains nothing and would be rejected.
            var f = new Fake(); f.Works[G(500)] = (0, 0, 0); f.Works[G(501)] = (0, 0, 0);
            for (int i = 0; i <= 32; i++)
            {
                f.Homes[G(100 + i)] = new Home { Capacity = 1, X = i };
                f.People[G(10 + i)] = new Person { Id = G(10 + i), Home = G(100 + i), Work = G(501), District = G(9000) };
            }
            f.People[G(42)].Work = G(500); f.Blocked.Add((G(131), G(500)));
            var e = Run(f);
            Check(e.LastReport.Moves == 0 && e.LastReport.Rejected == 0, $"{e.LastReport.Moves} moves proposed, {e.LastReport.Rejected} cycle(s) rejected");
        });
        Test("a cycle its fresh route check rejected is not proposed again the next day", () => {
            // The first pass proposes adult 10's swap to X=40 and its route check rejects it. Every later pass knows that
            // from the saved state (reloaded between passes, as a save does) and neither proposes it nor asks for that route.
            var f = FarSwap(); var e = Run(f);
            Check(e.LastReport.Moves == 2 && e.LastReport.Rejected == 1 && f.Applied.Count == 0, $"pass 1: {e.LastReport.Moves} moves, {e.LastReport.Rejected} rejected");
            for (int pass = 2; pass <= 4; pass++)
            {
                var asked = new HashSet<(Guid, Guid)>(); var probe = new Probe(f, asked) { Recording = true };
                e = Run(probe, Reload(e.State));
                Check(e.LastReport.Rejected == 0, $"pass {pass}: the rejected cycle came back ({e.LastReport.Moves} moves, {e.LastReport.Rejected} rejected)");
                Check(!asked.Contains((G(201), G(500))) && f.People[G(10)].Home == G(200), $"pass {pass}: asked again for the route known to be missing");
            }
            // A learned cost is only kept for a while: once the road is back, the move is found again.
            f.Blocked.Remove((G(201), G(500)));
            for (int pass = 5; pass <= PassEngine.LearnedPasses + 2; pass++) e = Run(f, Reload(e.State));
            Check(f.People[G(10)].Home == G(201), $"{PassEngine.LearnedPasses + 2} passes: the move stayed ruled out after its route came back");
        });
        Test("a cost learned on an earlier day is checked again before anyone moves on it", () => {
            // As above, but the route from X=40 exists on the first day; the swap is verified and then goes stale (the
            // home is paused as it is applied). By the next day that route is gone: the swap is proposed from the
            // remembered cost, and its fresh route check turns it down.
            var f = FarSwap(blocked: false); var e = new PassEngine(f); e.RequestPass();
            while (e.State.Stage < 3) e.Tick();
            f.Homes[G(201)].Usable = false;
            for (int t = 0; t < 100 && (e.Busy || e.State.Requested); t++) e.Tick();
            Check(e.LastReport.Stale == 1 && f.Applied.Count == 0, $"pass 1: {e.LastReport.Applied} applied, {e.LastReport.Stale} stale");
            f.Homes[G(201)].Usable = true; f.Blocked.Add((G(201), G(500)));
            var asked = new HashSet<(Guid, Guid)>(); var probe = new Probe(f, asked) { Recording = true };
            e = Run(probe, Reload(e.State));
            Check(e.LastReport.Moves == 2 && asked.Contains((G(201), G(500))), "pass 2: the remembered swap was not proposed and checked");
            Check(e.LastReport.Rejected == 1 && f.Applied.Count == 0 && f.People[G(10)].Home == G(200), "pass 2: moved on a remembered cost without a fresh check");
            e = Run(f, Reload(e.State));
            Check(e.LastReport.Moves == 0 && e.LastReport.Rejected == 0, $"pass 3: {e.LastReport.Moves} moves, {e.LastReport.Rejected} rejected; the fresh cost did not replace the remembered one");
        });
        Test("beavers who changed home or job mid-pass are skipped; everyone else is still moved", () => {
            var f = Colony(12, 40, 100, 45); var trial = f.Copy(); Run(trial);
            var movers = f.People.Values.Where(p => trial.People[p.Id].Home != p.Home && f.Works.ContainsKey(p.Work)).OrderBy(p => p.Id).Take(2).ToList();
            Check(movers.Count == 2, "Need two movers");
            var e = new PassEngine(f); e.RequestPass();
            while (e.State.Stage < 3) e.Tick();
            var moved = movers;
            moved[0].Home = f.Homes.Keys.OrderBy(k => k).Last();   // the vanilla assigner re-housed them
            moved[1].Work = moved[1].Work == G(5001) ? G(5002) : G(5001);   // ...or their job changed
            var pinned = moved.ToDictionary(p => p.Id, p => (p.Home, p.Work));
            for (int t = 0; t < 200 && (e.Busy || e.State.Requested); t++) e.Tick();
            foreach (var p in moved) Check(f.People[p.Id].Home == pinned[p.Id].Home, "A stale beaver was moved anyway");
            Check(e.LastReport.Applied > 0 && e.LastReport.Stale >= 1, "Expected some cycles skipped and the rest applied");
            foreach (var h in f.Homes) Check(f.People.Values.Count(p => p.Home == h.Key) <= h.Value.Capacity || h.Key == f.Homes.Keys.OrderBy(k => k).Last(), "Home over capacity");
        });
        Test("beavers never cross districts", () => {
            var f = Colony(13, 40, 100, 45, 0, districts: 3); var district = f.People.ToDictionary(p => p.Key, p => p.Value.District);
            Run(f); foreach (var p in f.People.Values) Check(f.Homes[p.Home].District == district[p.Id], "Crossed districts");
        });
        Test("route queries stay inside the workplace's district, where the game's roads are", () => {
            // The game's road pathfinding never leaves a district, so a query for a home in another district is wasted.
            for (int seed = 0; seed < 5; seed++)
            {
                var f = Colony(20 + seed, homes: 70, adults: 180, works: 60, districts: 2); var e = new PassEngine(f); e.RequestPass();
                for (int t = 0; t < 1000 && e.State.Stage < 2; t++) e.Tick();
                // Every workplace still prices its own district's nearest homes, as many as the row holds.
                var s = e.State.Snap; int expected = 0;
                for (int w = 0; w < s.Works.Length; w++) expected += Math.Min(s.NearK, s.HomeDistrict.Count(d => d == f.DistrictOf(s.Works[w])));
                Check(e.State.Stage == 2 && f.Calls == expected, $"seed {20 + seed}: {f.Calls} route queries while pricing, expected {expected}");
                for (int t = 0; t < 5000 && (e.Busy || e.State.Requested); t++) e.Tick();
                Check(f.CrossCalls == 0, $"seed {20 + seed}: {f.CrossCalls} of {f.Calls} route queries asked for a route between districts");
            }
        });
        Test("beside a district border, the pass does as well as solving each district on its own", () => {
            // The small district's homes are the nearest ones to the big district's workplaces. They must not crowd out
            // the big district's own candidates, nor make its unpriced homes look unreachable.
            int rejected = 0, referenceRejected = 0;
            for (int seed = 0; seed < 42; seed++)
            {
                // Six days in a row, each pass starting from the state the one before saved, as in the game.
                var start = BorderColony(seed); var whole = start.Copy(); PassEngine e = null;
                for (int pass = 0; pass < 6; pass++) e = Run(whole, e == null ? null : Reload(e.State));
                double alone = 0; int aloneRejected = 0;
                foreach (var district in new[] { G(9000), G(9001) })
                {
                    var part = start.Only(district); PassEngine p = null;
                    for (int pass = 0; pass < 6; pass++) p = Run(part, p == null ? null : Reload(p.State));
                    alone += part.Total(); aloneRejected += p.LastReport.Rejected;
                }
                Check(whole.Total() <= alone, $"seed {seed}: commute {whole.Total():F0} after 6 passes, but {alone:F0} when each district is solved on its own");
                // By the sixth day every cycle a route check turned down is known, so none is proposed again.
                Check(e.LastReport.Rejected == 0 && aloneRejected == 0, $"seed {seed}: pass 6 still rejected {e.LastReport.Rejected} cycle(s), {aloneRejected} when each district is solved on its own");
                rejected += e.LastReport.Rejected; referenceRejected += aloneRejected;
            }
            Console.WriteLine($"   42 border colonies: {rejected} cycle(s) rejected on pass 6, {referenceRejected} when each district is solved on its own");
        });
        Test("paused homes keep their residents and never gain new ones", () => {
            var f = Colony(14, 30, 80, 30); var paused = f.Homes.Keys.OrderBy(k => k).Take(3).ToList(); foreach (var h in paused) f.Homes[h].Usable = false;
            var residents = f.People.Values.Where(p => paused.Contains(p.Home)).ToDictionary(p => p.Id, p => p.Home);
            Run(f); Check(residents.All(r => f.People[r.Key].Home == r.Value), "A paused home's resident was moved");
            Check(f.People.Values.Where(p => !residents.ContainsKey(p.Id)).All(p => !paused.Contains(p.Home)), "Someone moved into a paused home");
        });
        Test("a colony with nobody employed, or a single adult, is left alone", () => {
            var f = Colony(15, 10, 20, 5); foreach (var p in f.People.Values) p.Work = Guid.Empty; var fp = f.Fingerprint(); Run(f); Check(f.Fingerprint() == fp, "Moved unemployed colony");
            var g = Colony(16, 1, 1, 1); Run(g); Check(g.Applied.Count == 0, "Moved a lone adult");
        });
        Test("a world that throws while moving abandons the pass and recovers", () => {
            var f = Colony(17, 30, 70, 30); f.ThrowOnApply = true; Exception seen = null; var e = new PassEngine(f); e.Faulted += x => seen = x; e.RequestPass();
            for (int t = 0; t < 2000 && seen == null; t++) e.Tick();
            Check(seen != null && !e.Busy && !e.State.Requested, "Fault not contained");
            f.ThrowOnApply = false; e.RequestPass(); for (int t = 0; t < 2000 && (e.Busy || e.State.Requested); t++) e.Tick();
            Check(e.State.Passes >= 1 && f.Applied.Count > 0, "Did not recover on the next request");
        });
        Test("saved state survives a JSON round trip at every stage", () => {
            foreach (int districts in new[] { 1, 2 })   // 35 homes per district: verified costs outside the candidate rows are kept
            {
                var f = Colony(18, 35 * districts, 85 * districts, 30, districts: districts); var e = new PassEngine(f); var stages = new HashSet<int>(); int learned = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    e.RequestPass();
                    for (int t = 0; t < 2000 && (e.Busy || e.State.Requested); t++)
                    {
                        e.Tick(); stages.Add(e.State.Stage); learned = Math.Max(learned, e.State.LearnedCost.Length); string json = JsonConvert.SerializeObject(e.State);
                        Check(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PassState>(json)) == json, "Round trip changed the state");
                    }
                }
                Check(stages.SetEquals(new[] { 0, 1, 2, 3 }), "Did not exercise every stage: " + string.Join(",", stages));
                Check(learned > 0, "No verified cost was kept");
            }
        });
        Test("an unknown saved version or stage is discarded, not trusted", () => {
            // Version 1 (1.0.1) ranked homes from every district and version 2 kept no verified costs: a pass either saved
            // starts over rather than resuming.
            foreach (int version in new[] { 1, 2, 99 })
            {
                var e = new PassEngine(new Fake(), new PassState { Version = version, Stage = 2, Requested = false, Passes = 5 });
                Check(e.State.Stage == 0 && e.State.Version == PassState.CurrentVersion && e.State.Requested, $"Version {version} state kept");
                Check(e.State.Passes == 5, $"Version {version}: the pass count was lost");
            }
        });
        Test("a save from 1.0.1 restarts the pass it was running, and keeps its counters and schedule", () => {
            // Written by the 1.0.1 engine (a87ba72) for this colony: after one pass, mid-way through the next (stage 2),
            // and idle.
            const string running = """{"Version":1,"Requested":false,"Stage":2,"Snap":{"Adults":["0000000a-0000-0000-0000-000000000000","0000000b-0000-0000-0000-000000000000","0000000c-0000-0000-0000-000000000000"],"AdultDistrict":["00002328-0000-0000-0000-000000000000","00002328-0000-0000-0000-000000000000","00002328-0000-0000-0000-000000000000"],"AdultHome":[0,2,1],"AdultWork":[0,1,-1],"Homes":["00000001-0000-0000-0000-000000000000","00000002-0000-0000-0000-000000000000","00000003-0000-0000-0000-000000000000"],"HomeDistrict":["00002328-0000-0000-0000-000000000000","00002328-0000-0000-0000-000000000000","00002328-0000-0000-0000-000000000000"],"HomePos":[0,0,0,10,0,0,20,0,0],"Works":["000001f4-0000-0000-0000-000000000000","000001f5-0000-0000-0000-000000000000"],"WorkPos":[0,0,0,20,0,0],"NearK":3,"Near":[0,1,2,2,1,0],"Costs":[80,240,400,80,240,400]},"Cursor":6,"Row":1,"U":[0,0,0,0],"V":[0,0,0,0],"P":[0,0,0,0],"VerifyCurrent":null,"VerifyTarget":null,"Queries":6,"Ticks":1,"Passes":1,"MovedAdults":2,"AppliedCycles":1,"RejectedCycles":0,"StaleCycles":0}""";
            const string idle = """{"Version":1,"Requested":false,"Stage":0,"Snap":null,"Cursor":0,"Row":1,"U":null,"V":null,"P":null,"VerifyCurrent":null,"VerifyTarget":null,"Queries":10,"Ticks":3,"Passes":1,"MovedAdults":2,"AppliedCycles":1,"RejectedCycles":0,"StaleCycles":0}""";
            Fake World()
            {
                var f = new Fake(); f.Works[G(500)] = (0, 0, 0); f.Works[G(501)] = (20, 0, 0);
                for (int h = 1; h <= 3; h++) f.Homes[G(h)] = new Home { Capacity = 1, X = (h - 1) * 10 };
                f.People[G(10)] = new Person { Id = G(10), Home = G(1), Work = G(500), District = G(9000) };
                f.People[G(11)] = new Person { Id = G(11), Home = G(3), Work = G(501), District = G(9000) };
                f.People[G(12)] = new Person { Id = G(12), Home = G(2), District = G(9000) };
                return f;
            }
            foreach (var (name, json, restarts) in new[] { ("running", running, true), ("idle", idle, false) })
            {
                var e = new PassEngine(World(), JsonConvert.DeserializeObject<PassState>(json)); Exception fault = null; e.Faulted += x => fault = x;
                Check(e.State.Version == PassState.CurrentVersion && e.State.Stage == 0 && e.State.Snap == null, $"{name}: the 1.0.1 state was kept");
                Check(e.State.Passes == 1 && e.State.MovedAdults == 2 && e.State.AppliedCycles == 1, $"{name}: the lifetime counters were lost");
                Check(e.State.Requested == restarts, $"{name}: pass requested {e.State.Requested}, expected {restarts}");
                for (int t = 0; t < 100 && (e.Busy || e.State.Requested); t++) e.Tick();
                Check(fault == null && !e.Busy && e.State.Passes == (restarts ? 2 : 1), $"{name}: {fault?.Message ?? $"{e.State.Passes} passes"}");
            }
        });

        if (args.Length == 2) Test("compiled adapter follows installed component API contract", () => AdapterApiChecks.Verify(args[0], args[1]));
        Console.WriteLine($"{passed} checks passed. Native Unity execution and two-player playtest are not exercised.");
    }

    // Records every (home, work) pair the engine priced or verified.
    sealed class Probe : IPassWorld
    {
        readonly Fake _f; readonly HashSet<(Guid, Guid)> _seen; public bool Recording;
        public Probe(Fake f, HashSet<(Guid, Guid)> seen) { _f = f; _seen = seen; }
        public Snapshot Capture() => _f.Capture();
        public bool TryRoute(Guid home, Guid work, out float cost) { if (Recording) _seen.Add((home, work)); return _f.TryRoute(home, work, out cost); }
        public bool ApplyCycle(Move[] cycle) => _f.ApplyCycle(cycle);
    }
}
