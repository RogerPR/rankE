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

- [ ] Create fresh Unity project `RankE/` at repo root (Unity 6.x, URP, 3D template).
      Nothing carried over from `abandoned_try/` (reference only).
- [ ] Folder layout: `Assets/Scripts/Sim` (asmdef, **noEngineReferences=true**),
      `Assets/Scripts/Game` (view/presentation), `Assets/Scripts/UI`,
      `Assets/Tests/Sim` (edit-mode test asmdef), `Assets/Data`, `Assets/Art`.
- [ ] Unity Test Framework set up; one trivial sim test passes via Unity CLI batchmode
      (`-runTests`) — document the exact command in CLAUDE.md.
- [ ] Install + verify Unity MCP bridge (CoplayDev/unity-mcp): Claude can create a
      GameObject and read the console.
- [ ] Git hygiene: Unity .gitignore, decide on Git LFS for art, first commit.
- [ ] Combat scene stub: orthographic camera, two placeholder capsules, ground plane.

**DoD:** `Sim` asmdef compiles with no UnityEngine refs; headless test run passes from
the command line; MCP round-trip works; scene shows two capsules facing each other.

## Phase 1 — Combat sim core (2–3 sessions)

Goal: the Python PoC fully ported to the pure C# sim, verified by tests, no visuals.

- [ ] Tick engine: 20 ticks/s, seeded RNG, deterministic.
- [ ] Data-driven ability definitions (plain C# data; ScriptableObject wrappers later).
- [ ] Port all PoC mechanics: GCD + quick GCD, cooldowns, casting + interrupts
      (interruptible flag), auto-attack, parry window → riposte counter, statuses
      (stun/poison/regen), spell gems, stance/weapon/armor build modifiers (as build
      state set by run rewards, not pre-fight picks — GAME_DESIGN §1/§3).
- [ ] New since PoC: break bar, combo tags (O/L/F), animation locks, delayed abilities,
      `Distance` status — per GAME_DESIGN §1.
- [ ] Port PoC enemy AI as `BehaviorProfile` v1.
- [ ] `BattleRunner`: headless AI-vs-AI fights with stats output (winner, duration,
      ability usage) — the balance tool for every later phase.
- [ ] Unit tests per mechanic + integration test running full seeded fights.

**DoD:** 1000 headless AI-vs-AI fights run in seconds with summary stats; all tests
green; zero UnityEngine usage in `Sim`.

## Phase 2 — Presentation & input v1 (2–3 sessions)

Goal: a human can play a full fight vs AI in Unity with the sketch UI. Placeholder art.

- [ ] Sim↔View binding: sim emits events (AbilityUsed, Parried, Damaged, Broken…);
      view layer consumes, never reaches into sim internals.
- [ ] 2D UI per sketch: HP bars, cast bars, ability bar with radial cooldowns, status
      icon columns, enemy intent display, floating combat text.
- [ ] Input System: gamepad + keyboard action maps per GAME_DESIGN §2.
- [ ] Debug loadout picker (set stance/weapon/armor/abilities for testing — real
      acquisition comes via run rewards in Phase 4), countdown, victory/defeat
      → restart loop.
- [ ] Capsules animate crudely (lunge on attack, flash on hit) — enough to read the fight.

**DoD:** you can play and win/lose a complete duel with controller or keyboard; every
sim mechanic is visible in the UI; no sim logic in MonoBehaviours.

## Phase 3 — Combat feel & 3D (2–4 sessions) ⭐ critical gate

Goal: the parry feels *satisfying*. Real 3D characters. The dance is fun.

- [ ] Parry juice: hitstop, flash, screen shake, SFX. Iterate until it feels right.
- [ ] Break bar + BROKEN state with big audiovisual payoff; riposte cinematic moment.
- [ ] Combo tracker UI + empowered finisher feedback.
- [ ] Buy/import 2 character packs with animations (player knight + bandit); map
      ability timings (pre/post locks) to animation clips; hit/stagger/cast/parry anims.
- [ ] Tune timings by playing (GCD, parry window, locks) — sim stays data-driven.

**DoD (subjective gate):** you play 10 fights and *want more*. Parry feels great on
controller. If not fun, iterate here — do NOT proceed to content.

## Phase 4 — Run loop (2–3 sessions)

Goal: battles chain into a run with build-changing choices.

- [ ] Run state machine: map → node → battle → reward → map.
- [ ] Node map UI (StS-style branching graph, ~12 nodes), node types: Battle, Elite,
      Event, Rest, Boss.
- [ ] Reward screen: pick 1 of 3 (% level-ups, skills/gear with ability types &
      levels, auto-cast slots/rules).
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
      BehaviorProfile and telegraphs.
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
