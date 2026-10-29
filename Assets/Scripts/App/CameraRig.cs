using RoomDecor.Core;
using UnityEngine;

namespace RoomDecor.App
{
    public enum ViewMode
    {
        Orbit,
        TopDown,
        Walk,
    }

    /// <summary>
    /// Three ways to look at the room: orbit around it, look straight down at
    /// the floor plan, or stand inside it at eye height and look around.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        const float EyeHeight = 1.6f;
        const float WalkSpeed = 1.2f;
        const float LookDegreesPerPixel = 0.25f;

        public ViewMode Mode { get; private set; } = ViewMode.Orbit;

        Camera cam;
        Room room;
        Wall[] walls = new Wall[0];

        float orbitYaw = 210f, orbitPitch = 40f, orbitDistance;
        float walkYaw, walkPitch;
        Vector3 walkPosition;

        public void Init(Camera camera, Room room, Wall[] walls)
        {
            cam = camera;
            this.room = room;
            this.walls = walls;
            orbitDistance = RoomSpan * 1.4f;
            walkPosition = new Vector3((float)room.WidthMeters / 2, EyeHeight, 0.4f);
            walkYaw = 0;
            walkPitch = 0;
            SetMode(ViewMode.Orbit);
        }

        float RoomSpan => Mathf.Max((float)room.WidthMeters, (float)room.LengthMeters);
        Vector3 RoomCenter => new Vector3((float)room.WidthMeters / 2, 0.6f, (float)room.LengthMeters / 2);

        public void SetMode(ViewMode mode)
        {
            Mode = mode;
            cam.orthographic = mode == ViewMode.TopDown;
            Apply();
        }

        /// <summary>One-finger or mouse drag on empty space, in screen pixels.</summary>
        public void Drag(Vector2 delta)
        {
            switch (Mode)
            {
                case ViewMode.Orbit:
                    orbitYaw += delta.x * LookDegreesPerPixel;
                    orbitPitch = Mathf.Clamp(orbitPitch - delta.y * LookDegreesPerPixel, 10f, 85f);
                    break;
                case ViewMode.Walk:
                    walkYaw += delta.x * LookDegreesPerPixel;
                    walkPitch = Mathf.Clamp(walkPitch - delta.y * LookDegreesPerPixel, -60f, 60f);
                    break;
            }
            Apply();
        }

        /// <summary>Pinch or scroll. Positive zooms in.</summary>
        public void Zoom(float amount)
        {
            if (Mode == ViewMode.Walk)
            {
                Walk(amount * 0.05f);
                return;
            }
            orbitDistance = Mathf.Clamp(orbitDistance * (1f - amount * 0.01f), RoomSpan * 0.5f, RoomSpan * 3f);
            Apply();
        }

        /// <summary>Moves forward (positive) or back in walk mode, staying inside the room.</summary>
        public void Walk(float meters)
        {
            if (Mode != ViewMode.Walk) return;
            var forward = Quaternion.Euler(0, walkYaw, 0) * Vector3.forward;
            var next = walkPosition + forward * meters;
            const float margin = 0.2f;
            next.x = Mathf.Clamp(next.x, margin, (float)room.WidthMeters - margin);
            next.z = Mathf.Clamp(next.z, margin, (float)room.LengthMeters - margin);
            walkPosition = next;
            Apply();
        }

        public void WalkForFrame(float direction) => Walk(direction * WalkSpeed * Time.deltaTime);

        void Apply()
        {
            if (cam == null || room == null) return;
            var t = cam.transform;

            switch (Mode)
            {
                case ViewMode.Orbit:
                {
                    var rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0);
                    t.position = RoomCenter - rotation * Vector3.forward * orbitDistance;
                    t.rotation = rotation;
                    break;
                }
                case ViewMode.TopDown:
                    t.position = RoomCenter + Vector3.up * 20f;
                    t.rotation = Quaternion.Euler(90, 0, 0);
                    float aspect = Mathf.Max(cam.aspect, 0.01f);
                    cam.orthographicSize = Mathf.Max((float)room.LengthMeters, (float)room.WidthMeters / aspect) * 0.6f;
                    break;
                case ViewMode.Walk:
                    t.position = walkPosition;
                    t.rotation = Quaternion.Euler(walkPitch, walkYaw, 0);
                    break;
            }

            // From outside, a wall between the camera and the room hides the view, so drop it.
            // Inside (walk) and from above, every wall shows.
            foreach (var wall in walls)
            {
                bool facingCamera = Mode == ViewMode.Orbit
                    && Vector3.Dot(wall.OutwardNormal, t.position - RoomCenter) > 0;
                wall.Renderer.enabled = !facingCamera;
            }
        }
    }
}
