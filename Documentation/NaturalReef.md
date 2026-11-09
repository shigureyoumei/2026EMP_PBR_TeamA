# Natural reef environment and fifty-fish school

2026-11-01 interaction update: the school now starts at 50 and decreases when net-caught fish disappear after struggling. Stationary and hidden rest last at most two seconds. See [NetCapture.md](NetCapture.md) for the current behavior, reset button and settings; references below to capture retaining fish describe the earlier environment-only version.

Open `Assets/PBR/Scenes/FishAttention360.unity` and press Play. The current configuration is `Assets/PBR/Generated360/PilotConfig.asset`. A local standalone player is generated at `BuildsNatural/PBRNatural.exe`; executable builds remain excluded from Git.

## Environment

- Scanned 2K albedo, OpenGL normal, and roughness maps for coastal limestone and sand, applied with world-space triplanar mapping. Texture import uses linear data for normal/roughness, sRGB for albedo, mipmaps and anisotropic filtering.
- Smooth irregular reef boulders, weathered cave shells and open arches, curved layered ribbon kelp, branching staghorn and gorgonian forms, and massive coral heads. Cave interiors remain actual geometry.
- 160 seagrass patches, 100 branching coral colonies, 20 additional sea-fan colonies, 150 seabed rubble pieces, and 48 distant outcrops, plus the existing 16 primary cover sites. Placement is deterministic and surrounds all four views.
- Water-colored fill light, directional soft shadows, animated surface caustics, distance fog, softened water-surface light, subtle sunbeam meshes and slowly suspended particles. ACES tonemapping and restrained bloom provide the final grade. Sunbeam meshes approximate scattered light; this is not a volumetric fluid/light simulation.
- Light plant sway is evaluated in the shader. Physical kelp uses the same base mesh as its renderer, with backface ray hits enabled for its two-sided leaves. Shader displacement is intentionally small (0.075 m for cover leaves); collision meshes remain static. Rock, cave, arch, and coral-head render/collision meshes match exactly.

The main cover sites retain their existing locations and navigation hints. The two former flat fan panels now use solid living-coral heads for hiding, with fine branching gorgonians as visible decoration. Small additional colonies, seagrass patches, rubble and distant outcrops are decorative and do not add experimental cover sites or navigation obstacles.

The result moves the environment toward natural underwater materials and shapes. Plant, coral, and fish geometry is generated, not scanned animal/vegetation geometry. Fish silhouettes are inspired by reef species rather than biologically validated reconstructions. The original low-poly imported fish assets remain available for historical prototypes; the natural scene uses its new fish meshes.

## Fish population and controls

`Fish Count` is **50**, counted across all species together. The population persists in the lobby, throughout blocks and after capture; capture retains the fish and applies the existing three-second score cooldown.

Eight silhouettes are provided: clownfish, butterflyfish, blue tang, wrasse, angelfish, fusilier, damselfish and bannerfish. Each has A, B and distractor color variants, giving 24 prefabs. The default school has 20 A targets, 20 B targets and 10 distractors. Species are distributed deterministically; no additional uncounted background fish are spawned. Responsibility colors remain orange/cream, blue/gold, and violet.

Curved bodies have small eyes, caudal/dorsal/anal/pectoral fins, surface markings and scale highlights. Tail bending is evaluated in the shader, with frequency/amplitude responding to movement speed. Smooth world-space turns, banking, the four motion states, collision-aware navigation and verified physical hiding retain the existing behavior.

Change `Fish Count`, `Swim Speed`, `Burst Speed`, `Acceleration` and `Background Detail` in `Assets/PBR/Generated360/PilotConfig.asset`. Stop and restart Play after configuration changes. The Inspector and runtime now share a maximum count of 100, replacing the former 40-fish cap. Only the current natural scene is explicitly set to 50.

`Background Detail` remains 0/1/2. Low keeps the primary cover sites and distant outcrops; medium adds part of the seagrass/coral distribution; high enables all additional details and atmospheric particles/beams. Main cover geometry and fish count are independent of this setting.

Projection room settings and desktop controls remain as described in [ProjectionRoom.md](ProjectionRoom.md) and [Reef360.md](Reef360.md). No hardware sensors or net-launch animation are introduced in this update.

## Sources and reproducibility

Powered by [Poly Haven](https://polyhaven.com/). Downloaded on 2026-11-01:

| Asset | Creator | Usage | License |
| --- | --- | --- | --- |
| [Coral Fort Wall 02](https://polyhaven.com/a/coral_fort_wall_02) | Dimitrios Savva | Limestone surface texture adapted to reef geometry; the source is a coastal wall, not a scanned underwater reef | [CC0](https://polyhaven.com/license) |
| [Coast Sand 01](https://polyhaven.com/a/coast_sand_01) | Rob Tuytel | Coastal sand albedo, normal and roughness | [CC0](https://polyhaven.com/license) |

Downloaded maps are stored in `Assets/PBR/Art/NaturalReef/`. Generated meshes, materials, fish prefabs and the volume profile are stored in `Assets/PBR/GeneratedNatural/`. Their checked-in assets are sufficient to run; users do not need to download source textures again. See `Assets/PBR/Art/NaturalReef/SOURCES.json` for file hashes.

`PBR > Upgrade current reef to natural environment` regenerates the natural environment and fish variants in the open scene. It preserves room parameters and the locations of the main cover sites, but replaces decorative children under the habitat, updates its materials and resets the school to 50. Save a scene copy before intentionally regenerating custom art edits. Ordinary parameter edits do not require regeneration.

The existing `PBR > Build 360 degree reef` also applies the natural upgrade when `naturalReef` is enabled. Its complete scene rebuild retains the existing behavior of replacing the generated scene; use the upgrade menu for the current scene rather than rebuilding a custom room from scratch.

## Verification

The opt-in player argument `-pbr-natural-smoke` validates the 50-fish count, eight silhouettes, role balance, supported materials, mesh/collider consistency, 120 simulated seconds of bounded movement and gradual turns, all four states, actual body-sized hiding, stable object identity across blocks, four-wall seams and a normal rendered timed block. `-pbr-natural-preview` only captures rendered views. Both are inactive in ordinary use.

Results and four-wall screenshots are stored in `Documentation/NaturalReefValidation/`. Timing measurements apply to the tested local desktop preview; projector-room hardware and physical output still require on-site verification. The pre-upgrade scene and configuration are backed up locally under `Backups/20261101_before_natural_reef/`.
