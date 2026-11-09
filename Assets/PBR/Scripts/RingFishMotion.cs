using System.Collections.Generic;
using UnityEngine;

namespace PBR
{
    public sealed class RingFishMotion : FishRoaming
    {
        RingHabitat habitat;
        ExperimentConfig settings;
        System.Random rng;
        readonly List<Vector3> path = new List<Vector3>();
        int waypoint;
        float timer, stuck, hideTravel, bank;
        Animation animationClip;
        Transform visual;
        ReefCover selectedCover;
        public override string CoverKind => selectedCover ? selectedCover.kind.ToString() : "";
        public override string CoverName => selectedCover ? selectedCover.name : "";
        public const float TurnDegreesPerSecond = 95;
        public override void Initialize(ExperimentConfig config, Transform unused, Vector3 eye, int id)
        {
            settings = config; habitat = FindAnyObjectByType<RingHabitat>(); habitat.Prepare();
            rng = new System.Random(unchecked(config.seed + id * 7919));
            animationClip = GetComponentInChildren<Animation>(); visual = transform.Find("Animated fish");
            float angle = (id + .3f) * Mathf.PI * 2 / config.fishCount;
            transform.position = RingHabitat.Polar(angle, id % 2 == 0 ? 10 : 21, 4.2f + id % 3 * .65f);
            // A larger school samples more azimuths; find a clear initial position beside tall cover.
            for(int attempt=0;attempt<48 && !habitat.Free(transform.position);attempt++)
                transform.position=RingHabitat.Polar(angle+(attempt+1)*.07f,id%2==0?10:22,5.2f+attempt%3*.6f);
            transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle)));
            SetState(FishMotionState.Swimming);
        }
        float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        public override bool FullyOccluded(Vector3 p) => habitat.FullyOccluded(p);
        public bool NavigateTo(Vector3 destination)
        {
            waypoint = 0; stuck = 0;
            return habitat.Plan(transform.position, destination, path);
        }
        bool Choose(bool hide)
        {
            if (hide)
            {
                if (habitat.covers != null && habitat.covers.Length > 0)
                {
                    var sites = new List<ReefCover>(habitat.covers);
                    sites.Sort((a,b) => (a.transform.position-transform.position).sqrMagnitude.CompareTo((b.transform.position-transform.position).sqrMagnitude));
                    int start = rng.Next(Mathf.Min(5, sites.Count));
                    for(int i=0;i<sites.Count;i++) if (ChooseSite(sites[(i+start)%sites.Count])) return true;
                    return false;
                }
                var candidates = new List<Vector3>();
                foreach (var rock in habitat.rocks)
                {
                    var collider = rock.GetComponent<Collider>();
                    Vector3 outward = new Vector3(rock.position.x, 0, rock.position.z).normalized;
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 p = rock.position + outward * (Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z) + 1.2f + j * .35f);
                        p.y = 1.8f + j * .18f;
                        if (habitat.Free(p) && habitat.FullyOccluded(p)) candidates.Add(p);
                    }
                }
                candidates.Sort((a, b) => (a - transform.position).sqrMagnitude.CompareTo((b - transform.position).sqrMagnitude));
                int offset = candidates.Count > 0 ? rng.Next(Mathf.Min(5, candidates.Count)) : 0;
                for (int i = 0; i < candidates.Count; i++) if (NavigateTo(candidates[(i + offset) % candidates.Count])) return true;
                return false;
            }
            float current = Mathf.Atan2(transform.position.x, transform.position.z);
            for (int i = 0; i < 30; i++)
            {
                float turn = Range(.3f, 1.35f) * (rng.Next(2) == 0 ? 1 : -1);
                var p = RingHabitat.Polar(current + turn, Range(8, 23), Range(1.6f, 7));
                if (NavigateTo(p)) return true;
            }
            return false;
        }
        bool ChooseSite(ReefCover site)
        {
            if (!site || site.hidingCandidates == null || site.hidingCandidates.Length==0) return false;
            int start=rng.Next(site.hidingCandidates.Length);
            for(int i=0;i<site.hidingCandidates.Length;i++)
            {
                Vector3 p=site.transform.TransformPoint(site.hidingCandidates[(i+start)%site.hidingCandidates.Length]);
                if(habitat.Free(p) && habitat.FullyOccluded(p) && NavigateTo(p)) { selectedCover=site; return true; }
            }
            return false;
        }
        public bool TryHideAt(ReefCover site)
        {
            if(!ChooseSite(site)) return false;
            HiddenAtRest=false; hideTravel=stuck=0; State=FishMotionState.Hiding; Transitions++;
            Report("state=Hiding;cover="+CoverKind+";site="+CoverName); return true;
        }
        public override void SetState(FishMotionState next)
        {
            HiddenAtRest = false; hideTravel = stuck = 0;
            if (next == FishMotionState.Hiding && !Choose(true)) next = FishMotionState.Swimming;
            if ((next == FishMotionState.Swimming || next == FishMotionState.Accelerating) && !Choose(false)) next = FishMotionState.Stationary;
            State = next; timer = next == FishMotionState.Accelerating ? Range(4, 7) : Range(5, 10);
            if(next==FishMotionState.Stationary)timer=Range(Mathf.Min(.4f,settings.MaximumHoverDuration),settings.MaximumHoverDuration);
            if(next != FishMotionState.Hiding) selectedCover=null;
            Transitions++; Report("state=" + next + ";cover="+CoverKind+";site="+CoverName);
        }
        void NextState()
        {
            // Avoid chaining hidden rest directly into another hovering state.
            if(State==FishMotionState.Stationary || HiddenAtRest){SetState(rng.Next(2)==0?FishMotionState.Swimming:FishMotionState.Accelerating);return;}
            int next = rng.Next(3); if (next >= (int)State) next++;
            SetState((FishMotionState)next);
        }
        public override void Tick(float dt)
        {
            float elapsed=Mathf.Max(0,dt);
            dt = Mathf.Clamp(dt, 0, .05f);
            bool resting = State == FishMotionState.Stationary || HiddenAtRest;
            if (HiddenAtRest) { Speed = 0; timer -= elapsed; if (timer <= 0) NextState(); Animate(dt, 0); return; }
            if (path.Count == 0 || waypoint >= path.Count)
            {
                Speed = Mathf.MoveTowards(Speed, 0, settings.acceleration * dt);
                if (State == FishMotionState.Hiding && habitat.FullyOccluded(transform.position))
                { HiddenAtRest = true; Speed = 0; timer = Mathf.Clamp(settings.hidePauseSeconds,.1f,settings.MaximumHoverDuration); Report("hide_arrived;cover="+CoverKind+";site="+CoverName); }
                else if (State == FishMotionState.Hiding) SetState(FishMotionState.Swimming);
                else if (!resting) Choose(false);
                if (resting) { timer -= elapsed; if (timer <= 0) NextState(); }
                Animate(dt, 0); return;
            }
            Vector3 delta = path[waypoint] - transform.position;
            float angle = Vector3.Angle(transform.forward, delta);
            float oldYaw = transform.eulerAngles.y;
            if (!resting && delta.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), TurnDegreesPerSecond * dt);
            float baseSpeed = State == FishMotionState.Accelerating ? settings.burstSpeed : settings.swimSpeed;
            float desired = resting ? 0 : baseSpeed * Mathf.Lerp(.12f, 1, Mathf.Clamp01(1 - angle / 100));
            // Ease into waypoints and hiding positions; forward propulsion follows the turning body.
            desired = Mathf.Min(desired, Mathf.Max(.18f, delta.magnitude * 1.3f));
            Speed = Mathf.MoveTowards(Speed, desired, settings.acceleration * dt);
            float distance = Speed * dt;
            Vector3 nextPosition = transform.position + transform.forward * distance;
            if (habitat.Free(nextPosition) && habitat.Clear(transform.position, nextPosition))
            { transform.position = nextPosition; stuck = 0; }
            else { Speed = 0; stuck += dt; }
            if (Vector3.Distance(transform.position, path[waypoint]) < .35f) waypoint++;
            if (resting)
            { if (Speed < .01f) { timer -= elapsed; if (timer <= 0) NextState(); } }
            else if (State == FishMotionState.Hiding)
            { hideTravel += dt; if (hideTravel > 55) SetState(FishMotionState.Swimming); }
            else { timer -= dt; if (timer <= 0) NextState(); }
            if (stuck > 2) SetState(FishMotionState.Swimming);
            Animate(dt, Mathf.DeltaAngle(oldYaw, transform.eulerAngles.y) / Mathf.Max(dt, .0001f));
        }
        void Animate(float dt, float yawRate)
        {
            bank = Mathf.Lerp(bank, Mathf.Clamp(-yawRate * .14f, -14, 14), 1 - Mathf.Exp(-5 * dt));
            if (visual) visual.localRotation = Quaternion.Euler(0, 0, bank);
            if (animationClip && animationClip.clip)
                animationClip[animationClip.clip.name].speed = Mathf.Lerp(.35f, 1.8f, Mathf.Clamp01(Speed / settings.burstSpeed));
        }
    }
}
