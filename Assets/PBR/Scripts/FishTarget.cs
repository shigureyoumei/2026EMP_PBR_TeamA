using UnityEngine;
using UnityEngine.Splines;

namespace PBR
{
    public sealed class FishTarget : MonoBehaviour
    {
        public int Id, HandoffPlayer = -1;
        public FishKind Kind;
        public int Owner => ExperimentRules.Owner(Kind);
        public float Yaw, GapSeconds;
        public double Born, HandoffUntil, NextCue;
        public bool WasVisible, HasVisibility, PauseAnnounced, ResumeAnnounced;
        public bool HandoffUsed;
        public SplineContainer Route;
        public float TravelSeconds, PauseSeconds;
        public double CaptureReadyAt;
        public FishRoaming Roaming;
        public bool Move(double now)
        {
            float age = (float)(now - Born);
            float halfway = TravelSeconds * .5f;
            float travelAge = age <= halfway ? age : age <= halfway + PauseSeconds ? halfway : age - PauseSeconds;
            float t = Mathf.Clamp01(travelAge / TravelSeconds);
            transform.position = Route.EvaluatePosition(t);
            Vector3 tangent = Route.EvaluateTangent(t);
            if (tangent.sqrMagnitude > .00001f) transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            return age >= TravelSeconds + PauseSeconds;
        }
    }
}
