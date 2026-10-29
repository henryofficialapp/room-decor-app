using System;
using RoomDecor.Core;
using Xunit;

namespace RoomDecor.Core.Tests
{
    public class CatalogItemTests
    {
        static CatalogItem Make(string imageUrl = "", string colorHex = "") =>
            new CatalogItem("bed", "Bed", FurnitureCategory.BedFrame, "Store", 500m, "USD", TestCatalog.Checked,
                1.6, 2.1, 1.0, "https://example.com/bed", imageUrl, colorHex);

        [Fact]
        public void KeepsPhotoAndColorFromTheFeed()
        {
            var item = Make("https://images.example.com/bed.jpg", "#8C613E");

            Assert.Equal("https://images.example.com/bed.jpg", item.ImageUrl);
            Assert.Equal("#8C613E", item.ColorHex);
        }

        [Fact]
        public void PhotoAndColorAreOptional()
        {
            var item = Make();

            Assert.Equal("", item.ImageUrl);
            Assert.Equal("", item.ColorHex);
        }

        [Fact]
        public void RefusesPhotosThatAreNotHttps()
        {
            Assert.Throws<ArgumentException>(() => Make(imageUrl: "http://images.example.com/bed.jpg"));
        }

        [Theory]
        [InlineData("8C613E")]
        [InlineData("#8C613")]
        [InlineData("#GG613E")]
        public void RefusesMalformedColors(string color)
        {
            Assert.Throws<ArgumentException>(() => Make(colorHex: color));
        }
    }
}
