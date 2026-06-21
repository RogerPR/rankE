# GAME DESIGN — Working title: *RANK E* (PROPOSED)

A real-time 1v1 dueling roguelite. Two fighters stand facing each other; combat is a
rhythmic dance of cooldowns, casts, parries and interrupts. Early game every button
press matters; late game your build automates itself into a gleeful auto-battler while
you focus on the big moments (parries, finishers, clutch calls).

Anything marked **(PROPOSED)** is a concrete fill for a gap in the original vision —
it unblocks development but is explicitly up for veto once playable. All numbers live
in data files, never hardcoded, so changing them is cheap.

> **This document is the overall direction, not a contract.** If, while implementing,
> a decision here seems wrong, inconsistent, or awkward — ask the user rather than
> following it blindly. And when two implementations cost about the same, prefer the
> more general one: the design will grow in predictable directions (e.g. new equipment
> categories like runes, new ability types, new statuses, new node types), so model
> these as open data-driven categories, not hardcoded enums of today's content.

## Pillars

1. **The dance** — combat has a rhythm. Rock-paper-scissors pressure (attack / parry /
   interrupt) makes fights feel like a duel, not a DPS race.
2. **The parry must feel amazing** — hitstop, flash, sound. This is the soul of the game
   and gets disproportionate polish budget.
3. **From pianist to conductor** — roguelite rewards shift you from pressing every button
   (early) to designing an automated machine and intervening at key moments (late).
4. **Readable at a glance** — enemy intent, cast bars, cooldowns: everything visible,
   nothing hidden. Fire Emblem clarity.

## References

- **Sekiro** — posture/break bar, parry feel.
- **Expedition 33** — break damage as a parallel offense track.
- **Neoverse** — ability-order combos.
- **Slay the Spire** — node-map run structure, 3-choice rewards.
- **Fairy Tail / Solo Leveling** — lore: guild ranks, missions, power climb.
- **Fire Emblem** — aesthetic: clean 2D UI, stylized 3D warriors/mages.
- **FF XII gambits** — auto-cast rule slots **(PROPOSED)**.

---

## 1. Core combat

### Simulation model

- Deterministic, tick-based simulation: **20 ticks/second (50 ms/tick)**, same as the
  Python PoC. All combat logic runs in the pure C# sim core (no UnityEngine).
- Seeded RNG for reproducibility (headless balance runs, replays, tests).
- The Python PoC in `functional_POC/` is the reference implementation for Phase 1.
  Its constants (in seconds): GCD 1.0s, quick-action GCD 0.3s, auto-attack every 2s,
  parry window 0.6s, fireball cast 2s, etc. Port behavior, then tune.

### Ability taxonomy

Every ability is data: damage, cooldown, cast time, tags, effects — plus a **type**
(physical/magic/support **(PROPOSED)**) and a **level** (I/II/III…) for the roguelite
acquisition/upgrade system (see §3); higher levels improve numbers or add effects.

| Type | GCD | Examples | Notes |
|---|---|---|---|
| Auto-attack | none | basic strike | Fires automatically on interval while not casting/stunned |
| Instant | normal (1.0s) | Slash, Bash | Effect on press |
| Cast | normal | Fireball, Vampiro | Cast bar; **interruptible** or **uninterruptible** per ability |
| Delayed | normal | e.g. Falling Star **(PROPOSED)** | Triggers after X seconds; visible timer on screen |
| Quick action | quick (0.3s) | Parry, Kick | Usable almost anytime; own cooldowns |

