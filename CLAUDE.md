# RANK E — project guide for Claude Code

Real-time 1v1 dueling roguelite in Unity. Controller-first, PC now, mobile-feasible later.

**Read first, every session:** `docs/GAME_DESIGN.md` (what we're building) and
`docs/ROADMAP.md` (current phase + DoD). Work on the current phase only.

## Repo layout

- `RankE/` — the Unity project (Unity 6.x, URP). The only code that ships.
- `functional_POC/` — Python/pygame PoC. **Reference implementation** for combat
  mechanics and constants. Never modify; never import from.
- `abandoned_try/` — old Unity attempt. Reference only (has its own nested .git). Never modify.
- `docs/` — design doc, roadmap, kickoff prompt.

## Architecture rules (non-negotiable)

1. **Pure sim core.** All combat/run logic lives in `RankE/Assets/Scripts/Sim` with an
   asmdef set to `noEngineReferences: true`. Zero `UnityEngine` usage there — if you need
   a vector or random, use plain C# / `System.Random`. Deterministic, tick-based
   (20 ticks/s), seeded RNG.
2. **View is reactive.** `Scripts/Game` and `Scripts/UI` consume sim events and render
   state. They never compute gameplay outcomes. 3D is presentation only — no physics,
   nothing gameplay-relevant in 3D.
3. **Data-driven.** No gameplay numbers hardcoded in logic. Abilities, statuses, enemies,
   rewards are data definitions (plain C# data in Sim; ScriptableObject wrappers on the
   Unity side where convenient).
4. **Tests are the workflow.** Every sim mechanic gets edit-mode tests. Run them headless
   before declaring anything done. Balance questions → `BattleRunner` headless sweeps.

## Working conventions

- **Session start:** read ROADMAP, state which phase/checkboxes you're tackling.
  **Session end:** update ROADMAP checkboxes, commit with a descriptive message.
- **Editor work:** prefer the Unity MCP bridge (create objects, read console, run tests).
  If MCP is unavailable, emit a short numbered checklist for the user to do manually, then
  wait for confirmation before depending on it.
- **Design gaps:** GAME_DESIGN marks inventions as **(PROPOSED)**. Follow them. If you
  must invent something new, implement the simplest version and mark it PROPOSED in the
  design doc in the same commit.
- **The design doc is direction, not contract.** If a design decision seems wrong or
  awkward during implementation, ask the user instead of complying silently. At equal cost,
  choose the more extensible implementation — equipment, ability types, statuses and
  node types are open categories that will grow (e.g. runes as a future equipment kind),
  so keep them data-driven, never hardcoded enums of current content.
- **Phase discipline:** don't start the next phase before the current DoD is met. Phase 3
  has a subjective fun-gate that only the user can pass.
- Commit at meaningful checkpoints; keep the Unity project always compiling.
- The user manages Unity basics but is not a Unity expert — when asking them to do editor
  steps, be explicit (exact menu paths, exact field names).

## Commands

- Run sim tests headless (Unity editor must be **closed** — the project is locked while open):

  ```sh
  /Applications/Unity/Hub/Editor/6000.1.10f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -projectPath "$(pwd)/RankE" \
    -runTests -testPlatform EditMode \
    -testResults /tmp/ranke-test-results.xml -logFile /tmp/ranke-test-run.log
  ```

  Exit code 0 = all passed. Details in the results XML (`test-run` attributes
  `total`/`passed`/`failed`). Do not pass `-quit` together with `-runTests`.

- Run balance sweep: `TODO — BattleRunner invocation (Phase 1)`

- Unity MCP bridge: registered with Claude Code as `UnityMCP` (HTTP,
  `http://127.0.0.1:8080/mcp`). The Unity editor must be open and focused once for the
  bridge to connect. If `claude mcp list` shows "Failed to connect", start the server
  manually:

  ```sh
  uvx --from "mcpforunityserver==9.7.1" mcp-for-unity --transport http --http-url http://127.0.0.1:8080
  ```
