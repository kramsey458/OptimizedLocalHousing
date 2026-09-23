---
name: Optimized Local Housing
description: The host's seating chart. Card stock pinned to a linen planner's board, drafted in blue for the plan as arranged and madder for the plan as it was.
colors:
  drafting-blue: "#2d5f9a"
  drafting-blue-deep: "#1f4677"
  madder: "#b4432f"
  lumber-brown: "#8a5a2b"
  farm-green: "#5b7a24"
  water-teal: "#2a7390"
  linen-board: "#e6e1d6"
  card-stock: "#fbf8f1"
  card-back: "#ebe4d4"
  ink: "#23201b"
  ink-muted: "#5a544a"
  rule: "#cfc7b8"
  button-ink: "#ffffff"
  drafting-blue-dark: "#8fb6e8"
  drafting-blue-deep-dark: "#b3cdf0"
  madder-dark: "#e98a74"
  lumber-brown-dark: "#d19a63"
  farm-green-dark: "#a3c46a"
  water-teal-dark: "#74bdd8"
  lamplit-board: "#1b1d21"
  card-stock-dark: "#26292e"
  card-back-dark: "#1f2226"
  ink-dark: "#ece6da"
  ink-muted-dark: "#b3ab9e"
  rule-dark: "#3b3f46"
  button-ink-dark: "#10192a"
typography:
  display:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "clamp(2.5rem, 5.6vw, 4.1rem)"
    fontWeight: 700
    lineHeight: 1.08
    letterSpacing: "-0.01em"
  display-guide:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "clamp(2.3rem, 5vw, 3.5rem)"
    fontWeight: 700
    lineHeight: 1.08
    letterSpacing: "-0.01em"
  headline:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "clamp(1.8rem, 3.5vw, 2.55rem)"
    fontWeight: 700
    lineHeight: 1.08
    letterSpacing: "-0.01em"
  title:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "1.28rem"
    fontWeight: 700
    lineHeight: 1.2
  pitch:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "clamp(1.4rem, 2.5vw, 1.85rem)"
    fontWeight: 400
    lineHeight: 1.2
  body:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 400
    lineHeight: 1.62
  body-large:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "1.1rem"
    fontWeight: 400
    lineHeight: 1.62
  label:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "0.98rem"
    fontWeight: 600
    lineHeight: 1
  caption:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "0.86rem"
    fontWeight: 400
    lineHeight: 1.62
  figure:
    fontFamily: "Libre Caslon Text, Georgia, Times New Roman, serif"
    fontSize: "1.3rem"
    fontWeight: 700
    lineHeight: 1
    fontFeature: "tnum"
  mono:
    fontFamily: "ui-monospace, Cascadia Mono, SFMono-Regular, Consolas, Liberation Mono, monospace"
    fontSize: "0.9rem"
    fontWeight: 400
    lineHeight: 1.55
rounded:
  card: "2px"
  svg-card: "1.5px"
  bar: "1px"
  round: "50%"
spacing:
  gutter: "clamp(16px, 4vw, 32px)"
  wrap: "1160px"
  section: "clamp(56px, 8vw, 100px)"
  sheet: "clamp(18px, 3vw, 28px)"
  column-gap: "clamp(28px, 5vw, 64px)"
  row: "8px"
  row-loose: "18px"
  target: "44px"
components:
  button-primary:
    backgroundColor: "{colors.drafting-blue}"
    textColor: "{colors.button-ink}"
    rounded: "{rounded.card}"
    padding: "0 22px"
    height: "50px"
  button-primary-hover:
    backgroundColor: "{colors.drafting-blue-deep}"
    textColor: "{colors.button-ink}"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "0 22px"
    height: "50px"
  theme-toggle:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.card}"
    padding: "0 14px"
    height: "44px"
  nav-link:
    backgroundColor: "transparent"
    textColor: "{colors.ink-muted}"
    typography: "{typography.label}"
    padding: "0 12px"
    height: "44px"
  nav-link-current:
    textColor: "{colors.ink}"
  card-stock:
    backgroundColor: "{colors.card-stock}"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "{spacing.sheet}"
  place-card:
    backgroundColor: "{colors.card-stock}"
    textColor: "{colors.ink}"
    rounded: "{rounded.svg-card}"
    width: "80px"
    height: "23px"
  chart-tooltip:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.linen-board}"
    rounded: "{rounded.card}"
    padding: "10px 12px"
    width: "260px"
---

# Design System: Optimized Local Housing

## Overview

**Creative North Star: "The Seating Chart"**

