using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The god camera: tilted down by <see cref="Pitch"/> degrees, looking at the island centre from the south, backed
    /// off until the whole island fits the screen at any aspect (16:9 included). View only.
    /// </summary>
    public static class IslandCamera
    {
        /// <summary>Degrees the camera looks down from the horizon.</summary>
        public const float Pitch = 50f;
        public const float FieldOfView = 30f;

        /// <summary>Places a camera so a circle of the given radius (m) round the origin fits the view.</summary>
        public static void Frame(Camera cam, float islandRadius)
        {
            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            cam.fieldOfView = FieldOfView;
            float vHalf = FieldOfView * 0.5f * Mathf.Deg2Rad;
            float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * aspect);
            // The circle seen from a pitched camera is a squashed ellipse: sin(pitch) tall, 1 wide, so the width limits first
            // on a wide screen and the height on a tall one.
            float pitch = Pitch * Mathf.Deg2Rad;
            float needW = islandRadius / Mathf.Tan(hHalf);
            float needH = islandRadius * Mathf.Sin(pitch) / Mathf.Tan(vHalf) + islandRadius * Mathf.Cos(pitch) * 0.5f;
            float dist = Mathf.Max(needW, needH) * 1.04f;
            var dir = new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch));
            cam.transform.position = dir * dist;
            cam.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            cam.nearClipPlane = 5f;
            cam.farClipPlane = dist + islandRadius * 4f;
        }
    }
}
