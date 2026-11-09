using System;
using UnityEngine;

namespace PBR
{
    public enum FishMotionState { Swimming, Accelerating, Hiding, Stationary }

    // Navigation stays in a bounded volume behind the physical reef, with swept collision checks.
    // A hiding timer starts only after the whole fish has reached an occluded destination.
    public class FishRoaming : MonoBehaviour
    {
        public FishMotionState State { get; protected set; }
        public float Speed { get; protected set; }
        public bool HiddenAtRest { get; protected set; }
        public int Transitions { get; protected set; }
        public virtual string CoverKind => "";
        public virtual string CoverName => "";
        public event Action<string> Changed;
        protected void Report(string detail) => Changed?.Invoke(detail);
        ExperimentConfig config;
        Transform sector;
        Vector3 observer, destination;
        System.Random rng;
        float remaining, approachTime;
        Animation animationClip;
        const float Clearance = .85f;

        public virtual void Initialize(ExperimentConfig settings, Transform reefSector, Vector3 eye, int id)
        {
            config = settings; sector = reefSector; observer = eye;
            rng = new System.Random(unchecked(settings.seed + id * 7919));
            animationClip = GetComponentInChildren<Animation>();
            // Assigned once, before the first frame; never repositioned on block start or capture.
            transform.position = sector.TransformPoint(new Vector3(-9 + (id % 5) * 4.5f, 4.8f + (id / 5 % 3) * 1.05f, 21 + Range(0, 2)));
            transform.rotation = sector.rotation * Quaternion.Euler(0, id % 2 == 0 ? 90 : -90, 0);
            SetState(FishMotionState.Swimming);
        }
        float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
        bool Blocked(Vector3 p)
        {
            Vector3 d = p - observer;
            return Physics.Raycast(observer, d.normalized, d.magnitude, 1 << 8, QueryTriggerInteraction.Ignore);
        }
        public virtual bool FullyOccluded(Vector3 p)
        {
            if (!Blocked(p)) return false;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                if (!Blocked(p + sector.TransformDirection(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Clearance))) return false;
            }
            return true;
        }
        bool ChooseDestination(bool hidden)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 local = hidden ? new Vector3(Range(-.7f, .7f), Range(2.1f, 2.8f), Range(21, 23))
                    : new Vector3(Range(-10, 10), Range(1.6f, 6.8f), Range(20, 24));
                Vector3 candidate = sector.TransformPoint(local);
                if (hidden ? !FullyOccluded(candidate) : Blocked(candidate)) continue;
                if (Physics.CheckSphere(candidate, Clearance, 1 << 8, QueryTriggerInteraction.Ignore)) continue;
                if (Vector3.Distance(candidate, transform.position) < 1.5f) continue;
                destination = candidate; return true;
            }
            return false;
        }
        public virtual void SetState(FishMotionState next)
        {
            HiddenAtRest = false; approachTime = 0;
            if (next == FishMotionState.Hiding && !ChooseDestination(true)) next = FishMotionState.Swimming;
            if (next == FishMotionState.Swimming || next == FishMotionState.Accelerating)
                if (!ChooseDestination(false)) next = FishMotionState.Stationary;
            State = next; Transitions++;
            remaining = next == FishMotionState.Accelerating ? Range(2.5f, 4.5f) : Range(3, 7);
            if(next==FishMotionState.Stationary)remaining=Range(Mathf.Min(.4f,config.MaximumHoverDuration),config.MaximumHoverDuration);
            if (next == FishMotionState.Stationary) Speed = 0;
            Changed?.Invoke("state=" + State);
        }
        void NextState()
        {
            if(State==FishMotionState.Stationary || HiddenAtRest){SetState(rng.Next(2)==0?FishMotionState.Swimming:FishMotionState.Accelerating);return;}
            int choice = rng.Next(3);
            if (choice >= (int)State) choice++;
            SetState((FishMotionState)choice);
        }
        public virtual void Tick(float dt)
        {
            float elapsed=Mathf.Max(0,dt);
            // Slow motion during a stalled frame is preferable to a visible jump.
            dt = Mathf.Clamp(dt, 0, .05f);
            if (State == FishMotionState.Stationary || HiddenAtRest)
            {
                Speed = 0; remaining -= elapsed;
                if (remaining <= 0) NextState();
            }
            else
            {
                Speed = Mathf.MoveTowards(Speed, State == FishMotionState.Accelerating ? config.burstSpeed : config.swimSpeed, config.acceleration * dt);
                Vector3 delta = destination - transform.position;
                float distance = Mathf.Min(delta.magnitude, Speed * dt);
                if (delta.sqrMagnitude > .00001f)
                {
                    Vector3 direction = delta.normalized;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, sector.up), 160 * dt);
                    if (!Physics.SphereCast(transform.position, Clearance, direction, out _, distance, 1 << 8, QueryTriggerInteraction.Ignore))
                        transform.position += direction * distance;
                    else { SetState(FishMotionState.Swimming); return; }
                }
                if (State == FishMotionState.Hiding)
                {
                    approachTime += dt;
                    if (Vector3.Distance(destination, transform.position) < .03f && FullyOccluded(transform.position))
                    {
                        HiddenAtRest = true; Speed = 0;
                        remaining = Mathf.Clamp(config.hidePauseSeconds,.1f,config.MaximumHoverDuration);
                        Changed?.Invoke("hide_arrived");
                    }
                    else if (approachTime > 25) SetState(FishMotionState.Swimming);
                }
                else
                {
                    remaining -= dt;
                    if (remaining <= 0) NextState();
                    else if (Vector3.Distance(destination, transform.position) < .1f) ChooseDestination(false);
                }
            }
            if (animationClip && animationClip.clip)
                animationClip[animationClip.clip.name].speed = Mathf.Lerp(.35f, 1.8f, Mathf.Clamp01(Speed / Mathf.Max(.1f, config.burstSpeed)));
        }
    }
}
