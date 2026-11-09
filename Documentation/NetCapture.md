# White net capture and short hovering

Open `Assets/PBR/Scenes/FishAttention360.unity`, press Play and start a block. Left-click inside an active wall viewport to launch a white woven net at that exact pointer direction. The launch direction is saved at button press, so moving the mouse afterward does not redirect an existing net. `F` and `J` launch along A/B's current direction.

The net expands during a 0.32-second flight. At impact, fish whose centers project inside its circular footprint are checked individually against physical cover. Eligible visible fish stop navigating, wiggle/roll and beat their tails rapidly for 1.1 seconds, then fade away and are removed. Capture score and remaining fish count update when that disappearance completes. A net can catch several fish, and a fish already caught by another net cannot be counted twice.

The circle is defined on the deployed net plane. Fish at different depths are included if their projected centers fall inside the visible circle; this is an area interaction for the projected display rather than a simulated physical net volume. Rocks and other primary cover block each fish's visibility ray. The same world-space net is rendered by all four cameras, including at wall seams.

## Population and repeated trials

- The initial school contains 50 fish. Successful catches reduce the count (for example, catching three fish changes 50 to 47).
- Uncaught fish remain between blocks. Starting another block does not replenish captured fish, even if the school is empty.
- Stop the block with Esc, then choose **RESET SCHOOL (50 FISH)** in the start panel to recreate the configured initial school. Exiting and restarting Play also starts a fresh school.
- Stopping a block while fish are still struggling cancels those unfinished catches, restores their appearance/colliders and leaves them in the remaining count. Completed catches remain removed. Pending net effects are cleaned up.

## Settings

Stop Play and select `Assets/PBR/Generated360/PilotConfig.asset` in the Inspector.

| Field | Default | Meaning |
| --- | --- | --- |
| `maxHoverSeconds` | 2 | Hard-capped at two seconds for stationary/hidden rest; the timers use real elapsed time even during slow frames |
| `hidePauseSeconds` | 2 | Hidden rest, clamped to the hovering limit; travel behind cover is separate |
| `netCaptureEnabled` | true | Enable the animated area net instead of the older single-target hit logic |
| `netRespectsRoles` | false | All species can be caught by default; enable to enforce the experiment's A/B, distractor and handoff permissions |
| `netRadius` | 2 m | Radius on the deployed net plane |
| `netDistance` | 18 m | Fallback deployment distance; a center-ray fish/cover hit shortens the flight |
| `netFlightSeconds` | 0.32 | Flight/opening time |
| `netStruggleSeconds` | 1.1 | Time between ensnaring and removing a fish |
| `shotCooldown` | 0.2 | Minimum time between launches by one participant |

Stationary and hidden rest exit into swimming or acceleration, avoiding consecutive rest states. Fish still swim physically into and out of hiding; they do not disappear as part of hiding. The former capture-recovery setting only applies to legacy single-target mode; it does not restore fish in net mode.

The current default is an all-species capture demonstration. Formal responsibility-condition trials should explicitly set `netRespectsRoles = true`; the choice is saved in each session configuration snapshot. The HUD displays whether all species are catchable.

## Implementation and evidence

`NetCaptureSystem` creates a shared runtime mesh for white net ropes, animates launch/opening/tension/fade, reserves caught fish and completes their removal. `FishExperiment` owns score/population updates and logging. Input snapshots preserve the click ray; clicks in desktop letterbox/background areas are ignored. The current mouse simulation uses the existing shared viewpoint, not independent tracked head/hand positions.

Logs include `net_launched`, `net_impact`, `net_ensnared`, `capture`, `fish_removed` and `net_capture_cancelled`. Capture events occur after the struggle finishes. `samples.csv` adds `capture_in_progress`; pending fish are excluded from monitoring visibility/workload estimates. Aborted incomplete catches do not increment the capture score.

Opt-in player verification: `-pbr-net-smoke`. It tests two-second rests, slow-frame timers, all desktop layouts, frozen pointer direction, multi-fish capture, outside/real-cover rejection, struggle/removal, duplicate prevention, role filtering, cancellation, actual input processing, capture-phase logs, empty-school persistence and reset. Controlled screenshot cases reposition fish only inside the validation player and never modify the saved scene.

Evidence: `Documentation/NetCaptureValidation/`. Local player: `BuildsLow/PBRLow.exe`. Earlier assets remain available in the project; the pre-net scene/configuration are backed up locally under `Backups/20261101_before_net_capture/`.
