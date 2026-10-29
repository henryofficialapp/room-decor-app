using System.Linq;
using RoomDecor.Core;
using Xunit;
using static RoomDecor.Core.Tests.TestCatalog;

namespace RoomDecor.Core.Tests
{
    public class RecommendationEngineTests
    {
        [Fact]
        public void BedroomStartsWithBedFrameThenMattress()
        {
            var categories = RecommendationEngine.CategoriesFor(RoomType.Bedroom);

            Assert.Equal(FurnitureCategory.BedFrame, categories[0].Category);
            Assert.Equal(FurnitureCategory.Mattress, categories[1].Category);
            Assert.Equal(Priority.Essential, categories[0].Priority);
        }

        [Fact]
        public void LivingRoomStartsWithSofaCoffeeTableAndRug()
        {
            var firstThree = RecommendationEngine.CategoriesFor(RoomType.LivingRoom)
                .Take(3).Select(rule => rule.Category);

            Assert.Equal(
                new[] { FurnitureCategory.Sofa, FurnitureCategory.CoffeeTable, FurnitureCategory.Rug },
                firstThree);
        }

        [Theory]
        [InlineData(RoomType.Bedroom)]
        [InlineData(RoomType.LivingRoom)]
        [InlineData(RoomType.DiningRoom)]
        [InlineData(RoomType.HomeOffice)]
        [InlineData(RoomType.KidsRoom)]
        public void EssentialsAlwaysComeBeforeNiceToHaves(RoomType type)
        {
            var priorities = RecommendationEngine.CategoriesFor(type).Select(rule => rule.Priority).ToList();
            var sorted = priorities.OrderBy(p => p).ToList();

            Assert.Equal(sorted, priorities);
            Assert.Contains(Priority.Essential, priorities);
        }

        [Fact]
        public void LeavesOutItemsTooBigForTheRoomAndSortsCheapestFirst()
        {
            var room = new Room(RoomType.Bedroom, 3.0, 3.5, 2.4);
            var catalog = new[]
            {
                Item("king", FurnitureCategory.BedFrame, price: 900m, width: 2.0, depth: 2.2),
                Item("queen", FurnitureCategory.BedFrame, price: 600m, width: 1.6, depth: 2.1),
                Item("giant", FurnitureCategory.BedFrame, price: 300m, width: 3.2, depth: 3.6),
                Item("sofa", FurnitureCategory.Sofa),
            };

            var bedFrames = RecommendationEngine.Recommend(room, catalog)
                .Single(r => r.Category == FurnitureCategory.BedFrame);

            Assert.Equal(new[] { "queen", "king" }, bedFrames.Items.Select(i => i.Id));
        }

        [Fact]
        public void ItemThatOnlyFitsTurnedSidewaysStillCounts()
        {
            var room = new Room(RoomType.LivingRoom, 2.0, 4.0, 2.4);
            var longSofa = Item("long", FurnitureCategory.Sofa, width: 3.0, depth: 0.9);

            Assert.True(longSofa.FitsIn(room));
        }

        [Fact]
        public void CategoryWithNoFittingItemsIsStillListed()
        {
            var room = new Room(RoomType.DiningRoom, 3.0, 3.0, 2.4);

            var recommendations = RecommendationEngine.Recommend(room, new CatalogItem[0]);

            Assert.Equal(FurnitureCategory.DiningTable, recommendations[0].Category);
            Assert.Empty(recommendations[0].Items);
        }

        [Fact]
        public void ItemTallerThanCeilingIsLeftOut()
        {
            var room = new Room(RoomType.HomeOffice, 3.0, 3.0, 2.2);
            var tallShelf = Item("tall", FurnitureCategory.Bookshelf, height: 2.4);

            Assert.False(tallShelf.FitsIn(room));
        }
    }
}
