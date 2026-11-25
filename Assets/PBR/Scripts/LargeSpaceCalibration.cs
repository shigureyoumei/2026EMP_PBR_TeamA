using System;
using UnityEngine;

namespace PBR
{
    [Serializable]
    internal sealed class LargeSpaceCalibration
    {
        public const int ChannelsPerBank = 6;
        public const string ResourcePath = "PBRLargeSpace/";

        public int channelPixelWidth;
        public int channelPixelHeight;
        public Vector3 observer;
        public Channel[] channels;

        [Serializable]
        internal sealed class Channel
        {
            public Vector3 position;
            public Quaternion rotation;
            public float near;
            public float far;
            public float width;
            public float height;
            public float distance;
            public float verticalOffset;
            public Vector4 viewport;
            public string mask;

            public Rect Viewport => new Rect(viewport.x, viewport.y, viewport.z, viewport.w);

            public void Configure(Camera camera)
            {
                float halfWidth = near * width / distance * .5f;
                float halfHeight = near * height / distance * .5f;
                float shift = halfHeight * verticalOffset;
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.nearClipPlane = near;
                camera.farClipPlane = far;
                camera.projectionMatrix = Matrix4x4.Frustum(-halfWidth, halfWidth,
                    -halfHeight + shift, halfHeight + shift, near, far);
            }
        }
    }
}
