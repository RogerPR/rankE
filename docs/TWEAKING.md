# RANK E — tweaking guide (self-service)

How to change the game **yourself** without going through Claude Code. Each thing below
is tagged by the mechanism you use:

- 🟢 **Live in-game** — the FIGHT SETUP panel; no recompile, applies next fight.
- 🔵 **Unity Inspector** — select a scene object, edit fields; saved in the scene.
- 🟡 **Code edit** — change a `.cs` file (recompiles on next play / focus).

> Architecture reminder: the sim core (`Assets/Scripts/Sim`) is pure, deterministic C#.
> Combat numbers have **one source of truth** — `Sim/Data/DefaultContent.cs` (abilities,
> statuses, gear) and `Sim/Data/CombatTuning.cs` (global rules). Edit those to change the
> shipped defaults, or use the live panel for per-fight tweaks. The edit-mode tests pin
> their *own* fixture numbers (see `Tests/Sim/TestKit.cs`), so retuning the game doesn't
> break them.

---

## 1. Params: global rules, character stats, per-ability numbers 🟢

All of this is editable live in the **FIGHT SETUP** screen (`ControlPanelScreen`), shown
before every fight and reachable mid-fight via **pause → return to initial screen**. It
edits the live `TuningProfile`; changes apply on the **next** fight (each fight clones the
profile at start, so "Start fight" is when they take effect).

| Want to change | Where in the panel |
|---|---|
| Global pace / rules (`GcdTicks`, etc. — all `CombatTuning` fields) | **Global modifiers** section |
| Character Max HP, Spell gems, full `StatSheet` | **Player** / **Opponent** sections |
| Stance / Weapon / Armor gear | per-fighter cyclers |
| Which model fights | **Character** / **Monster** cycler |
| Per-ability numbers (cooldown, cast, delay, pre/post-lock, gem cost, damage) | **"Edit abilities"** button → pick an ability → edit its fields |
| Save/load a tuning set | **preset bar** (writes named fixtures to disk) |

**Make a tuned value the permanent default** (so it survives without a preset): edit the
source of truth directly — `Sim/Data/DefaultContent.cs` for ability/gear numbers, or
`Sim/Data/CombatTuning.cs` for global rules. Current shipped pace is GCD 30 ticks, Slash
cooldown 60, auto-attack interval 60 (all defined there). The tests won't break — they pin
their own fixture numbers in `Tests/Sim/TestKit.cs` and per-test.

---

## 2. Camera zoom & angle 🔵

`Main Camera` in `Assets/Scenes/CombatScene.unity` — a normal scene object. Select it,
edit in the Inspector:

| Knob | Field | Current |
|---|---|---|
| **Zoom** | Camera ▸ **Orthographic Size** (smaller = closer) | `3` |
| Projection | **Orthographic** (FOV ignored while ortho) | on |
| **Angle** | Transform ▸ **Rotation** (tilt X for a 3/4 view) | `0,0,0` (straight-on) |
| Position | Transform ▸ **Position** | `0, 1.5, -10` |

Want perspective depth instead of flat ortho? Set Projection → **Perspective**; then
**Field of View** + camera distance become your zoom.

Other scene knobs: `Directional Light` (mood), `Ground`, and the two fighter anchors
**`PlayerCapsule`** (`-2,1,0`) / **`EnemyCapsule`** (`2,1,0`) — move these to change how
far apart the fighters stand. Spawn facing is `playerYaw`/`enemyYaw` on **`FighterStage`**
(`[SerializeField]`, also Inspector-editable on the CombatBootstrap GameObject).

---

## 3. Add / remove abilities

Two different things:

- **Which abilities a fighter carries** 🟢 — the ability slots per fighter in the
  FIGHT SETUP panel (4 main + 2 quick). Slots map to keys **by GCD class**: main (normal)
  abilities → Q/W/E/R, quick actions → SPC/F — so a fighter with only 2 main + 2 quick
  still binds its quick actions to the quick keys/bumpers.
