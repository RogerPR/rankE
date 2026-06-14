# ART & PRESENTATION PIPELINE

How RANK E turns the pure sim into rigged 3D fighters. Read this before touching
anything under `Scripts/Game/View`, the debug picker, or skill VFX.

> **The rule that governs everything here:** the sim core never changes for visuals.
> Presentation is *reactive* — it consumes `BattleDriver.SimEventEmitted` (each `SimEvent`
> once per tick) and read-only `Battle`/`Fighter` state. No `UnityEngine` in `Scripts/Sim`,
> no gameplay timing in MonoBehaviours, no model/visual fields on the sim `FighterConfig`.
> 3D is decoration that follows the simulation; it never feeds back into it.

## The art packs (imported, Git-LFS)

Two Meshtint Asset Store packs under `RankE/Assets/`:

- **Player — `Modular Fantasy Characters Mega Toon Series`** — Humanoid (Mecanim,
  retargetable). 35 shared clips. 8 ready-made sample prefabs in
  `Prefabs/00 Character Samples/` (Knight, Warrior, Mage, Ninja, Archer, King, Princess,
  Necromancer). Modular bases/props/colours exist for a future character creator.
- **Enemies — `Monsters Ultimate Pack 09 Cute Series`** — 14 creatures, **mostly Generic**
  rig (only Treant is humanoid). Each prefab ships **its own** AnimatorController + clips,
  one FBX per clip (`Dragon Ice@Bite Attack.FBX`, `@Cast Spell`, `@Take Damage`, `@Die`…).

Binary art (fbx/textures/audio) is tracked via Git LFS — see `.gitattributes`. Always commit
`.gitattributes` *before* importing new binary art.

## The core idea: drive animation by *semantic state name*

The player is humanoid with shared clips; monsters are generic with per-creature clips. To
wire both uniformly, the view never names a clip. Instead each visual carries a map from a
**semantic action** (and from specific **ability ids**) to an **Animator state name**, and we
`CrossFade` to that state. Missing states fall back gracefully (`HasState` guard).

- `AnimAction` (presentation vocabulary, not content): `Idle, Attack, Cast, Hit, Block,
  Broken, Riposte, Die, Spawn, Victory`.
- Ability ids come from the sim: `slash, bash, fireball, vampiro, parry, kick, riposte,
  interrupt_cast, auto_attack, falling_star, lunge`.

## The pieces

| File | Role |
|---|---|
| `Scripts/Game/View/FighterVisualDef.cs` | One visual's data: prefab, optional controller, `IsHumanoid`, `ModelScale`/`ModelYOffset`, and the `AnimAction → state` + `abilityId → state` maps. `StateFor` / `StateForAbility` resolve them. |
| `Scripts/Game/View/FighterVisualRegistry.cs` | ScriptableObject catalogue (`Players` + `Monsters`), loaded at runtime from `Resources/RankE/FighterVisualRegistry`. |
| `Scripts/Game/View/FighterAnimator.cs` | Subscribes to sim events → `CrossFade` to the mapped state. Tunable one-shot hold windows; `Update` polls Fighter state to return to Idle/Cast/Broken. Sets `applyRootMotion = false` (sim owns position). |
| `Scripts/Game/View/FighterStage.cs` | Spawns the selected player + monster prefab per fight under the (now invisible) `PlayerCapsule`/`EnemyCapsule` **anchors**, assigns the controller, binds `FighterAnimator` + `FighterViewBody`. `playerYaw=90 / enemyYaw=-90` face them at each other; grounds at `groundY + ModelYOffset`. Falls back to visible capsules if the registry isn't built. |
| `Scripts/Game/View/FighterViewBody.cs` | Body-level juice on the anchor: lunge on attack, colour flash on hit/parry/heal, shake while Broken, casting tint, Distance slide-apart. Flashes the real mesh via `MaterialPropertyBlock` (`_BaseColor` URP / `_Color` Standard). |
| `Editor/ArtSetupBuilder.cs` | **Generator** (see below). |
| `Scripts/Game/DebugLoadout.cs` + `Scripts/UI/Screens/LoadoutPickerScreen.cs` | Debug picker Character + Monster rows. `MatchController` names the enemy from the chosen monster. |

The two scene capsules (`PlayerCapsule`/`EnemyCapsule`) survive as **persistent invisible
anchors**: the spawned model is parented under them, and the world-space HUD bars
(`HpBarView`, `BreakBarView`, `FloatingCombatTextSpawner`) keep referencing the stable anchor
transforms. No scene authoring — everything is built in code.

## ArtSetupBuilder — the generator (re-run after adding art)

Menu **Tools ▸ RANK E ▸ Build Art Setup**, or headless
`-executeMethod RankE.Editor.ArtSetupBuilder.Build`. It writes into
`Assets/Resources/RankE/`:

1. **`PlayerCombat.controller`** — a clean humanoid controller built from the character clips
   with named states: `Idle, Attack_Slash, Attack_Cut, Attack_Stab, Cast, Block, Hit, Die,
   Spawn`. One controller retargets across all 8 humanoid samples.
2. **`FighterVisualRegistry.asset`** — 8 players + every monster. **Monster state maps are
   auto-derived** by scanning each creature's own controller (heuristics pick Idle / an
   Attack / Cast / Take-Damage / Die / Spawn), so new creatures need no hand wiring.
   **`ComputeFit`** measures each prefab's renderer bounds and bakes a uniform scale (player
   ≈1.8u, monster ≈2.1u tall) + a ground Y-offset so feet sit at y=0.

**To add a character or monster:** import it, then re-run Build Art Setup. That's the whole
step — selection in the picker and the animation map come for free.

## Conventions & gotchas

- **Never call `Resources.Load` from a field initializer / constructor** — Unity forbids it
  ("call it in Awake or Start"). Lazy-load instead (see `DebugLoadout.Visuals`).
- **Regenerate after importing art**, and commit the regenerated `FighterVisualRegistry.asset`
  + `PlayerCombat.controller` — the runtime loads the *asset*, not the builder.
- **A headless Unity run calls `AssetDatabase.SaveAssets()`**, which flushes Unity's
  URP-normalized material re-serialization — expect a few `.mat` files to show modified after
  running the builder or tests. Benign; commit them.
- **Headless CLI needs the editor closed** (project locks while open). The Unity MCP bridge
  has been unreliable about registering callable tools — fall back to headless + explicit
  editor checklists. Headless EditMode tests are the source of truth; keep them green.
- **Animation timing is presentation, sim is authoritative.** `FighterAnimator` hold windows
  and (future) clip-speed-to-lock scaling are *knobs*; they read `AbilityDef.PreLockTicks/
  PostLockTicks` but never change them. Same will hold for skill VFX/projectiles — spawn them
  reactively from sim events, time them to the locks, change no sim timing.
- **Tunable feel without recompiling:** view components are `AddComponent`'d at runtime (no
  scene inspector). Prefer a ScriptableObject config asset in `Resources` that the user edits
  in the Inspector for juice/feel knobs.
