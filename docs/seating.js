/*
 * The hero's seating plan: a toy hall of three homes and three workplaces. "Run the day's pass" works out, on the page,
 * the arrangement of the adults into the same beds that makes the hall's total walk smallest (every arrangement is
 * tried; on a tie the one that moves fewest beavers wins, as the mod prefers staying put), then moves the place cards
 * round in their cycles. Distances here are straight lines; the mod uses the game's own route costs. The page shows
 * the hall as the game left it without this file.
 */
(function () {
  'use strict';
  var fig = document.querySelector('[data-seating]');
  if (!fig) return;
  var svg = fig.querySelector('svg.hall');
  var button = fig.querySelector('[data-pass]');
  var out = fig.querySelector('[data-readout]');
  if (!svg || !button || !out) return;
  var reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // where each chair sits around a home, by the home's number of beds
  var CHAIRS = { 3: [[-50, -52], [50, -52], [0, 56]], 2: [[-50, -52], [50, -52]] };
  var stations = {}, homes = {}, guests = [];
  Array.prototype.forEach.call(svg.querySelectorAll('[data-station]'), function (s) {
    stations[s.getAttribute('data-station')] = { x: +s.getAttribute('data-x'), y: +s.getAttribute('data-y') };
  });
  Array.prototype.forEach.call(svg.querySelectorAll('.table'), function (t) {
    homes[t.getAttribute('data-home')] = { x: +t.getAttribute('data-x'), y: +t.getAttribute('data-y'), beds: +t.getAttribute('data-beds') };
  });
  Array.prototype.forEach.call(svg.querySelectorAll('.pc'), function (c) {
    guests.push({ el: c, name: c.getAttribute('data-name'), work: c.getAttribute('data-work'), home: c.getAttribute('data-home'), seat: +c.getAttribute('data-seat') });
  });
  var homeIds = Object.keys(homes);

  function seatAt(h, i) {
    var t = homes[h], c = CHAIRS[t.beds][i];
    return { x: t.x + c[0], y: t.y + c[1] };
  }
  // whole-number walking cost, as the mod counts in whole route-cost units
  function walk(h, w) { var t = homes[h], s = stations[w]; return Math.round(Math.hypot(t.x - s.x, t.y - s.y) / 10); }
  function total(plan) { return guests.reduce(function (sum, g, i) { return sum + walk(plan[i], g.work); }, 0); }

  function solve(from) {
    var best = null, cap = {}, plan = [];
    homeIds.forEach(function (h) { cap[h] = homes[h].beds; });
    (function place(i) {
      if (i === guests.length) {
        var cost = total(plan), moved = plan.filter(function (h, k) { return h !== from[k]; }).length;
        if (!best || cost < best.cost || (cost === best.cost && moved < best.moved)) best = { cost: cost, moved: moved, plan: plan.slice() };
        return;
      }
      homeIds.forEach(function (h) {
        if (!cap[h]) return;
        cap[h]--; plan.push(h); place(i + 1); plan.pop(); cap[h]++;
      });
    })(0);
    return best;
  }

  // move cycles: follow a beaver into the home it moves to, then someone moving out of that home, until back where it began
  function cycles(from, to) {
    var left = guests.map(function (g, i) { return from[i] !== to[i] ? i : -1; }).filter(function (i) { return i >= 0; });
    var count = 0;
    while (left.length) {
      var start = left.shift(), at = to[start];
      count++;
      while (at !== from[start]) {
        var k = left.findIndex(function (i) { return from[i] === at; });
        if (k < 0) break;
        at = to[left.splice(k, 1)[0]];
      }
    }
    return count;
  }

  var original = guests.map(function (g) { return { home: g.home, seat: g.seat }; });
  var arranged = false;

  function seatGuests(plan) {
    // stayers keep their chair; movers take the chairs the others left free
    var taken = {};
    homeIds.forEach(function (h) { taken[h] = []; });
    guests.forEach(function (g, i) { if (plan[i] === g.home) taken[g.home].push(g.seat); });
    return guests.map(function (g, i) {
      if (plan[i] === g.home) return g.seat;
      for (var s = 0; s < homes[plan[i]].beds; s++) if (taken[plan[i]].indexOf(s) < 0) { taken[plan[i]].push(s); return s; }
      return 0;
    });
  }

  function draw(stateClass) {
    guests.forEach(function (g) {
      var p = seatAt(g.home, g.seat), s = stations[g.work];
      g.el.style.transform = 'translate(' + p.x + 'px, ' + p.y + 'px)';
      var line = svg.querySelector('.walk[data-for="' + g.name + '"]');
      line.setAttribute('x1', p.x); line.setAttribute('y1', p.y);
      line.setAttribute('x2', s.x); line.setAttribute('y2', s.y - 17);
      line.setAttribute('class', 'walk ' + stateClass);
    });
  }

  function say(html) { out.innerHTML = html; }

  var before = total(guests.map(function (g) { return g.home; }));

  button.addEventListener('click', function () {
    var lines = svg.querySelector('.lines');
    if (!arranged) {
      var from = guests.map(function (g) { return g.home; });
      var best = solve(from);
      var chairs = seatGuests(best.plan);
      var farther = [];
      guests.forEach(function (g, i) {
        var d = walk(best.plan[i], g.work) - walk(g.home, g.work);
        if (d > 0) farther.push(g.name + ' (+' + d + ')');
        g.el.classList.toggle('is-moving', best.plan[i] !== g.home);
        g.el.classList.toggle('is-farther', d > 0);
        g.home = best.plan[i]; g.seat = chairs[i];
      });
      var n = cycles(from, best.plan);
      lines.classList.add('is-hidden');
      draw('after');
      setTimeout(function () { lines.classList.remove('is-hidden'); }, reduced ? 0 : 900);
      say('<span class="ro-total"><b>' + before + '</b> → <b class="after">' + best.cost + '</b> hall’s total walk</span>' +
          '<span>' + best.moved + ' beavers moved in ' + n + (n === 1 ? ' cycle' : ' cycles') + '; every home kept its adults and its kit.</span>' +
          (farther.length ? '<span class="ro-farther">' + farther.join(', ') + ' now walks a little farther, so the hall walks ' + (before - best.cost) + ' less.</span>' : ''));
      button.textContent = 'Seat them as they were';
      arranged = true;
    } else {
      guests.forEach(function (g, i) { g.home = original[i].home; g.seat = original[i].seat; g.el.classList.remove('is-moving', 'is-farther'); });
      lines.classList.add('is-hidden');
      draw('before');
      setTimeout(function () { lines.classList.remove('is-hidden'); }, reduced ? 0 : 900);
      say('<span class="ro-total"><b>' + before + '</b> hall’s total walk, as the game left them</span><span>Run the day’s pass to see the arrangement the mod would choose.</span>');
      button.textContent = 'Run the day’s pass';
      arranged = false;
    }
  });
  button.hidden = false;
})();
