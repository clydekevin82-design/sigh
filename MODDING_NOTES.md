# LTD Modding Notes

This workspace is an extracted Switch title layout with `ExeFS` and `RomFS`.
Most tunable gameplay data lives in BYML v7 files, often compressed as
`*.byml.zs`; many smaller parameter files are plain `*.bgyml`.

## Built Prototype Mods

Run:

```bash
.modenv/bin/python tools/ltd_mod_builder.py
```

Generated overlays:

- `mods/HighDramaSocialTuning/romfs`
  - Raises concurrent fight caps in
    `Parameter/TroubleSystem/TroubleSystem.actor__TroubleSystemParam.bgyml`.
  - Raises confession/rival/revenge drama rates.
  - Makes bad relationship background judgments harsher and shifts fight-count
    distribution in `Parameter/Relation/System.game__RelationRoot.bgyml`.
- `mods/GroupActivityPlus/romfs`
  - Raises `GroupActionRate` from `30` to `85`, nudging the island toward more
    background group interactions without changing the drama knobs.

These are data-only mods. They should be usable as LayeredFS-style RomFS
overlays in emulators or CFW mod folders after placing the generated `romfs`
contents under the title's mod directory.

## Feature Feasibility Map

### Legacy Feature Restorations

- Concert Hall: assets and data hooks exist around `SongMelody`, `GuitarMusic`,
  `AIBgmCtrlParam/Sing`, `DanceBalletGroup`, and stage-like interiors such as
  `Stage00` and `LightStick*`. A full restoration likely needs UI layout work
  plus executable hooks for lyric editing and performance selection.
- Tomodachi Quest: `MinigameParam.Product.100.rstbl.byml.zs` is present, but a
  brand-new RPG mode needs code and UI flow hooks. A practical first step is a
  reskin/rebalance of an existing minigame, not a new minigame slot.
- Classic Buildings: likely feasible as asset swaps. The likely tables are
  `MapObject*`, `MapFileParam`, `PlaceCondition`, `FloorModel`, and layout/icon
  assets. This needs model/texture replacements for Observation Deck/Ranking
  Board equivalents.

### Gameplay And Social Expansion

- Expanded Relationship Tiers: labels/UI are visible in layouts and relation
  data, but adding new semantic relationship types probably needs save/schema
  and executable logic work. Data-only label swaps may be possible.
- Higher Conflict Frequency: first prototype implemented as
  `HighDramaSocialTuning`.
- Group Activities: first prototype implemented as `GroupActivityPlus`. Deeper
  "introduce to group" behavior likely needs UI/action-flow hooks.

### Visual And Technical Overhauls

- 60 FPS / Ultra-wide: this is an `ExeFS/main` patch, not a RomFS data mod.
  Strings show FPS/frame symbols, but the actual patch needs NSO disassembly
  and emulator testing.
- Miitopia Makeup Port: Mii and makeup strings exist in the executable and
  `MiiParts`/`MiiEyeAccessoryParam` data, but a full port needs new UI, save
  fields, textures, and face render logic.
- Texture Fixes: likely feasible as texture replacements under `Tex`, `Model`,
  and floor/grass material tables. Needs a specific broken emulator/rendering
  case to target.

### Custom Asset Injections

- Medical Gear Items: feasible if implemented as clothing/accessory or held-item
  asset swaps first. Brand-new categories likely require shop/UI/category logic.
- Furniture Layout Mod: free placement is probably executable/UI logic. The
  current room system is table-driven through `Room*`, `RoomLdkDecoParam`, and
  `RoomDecoActorParam`, but arbitrary placement needs placement controls and
  persistence.