- **Animation lock (PROPOSED):** each ability defines `preEffectLock` and `postEffectLock`
  (seconds you can't act around the effect frame). Drives both feel and the 3D animation
  timing. Quick actions have near-zero lock.
- **Spell gems (PROPOSED, from PoC):** spells consume a limited stock of gems; gems are a
  build resource (more gems / gem regen via rewards). Keeps casters from being spam-bots.

### Character stats & resources (PROPOSED v2, locked 2026-06-13)

Stats are an **RPG sheet** and damage is **derived**, not flat per-ability (user
decision 2026-06-13). A fighter carries:

| Stat | Effect | Notes |
|---|---|---|
| Max HP | health pool | death at 0 |
| Attack | scales **physical** ability & auto-attack damage | offense stat A |
| Magic | scales **magic** ability damage and support potency (heals/regen) | offense stat B |
| Defense | reduces incoming damage | `defMult = 100 / (100 + DEF)` |
| Crit Chance | % chance to crit, seeded RNG (stays deterministic) | |
| Crit Damage | crit multiplier | default ×1.5 |
| Haste | % reduction to cooldowns **and** cast times | folds the old two reward lines into one stat |
| Break Power | multiplies break damage dealt | |
| Spell Gems / Max | crystal stock for casts | |
| Gem Regen | gems restored per N ticks | **new** — keeps casters from drying up |

**Damage formula (PROPOSED):**
`final = round( Base × (1 + Offense/100) × critMult ) × defMult × statusMults`
— `Offense` = Attack or Magic per the effect's **school** (physical / magic / true);
`critMult` = Crit Damage on a crit else 1; `defMult` = `100/(100+DEF)`; `statusMults`
folds Broken (×1.5), Guard (<1), Vulnerable (>1), Weaken (attacker's outgoing <1).
Effects gain a `School` field (physical default). Support effects scale off Magic via
the same `(1 + Magic/100)` term.

**Resources (locked 2026-06-13):** ability costs are **cooldowns + GCD + spell gems** —
**no mana**. Gems *are* the consumable ("crystals"); they now **regen** (Gem Regen stat /
reward) so casters aren't hard-capped. Quick actions stay gem-free, gated only by their
own cooldown + the 0.3s quick-GCD.

### The dance: parry, break, combos (Option 3 — all of it)

Three interacting systems. Quick actions live *outside* the combo system.

**a) Parry → Riposte (from PoC)**
- Parry opens a 0.6s window. Parrying a `parriable` attack negates it.
- Each successful parry adds **+2 to the riposte counter (max 8)**; at 8 you unleash an
  automatic **Riposte**: heavy damage + stun **(PROPOSED: also +30 break)**. Counter resets.
  **(PROPOSED:** the riposte fires automatically the instant the counter fills; the PoC
  instead replaced the next pressed ability.**)**
- Parry feedback: hitstop (~80 ms), white flash, distinct SFX — non-negotiable polish.

**b) Break bar (Sekiro/Exp33) (PROPOSED numbers)**
- Both fighters have a **Break bar, 0–100** (the bar under each character in the UI sketch).
- Sources of break damage: dedicated abilities (e.g. Bash: 20), successful parries
  (15 to the attacker).
- Bar decays slowly when out of pressure (-2/s after 3s without taking break damage).
- At 100: **BROKEN** — stunned 2.5s, takes +50% damage, bar resets. Big audiovisual moment.

**c) Colour-sequence combos (PROPOSED — replaces the earlier O→L→F tag combo)**
- Each non-quick ability carries a **combo colour** (open string id, e.g. red/yellow).
- The **player only** is shown a random target **sequence of 3–5 colours** drawn solely from
  the colours their own abilities carry. Pressing abilities whose colours match the sequence
  in order completes the combo.
- Completing it grants the **Empowered** status: the **next damaging hit deals ×2** (then the
  buff is spent). The completing hit itself is *not* doubled — the next one is.
- A wrong colour **reshuffles** into a fresh random sequence. Quick actions and colourless
  abilities are combo-neutral (neither advance nor reset).
- The sequence is generated from the seeded sim RNG, so combos stay deterministic/replayable.
- **(PROPOSED** colours for the dev/tutorial kit: Slash = red, Bash = yellow.**)**
- UI: a **player-only** colour-sequence tracker (bottom-left) — matched colours read bright,
  pending ones dim; the row flashes on completion. The opponent has no combo display.

### Statuses (PROPOSED v2)

**Built today** (`StatusDef` supports these): `Stun` (no actions, casts cancelled) ·
`Poison` (DoT) · `Regen` (HoT) · `Parry` · `Broken` (×1.5 dmg taken) · **`Distance`**
(fighters separated; melee whiffs, ranged unaffected; ends on timer or gap-closer).

**To add** (each notes the `StatusDef` extension it needs):
- `Burn` — stacking magic DoT → needs **stack support** on `StatusDef`.
- `Bleed` — physical DoT → `Poison` clone tagged physical school.
- `Slow` / `Haste` — ± cooldown & cast speed → needs a **`HasteMult`** field.
- `Guard` — incoming-damage reduction window → uses existing `DamageTakenMult` < 1.
- `Vulnerable` — +damage taken (a milder/shorter Broken) → `DamageTakenMult` > 1.
- `Weaken` — −attacker outgoing damage → needs an **`OutgoingDamageMult`** field.