The site is a host's planner's board. A linen-grey board carries card stock pinned to it; on the cards the hall is drafted as a seating plan: homes are round tables with one seat tick per bed, workplaces are small labelled plaques, and adult beavers are tented place cards with their names lettered in Caslon italic. The plan as the game left it is drawn in dashed madder, the plan as arranged in solid drafting blue. Every diagram is drawn with one stroke weight on a plain grid, depth comes from flat stacked card, and the only thing that casts a shadow is a pin.

Density is a well-set printed program: generous section padding, long ruled lists rather than tiles, hairline rules between every row, and a serif display face (the face of printed invitations and place cards) against a plain system body. Evidence is set as figures and labelled field lists, never as stat tiles. The two themes are the same board: daylight linen, and the same board by lamplight with the inks lifted.

Confirmed rejections: the shared Timbermods landing template (cream paper, icon tiles, stat tiles, pill badges, eyebrows), gradients, stacked drop shadows, and Fraunces (tried first, dropped as overused).

**Key Characteristics:**
- Card stock on a textured linen board, meeting it at a 1px rule edge with a 2px corner.
- Blue means after, madder means before, everywhere: walk lines, chart bars, tree captions, the readout.
- One stroke weight (1.4) for every drawn line in every diagram.
- Libre Caslon Text 700 for headings, numbers and labels on cards; its italic for names and the pitch; system-ui for reading.
- Ruled lists, field lists and tables carry the content; hairline rules separate every row.

## Colors

A quiet paper-and-ink palette with two drafting inks that carry meaning, plus three workplace hues used only inside diagrams.

### Primary
- **Drafting Blue** (light `{colors.drafting-blue}`, dark `{colors.drafting-blue-dark}`): the plan as arranged. Solid walk lines after the pass, the move-cycle arrows, the "after" bar and total, primary buttons, links, focus rings, the current nav underline, the "Tested" column rule, the "Optimized Local Housing" comparison header, check marks.
- **Drafting Blue Deep** (light `{colors.drafting-blue-deep}`, dark `{colors.drafting-blue-deep-dark}`): hover for links and primary buttons only.

### Secondary
- **Madder** (light `{colors.madder}`, dark `{colors.madder-dark}`): the plan as it was. Dashed walk lines before the pass, the "Before" bar, the guest who walks farther (card stroke and readout line), "never" crosses, the wrong-layout caption, and the pins.

### Tertiary
- **Lumber Brown, Farm Green, Water Teal** (`{colors.lumber-brown}`, `{colors.farm-green}`, `{colors.water-teal}`; lifted in dark): the three workplaces in the seating plan. They colour the station letter and the chip on each place card, and nothing outside the diagrams.

### Neutral
- **Linen Board** (light `{colors.linen-board}` under `textures/board-light.webp`) / **Lamplit Board** (dark `{colors.lamplit-board}` under `textures/board-dark.webp`): the page ground.
- **Card Stock** (`{colors.card-stock}` / `{colors.card-stock-dark}`, under `textures/card-*.webp`): every card, the plan's tables, plaques and place cards, code blocks.
- **Card Back** (`{colors.card-back}` / `{colors.card-back-dark}`): the darker back face of a tented place card, the one tonal step used for depth inside the plan.
- **Ink** (`{colors.ink}` / `{colors.ink-dark}`): text, every diagram stroke, the strong rule under the header and above each section.
- **Ink Muted** (`{colors.ink-muted}` / `{colors.ink-muted-dark}`): secondary paragraphs, captions, nav links at rest, kits, the "Not yet" column rule.
- **Rule** (`{colors.rule}` / `{colors.rule-dark}`): hairlines between rows, card edges, chart gridlines (dashed).
- **Band** (ink at 4% light, 3.5% dark): inline code, the raw Pass line, pinned notes, ghost-button hover.

### Named Rules
**The Before-and-After Rule.** Madder is the plan as it was, drafting blue is the plan as arranged. Never swap them and never use either as decoration; if a thing isn't before or after (or a link, focus or pin), it is ink.

**The Workplace Hues Stay in the Hall Rule.** Brown, green and teal exist to tell three workplaces apart inside a diagram. They never colour text, buttons or sections.

## Typography

**Display Font:** Libre Caslon Text 400, 400 italic, 700 (self-hosted woff2, OFL), with Georgia, Times New Roman, serif
**Body Font:** system-ui stack
**Label/Mono Font:** ui-monospace, Cascadia Mono, Consolas stack, for log lines and Pass-line field names

**Character:** A printed invitation's Caslon for everything that is a heading, a name or a number, set against a plain system sans for reading. The italic is the hand on the place cards.

