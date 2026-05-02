using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business
{
    public class AdManager
    {
                private static readonly Lazy<AdManager> CurrentAds = new(LoadFromXml);

        public static AdManager Ads { get; } = CurrentAds.Value;

        ///// <summary>
        ///// Gets the ad configuration loaded from wwwroot/admin/ads.xml.
        ///// </summary>
        //public static AdManager Ads => CurrentAds.Value;

        
        public string BottomPostAd { get; set; }

        public string TopPostAd { get; set; }
        
        public string TopPageAd { get; set; }

        public List<string> ContentAds { get; } = [];

        public List<string> SponsorBanners { get; } = [];

        public string GetFirstContentAd()
        {
            return GetFirstItem(ContentAds);
        }

        public string GetFirstSponsorBanner()
        {
            return GetFirstItem(SponsorBanners);
        }

        public IReadOnlyList<string> GetShuffledContentAds()
        {
            return ShuffleAllButFirst(ContentAds);
        }

        public IReadOnlyList<string> GetShuffledSponsorBanners()
        {
            return ShuffleAllButFirst(SponsorBanners);
        }

        private static AdManager LoadFromXml()
        {
            var webRootFolder = wlApp.WebRootFolder;
            if (string.IsNullOrWhiteSpace(webRootFolder))
                webRootFolder = Path.Combine(Environment.CurrentDirectory, "wwwroot");

            var adsFile = ResolveAdsFile(webRootFolder);
            if (string.IsNullOrWhiteSpace(adsFile))
                return new AdManager();

            var document = XDocument.Load(adsFile, LoadOptions.PreserveWhitespace);
            var root = document.Root;
            if (root == null)
                return new AdManager();

            var manager = new AdManager
            {
                BottomPostAd = GetElementValue(root, nameof(BottomPostAd)),
                TopPostAd = GetElementValue(root, nameof(TopPostAd)),
                TopPageAd = GetElementValue(root, nameof(TopPageAd))
            };

            manager.SponsorBanners.AddRange(ReadCollection(root, "SponsorBanners", "Banner"));
            if (manager.SponsorBanners.Count == 0)
                manager.SponsorBanners.AddRange(ReadLegacyCollection(root, "SponsorBanner"));

            manager.ContentAds.AddRange(ReadCollection(root, "ContentAds", "Ad"));
            if (manager.ContentAds.Count == 0)
                manager.ContentAds.AddRange(ReadLegacyCollection(root, "ContentAd"));


            return manager;
        }

        private static string ResolveAdsFile(string webRootFolder)
        {
            var newAdsFile = Path.Combine(webRootFolder, "admin", "adsnew.xml");
            if (File.Exists(newAdsFile))
                return newAdsFile;

            var legacyAdsFile = Path.Combine(webRootFolder, "admin", "ads.xml");
            return File.Exists(legacyAdsFile) ? legacyAdsFile : null;
        }

        private static string GetElementValue(XElement root, string elementName)
        {
            return root.Element(elementName)?.Value;
        }

        private static IEnumerable<string> ReadCollection(XElement root, string containerName, string itemName)
        {
            return root.Element(containerName)?
                       .Elements(itemName)
                       .Select(element => element.Value)
                       .Where(value => !string.IsNullOrWhiteSpace(value))
                   ?? Enumerable.Empty<string>();
        }

        private static IEnumerable<string> ReadLegacyCollection(XElement root, string elementPrefix)
        {
            return root.Elements()
                       .Where(element => element.Name.LocalName.StartsWith(elementPrefix, StringComparison.Ordinal))
                       .OrderBy(element => GetNumericSuffix(element.Name.LocalName, elementPrefix))
                       .Select(element => element.Value)
                       .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static int GetNumericSuffix(string elementName, string elementPrefix)
        {
            var suffix = elementName[elementPrefix.Length..];
            return int.TryParse(suffix, out var index) ? index : int.MaxValue;
        }


        private static string GetFirstItem(IReadOnlyList<string> items)
        {
            return items.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        }

        private static IReadOnlyList<string> ShuffleAllButFirst(IReadOnlyList<string> items)
        {
            var filteredItems = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
            if (filteredItems.Count <= 1)
                return filteredItems;

            var shuffledItems = filteredItems.Skip(1).ToList();
            var random = Random.Shared;

            for (var index = shuffledItems.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (shuffledItems[index], shuffledItems[swapIndex]) = (shuffledItems[swapIndex], shuffledItems[index]);
            }

            return shuffledItems;
        }
    }
}
