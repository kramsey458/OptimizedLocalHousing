---
target_identity: "file:C:\\Users\\Kyler\\code\\OptimizedLocalHousing-site-redesign\\file:C:\\Users\\Kyler\\code\\OptimizedLocalHousing-site-redesign\\docs"
timestamp: 2026-09-23T19-35-28Z
slug: optimizedlocalhousing-site-redesign-docs-39ffc4fb
---
---
target: Optimized Local Housing site
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
---
# Critique: Optimized Local Housing site (docs/)
Method: dual-agent (A design review, B detector + browser + audit)
Tests: (1) PARTIAL FAIL - h1 "Every beaver, as close to work as the beds allow" is a per-beaver promise (the mod minimizes the total; #still-far admits a watched beaver may walk farther), the hero "After" drawing puts every home above its own workplace, children-never-move and no-empty-beds absent from the first screen; (2) HONEST BUT HIDDEN - the FAQ comparison is fair (Workshop pages, 25.6 vs 24.1 called a model), but the home page has no comparison and the don't-run-together rule is a fifth bullet; (3) CONTENT RIGHT, PRESENTATION BROKEN - `.steps strong{display:block}` splits "under **Assets**" onto its own line in the key step, the Pass line scrolls sideways (1524px in 316px) and isn't explained on install, no Download button on install.
Heuristics 27/40 (Acceptable). Cognitive load moderate: 10.4k px phone home, 7-item nav in two-three rows.
Priority: [P1] headline + hero drawing per-beaver; [P1] bold words broken out of install steps; [P1] version history/stale facts (FAQ stability walk-through, "1.1.0 hasn't..." on install/FAQ multiplayer, "earlier versions" in update/state, "41 checks" vs 40); [P2] no comparison on home; [P2] header has no side gutter; [P2] Pass line unread on install; [P2] home length; [P3] 404 bare; "Features" nav duplicate; 18 of 21 FAQ entries without ids.
Audit: nothing fails AA in either theme; hero SVG text 7.5-9.3px on phones; nav 38px/toggle 37px/table summary 25px targets; Pass line and checksum scroll sideways; chart rows no role, desc only in aria-hidden tooltip. Perf: ~50 KB home, no fonts, CLS 0, release.js cached 30 min.
Identity: shared Timbermods template (cream, blue/orange, icon tiles, stat tiles, pill badges, eyebrows); authored bits are the favicon, the two diagrams, and the orange-before / blue-after rule (keep).
