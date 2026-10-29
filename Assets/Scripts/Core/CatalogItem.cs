using System;

namespace RoomDecor.Core
{
    /// <summary>
    /// One product as it comes from a retailer's affiliate feed. Dimensions
    /// drive the generic 3D shape; the app never uses the retailer's own 3D model.
    /// </summary>
    public sealed class CatalogItem
    {
        public string Id { get; }
        public string Name { get; }
        public FurnitureCategory Category { get; }
        public string Store { get; }
        public decimal Price { get; }
        public string Currency { get; }
        public DateTime PriceCheckedUtc { get; }
        public double WidthMeters { get; }
        public double DepthMeters { get; }
        public double HeightMeters { get; }
        public string BuyUrl { get; }

        public CatalogItem(
            string id,
            string name,
            FurnitureCategory category,
            string store,
            decimal price,
            string currency,
            DateTime priceCheckedUtc,
            double widthMeters,
            double depthMeters,
            double heightMeters,
            string buyUrl)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
            if (widthMeters <= 0 || depthMeters <= 0 || heightMeters <= 0)
                throw new ArgumentOutOfRangeException(nameof(widthMeters), "Dimensions must be positive.");

            Id = id;
            Name = name ?? id;
            Category = category;
            Store = store ?? "";
            Price = price;
            Currency = currency ?? "USD";
            PriceCheckedUtc = priceCheckedUtc;
            WidthMeters = widthMeters;
            DepthMeters = depthMeters;
            HeightMeters = heightMeters;
            BuyUrl = buyUrl ?? "";
        }

        /// <summary>True if the item fits on the room's floor in either orientation and under the ceiling.</summary>
        public bool FitsIn(Room room)
        {
            if (HeightMeters > room.HeightMeters) return false;
            bool straight = WidthMeters <= room.WidthMeters && DepthMeters <= room.LengthMeters;
            bool turned = DepthMeters <= room.WidthMeters && WidthMeters <= room.LengthMeters;
            return straight || turned;
        }
    }
}
