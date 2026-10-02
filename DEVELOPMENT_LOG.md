# PBR Development Log

Add every new update directly below this introduction, above all previous entries. Use English, include a timestamp in JST (UTC+09:00), and briefly describe completed features and verification. Keep older entries intact.

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
