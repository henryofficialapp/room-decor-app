using RoomDecor.Core;
using UnityEngine;

namespace RoomDecor.App
{
    /// <summary>A wall and the direction it faces out of the room.</summary>
    public struct Wall
    {
        public Renderer Renderer;
        public Vector3 OutwardNormal;
    }

    /// <summary>Draws the floor and walls for a room. Walls are thin boxes just outside the floor.</summary>
    public static class RoomView
    {
        const float WallThickness = 0.1f;

        public static GameObject Build(Room room, out Wall[] walls)
        {
            var root = new GameObject("Room");
            float w = (float)room.WidthMeters, l = (float)room.LengthMeters, h = (float)room.HeightMeters;

            Shapes.Box(root.transform, "Floor", new Vector3(w / 2, -0.01f, l / 2), new Vector3(w, 0.02f, l), Palette.Floor);

            // The orbit camera hides whichever walls stand between it and the room (see CameraRig).
            walls = new[]
            {
                MakeWall(root, "Wall South", new Vector3(w / 2, h / 2, -WallThickness / 2),
                    new Vector3(w + 2 * WallThickness, h, WallThickness), Vector3.back),
                MakeWall(root, "Wall North", new Vector3(w / 2, h / 2, l + WallThickness / 2),
                    new Vector3(w + 2 * WallThickness, h, WallThickness), Vector3.forward),
                MakeWall(root, "Wall West", new Vector3(-WallThickness / 2, h / 2, l / 2),
                    new Vector3(WallThickness, h, l), Vector3.left),
                MakeWall(root, "Wall East", new Vector3(w + WallThickness / 2, h / 2, l / 2),
                    new Vector3(WallThickness, h, l), Vector3.right),
            };
            return root;
        }

        static Wall MakeWall(GameObject root, string name, Vector3 center, Vector3 size, Vector3 outward) =>
            new Wall
            {
                Renderer = Shapes.Box(root.transform, name, center, size, Palette.Wall).GetComponent<Renderer>(),
                OutwardNormal = outward,
            };
    }

    static class Palette
    {
        public static readonly Color Floor = new Color(0.78f, 0.70f, 0.60f);
        public static readonly Color Wall = new Color(0.93f, 0.92f, 0.89f);
        public static readonly Color Wood = new Color(0.55f, 0.38f, 0.24f);
        public static readonly Color Fabric = new Color(0.42f, 0.50f, 0.58f);
        public static readonly Color Bedding = new Color(0.95f, 0.95f, 0.93f);
        public static readonly Color Rug = new Color(0.70f, 0.45f, 0.40f);
        public static readonly Color Metal = new Color(0.30f, 0.30f, 0.32f);
        public static readonly Color Shade = new Color(0.98f, 0.93f, 0.80f);
        public static readonly Color Selected = new Color(0.30f, 0.60f, 1.00f);
        public static readonly Color Problem = new Color(0.90f, 0.25f, 0.20f);
    }

    static class Shapes
    {
        /// <summary>A box primitive without its own collider; furniture roots carry one collider each.</summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 center, float diameter, float height, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            // A Unity cylinder is 2 units tall, so half the height goes in Y scale.
            go.transform.localScale = new Vector3(diameter, height / 2, diameter);
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }
    }
}
