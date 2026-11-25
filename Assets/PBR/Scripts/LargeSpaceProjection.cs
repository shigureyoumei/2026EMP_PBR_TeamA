using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace PBR
{
    public sealed class LargeSpaceProjection : MonoBehaviour
    {
        const string ModeArgument = "-pbr-large-space";
        const string ScreenLayerName = "LargeSpaceScreen";
        const int CaptureSize = 1024;
        const float CaptureFov = 92f;

        static readonly Quaternion[] FaceRotations =
        {
            Quaternion.LookRotation(Vector3.right, Vector3.up),
            Quaternion.LookRotation(Vector3.left, Vector3.up),
            Quaternion.LookRotation(Vector3.up, Vector3.back),
            Quaternion.LookRotation(Vector3.down, Vector3.forward),
            Quaternion.identity,
            Quaternion.LookRotation(Vector3.back, Vector3.up)
        };

        static readonly int[] FaceTextures =
        {
            Shader.PropertyToID("_FacePositiveX"), Shader.PropertyToID("_FaceNegativeX"),
            Shader.PropertyToID("_FacePositiveY"), Shader.PropertyToID("_FaceNegativeY"),
            Shader.PropertyToID("_FacePositiveZ"), Shader.PropertyToID("_FaceNegativeZ")
        };

        // Rows are Unity displays 1–6. Columns are the left and right projector indices
        // from the active multi-display mapping in the EMP DistortedDrawer.cs.
        static readonly int[,] EmpProjectors =
        {
            { 2, 3 }, { 6, 7 }, { 8, 9 }, { 0, 1 }, { 4, 11 }, { 10, 5 }
        };

        enum OutputMode { Preview, A, B, Dual, Emp }

        sealed class Output
        {
            public Camera camera;
            public RectTransform panel;
            public int display;
        }

        readonly List<RenderTexture> textures = new();
        readonly Camera[] capture = new Camera[6];
        readonly List<Output> outputs = new();
        FourWallProjection desktopProjection;
        ParticipantInput input;
        FishExperiment experiment;
        ExperimentHud hud;
        bool originalHudEnabled;
        ProjectionRoom room;
        MeshCollider screenCollider;
        Material screenMaterial;
        Vector3 physicalEye;

        internal static bool TryStart(FourWallProjection desktop)
        {
            string[] args = Environment.GetCommandLineArgs();
            int modeIndex = Array.IndexOf(args, ModeArgument);
            if (modeIndex < 0) return false;

            string modeName = ReadArgument(args, modeIndex) ?? "preview";
            if (!Enum.TryParse(modeName, true, out OutputMode mode) || !Enum.IsDefined(typeof(OutputMode), mode))
            {
                Debug.LogError($"Large Space: unknown mode '{modeName}'. Use preview, a, b, dual or emp.");
                return false;
            }

            var rig = new GameObject("Large Space projection").AddComponent<LargeSpaceProjection>();
            try
            {
                rig.Initialize(desktop, mode, args);
                return true;
            }
            catch (Exception exception)
            {
                rig.gameObject.SetActive(false);
                Destroy(rig.gameObject);
                Debug.LogError($"Large Space could not start. Using desktop projection.\n{exception}");
                return false;
            }
        }

        void Initialize(FourWallProjection desktop, OutputMode mode, string[] args)
        {
            var data = Resources.Load<TextAsset>(LargeSpaceCalibration.ResourcePath + "Calibration");
            var meshes = Resources.LoadAll<Mesh>(LargeSpaceCalibration.ResourcePath + "LargeSpaceScreen");
            var shader = Resources.Load<Shader>(LargeSpaceCalibration.ResourcePath + "Screen");
            int screenLayer = LayerMask.NameToLayer(ScreenLayerName);
            if (!data || meshes.Length != 1 || !shader || !shader.isSupported || screenLayer < 0 || !desktop.room ||
                desktop.walls == null || desktop.walls.Length == 0 || !desktop.walls[0])
                throw new InvalidOperationException("The calibration, screen mesh, shader, screen layer or PBR cameras are missing.");

            var calibration = JsonUtility.FromJson<LargeSpaceCalibration>(data.text);
            if (calibration?.channels?.Length != 2 * LargeSpaceCalibration.ChannelsPerBank ||
                calibration.channelPixelWidth <= 0 || calibration.channelPixelHeight <= 0)
                throw new InvalidOperationException("Two groups of six calibrated channels and a valid output resolution are required.");
            room = desktop.room;
            input = desktop.GetComponent<ParticipantInput>();
            experiment = FindAnyObjectByType<FishExperiment>();
            hud = FindAnyObjectByType<ExperimentHud>();
            physicalEye = ReadEye(args, calibration.observer);
            int requiredDisplays = mode == OutputMode.Emp ? EmpProjectors.GetLength(0) : mode == OutputMode.Dual ? 2 : 1;
            if (Display.displays.Length < requiredDisplays)
            {
                Debug.LogWarning($"Large Space: {mode} requires {requiredDisplays} displays; found {Display.displays.Length}. Showing both banks in preview.");
                mode = OutputMode.Preview;
            }
            int channelWidth = ReadChannelWidth(args, mode == OutputMode.Preview
                ? calibration.channelPixelWidth / 2 : calibration.channelPixelWidth);
            int channelHeight = Mathf.RoundToInt((float)channelWidth * calibration.channelPixelHeight / calibration.channelPixelWidth);
            var channelSize = new Vector2Int(channelWidth, channelHeight);

            CreateScreen(meshes[0], shader, screenLayer);
            CreateCaptureCameras(desktop.walls[0], screenLayer);
            if (mode == OutputMode.Emp)
            {
                CreateEmpOutputs(calibration, channelSize, screenLayer);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                for (int display = 1; display < requiredDisplays; display++) Display.displays[display].Activate();
            }
            else
            {
                var first = CreateDisplay(0).GetComponent<RectTransform>();
                if (mode == OutputMode.Preview) first = CreatePreviewArea(first, calibration);
                if (mode == OutputMode.Dual) Display.displays[1].Activate();
                if (mode != OutputMode.B)
                    CreateBank(calibration, 0, first, 0, mode == OutputMode.Preview ? new Rect(0, .5f, 1, .5f) : new Rect(0, 0, 1, 1), channelSize, screenLayer);
                if (mode != OutputMode.A)
                {
                    int display = mode == OutputMode.Dual ? 1 : 0;
                    var second = display == 1 ? CreateDisplay(display).GetComponent<RectTransform>() : first;
                    CreateBank(calibration, 1, second, display, mode == OutputMode.Preview ? new Rect(0, 0, 1, .5f) : new Rect(0, 0, 1, 1), channelSize, screenLayer);
                }
            }

            foreach (var camera in desktop.walls) if (camera) camera.enabled = false;
            desktopProjection = desktop;
            if (input) input.LargeSpace = this;
            if (hud)
            {
                originalHudEnabled = hud.enabled;
                hud.enabled = false;
            }
            if (mode == OutputMode.Emp)
                Debug.Log($"Large Space: EMP, {requiredDisplays} displays, {outputs.Count} image slots from {calibration.channels.Length} mono projector views, observer {physicalEye}.");
            else
                Debug.Log($"Large Space: {mode}, {outputs.Count} calibrated channels, observer {physicalEye}.");
        }

        void CreateScreen(Mesh mesh, Shader shader, int layer)
        {
            GameObject screen = new("Calibrated Large Space screen", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            screen.transform.SetParent(transform, false);
            screen.layer = layer;
            screen.GetComponent<MeshFilter>().sharedMesh = mesh;
            screenCollider = screen.GetComponent<MeshCollider>();
            screenCollider.sharedMesh = mesh;
            var screenRenderer = screen.GetComponent<MeshRenderer>();
            screenRenderer.shadowCastingMode = ShadowCastingMode.Off;
            screenRenderer.receiveShadows = false;
            screenMaterial = new Material(shader);
            screenMaterial.SetFloat("_CaptureTangent", Mathf.Tan(CaptureFov * .5f * Mathf.Deg2Rad));
            screenMaterial.SetVector("_PhysicalEye", physicalEye);
            screenRenderer.sharedMaterial = screenMaterial;
        }

        void CreateCaptureCameras(Camera source, int screenLayer)
        {
            bool renderShadows = !source.TryGetComponent<UniversalAdditionalCameraData>(out var sourceData) || sourceData.renderShadows;
            for (int i = 0; i < capture.Length; i++)
            {
                Camera camera = CreateCamera($"Reef view {i + 1}");
                camera.CopyFrom(source);
                camera.tag = "Untagged";
                camera.enabled = true;
                camera.ResetProjectionMatrix();
                camera.ResetWorldToCameraMatrix();
                camera.ResetCullingMatrix();
                camera.rect = new Rect(0, 0, 1, 1);
                // URP sorts cameras by depth. Fill the observer views before any projector samples them.
                camera.depth = -100;
                camera.aspect = 1;
                camera.fieldOfView = CaptureFov;
                camera.cullingMask &= ~(1 << screenLayer);
                camera.GetUniversalAdditionalCameraData().renderShadows = renderShadows;
                camera.transform.SetPositionAndRotation(room.virtualEye, FaceRotations[i]);
                camera.targetTexture = CreateTexture(camera.name, CaptureSize, CaptureSize);
                screenMaterial.SetTexture(FaceTextures[i], camera.targetTexture);
                capture[i] = camera;
            }
        }

        Canvas CreateDisplay(int display)
        {
            Camera background = CreateCamera($"Large Space display {display}");
            background.depth = 0;
            background.targetDisplay = display;
            background.cullingMask = 0;

            var canvas = new GameObject($"Projection output {display}", typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = display;
            return canvas;
        }

        static RectTransform CreatePreviewArea(RectTransform parent, LargeSpaceCalibration calibration)
        {
            var fitter = new GameObject("Undistorted channel layout", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<AspectRatioFitter>();
            fitter.transform.SetParent(parent, false);
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = LargeSpaceCalibration.ChannelsPerBank * calibration.channelPixelWidth / (2f * calibration.channelPixelHeight);
            return fitter.GetComponent<RectTransform>();
        }

        void CreateBank(LargeSpaceCalibration calibration, int bank, RectTransform parent, int display, Rect area, Vector2Int size, int layer)
        {
            int offset = bank * LargeSpaceCalibration.ChannelsPerBank;
            for (int i = 0; i < LargeSpaceCalibration.ChannelsPerBank; i++)
            {
                var channel = calibration.channels[offset + i];
                string name = $"Projector {(bank == 0 ? 'A' : 'B')}{i + 1}";
                var (camera, mask) = CreateProjector(channel, name, size, layer);
                Rect viewport = channel.Viewport;
                var rect = new Rect(area.min + Vector2.Scale(viewport.min, area.size), Vector2.Scale(viewport.size, area.size));
                AddOutput(name, parent, display, rect, camera, mask);
            }
        }

        void CreateEmpOutputs(LargeSpaceCalibration calibration, Vector2Int size, int layer)
        {
            for (int display = 0; display < EmpProjectors.GetLength(0); display++)
            {
                var parent = CreateDisplay(display).GetComponent<RectTransform>();
                for (int column = 0; column < EmpProjectors.GetLength(1); column++)
                {
                    int projector = EmpProjectors[display, column];
                    string name = $"EMP projector {projector}";
                    var (camera, mask) = CreateProjector(calibration.channels[projector], name, size, layer);
                    // Use the site's exact normalized viewports. Both eye slots share the mono view.
                    float x = column == 0 ? .1354167f : .5f;
                    AddOutput(name + " right eye slot", parent, display, new Rect(x, .5f, .3645833f, .4861111f), camera, mask);
                    AddOutput(name + " left eye slot", parent, display, new Rect(x, .01388889f, .3645833f, .4861111f), camera, mask);
                }
            }
        }

        (Camera camera, Texture2D mask) CreateProjector(LargeSpaceCalibration.Channel channel, string name, Vector2Int size, int layer)
        {
            var mask = Resources.Load<Texture2D>(LargeSpaceCalibration.ResourcePath + "Masks/" + channel.mask);
            if (!mask) throw new InvalidOperationException($"Missing blend mask '{channel.mask}' for {name}.");

            Camera camera = CreateCamera(name);
            camera.depth = -50;
            camera.cullingMask = 1 << layer;
            camera.useOcclusionCulling = false;
            camera.targetTexture = CreateTexture(name, size.x, size.y);
            channel.Configure(camera);
            return (camera, mask);
        }

        void AddOutput(string name, RectTransform parent, int display, Rect rect, Camera camera, Texture2D mask)
        {
            var panel = CreateImage(name, parent, camera.targetTexture);
            panel.anchorMin = rect.min;
            panel.anchorMax = rect.max;
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            // Blend the mask over the entire projector viewport, including its clear color.
            var overlay = CreateImage("Blend mask", panel, mask);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            outputs.Add(new Output { camera = camera, panel = panel, display = display });
        }

        static RectTransform CreateImage(string name, RectTransform parent, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.raycastTarget = false;
            return image.rectTransform;
        }

        Camera CreateCamera(string cameraName)
        {
            var camera = new GameObject(cameraName, typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.allowXRRendering = false;
            return camera;
        }

        RenderTexture CreateTexture(string textureName, int width, int height)
        {
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            textures.Add(texture);
            return texture;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (hud && keyboard.hKey.wasPressedThisFrame) hud.enabled = !hud.enabled;
            if (!experiment || experiment.Running) return;
            if (keyboard.enterKey.wasPressedThisFrame) experiment.Begin();
            if (!experiment.Running && keyboard.rKey.wasPressedThisFrame) experiment.ResetSchool();
        }

        void LateUpdate()
        {
            foreach (var camera in capture) camera.transform.position = room.virtualEye;
            if (input && experiment)
            {
                screenMaterial.SetVector("_AimA", Reticle(0));
                screenMaterial.SetVector("_AimB", Reticle(1));
            }
        }

        Vector4 Reticle(int player)
        {
            Vector3 direction = input.Aim(player).direction;
            return new Vector4(direction.x, direction.y, direction.z, experiment.Running && input.Valid(player) ? 1 : 0);
        }

        internal bool TryPointerRay(Vector2 point, int display, out Ray ray)
        {
            foreach (var output in outputs)
            {
                if (output.display != display || !RectTransformUtility.ScreenPointToLocalPointInRectangle(output.panel, point, null, out var local)) continue;
                Rect rect = output.panel.rect;
                if (rect.width <= 0 || rect.height <= 0 || !rect.Contains(local)) continue;
                var uv = Rect.PointToNormalized(rect, local);
                Ray projectorRay = output.camera.ViewportPointToRay(uv);
                bool previousBackfaceSetting = Physics.queriesHitBackfaces;
                try
                {
                    Physics.queriesHitBackfaces = true;
                    if (screenCollider.Raycast(projectorRay, out var hit, output.camera.farClipPlane))
                    {
                        ray = new Ray(room.virtualEye, (hit.point - physicalEye).normalized);
                        return true;
                    }
                }
                finally { Physics.queriesHitBackfaces = previousBackfaceSetting; }
            }
            ray = default;
            return false;
        }

        void OnDestroy()
        {
            if (desktopProjection)
            {
                if (input) input.LargeSpace = null;
                if (hud) hud.enabled = originalHudEnabled;
                desktopProjection.enabled = true;
                desktopProjection.Apply();
            }
            if (screenMaterial) Destroy(screenMaterial);
            foreach (var texture in textures) Destroy(texture);
        }

        static string ReadArgument(string[] args, int index) =>
            index >= 0 && index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal)
                ? args[index + 1] : null;

        static int ReadChannelWidth(string[] args, int fallback)
        {
            int index = Array.IndexOf(args, "-pbr-large-space-channel-width");
            if (index < 0) return fallback;
            if (int.TryParse(ReadArgument(args, index), out int width) && width >= 256 && width <= 3840) return width;
            throw new ArgumentException("Large Space channel width must be between 256 and 3840 pixels.");
        }

        static Vector3 ReadEye(string[] args, Vector3 fallback)
        {
            int index = Array.IndexOf(args, "-pbr-large-space-eye");
            if (index < 0) return fallback;
            var values = index + 1 < args.Length ? args[index + 1].Split(',') : Array.Empty<string>();
            var eye = Vector3.zero;
            if (values.Length != 3) throw new ArgumentException("Large Space eye position requires x,y,z in meters.");
            for (int i = 0; i < values.Length; i++)
            {
                if (!float.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
                    throw new ArgumentException("Large Space eye position requires finite x,y,z coordinates in meters.");
                eye[i] = value;
            }
            return eye;
        }
    }
}
