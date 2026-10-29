using System;
using System.Linq;
using RoomDecor.Core;
using Xunit;
using static RoomDecor.Core.Tests.TestCatalog;

namespace RoomDecor.Core.Tests
{
    public class PlacementTests
    {
        static readonly Room Room = new Room(RoomType.LivingRoom, 4.0, 5.0, 2.4);

        [Fact]
        public void RotatingSwapsWidthAndDepth()
        {
            var sofa = new PlacedItem(Item("sofa", FurnitureCategory.Sofa, width: 2.0, depth: 0.9), 2.0, 2.0);

            sofa.RotateQuarterTurn();

            Assert.Equal(90, sofa.RotationDegrees);
            Assert.Equal((0.9, 2.0), sofa.RotatedSize);
        }

        [Fact]
        public void FourQuarterTurnsComeBackToStart()
        {
            var sofa = new PlacedItem(Item("sofa", FurnitureCategory.Sofa), 1, 1);
            for (int i = 0; i < 4; i++) sofa.RotateQuarterTurn();

            Assert.Equal(0, sofa.RotationDegrees);
        }

        [Fact]
        public void ClampPullsItemBackOntoTheFloor()
        {
            var sofa = new PlacedItem(Item("sofa", FurnitureCategory.Sofa, width: 2.0, depth: 0.9), 10.0, -3.0);

            Assert.True(PlacementRules.ClampInsideRoom(Room, sofa));
            Assert.Equal(3.0, sofa.X, 6);
            Assert.Equal(0.45, sofa.Z, 6);
            Assert.True(PlacementRules.IsInsideRoom(Room, sofa));
        }

        [Fact]
        public void ClampFailsWhenItemIsBiggerThanTheRoom()
        {
            var huge = new PlacedItem(Item("huge", FurnitureCategory.Sofa, width: 6.0, depth: 1.0), 2, 2);

            Assert.False(PlacementRules.ClampInsideRoom(Room, huge));
        }

        [Fact]
        public void ReportsOverlapButNotWhenItemsJustTouch()
        {
            var a = new PlacedItem(Item("a", FurnitureCategory.Sofa, width: 2.0, depth: 1.0), 1.0, 1.0);
            var touching = new PlacedItem(Item("b", FurnitureCategory.Armchair, width: 1.0, depth: 1.0), 2.5, 1.0);
            var overlapping = new PlacedItem(Item("c", FurnitureCategory.SideTable, width: 1.0, depth: 1.0), 1.5, 1.5);

            var issues = PlacementRules.FindIssues(Room, new[] { a, touching, overlapping });

            var overlap = Assert.Single(issues);
            Assert.Equal(PlacementIssueKind.Overlap, overlap.Kind);
            Assert.Same(a, overlap.Item);
            Assert.Same(overlapping, overlap.Other);
        }

        [Fact]
        public void RugsMaySitUnderFurniture()
        {
            var rug = new PlacedItem(Item("rug", FurnitureCategory.Rug, width: 2.4, depth: 1.7, height: 0.01), 2.0, 2.5);
            var table = new PlacedItem(Item("table", FurnitureCategory.CoffeeTable), 2.0, 2.5);

            Assert.Empty(PlacementRules.FindIssues(Room, new[] { rug, table }));
        }

        [Fact]
        public void ReportsItemsOffTheFloor()
        {
            var outside = new PlacedItem(Item("lamp", FurnitureCategory.FloorLamp, width: 0.4, depth: 0.4), 4.1, 1.0);

            var issue = Assert.Single(PlacementRules.FindIssues(Room, new[] { outside }));
            Assert.Equal(PlacementIssueKind.OutsideRoom, issue.Kind);
        }

        [Theory]
        [InlineData(1.0, 3, 3)]
        [InlineData(31.0, 3, 3)]
        [InlineData(3, 3, 1.5)]
        public void RoomRejectsImpossibleSizes(double width, double length, double height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Room(RoomType.Bedroom, width, length, height));
        }

        [Fact]
        public void ConvertsFeetToMeters()
        {
            Assert.Equal(3.048, Units.FeetToMeters(10), 6);
            Assert.Equal(10.0, Units.MetersToFeet(3.048), 6);
        }
    }
}
