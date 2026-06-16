# Prompts for development sessions

## Kickoff prompt (Phase 0) — paste this into a new Claude Code session

```
Read CLAUDE.md, docs/GAME_DESIGN.md and docs/ROADMAP.md, then start Phase 0
(Foundation) from the roadmap.

Context: I have Unity 6 installed check what's
available with `Unity Hub` or ask me. Create the fresh project at RankE/ — nothing
is carried over from  functional_POC; they are reference only.

Work through the Phase 0 checklist in order. For the Unity MCP bridge
(CoplayDev/unity-mcp), guide me through any installation steps you can't do
yourself. For anything requiring the Unity editor GUI before MCP works, give me
exact numbered steps and wait for my confirmation.

Definition of done is in the roadmap — finish with the headless test command
documented in CLAUDE.md, the ROADMAP checkboxes updated, and a commit.
```

## Next session (Phase 3 — combat-feel tooling, then the fun-gate) — paste this

```
We're continuing RANK E (Unity 1v1 dueling roguelite, repo root, project RankE/).
Read CLAUDE.md, docs/GAME_DESIGN.md, docs/ROADMAP.md, and — essential — docs/ART_PIPELINE.md
(how the pure sim drives rigged 3D + skill VFX reactively: BattleDriver.SimEventEmitted,
the ScriptableObject registries under Resources/RankE built by ArtSetupBuilder, the
FighterStage/FighterAnimator/FighterViewBody/FighterVfx view components, and the gotchas).

WHERE WE ARE: Phase 3 (combat feel & 3D — the subjective fun-gate). Committed through 3064c03:
 - Rigged characters + monsters, animation reactive to sim events (semantic state names).
 - A modular CHARACTER CREATOR (Scripts/Game/View: CharacterPartCatalogue/CharacterAppearance/
   CharacterAssembler/ModelFit; UI/Screens/CharacterCreatorScreen) building a custom humanoid
   at runtime from Meshtint modular bases + accessories on named attach bones.
 - SKILL VFX (Cartoon FX Remaster Free, imported via Git LFS under Assets/JMO Assets/):
   AbilityVfxRegistry (Resources/RankE/AbilityVfxRegistry.asset) maps ability id → cast aura/
   muzzle/projectile/impact + reaction-cue prefabs, with Inspector-tunable feel knobs;
   FighterVfx spawns them reactively from SimEventEmitted (muzzle+projectile on AbilityUsed,
   cast aura across CastStarted→Completed/Interrupted, Hit/Heal/Parry/Break/Riposte/Death cues);
   VfxProjectile is a pure-lerp Travel/Fall mover (no physics). ArtSetupBuilder.BuildVfxRegistry
   (Tools ▸ RANK E ▸ Build VFX Registry) keyword-maps the pack — tune the result in the Inspector.
   Compiles clean. NOT yet runtime/play-tested; the combined creator+VFX playtest is still pending.

THIS SESSION — pick the focus at the start (I'll decide). Candidate next steps, all toward
making the long feel-tuning + fun-gate loop tractable:
 - An IN-PLAY PARAM-TWEAK SCREEN: adjust combat configs live (timings/knobs/loadout/VFX feel)
   to A/B how fun the combat is under distinct configurations, without recompiling. Sim values
   are data (PocContent/CombatTuning); presentation knobs live on the registry assets.
 - An ABILITY → EFFECT/ANIM AUTHORING TOOL: an EditorWindow listing each ability with pickers
   for its Animator state (FighterVisualRegistry humanoid template) + VFX slots (AbilityVfxRegistry),
   replacing hand-editing two .asset files / re-running heuristics.
 - DOWNLOAD + IMPLEMENT UI ASSETS (a UI kit; same LFS-first import pattern as the art/VFX packs).
 - Remaining Phase-3 juice: parry HITSTOP / camera SHAKE / slow-mo RIPOSTE, the BROKEN payoff,
   combo tracker UI + finisher feedback, and an SFX layer (no audio yet — parry SFX is named in
   the gate; mirror the VFX registry pattern with an AbilitySfxRegistry + FighterSfx).
 - …or something else I raise. Propose the architecture before building, mirroring the established
   reactive + data-driven + Resources-registry patterns. Presentation only — change NO sim timing,
   no physics, no gameplay in 3D.

STANDING ITEMS before the fun-gate sign-off:
 - Run the 82 EditMode tests HEADLESS (editor closed) — source of truth; deferred last session
   because no sim code changed, but re-confirm before declaring the gate.
 - CFXR URP pink-material check: if effects render magenta in Play mode, run the pack's one-click
   pipeline fixer (look under a JMO Assets / CFXR menu or the CFXR material inspector button).
 - The COMBINED PLAYTEST is still owed: verify creator preview/attach-points/grounding, then a full
   fight reading every ability's VFX + parry/break/riposte feel. Do NOT enter Phase 4 until I pass
   the gate (10 fights, want more, parry feels great on controller).

WORKFLOW: sim core never changes for visuals; view is reactive. Headless test/sweep CLI needs the
editor CLOSED. The Unity MCP bridge's refresh/console worked last session but its run_tests channel
did NOT register ("WebSocket not initialised") — fall back to headless + explicit editor checklists
if tools stop registering. I manage Unity basics but am not an expert — give exact menu paths /
field names for any editor steps. Update ROADMAP checkboxes and commit at meaningful checkpoints.
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
