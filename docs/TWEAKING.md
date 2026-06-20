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

- **Which abilities a fighter carries** 🟢 — the six ability slots per fighter in the
  FIGHT SETUP panel.
- **The enemy's scripted attack rotation** 🟡🔵 — the enemy does a fixed non-quick
  rotation on a cadence (with reactive Parry/Kick between beats). Grow the list in
  `MatchController.cs` (`new[] { DefaultContent.SlashId }` → add more ids); change the cadence
  via **`EnemyActionIntervalTicks`** (Inspector field on the CombatBootstrap GameObject,
  default `60` = 3 s) and **`EnemyTelegraphTicks`** (wind-up before each hit, default `10`).
- **Define a brand-new ability** 🟡 — add an `AbilityDef` in `Sim/Data/DefaultContent.cs` (the
  library): a stable `Id`, its `Effects`, and `GcdClass` (None/Normal/Quick). It then shows
  up automatically in the panel slots and the ability-number editor. Keep it data-driven —
  don't branch on the id in logic.

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

## Quick reference: file map

| Area | File |
|---|---|
| Live tuning panel | `Assets/Scripts/UI/Screens/ControlPanelScreen.cs` |
| Per-ability editor | `Assets/Scripts/UI/Screens/AbilitiesEditorScreen.cs` |
| Ability/gear/status defaults (source of truth) | `Assets/Scripts/Sim/Data/DefaultContent.cs` |
| Global rule defaults (GCD, break, combo) | `Assets/Scripts/Sim/Data/CombatTuning.cs` |
| Live tuning state (cloned per fight) | `Assets/Scripts/Game/TuningProfile.cs` |
| Test fixture numbers | `Assets/Tests/Sim/TestKit.cs` |
| Enemy rotation / cadence | `Assets/Scripts/Game/MatchController.cs` |
| Enemy brain | `Assets/Scripts/Sim/AI/ScriptedRhythmBehavior.cs` |
| Camera / lights / anchors | `Assets/Scenes/CombatScene.unity` (Inspector) |
| Fighter spawn facing | `Assets/Scripts/Game/View/FighterStage.cs` |
| HUD panel positions | `HudRoot` component (Inspector) · `Assets/Scripts/UI/HudLayout.cs` |

---

## Verifying a change

- **Gameplay/tuning numbers:** just play a fight (or run a balance sweep — see `CLAUDE.md`).
- **After editing any `.cs`:** run the headless EditMode tests with the **editor closed**
  (command in `CLAUDE.md`). Exit code 0 / `failed="0"` = good. The sim suite (89 tests) must
  stay green; view-only edits can't affect it, but a clean compile is the gate.
