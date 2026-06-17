# ROADMAP — Vertical slice: Tutorial + Map 1

Target: a polished, fully playable slice — guild tutorial (rank E→D) + Map 1 run with
roguelite choices (rank D→C). Detailed phases below; everything past the slice is an
outline only, to be detailed once combat is validated.

Cadence assumption: a few Claude Code sessions per week. A "session" ≈ one focused
Claude Code conversation. Each phase ends with a **Definition of Done (DoD)** — do not
start the next phase until the DoD is met and ROADMAP checkboxes are updated.

Status legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## Phase 0 — Foundation (1–2 sessions)

Goal: empty but correctly structured Unity project where sim tests run headless.

- [x] Create fresh Unity project `RankE/` at repo root (Unity 6000.1.10f1, URP
      "Universal 3D" template).
- [x] Folder layout: `Assets/Scripts/Sim` (asmdef, **noEngineReferences=true**),
      `Assets/Scripts/Game` (view/presentation), `Assets/Scripts/UI`,
      `Assets/Tests/Sim` (edit-mode test asmdef), `Assets/Data`, `Assets/Art`.
- [x] Unity Test Framework set up; one trivial sim test passes via Unity CLI batchmode
      (`-runTests`) — document the exact command in CLAUDE.md.
- [x] Install + verify Unity MCP bridge (CoplayDev/unity-mcp): Claude can create a
      GameObject and read the console.
- [x] Git hygiene: Unity .gitignore, decide on Git LFS for art, first commit.
      (LFS decision: **deferred** — no binary art until Phase 3; see Phase 3 note.)
- [x] Combat scene stub: orthographic camera, two placeholder capsules, ground plane
      (`Assets/Scenes/CombatScene.unity`).

**DoD:** `Sim` asmdef compiles with no UnityEngine refs; headless test run passes from
the command line; MCP round-trip works; scene shows two capsules facing each other.

## Phase 1 — Combat sim core (2–3 sessions)

Goal: the Python PoC fully ported to the pure C# sim, verified by tests, no visuals.

