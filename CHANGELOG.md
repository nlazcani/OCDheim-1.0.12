# Changelog

## Version 0.2.4

### Fixes
- Fix: Compatibility with Valheim 1.0.12 on Unity 6
- Fix: The Hoe's "Remove Terrain Modifications" works again. Valheim now transmits only a TerrainOp prefab's
  name hash and resolves the settings through `ObjectDB`, so the piece has to be registered there - and its
  prefab had to be renamed, because `Utils.GetPrefabName` truncates at the first space and the old name went
  over the wire as just "Remove"
- Fix: Grid Mode terraforming applies again. It used to be signalled by stamping a sentinel radius onto
  `TerrainOp.Settings`, which no longer survives the round trip now that only the prefab hash is sent
- Fix: `ALT` toggles Grid Mode once per press. `ZInput` reports a single press across several frames under
  Valheim's new Input System, so Grid Mode turned on and straight back off and every feature behind it - the
  grid overlay, the height readout, the scroll wheel - looked dead
- Fix: The Grid Mode overlays refresh while you play. The placement ghost's own `Update` only ran on pause-menu
  transitions, so the height readout froze and `MOUSE WHEEL` appeared to do nothing until you opened and closed
  the menu; `Player.UpdatePlacementGhost` drives the refresh now
- Fix: A rejected asset bundle no longer takes the whole plugin down - everything except the World Grid overlay keeps working
- Fix: A renamed or removed vanilla prefab now only disables its own tool instead of every tool registered after it

### Improvements
- Minor: Log level is configurable and defaults to `Info`; `Debug` used to be hardwired on and cost frames while building
- Minor: Per-tile paint logging moved from `Info` to `Debug`, matching every other terrain operation
- Minor: Jotunn dependency raised to 2.30.0

## Version 0.2.3

### Improvements
- Minor: Barrels now stack similarly to Stacks & Piles
- Minor: "Smooth Stone 2x2" Build Piece

### Fixes
- Fix: `SHIFT` [`LB`] properly DISABLES Snap Mode
- Fix: Roof Corner Piece Snapping
- Fix: misc NPE issues

## Version 0.2.2
- Fix: Missed DLL file in release v0.2.1 😉

## Version 0.2.1
- Fix: Compatibility with Docker Servers reintroduced
- Fix: Server + EVERY Player must have OCDheim installed → Server + SOME Players must have OCDheim installed
- Fix: Cultivator + Grass → sows Grass 100% of the time (used to be >99.9%) even if the OG Terrain Type ≠ Grass

## Version 0.2.0

### Improvements
- Major: [Grid Mode] World Grid Visualization
- Major: [Grid Mode] Stacks & Piles now stack & pile vertically
- Major: [Grid Mode] The Hoe & The Cultivator AoE is visible even under Ground Level

### Fixes
- Fix: Compatibility with Vanilla Valheim v0.221.12 reintroduced
- Fix: Ground Bound Build Piece Snapping (some Build Pieces used to instantly "explode")
- Fix: Roof Bound Build Piece Snapping (Build Pieces used to snap deeper than intended)
- Fix: Zoom +/- on Mouse Wheel Scroll ↑/↓ is blocked when Raise Ground is active

## Version 0.1.2
- Fix: The Remove Terrain Modifications Tool works as intended on Tar Pits
- Fix: The Remove Terrain Modifications Tool works as intended in The Mistlands

## Version 0.1.1
- Fix: Thunderstore Mod Manager installation

## Version 0.1.0
- Initial Release
