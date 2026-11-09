# PBR Development Log

Add every new update directly below this introduction, above all previous entries. Use English, include a timestamp in JST (UTC+09:00), and briefly describe completed features and verification. Keep older entries intact.

## 2026-11-01 14:19 JST — Near-Seabed Viewpoint

- Lowered the virtual observation height from Y=3 to Y=0.55, approximately 0.8 meters above the central seabed at Y=-0.25. Nearby sand, plant bases and reef foundations are now visible from a lower perspective.
- Added the Chinese-labeled seabed viewpoint height setting to the Projection Room Inspector. It is independent of the physical participant eye height used for room calibration.
- Synchronized all four wall cameras, the mouse/net aiming origin and the fish-occlusion viewpoint when virtual height changes. Preserved physical eye height and wall-frustum geometry.
- Verification: the player build succeeded; 24 geometry checks passed. The lowered-view player also passed all 360,103 net/hover integration checks, including all desktop layouts, real-cover rejection, area capture, count reduction, cancellation and reset. All four rendered wall views were inspected.
- Updated the three-language README and room instructions. Current scene: `Assets/PBR/Scenes/FishAttention360.unity`. Local executable: `BuildsLow/PBRLow.exe`. Evidence: `Documentation/SeabedViewValidation/`. Previous scene backup: local `Backups/20261101_before_low_view/`.

## 2026-11-01 13:47 JST — Animated Area Net Capture and Two-Second Hovering

- Limited stationary and hidden rest to at most two seconds, including slow-frame timer handling. Rest transitions lead back to swimming/acceleration instead of chaining another rest. Shorter configured limits are supported.
- Added a white woven net that launches toward the saved mouse-click direction, expands, tightens and fades. All four wall cameras render the same world-space effect. Clicks outside an active wall viewport are ignored; F/J use the corresponding participant's current direction.
- Added area capture at net impact with individual physical-cover checks. By default, every fish type is catchable; optional role filtering preserves the earlier ownership/distractor/handoff rules for protocol use.
- Caught fish stop navigating, struggle with body rotation and rapid tail movement, fade out after 1.1 seconds and are removed. Capture score and remaining population update together at removal; repeated nets cannot count a reserved fish twice.
- Changed population behavior from permanent retention to an initial fifty-fish school that decreases with catches. Starting a new block does not replenish it, including at zero. Added an explicit RESET SCHOOL button. Stopping a block restores unfinished catches and clears their net effects.
- Added net-launch, impact, ensnare, removal and cancellation events, plus `capture_in_progress` in samples. Updated the three-language README and added `Documentation/NetCapture.md`.
- Verification: Unity player build succeeded; 360,103 integration checks passed, including 120 simulated seconds of rest timing, all six desktop preview layouts, exact frozen click rays, a three-fish catch reducing 50 to 47, real-cover/outside rejection, duplicate prevention, role filtering, cancellation, actual input/update processing, capture-phase logs, empty-school persistence and manual reset. Launch/struggle/removal renders were inspected; the final player log contained no errors or exceptions.
- Current scene: `Assets/PBR/Scenes/FishAttention360.unity`. Local executable: `BuildsNet/PBRNet.exe`. Evidence: `Documentation/NetCaptureValidation/`. Pre-change scene/configuration backup: local `Backups/20261101_before_net_capture/`.

## 2026-11-01 13:00 JST — Natural Reef Materials and Fifty Persistent Fish

- Replaced the current scene's coarse background surfaces with 2K CC0 scanned coastal limestone and sand maps, world-space normal/roughness shading, irregular boulders, weathered cave/arch interiors, curved ribbon kelp and branching coral geometry. Asset sources, creators and file checksums are recorded.
- Added 160 seagrass patches, 100 branching coral colonies, 20 sea-fan colonies, 150 seabed rubble pieces and 48 distant outcrops around the existing 16 physical cover sites. Background detail remains adjustable independently of fish count.
- Added underwater scattered fill, soft sunlight shadows, animated caustics, gentle plant sway, subtle light shafts, marine particles, ACES tonemapping and restrained bloom. Main cover render/collision meshes match; two-sided leaf queries prevent capture through the back of kelp.
- Set the current scene to exactly 50 persistent fish using eight species-inspired silhouettes and 24 A/B/distractor prefab variants. The school contains 20 A targets, 20 B targets and 10 distractors. Added curved bodies, fins, smaller eyes, surface markings and speed-responsive tail bending.
- Replaced the former 40-fish runtime/Inspector cap with a shared maximum of 100. Preserved gradual turns, depth movement, all four behavior states, physical hiding, capture cooldown and stable population across blocks. Added collision-safe initial placement for denser schools.
- Verification: Unity player build succeeded without shader/compiler errors. The final environment passed 1,460,378 assertions over 120 simulated seconds; 49 fish reached physically occluded hiding positions. A normal 15-second rendered block retained 50 fish and averaged 60.0 FPS at a 1920 × 1080 desktop preview on the current machine. All four final wall renders were inspected.
- Updated the English/Chinese/Japanese README and added `Documentation/NaturalReef.md`. Evidence: `Documentation/NaturalReefValidation/`. Local executable: `BuildsNatural/PBRNatural.exe`. Scene: `Assets/PBR/Scenes/FishAttention360.unity`. Previous scene/configuration: local `Backups/20261101_before_natural_reef/`.
- Limits: animal/vegetation meshes are procedural species-inspired forms; scanned textures do not make them biological reconstructions. Plant collision stays static during small visual sway; extra low-level decorations are visual clutter, not additional experimental cover. Physical projector-room calibration and unknown sensor hardware remain future integration work.

