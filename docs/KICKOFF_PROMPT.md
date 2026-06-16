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

## Next session (Phase 3 — combat-tweak tooling toward the fun-gate) — paste this

```
We're continuing RANK E (Unity 1v1 dueling roguelite, repo root, project RankE/).
Read CLAUDE.md, docs/GAME_DESIGN.md, docs/ROADMAP.md, and — essential — docs/ART_PIPELINE.md
(how the pure sim drives rigged 3D + skill VFX reactively: BattleDriver.SimEventEmitted, the
Resources/RankE ScriptableObject registries built by editor builders, the
FighterStage/FighterAnimator/FighterViewBody/FighterVfx view components, and the gotchas).

WHERE WE ARE: Phase 3 (combat feel & 3D — the subjective fun-gate). Latest on master adds a
WOODEN-UI + ICON THEMING PASS on top of the rigged characters / modular character creator / skill
VFX (AbilityVfxRegistry + FighterVfx, all reactive to SimEventEmitted):
 - Data-driven UiSkin (Scripts/UI/UiSkin.cs → Resources/RankE/UiSkin.asset, built by
   Editor/UiSkinBuilder.cs, menu Tools ▸ RANK E ▸ Build UI Skin). UiFactory is now sprite-aware
   (framed panels + SpriteSwap wooden buttons, wooden bar troughs + a neutral tintable fill) and
   degrades to the old flat look when a slot is unbuilt. The ability bar shows per-ability icons
   from Assets/UI/Icons (file name = ability id; 7 game-icons.net placeholders in, CC BY 3.0).
   Re-running the builder is ADDITIVE — hand tuning in the UiSkin Inspector survives. 82 tests green.
 - KNOWN GAP (deferred on purpose): the in-fight HUD widgets (HpBarView/CastBarView/BreakBarView/
   StatusColumnView/EnemyIntentView/ComboRiposteView/FloatingCombatText) only inherited the sprite
   swaps for free — they still need a real layout/readability REDESIGN with the wooden kit.

THIS SESSION — build the COMBAT-TWEAK TOOLING that makes the long feel-tuning + fun-gate loop
tractable. Two tools; propose the architecture for each before building, mirror the established
reactive + data-driven + Resources-registry patterns, and change NO sim timing/physics/gameplay:
 1. IN-PLAY COMBAT-PARAMS SCREEN: adjust combat configs live to A/B feel without recompiling — sim
    tuning is data (CombatTuning/PocContent: GCD, parry window, animation locks, cooldowns, damage)
    and presentation knobs live on the registry assets (AbilityVfxRegistry feel knobs, UiSkin).
    Decide how edits apply (mid-run vs next fight) while keeping the sim deterministic/seeded.
 2. ABILITY → ANIM/VFX AUTHORING TOOL: an EditorWindow listing each ability with pickers for its
    Animator state (FighterVisualRegistry humanoid template) + VFX slots (AbilityVfxRegistry),
    replacing hand-editing .asset files / re-running keyword heuristics (could also cover the
    UiSkin icon map).

DEFERRED / ALSO OPEN (not this session unless I say so):
 - In-fight HUD REDESIGN with the wooden kit (the KNOWN GAP above) — likely its own session.
 - Remaining Phase-3 JUICE toward the gate: parry HITSTOP / camera SHAKE / slow-mo RIPOSTE, the
   BROKEN payoff, combo tracker + finisher feedback, and an SFX layer (AbilitySfxRegistry +
   FighterSfx mirroring the VFX registry; parry SFX is named in the gate).

STANDING ITEMS before the fun-gate sign-off:
 - CFXR URP pink-material check: if effects render magenta in Play mode, run the pack's one-click
   pipeline fixer (JMO Assets / CFXR menu or the material inspector button).
 - 82 EditMode tests must stay green (MCP run_tests worked this session; headless CLI is the source
   of truth and needs the editor CLOSED).
 - The COMBINED PLAYTEST gate is still owed: a full fight reading every ability's VFX +
   parry/break/riposte feel. Do NOT enter Phase 4 until I pass it (10 fights, want more, parry
   feels great on controller).

WORKFLOW: sim core never changes for visuals; view is reactive. Headless test/sweep CLI needs the
editor CLOSED. The Unity MCP bridge worked this session (refresh / console / run_tests all
registered) but has been flaky before — fall back to headless + explicit editor checklists if tools
stop registering. I manage Unity basics but am not an expert — give exact menu paths / field names
for any editor steps. Update ROADMAP checkboxes and commit at meaningful checkpoints. UI kit = the
imported Wooden UI pack; icons from https://game-icons.net/ (CC BY 3.0; credit lorc/delapouite/skoll).
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
