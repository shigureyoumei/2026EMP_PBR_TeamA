using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace PBR
{
    public sealed class FishExperiment : MonoBehaviour
    {
        public ExperimentConfig config;
        public ParticipantInput input;
        public SplineContainer[] routes;
        public ExperimentCondition condition;
        public string pairId = "pilot01";
        public int block = 1;
        public bool Running { get; private set; }
        public IReadOnlyList<FishTarget> Fish => fish;
        public string LastLogFolder { get; private set; } = "";
        public string Status { get; private set; } = "Ready for a pilot block";
        public double Elapsed => Running ? Time.realtimeSinceStartupAsDouble - start : stoppedAt;
        public int Captured { get; private set; }
        public int Missed { get; private set; }
        public int Wrong { get; private set; }
        public readonly string[] Cue = { "", "" };
        public readonly double[] CueUntil = { 0, 0 };
        readonly List<FishTarget> fish = new List<FishTarget>();
        readonly int[] workload = new int[2];
        readonly double[] lastShot = { -999, -999 };
        ExperimentLog log;
        System.Random random;
        double start, stoppedAt, nextSpawn, nextSample, nextFlush, previous;
        int nextId;
        bool schoolInitialized;
        NetCaptureSystem nets;
        public NetCaptureSystem Nets=>nets;

        void Awake() { Application.runInBackground = true; Application.targetFrameRate = 60; }
        void Start() { EnsureSchool(); }
        void EnsureSchool()
        {
            if (!config || !config.persistentSchool || schoolInitialized) return;
            random = new System.Random(config.seed); nextId = 0;
            for (int i = 0; i < Mathf.Clamp(config.fishCount, 1, ExperimentConfig.MaximumFishCount); i++) Spawn(0);
            schoolInitialized=true;
            Physics.SyncTransforms();
        }
        public void ResetSchool()
        {
            if(Running || !config || !config.persistentSchool)return;
            if(nets)nets.CancelAll();ClearFish();schoolInitialized=false;EnsureSchool();
            Status="School reset: "+fish.Count+" fish";
        }

        public void Begin()
        {
            if (Running || !config || (!config.ringWorld && (routes == null || routes.Length == 0))) return;
            if (config.persistentSchool) EnsureSchool(); else ClearFish();
            input.ClearShots();
            try { log = new ExperimentLog(config, condition, pairId, block); }
            catch (Exception e) { Status = "Cannot start: log file unavailable"; Debug.LogError(e); return; }
            LastLogFolder = log.Folder;
            random = new System.Random(config.seed);
            nextId = 0; Captured = Missed = Wrong = 0;
            start = Time.realtimeSinceStartupAsDouble;
            previous = nextSpawn = nextSample = nextFlush = stoppedAt = 0;
            lastShot[0] = lastShot[1] = -999;
            Cue[0] = Cue[1] = ""; CueUntil[0] = CueUntil[1] = 0;
            Running = true; Status = "Block running";
            log.Event(0, "block_start", detail: condition.ToString());
            if (config.persistentSchool) foreach (var f in fish)
            {
                f.Born = 0; f.CaptureReadyAt = 0; f.GapSeconds = 0;
                f.HandoffPlayer = -1; f.HandoffUsed = false; f.HandoffUntil = f.NextCue = 0;
                f.HasVisibility = false;
                log.Event(0, "fish_present", f, detail: "state=" + f.Roaming.State);
            }
        }
        public void End(string reason = "operator_abort")
        {
            if (!Running) return;
            stoppedAt = Elapsed;
            if(nets)nets.CancelAll();
            foreach (var f in fish) log.Event(stoppedAt, config.persistentSchool ? "fish_present_at_end" : "target_censored", f, detail: reason);
            log.Event(stoppedAt, "block_end", detail: reason);
            log.Dispose(); log = null; Running = false;
            Status = reason == "completed" ? "Block complete" : "Block stopped: " + reason;
            if (!config.persistentSchool) ClearFish();
        }
        void ClearFish() { foreach (var f in fish) if (f){f.gameObject.SetActive(false);Destroy(f.gameObject);} fish.Clear(); }
        void LateUpdate()
        {
            if (config.persistentSchool)
                foreach (var f in fish) if(!f.IsBeingCaptured)f.Roaming.Tick(Time.unscaledDeltaTime);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) End();
            if (!Running) { input.ClearShots(); return; }
            double now = Elapsed;
            if (now >= config.durationSeconds) { End("completed"); return; }
            if (log.Failed) { End("recording_failure"); return; }
            float dt = (float)(now - previous); previous = now;
            if (dt > .25f) log.Event(now, "frame_gap", detail: dt.ToString("R", CultureInfo.InvariantCulture));
            if (!config.persistentSchool && now >= nextSpawn)
            {
                Spawn(now);
                // Do not create an uncontrolled burst after a stalled frame; record lateness explicitly.
                if (now - nextSpawn > .1) log.Event(now, "spawn_late", detail: (now - nextSpawn).ToString("R", CultureInfo.InvariantCulture));
                nextSpawn += config.spawnInterval;
                if (nextSpawn < now)
                {
                    log.Event(now, "schedule_gap", detail: "stalled frame; skipped overdue spawn slots");
                    nextSpawn = now + config.spawnInterval;
                }
            }
            for (int i = fish.Count - 1; i >= 0; i--)
            {
                var f = fish[i];
                if (!config.persistentSchool && f.Move(now))
                {
                    if (f.Owner >= 0) { Missed++; log.Event(now, "target_missed", f); }
                    else log.Event(now, "distractor_exit", f);
                    Remove(f); continue;
                }
                float age = (float)(now - f.Born);
                if (!config.persistentSchool && !f.PauseAnnounced && age >= f.TravelSeconds * .5f) { f.PauseAnnounced = true; log.Event(now, "route_pause", f); }
                if (!config.persistentSchool && !f.ResumeAnnounced && age >= f.TravelSeconds * .5f + f.PauseSeconds) { f.ResumeAnnounced = true; log.Event(now, "route_resume", f); }
                Vector3 direction = f.transform.position - input.Origin;
                f.Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            }
            Physics.SyncTransforms();
            workload[0] = workload[1] = 0;
            foreach (var f in fish) if (f.Owner >= 0 && LineVisible(f) && Covered(f.Owner, f)) workload[f.Owner]++;
            foreach (var f in fish)
            {
                bool visible = LineVisible(f);
                if (!f.HasVisibility || visible != f.WasVisible)
                {
                    log.Event(now, visible ? (f.HasVisibility ? "fish_reappeared" : "fish_appeared") : "occlusion_enter", f);
                    f.HasVisibility = true; f.WasVisible = visible;
                }
                Assist(f, now, dt, visible);
            }
            for (int p = 0; p < 2; p++) if (input.ConsumeShot(p,out var shotRay)) Shoot(p, now,shotRay);
            if (now >= nextSample)
            {
                if (now - nextSample > 2.0 / config.sampleHz) log.Event(now, "sample_late");
                log.Pose(now, input);
                foreach (var f in fish) log.Sample(now, f, input, LineVisible(f), Covered(0, f), Covered(1, f), workload[0], workload[1]);
                // Still record orientation when there are no live fish.
                if (fish.Count == 0) log.Event(now, "orientation_empty_scene", detail: string.Format(CultureInfo.InvariantCulture, "{0:R}|{1:R}|{2}|{3}", input.Yaw[0], input.Yaw[1], input.Valid(0), input.Valid(1)));
                nextSample = now + 1.0 / config.sampleHz;
            }
            if (now >= nextFlush) { log.Flush(); nextFlush = now + 1; }
        }
        public bool Covered(int p, FishTarget f) => input.Valid(p) && ExperimentRules.Covered(input.Yaw[p], f.Yaw, config.monitoringHalfAngle);
        public bool LineVisible(FishTarget f)
        {
            if(f.IsBeingCaptured)return false;
            Vector3 dir = f.transform.position - input.Origin;
            // Only opaque environment meshes count as occluders. Fish do not occlude one another in coverage estimates.
            return !Physics.Raycast(input.Origin, dir.normalized, dir.magnitude, 1 << 8, QueryTriggerInteraction.Ignore);
        }
        void Assist(FishTarget f, double now, float dt, bool visible)
        {
            if (f.Owner < 0) return;
            int owner = f.Owner, teammate = 1 - owner;
            if (f.HandoffPlayer >= 0 && (now >= f.HandoffUntil || !input.Valid(f.HandoffPlayer)))
            { log.Event(now, "handoff_end", f, f.HandoffPlayer); f.HandoffPlayer = -1; }
            // Invalid sensor data is missing data, never evidence of looking away.
            if (!input.Valid(owner)) { f.GapSeconds = 0; return; }
            bool gap = visible && !Covered(owner, f);
            f.GapSeconds = gap ? f.GapSeconds + dt : 0;
            if (condition == ExperimentCondition.FixedRole || condition == ExperimentCondition.SharedRole) return;
            if (f.GapSeconds >= config.cueDelay && now >= f.NextCue)
            {
                Cue[owner] = "TARGET " + f.Id + "  " + Mathf.RoundToInt(f.Yaw) + " deg";
                CueUntil[owner] = now + 2; f.NextCue = now + 3;
                log.Event(now, "responsible_cue", f, owner);
            }
            if (condition == ExperimentCondition.AdaptiveHandoff && ExperimentRules.Transferable(f.Kind) &&
                !f.HandoffUsed && f.HandoffPlayer < 0 && visible && Covered(teammate, f) &&
                (f.GapSeconds >= config.handoffDelay || (workload[owner] >= config.workloadThreshold && workload[teammate] < workload[owner])))
            {
                f.HandoffPlayer = teammate; f.HandoffUntil = now + config.handoffSeconds;
                f.HandoffUsed = true;
                Cue[teammate] = "HANDOFF " + f.Id + "  " + Mathf.RoundToInt(f.Yaw) + " deg";
                CueUntil[teammate] = f.HandoffUntil;
                log.Event(now, "handoff_start", f, teammate);
            }
        }
        void Spawn(double now)
        {
            int id = nextId++;
            FishKind kind = (FishKind)(id % 5);
            int routeIndex = config.ringWorld ? -1 : random.Next(routes.Length);
            var go = new GameObject("Target_" + id + "_" + kind);
            var f = go.AddComponent<FishTarget>(); f.Id = id; f.Kind = kind; f.Born = now;
            f.Route = config.ringWorld ? null : routes[routeIndex]; f.TravelSeconds = config.travelSeconds; f.PauseSeconds = config.hidePauseSeconds;
            var collider = go.AddComponent<SphereCollider>(); collider.radius = .65f;
            go.layer = 9;
            int visualIndex = kind == FishKind.Distractor ? 2 : f.Owner;
            // Each species has A, B and distractor variants, preserving responsibility cues.
            if(config.naturalReef && config.fishPrefabs.Length>=6)
                visualIndex+=3*((id/5)%(config.fishPrefabs.Length/3));
            GameObject visual = Instantiate(config.fishPrefabs[visualIndex % config.fishPrefabs.Length], go.transform);
            visual.name = "Animated fish";
            var anim = visual.GetComponentInChildren<Animation>();
            if (anim && anim.clip) { anim.wrapMode = WrapMode.Loop; anim.Play(); }
            if (ExperimentRules.Transferable(kind))
            {
                // A small dorsal marker distinguishes transferable targets; it is part of the occluded 3D scene.
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Transferable marker"; marker.transform.SetParent(go.transform, false);
                marker.transform.localPosition = new Vector3(0, .6f, 0); marker.transform.localScale = Vector3.one * .18f;
                Destroy(marker.GetComponent<Collider>());
                marker.GetComponent<Renderer>().sharedMaterial = config.transferableMarkerMaterial;
            }
            if (config.persistentSchool)
            {
                f.Roaming = config.ringWorld ? go.AddComponent<RingFishMotion>() : go.AddComponent<FishRoaming>();
                f.Roaming.Initialize(config, f.Route ? f.Route.transform.parent : null, input.Origin, id);
                f.Roaming.Changed += detail => { if (Running) log.Event(Elapsed, "fish_behavior", f, detail: detail); };
            }
            else f.Move(now);
            fish.Add(f);
            if (Running) log.Event(now, "fish_spawn", f, detail: "route=" + routeIndex);
        }
        public void Shoot(int player, double now)
        {if(player>=0 && player<2)Shoot(player,now,input.Aim(player));}
        void Shoot(int player,double now,Ray ray)
        {
            if (!Running || player < 0 || player > 1) return;
            log.Event(now, "button_press", actor: player);
            if (!input.Valid(player)) { log.Event(now, "shot_rejected_stale_input", actor: player); return; }
            if (now - lastShot[player] < config.shotCooldown) { log.Event(now, "shot_rejected_cooldown", actor: player); return; }
            lastShot[player] = now;
            if(config.netCaptureEnabled)
            {
                if(!nets){nets=gameObject.AddComponent<NetCaptureSystem>();nets.Initialize(this);}
                if(!nets.Launch(ray,player))log.Event(now,"net_launch_failed",actor:player,detail:"Check net material configuration");
                return;
            }
            if (!Physics.Raycast(ray, out RaycastHit hit, 100, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore))
            { log.Event(now, "shot_miss", actor: player); return; }
            FishTarget target = hit.collider.GetComponentInParent<FishTarget>();
            if (!target) { log.Event(now, "shot_blocked", actor: player); return; }
            if (!ExperimentRules.CanCapture(condition, target.Kind, player, target.HandoffPlayer))
            {
                Wrong++; log.Event(now, target.Owner < 0 ? "wrong_distractor" : "wrong_owner", target, player); return;
            }
            if (config.persistentSchool && now < target.CaptureReadyAt)
            { log.Event(now, "shot_rejected_fish_recovery", target, player); return; }
            Captured++; log.Event(now, "capture", target, player, config.persistentSchool ? "persistent_hit; fish retained" : "rt_from_spawn=" + (now - target.Born).ToString("R", CultureInfo.InvariantCulture));
            if (config.persistentSchool)
            {
                target.CaptureReadyAt = now + config.captureRecoverySeconds;
                Cue[player] = "HIT " + target.Id + "  +1"; CueUntil[player] = now + .8;
            }
            else Remove(target);
        }
        public bool NetLineVisible(Vector3 origin,FishTarget target)
        {
            Vector3 delta=target.transform.position-origin;
            return !Physics.Raycast(origin,delta.normalized,delta.magnitude,1<<8,QueryTriggerInteraction.Ignore);
        }
        public bool CanNetCapture(FishTarget target,int player)
        {
            if(!config.netRespectsRoles || ExperimentRules.CanCapture(condition,target.Kind,player,target.HandoffPlayer))return true;
            Wrong++;RecordNetEvent(target.Owner<0?"wrong_distractor":"wrong_owner",target,player);return false;
        }
        public void RecordNetEvent(string type,FishTarget target,int player,string detail="")
        {if(Running)log.Event(Elapsed,type,target,player,detail);}
        public void CompleteNetCapture(FishTarget target,int player)
        {
            if(!Running || !target || !target.IsBeingCaptured || !fish.Contains(target))return;
            Captured++;log.Event(Elapsed,"capture",target,player,"area_net; fish removed after struggle");
            log.Event(Elapsed,"fish_removed",target,player,"remaining="+(fish.Count-1));
            Cue[player]="CAUGHT "+target.Id+" / "+(fish.Count-1)+" LEFT";CueUntil[player]=Elapsed+.8;
            Remove(target);
        }
        void Remove(FishTarget f)
        {
            // Disable immediately so a second press in the same frame cannot capture the same target twice.
            f.gameObject.SetActive(false); fish.Remove(f); Destroy(f.gameObject);
        }
        void OnApplicationFocus(bool focus) { if (Running) log.Event(Elapsed, focus ? "focus_gained" : "focus_lost"); }
        void OnDisable() { End("component_disabled"); }
        void OnApplicationQuit() { End("application_quit"); }
    }
}