- [x] Tick engine: 20 ticks/s, seeded RNG, deterministic.
- [x] Data-driven ability definitions (plain C# data; ScriptableObject wrappers later).
- [x] Port all PoC mechanics: GCD + quick GCD, cooldowns, casting + interrupts
      (interruptible flag), auto-attack, parry window → riposte counter, statuses
      (stun/poison/regen), spell gems, stance/weapon/armor build modifiers (as build
      state set by run rewards, not pre-fight picks — GAME_DESIGN §1/§3).
- [x] New since PoC: break bar, combo tags (O/L/F), animation locks, delayed abilities,
      `Distance` status — per GAME_DESIGN §1.
- [x] Port PoC enemy AI as `BehaviorProfile` v1.
- [x] `BattleRunner`: headless AI-vs-AI fights with stats output (winner, duration,
      ability usage) — the balance tool for every later phase.
- [x] Unit tests per mechanic + integration test running full seeded fights.

**DoD:** 1000 headless AI-vs-AI fights run in seconds with summary stats; all tests
green; zero UnityEngine usage in `Sim`.

## Phase 2 — Presentation & input v1 (2–3 sessions)

Goal: a human can play a full fight vs AI in Unity with the sketch UI. Placeholder art.

- [x] Sim↔View binding: sim emits events (AbilityUsed, Parried, Damaged, Broken…);
      view layer consumes, never reaches into sim internals.
      (`BattleDriver` fixed-timestep accumulator + event cursor; enemy intent via new
      sim-side `TelegraphBehavior` decorator, tested.)
- [x] 2D UI per sketch: HP bars, cast bars, ability bar with radial cooldowns, status
      icon columns, enemy intent display, floating combat text.
      (Built 100% programmatically — `UiFactory`/`HudRoot`, no scene-authored UI.)
- [x] Input System: gamepad + keyboard action maps per GAME_DESIGN §2.
      (Action maps in code, `CombatInputActions`; sticky `PlayerIntentBuffer` with
      event ack + 0.3s expiry; potions deferred to Phase 4 — no item mechanic yet.)
- [x] Debug loadout picker (set stance/weapon/armor/abilities for testing — real
      acquisition comes via run rewards in Phase 4), countdown, victory/defeat
      → restart loop. (`MatchController` state machine.)
- [x] Capsules animate crudely (lunge on attack, flash on hit) — enough to read the fight.

**DoD:** you can play and win/lose a complete duel with controller or keyboard; every
sim mechanic is visible in the UI; no sim logic in MonoBehaviours.
*(✅ DoD met — user play-tested 2026-06-13, "the playstyle is good". 71 headless tests green.)*

## Phase 2.5 — Systems & content design pass (design only — NO feel/3D yet)

Goal: lock the data model + content catalogue so Phases 4/6 have material to build from.
Output is **design + scaffolding plans only** — building combat content still waits behind
the Phase 3 fun-gate. Decisions taken 2026-06-13.

- [x] Resource model **locked**: cooldowns + GCD + spell gems, *no mana*, gems regen
      (GAME_DESIGN §1 "Character stats & resources").
- [x] Character **stat sheet** defined: RPG sheet (Attack/Magic/Defense/Crit/Haste/
      Break Power/Gem Regen) + derived damage formula (GAME_DESIGN §1).
- [x] **Status library** v2 specced incl. the `StatusDef` fields each new status needs
      (GAME_DESIGN §1 "Statuses v2").
- [x] **Ability catalogue v2** + gear-as-stat-deltas model (GAME_DESIGN Appendix A).
- [x] **Stat-sheet sim refactor** (added `StatSheet` to `Fighter`/`FighterConfig`, the
      derived damage formula + crit/Break Power/Haste/Gem Regen in `Battle`, `Effect.School`,
      generalized `GearDef` to stat deltas with the PoC gear migrated to Appendix A). Neutral
      stat defaults keep the slice working; 1000-fight seed-42 sweep is byte-identical to the
      pre-refactor baseline (re-baselined 2026-06-13). 82 headless tests green.

**DoD:** GAME_DESIGN reflects the agreed model; Phases 4/6 reference it; **no sim behavior
changed yet** (paper + plan only).

## Phase 3 — Combat feel & 3D (2–4 sessions) ⭐ critical gate

Goal: the parry feels *satisfying*. Real 3D characters. The dance is fun.

- [~] Parry juice: hitstop, flash, screen shake, SFX. Iterate until it feels right.
      (Flash/shake/lunge in via `FighterViewBody`; reactive skill VFX in via `FighterVfx`.
      Still TODO: hitstop, camera shake, slow-mo riposte, all SFX.)
- [~] Break bar + BROKEN state with big audiovisual payoff; riposte cinematic moment.
      (Break bar UI + Broken shake/anim in; big payoff + riposte cinematic still TODO.)
- [ ] Combo tracker UI + empowered finisher feedback.
- [x] Set up Git LFS **before** importing art packs (`.gitattributes` tracks fbx/
      textures/audio; verified the VFX pack's PNG/FBX land in LFS).
- [x] Import art packs + map ability timings/anims: Meshtint player + monster packs
      (rigged, sim-reactive animation by semantic state name), **plus** a modular
      character creator and **skill VFX** (Cartoon FX Remaster Free) — reactive,
      data-driven registries built by `ArtSetupBuilder`. (commits through 3064c03)
- [x] Wooden-UI + icon theming pass: data-driven `UiSkin` (Resources/RankE/UiSkin.asset,
      built by `UiSkinBuilder`) skins the programmatic HUD/menus — `UiFactory` is now
      sprite-aware (framed panels/buttons via SpriteSwap, wooden bar troughs + tintable
      fills), the ability bar shows per-ability icons (`Assets/UI/Icons`, file name =
      ability id). Fallback-safe to the flat look when a slot is unbuilt. 82 tests green.
      Owed: drop game-icons.net PNGs in `Assets/UI/Icons` + re-run Build UI Skin; visual
      Play-mode pass on the themed UI; final slot/icon tuning in the Inspector.
- [~] Tune timings by playing (GCD, parry window, locks) — sim stays data-driven.
      (Tooling in: **Combat Tuning** window `Tools ▸ RANK E ▸ Combat Tuning` edits a held
      `TuningProfile` — CombatTuning globals + per-ability cd/cast/lock/effect amounts +
      presentation knobs. Sim edits apply on the **next fight** (each fight clones the
      profile in `BattleDriver.Begin`, keeping fights deterministic); Rematch to feel a
      change. **In-game twin added**: the ESC pause menu is now a real menu (Resume / Tune /
      Restart Fight / Back to Loadout) and **Tune…** opens `TuningPanelScreen` — a
      controller-friendly runtime panel editing the same `TuningProfile.Active` + VFX feel
      knobs, with **Apply & Restart** (`MatchController.RestartFight`) to feel a sim edit
      without alt-tabbing to the editor window. Still owed: the actual play-and-tune pass to
      find good numbers.)
- [x] Arena dressing (`Tools ▸ RANK E ▸ Build Arena`): the backdrop is no longer a bare
      gradient — gradient sky + two arched-colonnade silhouettes (depth) + horizon contact
      band + camera-space vignette, all generated/asset-free and re-runnable. Run it on the
      CombatScene and save. (CFXR URP pink-material check still owed.)
- [x] Combat-tweak authoring: **Ability Authoring** window `Tools ▸ RANK E ▸ Ability
      Authoring` — per-ability Animator-state pickers (`FighterVisualDef.AbilityStates`)
      + VFX slot pickers (`AbilityVfxRegistry`), reaction cues, feel knobs, icon map.
      Replaces hand-editing the registry `.asset` files / re-running keyword heuristics.
      Sim Clone() helpers (`CombatTuning`/`AbilityDef`/`EffectDef`) added; 84 tests green.

Next-session candidates (decide at session start): remaining parry juice (hitstop /
camera shake / slow-mo riposte), BROKEN payoff, combo tracker + finisher UI, an SFX
layer (AbilitySfxRegistry + FighterSfx), the in-fight HUD redesign with the wooden kit,
more TBD. Before the gate sign-off: re-run the EditMode tests (84) and do the CFXR URP
pink-material check.

**DoD (subjective gate):** you play 10 fights and *want more*. Parry feels great on
controller. If not fun, iterate here — do NOT proceed to content.

## Phase 4 — Run loop (2–3 sessions)

Goal: battles chain into a run with build-changing choices.

- [ ] Run state machine: map → node → battle → reward → map.
- [ ] Node map UI (StS-style branching graph, ~12 nodes), node types: Battle, Elite,
      Event, Rest, Boss.
- [ ] Reward screen: pick 1 of 3 — stat-sheet level-ups (per GAME_DESIGN §1 stats),
      skills/gear from the dev-kit-v2 catalogue (Appendix A) with ability types & levels,
      auto-cast slots/rules.
- [ ] Build state: stats, skill bar management (swap when full), ability levels,
      equipped gear, gold. Class selection stub (single class in slice).
- [ ] Run save/load (quit mid-run, resume).

**DoD:** complete a 5+ node mini-run where reward picks visibly change the build;
quit/resume works.

## Phase 5 — Auto-casts / gambits (1–2 sessions)

Goal: the pianist→conductor transition exists.

- [ ] Gambit engine in sim: ordered `condition → action` rules, player override priority.
- [ ] Rule/slot rewards integrated into the reward pool.
- [ ] Gambit management UI (assign/reorder rules between fights).
- [ ] Validate with BattleRunner: a 6-slot build beats Map-1 enemies ~unattended.

**DoD:** a late-run build mostly plays itself while manual parries still matter.

## Phase 6 — Content: Tutorial + Map 1 (2–3 sessions)

Goal: the actual slice content, balanced.

- [ ] Tutorial: scripted guild-exam fight teaching attack → parry → cast → interrupt;
      grants rank D.
- [ ] Map 1 roster per GAME_DESIGN §4: 4 normals + elite + boss, each with its own
      BehaviorProfile and telegraphs, built from the dev-kit-v2 catalogue (Appendix A).
- [ ] Reward pools tuned for Map 1 length (~20–30 min run).
- [ ] Balance pass driven by headless BattleRunner sweeps + manual play.

**DoD:** full run tutorial→Map 1 boss is beatable, loseable, and ~20–30 min.

## Phase 7 — Slice polish & build (1–2 sessions)

Goal: a demo you could hand to a friend.

- [ ] Main menu, settings (rebinding, volume), pause.
- [ ] Rank persistence (E→D→C across runs), profile save.
- [ ] Game over / victory flow with run summary.
- [ ] Pass on UI consistency, SFX coverage, bug sweep.
- [ ] Standalone build (Mac + Windows); a friend plays it without help.

**DoD:** shippable demo build of the vertical slice. 🎉

---

## Beyond the slice (outline only — detail after slice ships)

Maps 2–3 with new enemy mechanics and statuses · rank A → build snapshot · rank S
scripted boss gauntlet for saved builds · events/shop expansion · meta progression
between runs · Steam page/build · mobile port (touch UI, performance) · music.
