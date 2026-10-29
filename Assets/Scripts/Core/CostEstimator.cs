using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomDecor.Core
{
    public sealed class CostLine
    {
        public CatalogItem Item { get; }
        public int Quantity { get; }
        public decimal Subtotal => Item.Price * Quantity;

        public CostLine(CatalogItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public sealed class CostEstimate
    {
        /// <summary>Shown with every total. Prices come from retailers and change; we never promise them.</summary>
        public const string Disclaimer =
            "Estimate only. Prices are set by each retailer, may have changed since they were last checked, " +
            "and do not include tax, shipping or assembly.";

        public IReadOnlyList<CostLine> Lines { get; }
        public decimal Total { get; }
        public string Currency { get; }
        public IReadOnlyDictionary<string, decimal> TotalByStore { get; }

        /// <summary>The oldest price check among the lines, or null when the estimate is empty.</summary>
        public DateTime? OldestPriceCheckUtc { get; }

        /// <summary>True when any price is older than the staleness limit and should be refreshed before showing as current.</summary>
        public bool HasStalePrices { get; }

        public CostEstimate(IReadOnlyList<CostLine> lines, string currency, DateTime? oldestPriceCheckUtc, bool hasStalePrices)
        {
            Lines = lines;
            Currency = currency;
            Total = lines.Sum(line => line.Subtotal);
            TotalByStore = lines
                .GroupBy(line => line.Item.Store)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Subtotal));
            OldestPriceCheckUtc = oldestPriceCheckUtc;
            HasStalePrices = hasStalePrices;
        }
    }

    public static class CostEstimator
    {
        public static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromHours(24);

        /// <summary>
        /// Totals the chosen items. The same item chosen more than once (four
        /// dining chairs) becomes one line with a quantity.
        /// </summary>
        public static CostEstimate Estimate(IEnumerable<CatalogItem> chosen, DateTime nowUtc, TimeSpan? staleAfter = null)
        {
            if (chosen == null) throw new ArgumentNullException(nameof(chosen));
            var items = chosen.ToList();

            var currencies = items.Select(item => item.Currency).Distinct().ToList();
            if (currencies.Count > 1)
                throw new InvalidOperationException("Cannot total items priced in different currencies: " + string.Join(", ", currencies));

            var lines = items
                .GroupBy(item => item.Id)
                .Select(group => new CostLine(group.First(), group.Count()))
                .ToList();

            DateTime? oldest = items.Count == 0 ? (DateTime?)null : items.Min(item => item.PriceCheckedUtc);
            var limit = staleAfter ?? DefaultStaleAfter;
            bool stale = oldest.HasValue && nowUtc - oldest.Value > limit;

            return new CostEstimate(lines, currencies.FirstOrDefault() ?? "USD", oldest, stale);
        }
    }
}
