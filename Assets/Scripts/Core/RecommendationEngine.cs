using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomDecor.Core
{
    public enum Priority
    {
        Essential,
        NiceToHave,
    }

    public sealed class Recommendation
    {
        public FurnitureCategory Category { get; }
        public Priority Priority { get; }

        /// <summary>Catalog items in this category that fit the room, cheapest first. Empty when nothing fits.</summary>
        public IReadOnlyList<CatalogItem> Items { get; }

        public Recommendation(FurnitureCategory category, Priority priority, IReadOnlyList<CatalogItem> items)
        {
            Category = category;
            Priority = priority;
            Items = items;
        }
    }

    /// <summary>
    /// Suggests what to buy for a room. Version one is a hand-written rules
    /// table per room type; later versions can re-rank by what users add.
    /// </summary>
    public static class RecommendationEngine
    {
        static readonly Dictionary<RoomType, (FurnitureCategory Category, Priority Priority)[]> Rules =
            new Dictionary<RoomType, (FurnitureCategory, Priority)[]>
            {
                [RoomType.Bedroom] = new[]
                {
                    (FurnitureCategory.BedFrame, Priority.Essential),
                    (FurnitureCategory.Mattress, Priority.Essential),
                    (FurnitureCategory.Nightstand, Priority.Essential),
                    (FurnitureCategory.Dresser, Priority.Essential),
                    (FurnitureCategory.Rug, Priority.NiceToHave),
                    (FurnitureCategory.Lamp, Priority.NiceToHave),
                    (FurnitureCategory.Headboard, Priority.NiceToHave),
                    (FurnitureCategory.Mirror, Priority.NiceToHave),
                    (FurnitureCategory.Curtains, Priority.NiceToHave),
                },
                [RoomType.LivingRoom] = new[]
                {
                    (FurnitureCategory.Sofa, Priority.Essential),
                    (FurnitureCategory.CoffeeTable, Priority.Essential),
                    (FurnitureCategory.Rug, Priority.Essential),
                    (FurnitureCategory.TvStand, Priority.NiceToHave),
                    (FurnitureCategory.Armchair, Priority.NiceToHave),
                    (FurnitureCategory.SideTable, Priority.NiceToHave),
                    (FurnitureCategory.FloorLamp, Priority.NiceToHave),
                    (FurnitureCategory.Bookshelf, Priority.NiceToHave),
                },
                [RoomType.DiningRoom] = new[]
                {
                    (FurnitureCategory.DiningTable, Priority.Essential),
                    (FurnitureCategory.DiningChair, Priority.Essential),
                    (FurnitureCategory.Sideboard, Priority.NiceToHave),
                    (FurnitureCategory.PendantLight, Priority.NiceToHave),
                    (FurnitureCategory.Rug, Priority.NiceToHave),
                },
                [RoomType.HomeOffice] = new[]
                {
                    (FurnitureCategory.Desk, Priority.Essential),
                    (FurnitureCategory.OfficeChair, Priority.Essential),
                    (FurnitureCategory.Bookshelf, Priority.NiceToHave),
                    (FurnitureCategory.DeskLamp, Priority.NiceToHave),
                    (FurnitureCategory.FilingCabinet, Priority.NiceToHave),
                },
                [RoomType.KidsRoom] = new[]
                {
                    (FurnitureCategory.BedFrame, Priority.Essential),
                    (FurnitureCategory.Mattress, Priority.Essential),
                    (FurnitureCategory.Storage, Priority.Essential),
                    (FurnitureCategory.Desk, Priority.NiceToHave),
                    (FurnitureCategory.Rug, Priority.NiceToHave),
                    (FurnitureCategory.ToyStorage, Priority.NiceToHave),
                },
            };

        /// <summary>The categories to suggest for a room type, essentials first, in display order.</summary>
        public static IReadOnlyList<(FurnitureCategory Category, Priority Priority)> CategoriesFor(RoomType type)
        {
            if (!Rules.TryGetValue(type, out var rules))
                throw new ArgumentOutOfRangeException(nameof(type), type, "No recommendation rules for this room type.");
            return rules;
        }

        public static IReadOnlyList<Recommendation> Recommend(Room room, IEnumerable<CatalogItem> catalog)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var byCategory = catalog.ToLookup(item => item.Category);
            return CategoriesFor(room.Type)
                .Select(rule => new Recommendation(
                    rule.Category,
                    rule.Priority,
                    byCategory[rule.Category]
                        .Where(item => item.FitsIn(room))
                        .OrderBy(item => item.Price)
                        .ThenBy(item => item.Name, StringComparer.Ordinal)
                        .ToList()))
                .ToList();
        }
    }
}
