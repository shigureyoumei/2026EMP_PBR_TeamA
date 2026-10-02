using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PBR
{
    public sealed class PersistentFishSmokeRun : MonoBehaviour
    {
        int checks;
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-persistent-smoke") >= 0)
                new GameObject("Persistent school validation").AddComponent<PersistentFishSmokeRun>();
        }
        void Check(bool value, string message)
        {
            checks++;
            if (!value) throw new Exception(message);
        }
        void Step(FishTarget f, float dt)
        {
            var before = f.transform.position;
            f.Roaming.Tick(dt);
            Check(Vector3.Distance(before, f.transform.position) <= 4.001f * dt, "Discontinuous movement");
            Check(f.gameObject.activeInHierarchy && f.GetComponent<Collider>().enabled, "Fish deactivated");
            Check(!Physics.CheckSphere(f.transform.position, .8f, 1 << 8), "Fish intersects rock");
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../persistent-results.txt"));
            yield return null;
            var session = FindAnyObjectByType<FishExperiment>();
            try
            {
                Check(session.config.persistentSchool, "Wrong scene mode");
                var ids = session.Fish.ToArray();
                Check(ids.Length == 10 && FindObjectsByType<FishTarget>().Length == 10, "Wrong population");
                Check(GameObject.Find("Lobby fish preview") == null, "Duplicate decorative school");
                var positions = session.Fish.Select(f => f.transform.position).ToArray();
                session.input.DesktopInput = false;
                session.Begin();
                Check(session.Running, "Block did not start");
                Check(session.Fish.Select((f, i) => f.transform.position == positions[i]).All(x => x), "Block start moved fish");
                var target = session.Fish[0];
                Vector3 aim = (target.transform.position - session.input.Origin).normalized;
                session.input.SubmitOrientation(0, Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, Mathf.Asin(aim.y) * Mathf.Rad2Deg);
                Physics.SyncTransforms();
                session.Shoot(0, .5);
                Check(session.Captured == 1 && session.Fish.Count == 10 && target.gameObject.activeInHierarchy, "Capture removed fish or did not score");
                session.Shoot(0, 1);
                Check(session.Captured == 1, "Fish recovery did not prevent duplicate score");
                session.End("persistent_smoke_phase_one");
                Check(session.Fish.Select((f, i) => f.transform.position == positions[i]).All(x => x), "Block end moved fish");
                const float dt = 1f / 60;
                target.Roaming.SetState(FishMotionState.Stationary);
                var still = target.transform.position;
                for (int i = 0; i < 60; i++) Step(target, dt);
                Check(target.transform.position == still, "Stationary fish moved");
                target.Roaming.SetState(FishMotionState.Accelerating);
                for (int i = 0; i < 120; i++) Step(target, dt);
                Check(target.Roaming.Speed > session.config.swimSpeed + .5f, "Acceleration failed");
                target.Roaming.SetState(FishMotionState.Hiding);
                for (int i = 0; i < 1800 && !target.Roaming.HiddenAtRest; i++) Step(target, dt);
                Check(target.Roaming.HiddenAtRest && target.Roaming.FullyOccluded(target.transform.position), "Fish did not swim behind rock");
                still = target.transform.position;
                for (int i = 0; i < 60; i++) Step(target, dt);
                Check(target.transform.position == still, "Hidden fish did not hold");
                target.Roaming.SetState(FishMotionState.Swimming);
                for (int i = 0; i < 1800 && !session.LineVisible(target); i++) Step(target, dt);
                Check(session.LineVisible(target), "Fish failed to emerge");
                int states = 0, hiddenHolds = 0;
                for (int step = 0; step < 7200; step++) foreach (var f in session.Fish)
                {
                    Step(f, dt); states |= 1 << (int)f.Roaming.State;
                    if (f.Roaming.HiddenAtRest) { hiddenHolds++; Check(f.Roaming.FullyOccluded(f.transform.position), "False hide"); }
                }
                Check(states == 15 && hiddenHolds > 0, "Random behavior coverage incomplete");
                Check(session.Fish.SequenceEqual(ids), "Fish identities changed");
                session.Begin();
                Check(session.Fish.SequenceEqual(ids), "Restart replaced fish");
                File.WriteAllText(output, "PASS: " + checks + " assertions; 120 simulated seconds; all four states; physical hiding and exit; capture retention and cooldown; stable IDs across blocks.\n");
            }
            catch (Exception e)
            {
                File.WriteAllText(output, "FAIL: " + e); Debug.LogException(e); Application.Quit(1); yield break;
            }
            // Also exercise normal player updates, recording, and automatic block completion.
            session.config.durationSeconds = 12;
            yield return new WaitForSecondsRealtime(13);
            try
            {
                Check(!session.Running && session.Fish.Count == 10 && session.Fish.All(f => f.gameObject.activeInHierarchy), "Timed block completion changed school");
                File.AppendAllText(output, "PASS: normal player loop and timed block end. Log: " + session.LastLogFolder + "\n");
            }
            catch (Exception e) { File.AppendAllText(output, "FAIL: " + e); Application.Quit(1); yield break; }
            Application.Quit(0);
        }
    }
}
