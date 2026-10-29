using System;

namespace RoomDecor.Core
{
    /// <summary>
    /// A rectangular room. Width runs along X, length along Z, and the origin
    /// is the room's floor corner, so the floor spans (0,0) to (Width, Length).
    /// </summary>
    public sealed class Room
    {
        public const double MinSideMeters = 1.5;
        public const double MaxSideMeters = 30.0;
        public const double MinHeightMeters = 2.0;
        public const double MaxHeightMeters = 6.0;

        public RoomType Type { get; }
        public double WidthMeters { get; }
        public double LengthMeters { get; }
        public double HeightMeters { get; }

        public Room(RoomType type, double widthMeters, double lengthMeters, double heightMeters)
        {
            CheckRange(nameof(widthMeters), widthMeters, MinSideMeters, MaxSideMeters);
            CheckRange(nameof(lengthMeters), lengthMeters, MinSideMeters, MaxSideMeters);
            CheckRange(nameof(heightMeters), heightMeters, MinHeightMeters, MaxHeightMeters);

            Type = type;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            HeightMeters = heightMeters;
        }

        public double FloorAreaSquareMeters => WidthMeters * LengthMeters;

        static void CheckRange(string name, double value, double min, double max)
        {
            if (double.IsNaN(value) || value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(name, value,
                    $"Must be between {min} and {max} meters.");
            }
        }
    }

    public static class Units
    {
        public const double MetersPerFoot = 0.3048;

        public static double FeetToMeters(double feet) => feet * MetersPerFoot;
        public static double MetersToFeet(double meters) => meters / MetersPerFoot;
    }
}