Statuses show as icons in the side columns (per sketch).

**(PROPOSED** dev content exercising the new mechanics, numbers tunable: *Falling Star*
— delayed ability, 30 dmg landing 3s after use, 10s CD, 1 gem; *Lunge* — gap-closer,
clears `Distance` from both fighters, 5s CD.**)**

### Build base: class at run start, everything else earned

- The **only pre-run choice is a class** (starting stats, starting skills, quick action).
  Not yet concrete — design open. **(PROPOSED for the slice: ship with a single class,
  e.g. *Traveler*, and keep the class system a thin stub until the run loop is proven.)**
- The PoC's stance/weapon/armor picks are **not** pre-fight choices in the real game.
  They become **roguelite rewards** — gear and stances drop during the run, the StS-card
  analogue (see §3). The sim still models them as build modifiers; only *when/how you
  acquire them* changes.

### Enemy AI

- Per-enemy behavior profiles: ability priorities, parry tendencies, interrupt reactions
  (the PoC AI is the v1 template).
- Enemies telegraph: their next intended action and cast bars are visible (top-right in
  sketch). Late maps reduce telegraph time rather than hiding it **(PROPOSED)**.

---

## 2. Controls (controller-first)

| Action | Gamepad | Keyboard | Mobile (later) |
|---|---|---|---|
| Abilities 1–4 | North/West/East/South | Q / W / E / R | bottom button row |
| **Parry** | **RB / R1** | **Space** | big right-thumb button |
| Kick / quick 2 | LB / L1 | F | left of parry |
| Potion / item | D-pad | 1–3 | item row |
| Pause/menu | Start | Esc | corner button |

(PROPOSED — finalize in Phase 2 playtests.) Use Unity **Input System** (action maps,
rebindable). UI must never depend on hover or right-click (mobile feasibility).

---

## 3. Roguelite run layer

### Run structure (Slay-the-Spire style)

- A map is a **branching node graph (~12–15 nodes, 3 lanes)** **(PROPOSED)**.
- Node types: **Battle**, **Elite**, **Event** (text choice), **Rest** (heal or upgrade
  a skill), **Boss**. Shop **(PROPOSED, may cut from slice)** — gold shows in top bar.
- Run ends on death (back to guild, keep rank progress) or boss kill (rank up).

### Rewards — the three roguelite axes

After each battle, **choose 1 of 3** offers drawn from:
1. **% level-ups** — +10% max HP · +10% damage · -8% all cooldowns · +15% break damage ·
   +1 spell gem · +1 gem regen/30s · -10% cast times **(PROPOSED pool)**.
2. **New skills & gear** — the StS-card analogue; exact pools not yet concrete.
   - **Abilities** drop with a **type** and a **level**: type (e.g. physical / magic /
     support **(PROPOSED)**) drives synergies with gear and level-up offers; duplicates
     or Rest nodes **upgrade** an ability's level (Slash I → Slash II: better numbers
     and/or an added effect, like StS card upgrades) **(PROPOSED)**.
   - Bar has 4 slots + 2 quick slots; taking a 5th ability forces a swap **(PROPOSED)**.
   - **Gear** drops modify the build: weapons/armor/stances (the PoC's
     sword/dagger/wand, light/heavy armor, rock/wind/water stances live here as drops).
3. **Auto-casts** — the auto-battler engine, see below.

Elites/bosses additionally drop a rare pick (stronger versions) **(PROPOSED)**.

### Auto-casts / gambits (PROPOSED design)

- Rewards grant **gambit slots** and **rules**. A rule = `condition → action`:
  e.g. `every 10s → Heal`, `HP < 50% → drink potion`, `off cooldown → Fireball`,
  `enemy casting → Kick`.
- Rules evaluate top-to-bottom each tick; first valid fires. Player input always overrides.
- Early game: 0 slots. Late game: 5–6 slots and the build mostly plays itself — the player
  curates the machine and manually takes the high-value actions (perfect parries, finishers).

---

## 4. Progression & lore

You are a **rank E traveler** registered at the guild. Missions raise your rank:

