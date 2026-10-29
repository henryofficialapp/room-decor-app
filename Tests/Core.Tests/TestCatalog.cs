using System;
using RoomDecor.Core;

namespace RoomDecor.Core.Tests
{
    static class TestCatalog
    {
        public static readonly DateTime Checked = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        public static CatalogItem Item(
            string id,
            FurnitureCategory category,
            decimal price = 100m,
            double width = 1.0,
            double depth = 1.0,
            double height = 1.0,
            string store = "Sample Store",
            string currency = "USD",
            DateTime? priceChecked = null) =>
            new CatalogItem(id, id, category, store, price, currency, priceChecked ?? Checked,
                width, depth, height, "https://example.com/" + id);
    }
}
