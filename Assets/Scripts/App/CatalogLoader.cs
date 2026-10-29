using System;
using System.Collections.Generic;
using System.Globalization;
using RoomDecor.Core;
using UnityEngine;

namespace RoomDecor.App
{
    /// <summary>
    /// Reads the product catalog from JSON. Today that is the bundled sample
    /// file; later it is the app's own server, which syncs retailers'
    /// affiliate feeds. The app never scrapes retailer websites.
    /// </summary>
    public static class CatalogLoader
    {
        public const string SampleResource = "sample_catalog";

        [Serializable]
        class CatalogFile
        {
            public bool isSample;
            public CatalogEntry[] items;
        }

        [Serializable]
        class CatalogEntry
        {
            public string id;
            public string name;
            public string category;
            public string store;
            public string price;
            public string currency;
            public string priceCheckedUtc;
            public float widthMeters;
            public float depthMeters;
            public float heightMeters;
            public string buyUrl;
            public string imageUrl;
            public string colorHex;
        }

        public sealed class Result
        {
            public bool IsSample;
            public List<CatalogItem> Items = new List<CatalogItem>();
        }

        public static Result LoadSample()
        {
            var asset = Resources.Load<TextAsset>(SampleResource);
            if (asset == null)
            {
                Debug.LogError($"Missing Resources/{SampleResource}.json");
                return new Result();
            }
            return Parse(asset.text);
        }

        public static Result Parse(string json)
        {
            var file = JsonUtility.FromJson<CatalogFile>(json);
            var result = new Result { IsSample = file != null && file.isSample };
            if (file?.items == null) return result;

            foreach (var entry in file.items)
            {
                try
                {
                    result.Items.Add(ToItem(entry));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Skipping catalog item '{entry?.id}': {e.Message}");
                }
            }
            return result;
        }

        static CatalogItem ToItem(CatalogEntry entry)
        {
            if (!Enum.TryParse(entry.category, out FurnitureCategory category))
                throw new FormatException($"unknown category '{entry.category}'");

            var price = decimal.Parse(entry.price, NumberStyles.Number, CultureInfo.InvariantCulture);
            var priceChecked = DateTime.Parse(entry.priceCheckedUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            return new CatalogItem(entry.id, entry.name, category, entry.store, price, entry.currency,
                priceChecked, entry.widthMeters, entry.depthMeters, entry.heightMeters, entry.buyUrl,
                entry.imageUrl, entry.colorHex);
        }
    }
}
