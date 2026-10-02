using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBR
{
    // A shared cylindrical navigation volume. Angular neighbors wrap; world positions never teleport.
    public sealed class RingHabitat : MonoBehaviour
    {
        public Transform[] rocks;
        public ReefCover[] covers;
        public Vector3 Eye = new Vector3(0, 3, 0);
        public const float Clearance = .9f;
        const int Angles = 48, Rings = 6, Levels = 3;
        readonly List<Vector3> nodes = new List<Vector3>();
        readonly List<List<int>> edges = new List<List<int>>();
        bool[] valid;
        public static float Radius(Vector3 p) => new Vector2(p.x, p.z).magnitude;
        public bool Free(Vector3 p) => Radius(p) >= 6.5f && Radius(p) <= 25 && p.y >= 1.05f && p.y <= 8
            && !Physics.CheckSphere(p, Clearance, 1 << 8, QueryTriggerInteraction.Ignore);
        public bool Clear(Vector3 a, Vector3 b)
        {
            var d = b - a;
            // Chords across the participant's central clear zone are not valid water routes.
            var horizontal = new Vector2(d.x, d.z); var start = new Vector2(a.x, a.z);
            float t = horizontal.sqrMagnitude > .0001f ? Mathf.Clamp01(-Vector2.Dot(start, horizontal) / horizontal.sqrMagnitude) : 0;
            if ((start + horizontal * t).magnitude < 6.5f) return false;
            return !Physics.SphereCast(a, Clearance, d.normalized, out _, d.magnitude, 1 << 8, QueryTriggerInteraction.Ignore);
        }
        public bool Occluded(Vector3 p)
        {
            var d = p - Eye;
            return Physics.Raycast(Eye, d.normalized, d.magnitude, 1 << 8, QueryTriggerInteraction.Ignore);
        }
        public bool FullyOccluded(Vector3 p)
        {
            if (!Occluded(p)) return false;
            Vector3 right = Vector3.Cross(Vector3.up, (p - Eye).normalized).normalized;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                if (!Occluded(p + (right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * .85f)) return false;
            }
            return true;
        }
        static int Index(int a, int r, int y) => (y * Rings + r) * Angles + (a + Angles) % Angles;
        public static Vector3 Polar(float angle, float radius, float height) => new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
        public void Prepare()
        {
            if (nodes.Count != 0) return;
            Physics.SyncTransforms();
            for (int y = 0; y < Levels; y++) for (int r = 0; r < Rings; r++) for (int a = 0; a < Angles; a++)
            { nodes.Add(Polar(a * Mathf.PI * 2 / Angles, 8 + r * 3, 1.6f + y * 2.6f)); edges.Add(new List<int>()); }
            valid = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) valid[i] = Free(nodes[i]);
            for (int y = 0; y < Levels; y++) for (int r = 0; r < Rings; r++) for (int a = 0; a < Angles; a++)
            {
                int i = Index(a, r, y); if (!valid[i]) continue;
                for (int dy = -1; dy <= 1; dy++) for (int dr = -1; dr <= 1; dr++) for (int da = -1; da <= 1; da++)
                {
                    if (da == 0 && dr == 0 && dy == 0 || y + dy < 0 || y + dy >= Levels || r + dr < 0 || r + dr >= Rings) continue;
                    int j = Index(a + da, r + dr, y + dy);
                    if (j <= i || !valid[j] || !Clear(nodes[i], nodes[j])) continue;
                    edges[i].Add(j); edges[j].Add(i);
                }
            }
        }
        public bool Plan(Vector3 start, Vector3 goal, List<Vector3> path)
        {
            Prepare(); path.Clear(); if (!Free(goal)) return false;
            if (Clear(start, goal)) { path.Add(goal); return true; }
            int count = nodes.Count;
            var cost = new float[count]; var parent = new int[count]; var closed = new bool[count];
            for (int i = 0; i < count; i++) { cost[i] = float.PositiveInfinity; parent[i] = -1; }
            // Local graph attachment avoids expensive all-pairs endpoint visibility tests.
            for (int i = 0; i < count; i++) if (valid[i] && Vector3.Distance(start, nodes[i]) < 6 && Clear(start, nodes[i])) cost[i] = Vector3.Distance(start, nodes[i]);
            for (int iteration = 0; iteration < count; iteration++)
            {
                int best = -1; float score = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    float f = cost[i] + Vector3.Distance(nodes[i], goal);
                    if (!closed[i] && f < score) { best = i; score = f; }
                }
                if (best < 0) return false;
                if (Vector3.Distance(nodes[best], goal) < 6 && Clear(nodes[best], goal))
                {
                    var reverse = new List<Vector3> { goal };
                    for (int i = best; i >= 0; i = parent[i]) reverse.Add(nodes[i]);
                    reverse.Reverse(); Vector3 from = start;
                    for (int i = 0; i < reverse.Count;)
                    {
                        int far = i;
                        for (int j = reverse.Count - 1; j > i; j--) if (Clear(from, reverse[j])) { far = j; break; }
                        path.Add(reverse[far]); from = reverse[far]; i = far + 1;
                    }
                    return true;
                }
                closed[best] = true;
                foreach (int next in edges[best])
                {
                    float candidate = cost[best] + Vector3.Distance(nodes[best], nodes[next]);
                    if (candidate < cost[next]) { cost[next] = candidate; parent[next] = best; }
                }
            }
            return false;
        }
    }
}