### Hierarchy
- **Display** (700, `clamp(2.5rem, 5.6vw, 4.1rem)`, 1.08, -0.01em, balanced): the home page title. Guide pages use the smaller display-guide (`clamp(2.3rem, 5vw, 3.5rem)`).
- **Pitch** (Caslon italic 400, `clamp(1.4rem, 2.5vw, 1.85rem)`, 1.2, drafting blue): the one line under the home title.
- **Headline** (700, `clamp(1.8rem, 3.5vw, 2.55rem)`, 1.08): section heads; guide sections use `clamp(1.55rem, 2.8vw, 2rem)` with a rule above.
- **Title** (700, 1.28rem, 1.2): card heads, rule-key heads; the same Caslon 700 at 1.02 to 1.15rem sets step names, field-list terms, table heads, tree captions, button text and the TOC title.
- **Figure** (Caslon 700, 1.15 to 1.3rem, tabular figures): totals in the readout, bar values, the tooltip value.
- **Body** (400, 1.0625rem, 1.62): all reading text; lead and section intros at 1.1rem, held to 56 to 64ch; long guides at 760px.
- **Label** (system 600, 0.86 to 0.98rem): nav, theme toggle, captions, legends, compare labels on phones. Strong text is 650.
- **Mono** (0.86 to 0.92rem, 1.5 to 1.55): log lines and the Pass line, wrapped on a strict character grid.

### Named Rules
**The Caslon for Things on Cards Rule.** Headings, names, numbers and anything lettered on a card are Caslon; anything you read at length is system-ui. Names of guests are always Caslon italic.

**The Numbers Are Figures Rule.** Evidence numbers are set as Caslon 700 with tabular figures in a labelled field list or a readout, never as oversized stat tiles.

## Layout

A single centred column of 1160px with a fluid gutter (`clamp(16px, 4vw, 32px)`). Sections are separated by a full-width ink rule (`ruled`) and padded `clamp(56px, 8vw, 100px)` vertically. Section heads cap at 760px.

Grids are two-column and uneven where content is uneven: the hero is `.95fr / 1.05fr` (pitch left, seating plan right), results `1.1fr / .9fr`; everything else (why, rules key, steps with the move cycle, always/never, tested/not yet, guide tree pairs) is two equal columns with `clamp(28px, 5vw, 64px)` gaps. Guide pages use a 220px sticky table of contents beside a 760px body.

Lists are ruled rather than boxed: facts, rule keys, steps, field lists, FAQ entries and TOC links each sit between 1px hairlines with 8 to 18px of row padding. Numbered steps hang a 38px outlined Caslon numeral in a 56px left margin.

Responsive: at 960px the hero and results stack and the plan caps at 640px; at 860px every two-column grid stacks and the TOC flows into two columns; at 720px the nav wraps under the brand, the footer stacks, the comparison table becomes a labelled list per row, and the seating plan runs edge to edge (negative gutter margins, square corners); at 560px the plan's lettering grows through CSS geometry on the SVG (names 16px, wider cards, larger kit and table labels, bed counts and chip letters hidden). Touch targets are 44px minimum.

## Elevation & Depth

Flat. Depth is card stock laid on the board, stated by the card's own tone and texture and a 1px rule edge, never by shading. Inside the seating plan, the place card's darker back face above the fold is the only tonal depth cue. Two shadows exist: the pin, and the chart tooltip while it floats.

### Shadow Vocabulary
- **Pin** (`box-shadow: 0 2px 2px rgba(0, 0, 0, .25)`): the two 10px madder pins at the top corners of a pinned card (the seating plan and the move-cycle figure).
- **Tooltip lift** (`box-shadow: 0 10px 24px -10px rgba(0, 0, 0, .5)`): the ink chart tooltip on hover or focus only; never at rest.

### Named Rules
**The Only Pins Cast Shadows Rule.** Cards meet the board at a 1px rule edge and cast nothing. A pin is the one resting shadow on the page; don't add shadows to cards, buttons or diagrams.

## Shapes