## 2026-10-02 12:52 JST — Trilingual Project README

- Replaced the Chinese-only README with equivalent English, Chinese, and Japanese sections, in that order, with language navigation links.
- Documented the current four-wall scene, controls, fish and experiment settings, projection room, logging, hardware interfaces, and remaining integration limits in all three languages.
- Corrected startup instructions to use the included `FishAttention360.unity` scene and separated historical prototypes from current functionality.
- Verified language order, local documentation links, shared technical references, and diff whitespace. No Unity runtime code or assets changed.

## 2026-10-02 12:28 JST — GitHub Repository Preparation

- Moved project entry instructions to the repository root at `D:\projects\2026EMP_PBR_TeamA`, matching the relocated Unity project.
- Expanded Git exclusions for generated builds, Unity caches, backup copies, downloaded archives, the separate validation project, and runtime logs.
- Prepared Unity assets and their metadata, package dependencies, project settings, development history, documentation, and validation tools for version control. Historical log paths remain unchanged as records of earlier development.

## 2026-10-01 13:32 JST — Configurable Large Space Projection Room

- Added a Projection Room scene object with editable width, depth, height, observer eye height, center offset, and active screen boundaries. A Chinese Inspector and the `PBR > Select projection room settings` menu provide direct access.
- Set the initial room size to 25 × 15 × 7.7 meters, based on published University of Tsukuba LargeSpace research. Eye height defaults to an adjustable 1.6 meters. The official 18 × 9 × 7.4 meter figure describes motion-base travel, not room size.
- Added Scene-view room outlines and an observer marker, four off-axis camera projections, and proportional desktop previews. Adjacent wall views remain continuous for rectangular rooms and offset observers.
- Added physical-position-plus-direction ray intersection with finite projection walls, returning wall index, intersection position, and UV coordinates for future sensor integration.
- Preserved the existing reef, diverse covers, fixed fish population, and smooth behavior. Physical room coordinates map to the existing virtual viewpoint without stretching the environment.
- Verification: Unity build succeeded; 150 geometry checks passed across three room sizes and two observer positions. The built player passed four-wall seam checks and a ten-second timed block with ten fish. All four rendered views were inspected; the player log contained no errors or exceptions.
- Latest executable: `PBR/BuildsRoom/PBRRoom.exe`. Instructions and sources: `PBR/Documentation/ProjectionRoom.md`. Evidence: `PBR/Documentation/ProjectionRoomValidation/`. Previous scene backup: `Backups/20261001_before_projection_room/`.
- Limitations: the room is currently a four-plane approximation of the published dimensions. Actual curved screen geometry, active projection bounds, warping, blending, stereo/floor projection, and sensor hardware require site integration and calibration.

## 2026-09-30 18:59 JST — Diverse Hiding Habitats and Faster Fish

- Replaced the repeated rock layout with 16 physical covers: four rocks, three caves, three open arches, four dense kelp patches, and two large fan corals.
- Added actual cave interiors, passable arch openings, layered leaf meshes, coral veins, distant canyon walls, seagrass beds, colorful coral gardens, and seabed shells.
- Increased normal swim speed from 1.5 to 2.4, burst speed from 4 to 6, and acceleration from 2 to 3. Retained gradual turns, arrival braking, the fixed ten-fish population, and continuous four-wall movement.
- Added low/medium/high background detail (0/1/2, default 2). Decorative complexity can change independently of fish count and primary hiding geometry; restart Play after changing the setting.
- Extended hiding selection to all five cover types using actual mesh occlusion and reachable positions. Added cover type and site names to behavior events and continuous samples.
- Verification: five-minute simulation passed with all ten fish hiding and all five cover types used; arch openings and four-wall continuity passed. Normal runtime sampled the new speed limit of 6 at approximately 60 FPS on the current machine. Final rendering was inspected after fixing leaf surface artifacts.
- Updated scene: `PBR/Assets/PBR/Scenes/FishAttention360.unity`. Latest executable: `PBR/Builds360Diverse/PBR360.exe`. Evidence: `PBR/Documentation/DiverseReefValidation/`. The previous scene was backed up before replacement.

## 2026-09-30 18:23 JST — Continuous 360-Degree 3D Reef

