using System.Collections.Generic;
using RoomDecor.Core;
using UnityEngine;

namespace RoomDecor.App
{
    /// <summary>
    /// The 3D stand-in for one placed item: a generic shape for its category,
    /// scaled to the product's real width, depth and height and painted in its
    /// color from the feed. We draw our own shapes instead of using retailers'
    /// 3D models or photos, which are their copyright.
    /// The item's back faces -Z before rotation, so it sits against a wall at 0 degrees.
    /// </summary>
    public sealed class FurnitureView : MonoBehaviour
    {
        public PlacedItem Placed { get; private set; }

        readonly List<(Renderer Renderer, Color Color)> parts = new List<(Renderer, Color)>();

        public static FurnitureView Create(PlacedItem placed, Transform parent)
        {
            var root = new GameObject(placed.Item.Name);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<FurnitureView>();
            view.Placed = placed;

            var item = placed.Item;
            float w = (float)item.WidthMeters, d = (float)item.DepthMeters, h = (float)item.HeightMeters;
            view.BuildShape(item.Category, w, d, h, MainColor(item));

            // One collider over the whole footprint makes the item easy to tap.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, Mathf.Max(h, 0.05f) / 2, 0);
            collider.size = new Vector3(w, Mathf.Max(h, 0.05f), d);

            view.SyncTransform();
            return view;
        }

        public void SyncTransform()
        {
            transform.localPosition = new Vector3((float)Placed.X, 0, (float)Placed.Z);
            transform.localRotation = Quaternion.Euler(0, Placed.RotationDegrees, 0);
        }

        /// <summary>Tints the whole item, or restores its own colors when tint is null.</summary>
        public void SetTint(Color? tint)
        {
            foreach (var (renderer, color) in parts)
                renderer.material.color = tint.HasValue ? Color.Lerp(color, tint.Value, 0.6f) : color;
        }

        /// <summary>The feed's color when it has one, else a plain default for the category.</summary>
        static Color MainColor(CatalogItem item)
        {
            if (!string.IsNullOrEmpty(item.ColorHex) && ColorUtility.TryParseHtmlString(item.ColorHex, out var color))
                return color;
            switch (item.Category)
            {
                case FurnitureCategory.Sofa:
                case FurnitureCategory.Armchair:
                    return Palette.Fabric;
                case FurnitureCategory.Rug:
                    return Palette.Rug;
                default:
                    return Palette.Wood;
            }
        }

        void BuildShape(FurnitureCategory category, float w, float d, float h, Color main)
        {
            switch (category)
            {
                case FurnitureCategory.BedFrame:
                {
                    float baseH = Mathf.Min(0.3f, h);
                    Box("Base", 0, baseH / 2, 0, w, baseH, d, main);
                    Box("Bedding", 0, baseH + 0.1f, 0.05f, w * 0.96f, 0.2f, d * 0.9f, Palette.Bedding);
                    Box("Headboard", 0, h / 2, -d / 2 + 0.04f, w, h, 0.08f, main);
                    break;
                }
                case FurnitureCategory.Sofa:
                case FurnitureCategory.Armchair:
                {
                    float seatH = h * 0.45f, armW = Mathf.Min(0.15f, w * 0.15f), backD = d * 0.25f;
                    Box("Seat", 0, seatH / 2, backD / 2, w, seatH, d - backD, main);
                    Box("Back", 0, h / 2, -d / 2 + backD / 2, w, h, backD, main);
                    Box("Arm L", -w / 2 + armW / 2, h * 0.65f / 2, 0, armW, h * 0.65f, d, main);
                    Box("Arm R", w / 2 - armW / 2, h * 0.65f / 2, 0, armW, h * 0.65f, d, main);
                    break;
                }
                case FurnitureCategory.CoffeeTable:
                case FurnitureCategory.DiningTable:
                case FurnitureCategory.Desk:
                case FurnitureCategory.SideTable:
                    Table(w, d, h, main, main);
                    break;
                case FurnitureCategory.DiningChair:
                case FurnitureCategory.OfficeChair:
                {
                    float seatH = Mathf.Min(0.47f, h * 0.5f);
                    Table(w, d, seatH, main, Palette.Wood);
                    Box("Back", 0, (seatH + h) / 2, -d / 2 + 0.03f, w, h - seatH, 0.05f, main);
                    break;
                }
                case FurnitureCategory.Rug:
                    Box("Rug", 0, 0.005f, 0, w, 0.01f, d, main);
                    break;
                case FurnitureCategory.Lamp:
                case FurnitureCategory.FloorLamp:
                {
                    float shadeH = Mathf.Min(0.35f, h * 0.3f);
                    Shapes.Cylinder(transform, "Base", new Vector3(0, 0.015f, 0), Mathf.Min(w, d) * 0.8f, 0.03f, Palette.Metal);
                    Shapes.Cylinder(transform, "Pole", new Vector3(0, (h - shadeH) / 2, 0), 0.03f, h - shadeH, Palette.Metal);
                    Shapes.Cylinder(transform, "Shade", new Vector3(0, h - shadeH / 2, 0), Mathf.Min(w, d), shadeH, Palette.Shade);
                    break;
                }
                default:
                    // Cabinets, shelves, dressers and anything new: a plain box.
                    Box("Body", 0, h / 2, 0, w, h, d, main);
                    break;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>())
                parts.Add((renderer, renderer.material.color));
        }

        void Table(float w, float d, float h, Color topColor, Color legColor)
        {
            const float top = 0.04f, leg = 0.05f;
            Box("Top", 0, h - top / 2, 0, w, top, d, topColor);
            float lx = w / 2 - leg / 2, lz = d / 2 - leg / 2, lh = h - top;
            Box("Leg 1", -lx, lh / 2, -lz, leg, lh, leg, legColor);
            Box("Leg 2", lx, lh / 2, -lz, leg, lh, leg, legColor);
            Box("Leg 3", -lx, lh / 2, lz, leg, lh, leg, legColor);
            Box("Leg 4", lx, lh / 2, lz, leg, lh, leg, legColor);
        }

        void Box(string name, float x, float y, float z, float sx, float sy, float sz, Color color) =>
            Shapes.Box(transform, name, new Vector3(x, y, z), new Vector3(sx, sy, sz), color);
    }
}