- **The enemy's attack rotation** 🟢🟡 — when a scenario references an **opponent** (see §5),
  the rotation, cadence and telegraph come from that opponent's JSON file (data-driven,
  weighted steps). With no opponent referenced, the enemy falls back to the default sparring
  rhythm built in `MatchController.cs` (`new[] { DefaultContent.SlashId }`), with cadence
  **`EnemyActionIntervalTicks`** and **`EnemyTelegraphTicks`** (Inspector fields on the
  CombatBootstrap GameObject, defaults `60`/`10`).
- **Define a brand-new ability** 🟡 — add an `AbilityDef` in `Sim/Data/DefaultContent.cs` (the
  library): a stable `Id`, its `Effects`, and `GcdClass` (None/Normal/Quick). It then shows
  up automatically in the panel slots and the ability-number editor. Keep it data-driven —
  don't branch on the id in logic.
- **Passive skills** 🟡 — a build carries a list of passive ids (`passiveIds`). Auto-attack
  is now a passive (`"auto_attack"`), not always-on: a fighter with an empty `passiveIds`
  makes no auto-attacks (the tutorial fighters do this). Add passive kinds in
  `Sim/Data/PassiveDef.cs` + `DefaultContent.Passive(...)`; only the auto-attack kind is
  wired today.

---

## 4. Move / resize HUD panels 🔵 (new — data-driven layout)

The HUD is built in code, but **panel positions are now Inspector fields**. Select the
**CombatBootstrap** GameObject in `CombatScene` → the **HudRoot** component → expand
**Layout**. Each entry is a `HudPlacement` with three values:

```
anchor  (x, y)  0–1 screen anchor: (0,0)=bottom-left, (1,1)=top-right, (0.5,0.5)=centre
offset  (x, y)  pixels from that anchor (sign follows the anchor corner)
size    (x, y)  panel width/height in pixels
```

| Field | Panel | Default anchor / offset / size |
|---|---|---|
| `nextActions` | Opponent's next-3 actions (top-right) | (1,1) / (-24,-132) / (384,264) |
| `abilityBar` | Player ability grid (bottom-left) | (0,0) / (40,40) / (392,200) |
| `combo` | Combo / riposte tracker | (0,0) / (40,250) / (320,110) |
| `playerCast` | Player casting indicator | (0,0.5) / (300,150) / (420,56) |
| `playerStatuses` | Player buff column (left edge) | (0,1) / (24,-440) / (220,420) |
| `enemyStatuses` | Opponent buff column (right edge) | (1,1) / (-24,-440) / (220,420) |
| `topBarHeight` | Height of the full-width top status strip | `112` |

Changes apply when the HUD is built (entering a fight / pressing Play) and persist in the
scene. Moving a panel's root moves its children with it; for `nextActions` and
`abilityBar` the panel internals also scale to the `size.x` you set.

**Defaults & code:** the same defaults live in `Assets/Scripts/UI/HudLayout.cs` (the
`HudLayout` class) if you ever want to change them in code. The top bar's internal layout
(names, item slots, menu cluster) and the under-character break bars are still hand-placed
in their widget files (`TopStatusBar.cs`, `BreakBarView.cs`) — only the panel roots above
are data-driven.

---

## 5. Opponents & saved scenarios 🟢🟡

Two on-disk JSON catalogues (siblings of `Assets/`, committed, not Unity-imported):

- **`RankE/TuningPresets/*.json`** — *scenarios*: a full tunable fight (global rules, ability
  numbers, the **player** build, the chosen visuals, and optionally an `opponentId`). Saved /
  loaded from the FIGHT SETUP **preset bar**. `Tutorial.json` is the early-game tutorial.
- **`RankE/Opponents/*.json`** — *opponents*: a reusable enemy = its build (stats, abilities,
  passives, gear) **and** its AI logic (a telegraphed, weighted rotation). `tutorial.json` is
  the first one.

A scenario with `"opponentId": "tutorial"` resolves its adversary entirely from
`Opponents/tutorial.json` (build + AI + visual), overriding any inline `adversary` block. With
no `opponentId`, the inline adversary build + the default sparring rhythm are used.

**Authoring an opponent** (edit the JSON directly):