Nearly square. Cards, buttons, code blocks, the toggle and tooltips turn a 2px corner; SVG place cards and plaques 1.5 to 2px; chart bars 1px. Circles are reserved for meaning: tables, pins, kits (dashed), workplace chips on place cards, and step numerals. Diagram strokes are 1.4 everywhere, rising to 2.4 only on a place card that moved (blue) or walks farther (madder). Dashes mean "before" (4 4 on walk lines), "not settled" (the open-item mark), or "never moves" (the kit's 2 2).

## Components

### Buttons
Printed, square, and quiet.
- **Shape:** 2px corner, 50px tall, 22px side padding, Caslon 700 at 1.08rem, an optional 18px line icon.
- **Primary:** drafting blue fill with button ink (white light, deep navy dark).
- **Hover / Focus:** deep blue fill and a 1px lift over 150ms `cubic-bezier(.2, .8, .2, 1)`; focus is a 3px drafting-blue outline at 3px offset.
- **Ghost:** transparent with a 1.5px ink border; hover lays the band tint and lifts 1px.
- **Theme toggle:** a 44px text button ("Dark mode" / "Light mode") with a 1px rule border that darkens to ink on hover.

### Cards / Containers (card stock)
- **Corner Style:** 2px.
- **Background:** card stock plus the card texture.
- **Shadow Strategy:** none; see the Only Pins Cast Shadows Rule. Figures that belong to the plan (the seating plan, the move cycle) are pinned.
- **Border:** 1px rule.
- **Internal Padding:** `clamp(18px, 3vw, 28px)`; pinned figures take 30px at the top to clear the pins.

### Navigation
System 600 at 0.98rem in ink muted, 44px tall, 12px padding; hover goes to ink; the current page is ink with a 2px drafting-blue underline. The brand is Caslon 700 with the house-and-workplace favicon. The header and footer are ruled off in strong ink. On phones the nav wraps to its own row with tighter padding.

### Ruled Lists (rules key, facts, steps, checks)
The content model in place of tiles. The rules key is a two-column list of Caslon titles with muted paragraphs, each under a hairline. Facts carry a 24px drafting-blue line icon. Steps hang an outlined Caslon numeral. Checks use a masked mark: a blue tick (always), a madder cross (never), a muted dashed circle (not yet).

### Field Lists and the Pass Line
Evidence is a two-column labelled list: a Caslon 700 term (tabular) and a muted meaning, each row between hairlines. The Pass line sets the raw log line in mono on a band, word-wrapped, then names its fields in the same list with mono terms.

### Comparison Table
Three-way table on card stock: Caslon heads over a 1.5px ink rule, row heads at 650, hairline rows, "Optimized Local Housing" headed in blue. Under 720px each row becomes a block with its column name labelled above every cell (the last label in blue).

### Chart and Tooltip
Horizontal bars (20px, 1px corner) in madder for before and blue for after, direct Caslon values at the end, dashed rule gridlines and muted ticks. Every row is focusable; the tooltip is an ink panel with ground-coloured text that fades in over 120ms. A "View chart data as a table" disclosure carries the same numbers.

### FAQ Disclosure
Ruled `details` rows at least 56px tall, 650 weight question, a masked chevron that turns 180 degrees over 200ms; hover turns the question blue. A linked hash opens its entry.

### The Seating Plan (signature)
A pinned card holding an SVG hall: three workplace plaques along the bottom, round opaque tables with a Caslon name, a muted bed count and one tick per bed, dashed kits beside the homes they belong to, and tented place cards (a card-back strip above a card-stock front, a workplace chip, the name in Caslon italic). Walk lines run from each seat to its workplace: madder dashed as the game left them, drafting blue after. "Run the day's pass" computes the best arrangement on the page, moves the cards round in cycles over 900ms `cubic-bezier(.45, 0, .2, 1)` with the lines hidden in transit, strokes moved cards blue and the one guest who walks farther madder, and reports before, after, beavers moved, cycles, and who walks farther in a polite live region. The button toggles back to "Seat them as they were". Under reduced motion the cards jump and the totals still update. A legend explains the two line styles and the caption says plainly it is an illustration. The move-cycle figure lower on the page redraws the same pass with the same strokes and cards.

## Do's and Don'ts

### Do:
- **Do** keep blue for after and madder for before on every chart, diagram, caption and readout.
- **Do** draw every diagram line at stroke 1.4 in ink or a drafting ink, on card-stock fills, and state depth with the card back's tone, not shading.
- **Do** set content as ruled lists, labelled field lists and tables with 1px rule hairlines between rows.
- **Do** put headings, names, numbers and card lettering in Libre Caslon Text (names in italic), and reading text in system-ui.
- **Do** keep corners at 2px or less and circles for tables, pins, kits, chips and step numerals only.
- **Do** keep 44px touch targets, a 3px focus outline at 3px offset, and a reduced-motion path where motion is switched off and the outcome still shows.
- **Do** give every chart a direct label and a table view.

### Don't:
- **Don't** bring back the Timbermods landing template: cream paper, icon tiles, stat tiles, pill badges or eyebrows.
- **Don't** add shadows to cards, buttons or diagrams; the pin is the only resting shadow.
- **Don't** use gradients.
- **Don't** use the workplace hues outside the diagrams.
- **Don't** use Fraunces or any display face besides Libre Caslon Text.
- **Don't** use official Timberborn logos or key art; the site's marks are its own favicon and simple line icons.
