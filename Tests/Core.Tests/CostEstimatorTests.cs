using System;
using RoomDecor.Core;
using Xunit;
using static RoomDecor.Core.Tests.TestCatalog;

namespace RoomDecor.Core.Tests
{
    public class CostEstimatorTests
    {
        [Fact]
        public void TotalsItemsAndGroupsRepeatsIntoOneLine()
        {
            var table = Item("table", FurnitureCategory.DiningTable, price: 450m, store: "Store A");
            var chair = Item("chair", FurnitureCategory.DiningChair, price: 79.99m, store: "Store B");

            var estimate = CostEstimator.Estimate(new[] { table, chair, chair, chair, chair }, Checked);

            Assert.Equal(769.96m, estimate.Total);
            Assert.Equal(2, estimate.Lines.Count);
            Assert.Equal(4, estimate.Lines[1].Quantity);
            Assert.Equal(450m, estimate.TotalByStore["Store A"]);
            Assert.Equal(319.96m, estimate.TotalByStore["Store B"]);
        }

        [Fact]
        public void EmptyRoomCostsNothing()
        {
            var estimate = CostEstimator.Estimate(new CatalogItem[0], Checked);

            Assert.Equal(0m, estimate.Total);
            Assert.Null(estimate.OldestPriceCheckUtc);
            Assert.False(estimate.HasStalePrices);
        }

        [Fact]
        public void FlagsPricesOlderThanADay()
        {
            var fresh = Item("fresh", FurnitureCategory.Sofa, priceChecked: Checked);
            var old = Item("old", FurnitureCategory.Rug, priceChecked: Checked.AddDays(-2));

            var estimate = CostEstimator.Estimate(new[] { fresh, old }, Checked.AddHours(1));

            Assert.True(estimate.HasStalePrices);
            Assert.Equal(Checked.AddDays(-2), estimate.OldestPriceCheckUtc);
        }

        [Fact]
        public void FreshPricesAreNotStale()
        {
            var estimate = CostEstimator.Estimate(new[] { Item("sofa", FurnitureCategory.Sofa) }, Checked.AddHours(3));

            Assert.False(estimate.HasStalePrices);
        }

        [Fact]
        public void RefusesToAddDifferentCurrencies()
        {
            var usd = Item("usd", FurnitureCategory.Sofa, currency: "USD");
            var cad = Item("cad", FurnitureCategory.Rug, currency: "CAD");

            Assert.Throws<InvalidOperationException>(() => CostEstimator.Estimate(new[] { usd, cad }, Checked));
        }
    }
}
