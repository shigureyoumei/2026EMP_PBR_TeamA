using UnityEngine;

namespace PBR
{
    public enum ReefCoverKind { Rock, Cave, Arch, Kelp, FanCoral }

    // Candidates are only hints. Runtime verifies clearance, real mesh occlusion, and a route.
    public sealed class ReefCover : MonoBehaviour
    {
        public ReefCoverKind kind;
        public Vector3[] hidingCandidates;
    }
}