| Content | Rank gained | Slice? |
|---|---|---|
| Tutorial — guild entrance exam (scripted fight teaching parry/cast/interrupt) | E → D | ✅ |
| Map 1 — *Bandit Roads* **(PROPOSED name)** | D → C | ✅ |
| Map 2 | C → B | later |
| Map 3 | B → A | later |
| Rank S — scripted bosses, fought with **saved rank-A builds** | — | later |

- Rank persists between runs (meta progression). Higher rank unlocks the next map.
- **Build saving:** clearing Map 3 snapshots your build; rank S content is a boss gauntlet
  for saved builds (no new rewards mid-fight — pure execution test).

### Map 1 roster (PROPOSED)

Bandit (melee basics) · Archer (applies `Distance`) · Apprentice Mage (interruptible
casts — teaches Kick) · Brigand Knight (break-bar pressure) · Elite: Berserker (combo
chains you must parry) · Boss: **Bandit Captain** (all mechanics, 2 phases).

---

## 5. Presentation

- **Camera:** orthographic, side-on. 3D characters and combat animations are the *only*
  3D; everything is reactive to sim state — no physics, no 3D logic.
- **UI (from the sketch):** top bar (name, HP, gold, menus) · status/buff icon columns
  at left/right edges · player cast bar top-left, enemy cast bar + upcoming-action queue
  top-right · combo tracker bottom-left · break bars under each fighter · ability bar
  bottom-center with radial cooldowns · vertical HP bars at far left/right.
- **Art direction:** Fire Emblem-adjacent — clean 2D panels, stylized 3D fantasy
  characters. Buy 3D character/animation packs; placeholders (capsules/free assets)
  until Phase 3. Floating combat text for ability names and damage (as in PoC).
- **Audio:** parry/break/riposte get bespoke SFX early; music can wait **(PROPOSED)**.

---

## 6. Explicitly out of scope (for the slice)

Multiplayer · localization · Steam integration · mobile build (design for it, don't build
it) · Maps 2–3 and rank S content · meta-currency shop between runs.

---

## Appendix A — Content catalogue, dev kit v2 (PROPOSED 2026-06-13)

The goal of v2 is to fill **every category × school** so reward pools, combos and the
enemy roster have material to draw from. **Numbers are starting points — tune via
BattleRunner.** Combo tags: O = Opener, L = Linker, F = Finisher. Schools: P = physical,
M = magic, S = support. All scale through the §1 damage formula.

**Built kit (11):** Auto-attack, Slash (O/P), Bash (L/P), Fireball (F/M), Vampiro (S),
Parry, Kick, Stop-Casting, Riposte (auto), Falling Star (delayed/M), Lunge (gap-closer).

**Instant — physical:**
- *Cleave* — O/P, solid hit + small break. The aggressive opener.
- *Shield Bash* — L/P, break + short stun; the defensive linker.
- *Execute* — F/P, bonus damage vs low-HP or Broken targets; cashes in pressure.

**Cast — magic / support:**
- *Ice Lance* — F/M, magic damage + apply `Slow`. Interruptible.
- *Chain Lightning* — F/M, magic damage, **ignores `Distance`** (true ranged).
- *Heal* — S, large self `Regen`, Magic-scaled, gem cost; the sustain caster.

**Delayed:**
- *Falling Star* (built). *Meteor* — heavy delayed magic, long CD/high gem cost; a
  read-and-punish or combo finisher you set up in advance.

**Quick actions:**
- Parry, Kick, Stop-Casting (built).
- *Sidestep* — applies `Distance` to self (dodge ranged / reposition), short CD.
- *Guard* — apply `Guard` status (brief damage reduction); the "block" to Parry's "counter".
- *Cleanse* — clear one debuff from self.

**Gear — now grants stat-sheet deltas** (generalizes today's mult-only `GearDef`):
- **Weapons** set Attack/Magic and shape the auto-attack: *Sword* (balanced ATK), *Dagger*
  (fast/low auto, +Crit), *Wand* (+Magic, −cast time), *Greataxe* (+ATK, +Break Power, −Haste).
- **Armor** trades defense for mobility: *Light* (+Haste, +Crit, −DEF), *Medium* (balanced),
  *Heavy* (+DEF, +Max HP, −Haste).
- **Stances** are playstyle modifiers (Rock/Wind/Water as today, re-expressed as stat deltas).
- **Runes** (future) are pure stat shards — the open `Slot` already admits a new category.
