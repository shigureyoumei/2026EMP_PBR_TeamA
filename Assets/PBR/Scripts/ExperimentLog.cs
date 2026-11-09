using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PBR
{
    public sealed class ExperimentLog : IDisposable
    {
        StreamWriter events, samples, poses;
        public string Folder { get; private set; }
        public bool Failed { get; private set; }
        static string N(double x) => x.ToString("0.000000", CultureInfo.InvariantCulture);
        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        public ExperimentLog(ExperimentConfig config, ExperimentCondition condition, string pair, int block)
        {
            Folder = Path.Combine(Application.persistentDataPath, "PBRLogs", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "_" + Guid.NewGuid().ToString("N").Substring(0,8));
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "session.json"), JsonUtility.ToJson(new SessionMetadata {
                utc = DateTime.UtcNow.ToString("O"), pair = pair, block = block, condition = condition.ToString(),
                unityVersion = Application.unityVersion, config = JsonUtility.ToJson(config),
                dataStatus = "prototype_pilot_only", input = "desktop_simulation_or_external_api"
            }, true));
            events = new StreamWriter(Path.Combine(Folder, "events.csv"), false, new UTF8Encoding(false));
            samples = new StreamWriter(Path.Combine(Folder, "samples.csv"), false, new UTF8Encoding(false));
            poses = new StreamWriter(Path.Combine(Folder, "orientation.csv"), false, new UTF8Encoding(false));
            poses.WriteLine("time_s,frame,yaw_a,yaw_b,pitch_a,pitch_b,input_a,input_b,valid_a,valid_b");
            events.WriteLine("time_s,frame,event,fish_id,kind,owner,actor,handoff_player,detail");
            samples.WriteLine("time_s,frame,fish_id,kind,owner,x,y,z,target_yaw,yaw_a,yaw_b,pitch_a,pitch_b,input_a,input_b,valid_a,valid_b,line_visible,covered_a,covered_b,handoff_player,workload_a,workload_b,gap_s,motion_state,speed,hidden_at_rest,cover_kind,cover_site,capture_in_progress");
            events.Flush(); samples.Flush(); poses.Flush();
        }
        public void Pose(double t, ParticipantInput input)
        {
            Write(poses, string.Join(",", N(t), Time.frameCount, N(input.Yaw[0]), N(input.Yaw[1]), N(input.Pitch[0]), N(input.Pitch[1]),
                Q(input.Source[0]), Q(input.Source[1]), input.Valid(0) ? 1 : 0, input.Valid(1) ? 1 : 0));
        }
        public void Event(double t, string type, FishTarget fish = null, int actor = -1, string detail = "")
        {
            Write(events, string.Join(",", N(t), Time.frameCount, Q(type), fish ? fish.Id : -1,
                Q(fish ? fish.Kind.ToString() : ""), fish ? fish.Owner : -1, actor, fish ? fish.HandoffPlayer : -1, Q(detail)));
        }
        public void Sample(double t, FishTarget f, ParticipantInput input, bool visible, bool a, bool b, int wa, int wb)
        {
            Vector3 p = f.transform.position;
            Write(samples, string.Join(",", N(t), Time.frameCount, f.Id, Q(f.Kind.ToString()), f.Owner,
                N(p.x), N(p.y), N(p.z), N(f.Yaw), N(input.Yaw[0]), N(input.Yaw[1]), N(input.Pitch[0]), N(input.Pitch[1]),
                Q(input.Source[0]), Q(input.Source[1]), input.Valid(0) ? 1 : 0, input.Valid(1) ? 1 : 0,
                visible ? 1 : 0, a ? 1 : 0, b ? 1 : 0, f.HandoffPlayer, wa, wb, N(f.GapSeconds),
                Q(f.Roaming ? f.Roaming.State.ToString() : "Spline"), N(f.Roaming ? f.Roaming.Speed : 0), f.Roaming && f.Roaming.HiddenAtRest ? 1 : 0,
                Q(f.Roaming ? f.Roaming.CoverKind : ""), Q(f.Roaming ? f.Roaming.CoverName : ""), f.IsBeingCaptured?1:0));
        }
        void Write(StreamWriter writer, string line)
        {
            if (Failed) return;
            try { writer.WriteLine(line); }
            catch (Exception e) { Failed = true; Debug.LogError("PBR recording failed: " + e.Message); }
        }
        public void Flush()
        {
            if (Failed) return;
            try { events?.Flush(); samples?.Flush(); poses?.Flush(); }
            catch (Exception e) { Failed = true; Debug.LogError("PBR recording failed: " + e.Message); }
        }
        public void Dispose()
        {
            Flush();
            foreach (var writer in new[] {events, samples, poses})
                try { writer?.Dispose(); } catch (IOException e) { Failed = true; Debug.LogError(e.Message); }
            events = samples = poses = null;
        }
        [Serializable] sealed class SessionMetadata
        {
            public string utc, pair, condition, unityVersion, config, dataStatus, input;
            public int block;
        }
    }
}
