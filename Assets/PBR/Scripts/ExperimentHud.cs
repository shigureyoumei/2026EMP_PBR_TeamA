using UnityEngine;

namespace PBR
{
    public sealed class ExperimentHud : MonoBehaviour
    {
        public FishExperiment experiment;
        public string fishLegend = "A  ORANGE     B  CYAN     IGNORE  GREY     WHITE DOT  TRANSFERABLE";
        GUIStyle title, text, small;
        readonly string[] conditions = { "1  FIXED ROLE", "2  SHARED ROLE", "3  OWNER CUE", "4  ADAPTIVE HANDOFF" };
        void OnGUI()
        {
            if (!experiment || !experiment.config) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            }
            float scale = Mathf.Max(.6f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.color = new Color(.025f, .1f, .15f, .94f); GUI.DrawTexture(new Rect(0, 0, width, 108), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(26, 12, 560, 34), "PBR  /  COLLABORATIVE REEF SEARCH", title);
            bool netMode=experiment.config.netCaptureEnabled;
            GUI.Label(new Rect(26, 52, 1050, 25), netMode && !experiment.config.netRespectsRoles?"WHITE NET / ALL SPECIES CATCHABLE / FISH DISAPPEAR AFTER STRUGGLING":fishLegend, text);
            GUI.Label(new Rect(26, 80, 850, 23), "Mouse aim · TAB selects player · F / J capture for A / B · Arrows aim other player · ESC stops", small);
            if (!experiment.Running)
            {
                GUI.Box(new Rect(24, 134, 445, 347), "");
                GUI.Label(new Rect(42, 150, 410, 28), experiment.Status, text);
                GUI.Label(new Rect(42, 186, 65, 25), "Pair ID", small);
                experiment.pairId = GUI.TextField(new Rect(113, 184, 160, 26), experiment.pairId, 40);
                experiment.condition = (ExperimentCondition)GUI.SelectionGrid(new Rect(42, 225, 405, 82), (int)experiment.condition, conditions, 2);
                GUI.Label(new Rect(42, 323, 400, 48), "Pilot prototype · Desktop head-direction simulation\nShared operator screen; cues are not private yet.", small);
                if (GUI.Button(new Rect(42, 389, 405, 50), "START " + experiment.config.durationSeconds.ToString("F0") + "s PILOT BLOCK")) experiment.Begin();
                if(experiment.config.persistentSchool && GUI.Button(new Rect(42,449,405,28),"RESET SCHOOL ("+experiment.config.fishCount+" FISH)"))experiment.ResetSchool();
            }
            else
            {
                GUI.Label(new Rect(width - 350, 18, 330, 30), experiment.condition.ToString(), text);
                GUI.Label(new Rect(width - 350, 49, 330, 30), "TIME " + Mathf.CeilToInt(experiment.config.durationSeconds - (float)experiment.Elapsed) + "s     CAPTURED " + experiment.Captured, text);
                for (int p = 0; p < 2; p++)
                {
                    GUI.color = p == 0 ? new Color(1,.65f,.25f) : new Color(.3f,.9f,1);
                    GUI.Label(new Rect(25 + p * 460, height - 83, 455, 26), (p == 0 ? "A" : "B") + "  " + experiment.input.Yaw[p].ToString("F0") + " deg  " + (experiment.input.Valid(p) ? experiment.input.Source[p] : "INPUT STALE") + (experiment.input.Selected == p ? "  [MOUSE]" : ""), text);
                    if (experiment.Elapsed < experiment.CueUntil[p]) GUI.Label(new Rect(25 + p * 460, height - 51, 455, 30), experiment.Cue[p], text);
                    DrawReticle(p, scale);
                }
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(26, height - 24, width - 52, 23), "PROTOTYPE / PILOT ONLY     Head direction is not eye gaze     Wrong " + experiment.Wrong + (experiment.config.persistentSchool ? "  Fish " + experiment.Fish.Count + " (including hidden) / "+(netMode?"net catches remove fish":"hits retain fish") : "  Missed " + experiment.Missed), small);
            GUI.matrix = Matrix4x4.identity;
        }
        void DrawReticle(int player, float scale)
        {
            Vector3 pos = experiment.input.Aim(player).GetPoint(20);
            foreach (var camera in experiment.input.Cameras)
            {
                if (!camera || !camera.isActiveAndEnabled) continue;
                Vector3 view = camera.WorldToViewportPoint(pos);
                if (view.z <= 0 || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1) continue;
                Vector3 pixel = camera.WorldToScreenPoint(pos);
                float x = pixel.x / scale, y = (Screen.height - pixel.y) / scale;
                GUI.DrawTexture(new Rect(x - 10, y - 1, 20, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - 1, y - 10, 2, 20), Texture2D.whiteTexture);
                GUI.Label(new Rect(x + 12, y - 12, 25, 25), player == 0 ? "A" : "B", text);
            }
        }
    }
}