```jsonc
{
  "id": "tutorial",
  "displayName": "Sparring Mage",
  "visualName": "Imp Devil",          // an existing monster model
  "build": {
    "maxHp": 120, "spellGems": 8,
    "passiveIds": [],                  // [] = no auto-attack
    "mainSlotCount": 2,
    "abilityIds": ["slash", "fireball", "kick"],
    "gearIds": []
  },
  "behavior": {
    "intervalTicks": 60,              // one telegraphed beat every 3 s
    "telegraphTicks": 10,            // wind-up before each beat
    "steps": [                        // the rotation, repeated; each step = a weighted choice
      { "options": [ { "id": "slash", "weight": 1.0 } ] },
      { "options": [ { "id": "slash", "weight": 0.66 }, { "id": "fireball", "weight": 0.34 } ] }
    ]
  }
}
```

Quick actions (e.g. `kick`) stay **reactive** (used between beats, interrupting casts) and are
not part of `steps`. A single-option step is a fixed beat; multi-option steps roll by weight
off the seeded fight RNG (deterministic per seed). The plan/telegraph show the rolled choice.

The opponent's **NEXT** queue is a committed look-ahead: the brain pre-rolls the next several
beats, so what the HUD lists is exactly what will fire (a weighted Fireball appears in the
queue *before* it lands, never as a surprise). The queue stacks directly above the opponent
**cast bar** as one top-right timeline — soonest beat nearest the bar (its bar is the cadence
countdown), later beats rising upward; the action that is actually telegraphing/casting shows
only on the cast bar.

**Block & the physical shield** 🟡 — `Block` (quick action) applies two independent,
**physical-only** layers for 2 s: a −60% damage status (`block_phys`) and a small absorb pool
(`phys_shield`, size = the Block ability's shield-effect amount, editable in the ability
editor). Magic (e.g. Fireball) ignores both — Kick (interrupt) is the answer to casts. Tune
Block's cooldown and the shield amount/duration like any ability number.

---

## Quick reference: file map

| Area | File |
|---|---|
| Live tuning panel | `Assets/Scripts/UI/Screens/ControlPanelScreen.cs` |
| Per-ability editor | `Assets/Scripts/UI/Screens/AbilitiesEditorScreen.cs` |
| Ability/gear/status defaults (source of truth) | `Assets/Scripts/Sim/Data/DefaultContent.cs` |
| Global rule defaults (GCD, break, combo) | `Assets/Scripts/Sim/Data/CombatTuning.cs` |
| Live tuning state (cloned per fight) | `Assets/Scripts/Game/TuningProfile.cs` |
| Test fixture numbers | `Assets/Tests/Sim/TestKit.cs` |
| Enemy rotation / cadence (default fallback) | `Assets/Scripts/Game/MatchController.cs` |
| Enemy brains | `Assets/Scripts/Sim/AI/ScriptedRhythmBehavior.cs` · `WeightedRotationBehavior.cs` |
| Opponent files + store | `RankE/Opponents/*.json` · `Assets/Scripts/Game/Opponents/OpponentStore.cs` |
| Scenario files + store | `RankE/TuningPresets/*.json` · `Assets/Scripts/Game/Tuning/TuningPresetStore.cs` |
| Passive defs (auto-attack) | `Assets/Scripts/Sim/Data/PassiveDef.cs` · `DefaultContent.Passive(...)` |
| Camera / lights / anchors | `Assets/Scenes/CombatScene.unity` (Inspector) |
| Fighter spawn facing | `Assets/Scripts/Game/View/FighterStage.cs` |
| HUD panel positions | `HudRoot` component (Inspector) · `Assets/Scripts/UI/HudLayout.cs` |

---

## Verifying a change

- **Gameplay/tuning numbers:** just play a fight (or run a balance sweep — see `CLAUDE.md`).
- **After editing any `.cs`:** run the headless EditMode tests with the **editor closed**
  (command in `CLAUDE.md`). Exit code 0 / `failed="0"` = good. The whole EditMode suite must
  stay green; view-only edits can't affect the sim, but a clean compile is the gate.
