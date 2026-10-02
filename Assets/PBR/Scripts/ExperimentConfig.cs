using UnityEngine;

namespace PBR
{
    public enum ExperimentCondition { FixedRole, SharedRole, ResponsibleCue, AdaptiveHandoff }
    public enum FishKind { PlayerA, PlayerB, Distractor, TransferableA, TransferableB }

    [CreateAssetMenu(menuName = "PBR/Experiment configuration")]
    public sealed class ExperimentConfig : ScriptableObject
    {
        public int seed = 20260930;
        [Header("Persistent school")]
        public bool persistentSchool;
        public bool ringWorld;
        [Range(0, 2)] public int backgroundDetail = 2;
        [Range(1, 40)] public int fishCount = 10;
        [Min(.1f)] public float swimSpeed = 1.5f;
        [Min(.1f)] public float burstSpeed = 4f;
        [Min(.1f)] public float acceleration = 2f;
        [Min(.1f)] public float captureRecoverySeconds = 3f;
        [Min(10)] public float durationSeconds = 90;
        [Min(.5f)] public float spawnInterval = 3;
        [Min(3)] public float travelSeconds = 16;
        [Min(0)] public float hidePauseSeconds = 2;
        [Range(5, 80)] public float monitoringHalfAngle = 25;
        [Min(.1f)] public float cueDelay = 1;
        [Min(.1f)] public float handoffDelay = 2;
        [Min(.1f)] public float handoffSeconds = 4;
        [Min(1)] public int workloadThreshold = 2;
        [Range(1, 60)] public int sampleHz = 20;
        [Min(.05f)] public float shotCooldown = .2f;
        public bool panoramicPreview;
        public GameObject[] fishPrefabs;
        public GameObject[] rockPrefabs;
        public Material transferableMarkerMaterial;
    }

    public static class ExperimentRules
    {
        public static int Owner(FishKind kind) => kind == FishKind.Distractor ? -1 :
            (kind == FishKind.PlayerA || kind == FishKind.TransferableA ? 0 : 1);
        public static bool Transferable(FishKind kind) => kind == FishKind.TransferableA || kind == FishKind.TransferableB;
        public static bool Covered(float yaw, float targetYaw, float halfAngle) =>
            Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) <= halfAngle;
        public static bool CanCapture(ExperimentCondition condition, FishKind kind, int player, int handoffPlayer)
        {
            if (player < 0 || player > 1 || Owner(kind) < 0) return false;
            return condition == ExperimentCondition.SharedRole || Owner(kind) == player ||
                (condition == ExperimentCondition.AdaptiveHandoff && Transferable(kind) && handoffPlayer == player);
        }
        // Balanced Williams order; pairs rotate across rows, not an unbalanced random shuffle.
        public static int[] ConditionOrder(int pairIndex)
        {
            int[][] rows = { new[] {0,1,3,2}, new[] {1,2,0,3}, new[] {2,3,1,0}, new[] {3,0,2,1} };
            return rows[((pairIndex % 4) + 4) % 4];
        }
    }
}
