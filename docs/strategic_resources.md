# Strategic Resources — Design & Status

Materials gate industry — softly. Iron, Coal, and Oil exist on the map,
can be claimed by remote camps far outside any city's working radius, and
missing one makes the affected production cost +50% shields. Never a wall,
always a price.

## The model (implemented July 2026)

- **Deposits are derived, not placed.** `Game.ResourceAt(tile)` reads the
  map's existing special tiles: Mountains special = **Iron**, Hills special
  = **Coal** or **Copper**, Desert special = **Oil** or **Salt**, Swamp
  special = **Oil**. The split is a checkerboard over the lattice's 4x4
  blocks (`BaseTile.SecondSeam`). No new map state, no mapgen changes;
  distribution follows the classic special pattern.
- **Possession** = any of your cities *works* the deposit tile, **or** you
  hold a **camp** on it (`Game.ResourceCamps`, tile → owner, .cos-saved).
- **Camps**: a Settlers order ("Build <Resource> Camp", 3 turns) on any
  unclaimed deposit outside a city — including deep wilderness. Renders as
  a fortified mine. A city founded over a camp absorbs it.
- **Capture by occupation**: any unit standing on a rival's camp at turn's
  end takes it — flags on mines, not ashes (`ProcessResourceCamps`).
  Barbarians count: a barbarian-held camp yields to nobody until retaken.
  Grey goo destroys camps outright (the goo does not mine; it only takes).
- **The soft gate** (`City.ProductionCost`): +50% shields without the
  material, applied to the completion target, rush-buy pricing, AI
  rush-buy math, the bond pool cap, and every city-screen progress display
  — the higher target is *visible*, not hidden.
- **What wants what** (`Game.RequiredResource`):
  - **Copper** (Oct 2026, reaching back to the bronze age by the user's
    choice): Phalanx, Chariot, Catapult, Trireme, Mech Inf, Cruise Missile,
    Reaper Drone, Nuclear; Hydro Plant, Observatory, SAM Battery, Neural Lab,
    Mission Control
  - **Salt** (Oct 2026): Granary. Also a luxury: held anywhere, +1 content
    in every city. Salt pans pay +2 trade.
  - **Iron**: Cannon, Artillery, Ironclad
  - **Coal**: Factory, Power Plant
  - **Oil**: Armor, Fighter, Bomber, Cruiser, Battleship, Submarine,
    Carrier, Transport
  - Fusion-era units (HoverTank, FusionInf) need nothing — that's the point
    of fusion.
- **AI**: production penalties apply automatically (same cost path); AI
  settlers claim deposits opportunistically when standing on one.
  Dedicated camp-*seeking* AI is deferred pending an autoplay read.

## Deliberately deferred

- Road/rail connectivity requirements for camps (assume wagons).
- Resource depletion.
- Resource trading via the diplomacy console — the tribute machinery
  ("$N/turn while the deal holds") is the natural carrier when wanted.
- Camp yields (camps gate; they don't produce).

## Planned expansion (user roadmap)

- **Copper** and **Salt** — BUILT Oct 2026, see above. Salt is the bridge
  between the strategic and luxury systems: a mineral that trades like a
  luxury and preserved the food of every empire that ever moved an army.
- **Luxury resources** — happiness-side twins of the strategic trio.
