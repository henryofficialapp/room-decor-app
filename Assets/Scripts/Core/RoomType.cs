namespace RoomDecor.Core
{
    public enum RoomType
    {
        Bedroom,
        LivingRoom,
        DiningRoom,
        HomeOffice,
        KidsRoom,
    }

    public enum FurnitureCategory
    {
        BedFrame,
        Mattress,
        Nightstand,
        Dresser,
        Headboard,
        Mirror,
        Curtains,
        Rug,
        Lamp,
        FloorLamp,
        DeskLamp,
        PendantLight,
        Sofa,
        CoffeeTable,
        TvStand,
        Armchair,
        SideTable,
        Bookshelf,
        DiningTable,
        DiningChair,
        Sideboard,
        Desk,
        OfficeChair,
        FilingCabinet,
        Storage,
        ToyStorage,
    }

    public static class FurnitureCategoryInfo
    {
        /// <summary>
        /// Whether the item stands on the floor and is drawn and placed in the
        /// 3D room. Items that sit on or hang from something else (a mattress,
        /// curtains, a ceiling light) count toward the cost but are not placed.
        /// </summary>
        public static bool IsPlacedOnFloor(FurnitureCategory category)
        {
            switch (category)
            {
                case FurnitureCategory.Mattress:
                case FurnitureCategory.Headboard:
                case FurnitureCategory.Mirror:
                case FurnitureCategory.Curtains:
                case FurnitureCategory.PendantLight:
                case FurnitureCategory.DeskLamp:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Rugs lie under other furniture, so they never count as overlapping.</summary>
        public static bool CanOverlap(FurnitureCategory category) => category == FurnitureCategory.Rug;

        public static string DisplayName(FurnitureCategory category)
        {
            switch (category)
            {
                case FurnitureCategory.BedFrame: return "Bed frame";
                case FurnitureCategory.FloorLamp: return "Floor lamp";
                case FurnitureCategory.DeskLamp: return "Desk lamp";
                case FurnitureCategory.PendantLight: return "Pendant light";
                case FurnitureCategory.CoffeeTable: return "Coffee table";
                case FurnitureCategory.TvStand: return "TV stand";
                case FurnitureCategory.SideTable: return "Side table";
                case FurnitureCategory.DiningTable: return "Dining table";
                case FurnitureCategory.DiningChair: return "Dining chair";
                case FurnitureCategory.OfficeChair: return "Office chair";
                case FurnitureCategory.FilingCabinet: return "Filing cabinet";
                case FurnitureCategory.ToyStorage: return "Toy storage";
                default: return category.ToString();
            }
        }
    }
}
