using System;
using System.Collections.Generic;

namespace RoomDecor.Core
{
    /// <summary>An axis-aligned rectangle on the floor, in meters.</summary>
    public readonly struct Footprint
    {
        public readonly double MinX, MinZ, MaxX, MaxZ;

        public Footprint(double minX, double minZ, double maxX, double maxZ)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public bool Overlaps(Footprint other) =>
            MinX < other.MaxX && other.MinX < MaxX && MinZ < other.MaxZ && other.MinZ < MaxZ;
    }

    /// <summary>
    /// An item placed in the room. X and Z are the item's center on the floor.
    /// Rotation snaps to quarter turns, which covers furniture against walls.
    /// </summary>
    public sealed class PlacedItem
    {
        public CatalogItem Item { get; }
        public double X { get; set; }
        public double Z { get; set; }
        public int QuarterTurns { get; private set; }

        public PlacedItem(CatalogItem item, double x, double z, int quarterTurns = 0)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            X = x;
            Z = z;
            QuarterTurns = Normalize(quarterTurns);
        }

        public int RotationDegrees => QuarterTurns * 90;

        public void RotateQuarterTurn() => QuarterTurns = Normalize(QuarterTurns + 1);

        /// <summary>Size along X and Z after rotation.</summary>
        public (double SizeX, double SizeZ) RotatedSize =>
            QuarterTurns % 2 == 0
                ? (Item.WidthMeters, Item.DepthMeters)
                : (Item.DepthMeters, Item.WidthMeters);

        public Footprint Footprint
        {
            get
            {
                var (sizeX, sizeZ) = RotatedSize;
                return new Footprint(X - sizeX / 2, Z - sizeZ / 2, X + sizeX / 2, Z + sizeZ / 2);
            }
        }

        static int Normalize(int turns) => ((turns % 4) + 4) % 4;
    }

    public enum PlacementIssueKind
    {
        OutsideRoom,
        Overlap,
    }

    public sealed class PlacementIssue
    {
        public PlacementIssueKind Kind { get; }
        public PlacedItem Item { get; }

        /// <summary>The other item for an overlap; null otherwise.</summary>
        public PlacedItem Other { get; }

        public PlacementIssue(PlacementIssueKind kind, PlacedItem item, PlacedItem other = null)
        {
            Kind = kind;
            Item = item;
            Other = other;
        }
    }

    public static class PlacementRules
    {
        const double Tolerance = 1e-6;

        public static bool IsInsideRoom(Room room, PlacedItem placed)
        {
            var f = placed.Footprint;
            return f.MinX >= -Tolerance && f.MinZ >= -Tolerance
                && f.MaxX <= room.WidthMeters + Tolerance && f.MaxZ <= room.LengthMeters + Tolerance;
        }

        /// <summary>Moves the item's center so its footprint stays on the floor. Returns false if it is too big to fit at all.</summary>
        public static bool ClampInsideRoom(Room room, PlacedItem placed)
        {
            var (sizeX, sizeZ) = placed.RotatedSize;
            if (sizeX > room.WidthMeters + Tolerance || sizeZ > room.LengthMeters + Tolerance) return false;
            placed.X = Clamp(placed.X, sizeX / 2, room.WidthMeters - sizeX / 2);
            placed.Z = Clamp(placed.Z, sizeZ / 2, room.LengthMeters - sizeZ / 2);
            return true;
        }

        /// <summary>Lists items off the floor and pairs of floor items that overlap. Rugs may sit under anything.</summary>
        public static IReadOnlyList<PlacementIssue> FindIssues(Room room, IReadOnlyList<PlacedItem> placed)
        {
            var issues = new List<PlacementIssue>();
            for (int i = 0; i < placed.Count; i++)
            {
                var a = placed[i];
                if (!IsInsideRoom(room, a)) issues.Add(new PlacementIssue(PlacementIssueKind.OutsideRoom, a));
                if (FurnitureCategoryInfo.CanOverlap(a.Item.Category)) continue;

                for (int j = i + 1; j < placed.Count; j++)
                {
                    var b = placed[j];
                    if (FurnitureCategoryInfo.CanOverlap(b.Item.Category)) continue;
                    if (a.Footprint.Overlaps(b.Footprint))
                        issues.Add(new PlacementIssue(PlacementIssueKind.Overlap, a, b));
                }
            }
            return issues;
        }

        static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
    }
}
