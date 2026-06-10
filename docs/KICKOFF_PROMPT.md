# Prompts for development sessions

## Kickoff prompt (Phase 0) — paste this into a new Claude Code session

```
Read CLAUDE.md, docs/GAME_DESIGN.md and docs/ROADMAP.md, then start Phase 0
(Foundation) from the roadmap.

Context: I have Unity 6 installed (abandoned_try used 6000.1.9f1 — check what's
available with `Unity Hub` or ask me). Create the fresh project at RankE/ — nothing
is carried over from abandoned_try or functional_POC; they are reference only.

Work through the Phase 0 checklist in order. For the Unity MCP bridge
(CoplayDev/unity-mcp), guide me through any installation steps you can't do
yourself. For anything requiring the Unity editor GUI before MCP works, give me
exact numbered steps and wait for my confirmation.

Definition of done is in the roadmap — finish with the headless test command
documented in CLAUDE.md, the ROADMAP checkboxes updated, and a commit.
```

## Template for every later session

```
Read CLAUDE.md and docs/ROADMAP.md. Continue with Phase <N>: <phase name>.
[Optional: feedback from my last playtest / what I want changed.]
Update the roadmap checkboxes and commit when done.
```

Notes:
- One phase can span several sessions — the checkboxes track where you left off.
- After Phase 2, start sessions with playtest feedback ("parry window feels too tight",
  "fireball is boring") — that feedback is the veto mechanism for everything marked
  (PROPOSED) in the design doc.
- Phase 3 ends with a subjective gate: don't let a session talk you into Phase 4
  until 10 fights in a row are genuinely fun.