- **Four-wall scene:** Added one continuous surrounding water volume with front, right, back, and left cameras. Fish cross adjacent wall edges naturally, including the last-to-first seam, without teleporting or spawning replacements.
- **Projection preview:** Added a four-view grid, individual wall views, an ordered horizontal strip, and an optional four-display launch mode. Horizontal coverage stays at 90 degrees per wall as the window changes.
- **Spatial environment:** Replaced the frontal illustrated backdrop in the new scene with a 3D seabed, surrounding water, foreground vegetation, middle-distance coral, and distant reef geometry. Added 16 distributed small physical rocks.
- **Depth-aware swimming:** Fish navigate around rocks at different distances and heights and can swim both in front of and behind the reefs. The school remains fixed at 10 fish by default.
- **Smooth movement:** Added gradual heading changes, forward propulsion, turn-related deceleration, gentle banking, and braking into rest. A shared cylindrical navigation graph plans collision-free routes and wraps angular neighbors.
- **Physical hiding:** Hiding positions are selected behind actual rock meshes and checked for body-sized occlusion; fish swim to them and later leave normally.
- **Verification:** Unity build and five-minute motion simulation passed, including stable population, wall seams, forward movement, bounded turn rate, collision clearance, all four states, hiding, and depth travel. Normal rendered block completion also passed. Actual four-wall renders were inspected.
- **Entry points:** `PBR/Assets/PBR/Scenes/FishAttention360.unity`; `PBR/Builds360/PBR360.exe`; settings in `PBR/Assets/PBR/Generated360/PilotConfig.asset`. Instructions: `PBR/Documentation/Reef360.md`; evidence: `PBR/Documentation/RingValidation/`.
- **Remaining integration:** Physical projector/display mapping, measured wall geometry, off-axis calibration, and edge blending require the projection room. Four-display hardware output is implemented but not verified on four physical displays.

## 2026-09-30 17:56 JST — Persistent Fish and Completed Project Baseline

This first entry summarizes work completed before the log was created. The timestamp is the recording time, not the exact completion time of each earlier feature.

### Latest Features

- **Fixed population:** The current reef scene contains 10 persistent fish by default. Starting or ending an experimental block does not replace or remove them. The count is configurable.
- **Four behavior states:** Each fish independently switches between random swimming, acceleration, hiding, and stationary behavior. Swimming animation speed follows movement speed; stationary fish retain gentle tail motion.
- **Physical hiding:** Fish swim continuously behind real reef geometry, stop only after reaching an occluded hiding position, and swim out again. Hiding never disables the fish mesh or collider.
- **Persistent capture scoring:** A valid hit awards a point without removing the fish. Each fish has a three-second scoring cooldown. Rocks still block capture rays.
- **Behavior recording:** Logs now include motion state, speed, hiding arrival, and hidden resting status. Session summaries leave legacy capture-rate and spawn-response-time metrics empty for the repeatable-hit mode.

### Previously Completed Features

- **Unity project:** Created a runnable research prototype with Unity 6000.6.3f1, URP, and Unity Splines. Preserved the original route-based experiment scene alongside the newer reef scene.
- **Underwater art:** Added an original colorful reef backdrop, physical rocks, coral, sea plants, decorative elements, lighting, and animated fish with vivid role-specific colors.
- **Desktop interaction:** Implemented mouse aiming, keyboard capture buttons, player switching, reticles, scoring feedback, and block start/stop controls.
- **Collaborative experiment logic:** Implemented Fixed Role, Shared Role, Responsible Cue, and Adaptive Handoff conditions, including ownership checks, distractor rejection, temporary handoff permissions, and expiry.
- **Research logging:** Added session metadata, timestamped events, participant orientation samples, fish position and visibility samples, input-validity checks, and a descriptive summary script.
- **Integration interfaces:** Added orientation/button input entry points, forward-direction calibration, stale-input handling, and a three-camera panoramic preview option.
- **Builds and documentation:** Produced Windows executables, project setup instructions, research and asset documentation, and validation artifacts.

### Verification

- Unity compilation and Windows player build succeeded; the existing 79 editor assertions passed.
- A 120-second simulated motion test verified stable fish identities and population, all four states, continuous movement, physical hiding and emergence, stationary behavior, capture retention, and cooldown.
- A normal player run verified timed block completion without removing fish. An actual Unity render was inspected, and the updated session summary script was checked.
- Evidence: `PBR/Documentation/PersistentFishValidation/`.

### Current Entry Points

- Unity project: `PBR/`
- Current scene: `PBR/Assets/PBR/Scenes/FishAttentionArcade.unity`
- Fish and experiment settings: `PBR/Assets/PBR/GeneratedArcade/PilotConfig.asset`
- Latest executable: `PBR/BuildsPersistent/PBRReef.exe`

### Remaining Work

- Connect and test the actual head-mounted IMUs and handheld buttons.
- Calibrate the projection-room output, wall geometry, aiming, and multi-wall display alignment.
- Provide a private participant cue channel and calibrate experimental parameters through pilot testing.

The current deliverable is a desktop research prototype; hardware integration and on-site projection validation are not yet complete.
