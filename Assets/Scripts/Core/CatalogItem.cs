using System;

namespace RoomDecor.Core
{
    /// <summary>
    /// One product as it comes from a retailer's affiliate feed. Dimensions
    /// and color drive the generic 3D shape; the app never uses the retailer's
    /// own 3D model. The photo is the feed's official product image, shown
    /// as supplied (never cut out, edited or placed into the 3D room).
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

        /// <summary>The feed's product photo, or empty when the feed has none.</summary>
        public string ImageUrl { get; }

        /// <summary>The product's main color as #RRGGBB, used to color its 3D shape; empty when unknown.</summary>
        public string ColorHex { get; }

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
            string buyUrl,
            string imageUrl = "",
            string colorHex = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
            if (widthMeters <= 0 || depthMeters <= 0 || heightMeters <= 0)
                throw new ArgumentOutOfRangeException(nameof(widthMeters), "Dimensions must be positive.");
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Product photos must load over https.", nameof(imageUrl));
            if (!string.IsNullOrEmpty(colorHex) && !IsHexColor(colorHex))
                throw new ArgumentException("Color must look like #A1B2C3.", nameof(colorHex));

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
            ImageUrl = imageUrl ?? "";
            ColorHex = colorHex ?? "";
        }

        static bool IsHexColor(string value)
        {
            if (value.Length != 7 || value[0] != '#') return false;
            for (int i = 1; i < 7; i++)
            {
                if (!Uri.IsHexDigit(value[i])) return false;
            }
            return true;
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
