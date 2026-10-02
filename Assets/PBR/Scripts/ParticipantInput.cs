using UnityEngine;
using UnityEngine.InputSystem;

namespace PBR
{
    public sealed class ParticipantInput : MonoBehaviour
    {
        public readonly float[] Yaw = { -15, 15 };
        public readonly float[] Pitch = { 0, 0 };
        public readonly string[] Source = { "desktop", "desktop" };
        readonly bool[] shots = new bool[2];
        readonly float[] rawYaw = new float[2], offset = new float[2];
        readonly double[] received = { -999, -999 };
        public int Selected { get; private set; }
        public bool DesktopInput = true;
        public Vector3 Origin = new Vector3(0, 3, -6);
        public Camera[] Cameras;
        public bool Valid(int player) => Source[player] == "desktop" || Time.realtimeSinceStartupAsDouble - received[player] < .5;
        public Ray Aim(int player) => new Ray(Origin, Quaternion.Euler(-Pitch[player], Yaw[player], 0) * Vector3.forward);
        // Adapter boundary: call these methods on Unity's main thread. Hardware transport is intentionally separate.
        public void SubmitOrientation(int player, float yawDegrees, float pitchDegrees = 0)
        {
            if (player < 0 || player > 1 || float.IsNaN(yawDegrees) || float.IsInfinity(yawDegrees) ||
                float.IsNaN(pitchDegrees) || float.IsInfinity(pitchDegrees)) return;
            rawYaw[player] = yawDegrees;
            Yaw[player] = Mathf.DeltaAngle(offset[player], yawDegrees);
            Pitch[player] = Mathf.Clamp(pitchDegrees, -70, 70);
            Source[player] = "external";
            received[player] = Time.realtimeSinceStartupAsDouble;
        }
        public void CalibrateForward(int player) { if (player >= 0 && player < 2) { offset[player] = rawYaw[player]; Yaw[player] = 0; } }
        public void SubmitButton(int player) { if (player >= 0 && player < 2) shots[player] = true; }
        public bool ConsumeShot(int player) { bool value = shots[player]; shots[player] = false; return value; }
        public void ClearShots() { shots[0] = shots[1] = false; }
        void Update()
        {
            if (!DesktopInput) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) Selected = 1 - Selected;
            if (Source[Selected] == "desktop" && mouse != null && Cameras != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                foreach (var camera in Cameras)
                {
                    if (!camera || !camera.isActiveAndEnabled || !camera.pixelRect.Contains(pos)) continue;
                    Vector3 dir = camera.ScreenPointToRay(pos).direction;
                    Yaw[Selected] = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    Pitch[Selected] = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
                    break;
                }
            }
            int other = 1 - Selected;
            if (kb != null && Source[other] == "desktop")
            {
                Yaw[other] += ((kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0)) * 50 * Time.unscaledDeltaTime;
                Pitch[other] = Mathf.Clamp(Pitch[other] + ((kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0)) * 35 * Time.unscaledDeltaTime, -65, 65);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) SubmitButton(Selected);
            if (kb != null)
            {
                if (kb.fKey.wasPressedThisFrame) SubmitButton(0);
                if (kb.jKey.wasPressedThisFrame) SubmitButton(1);
            }
        }
    }
}
