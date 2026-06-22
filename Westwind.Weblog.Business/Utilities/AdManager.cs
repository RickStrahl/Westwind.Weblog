using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Westwind.Utilities;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business
{
    public class AdManager
    {
        private static Lazy<AdManager> _currentAds = new(LoadFromXml);

        public static AdManager Ads => _currentAds.Value;

        public static void Reload() => _currentAds = new Lazy<AdManager>(LoadFromXml);

        ///// <summary>
        ///// Gets the ad configuration loaded from wwwroot/admin/ads.xml.
        ///// </summary>
        //public static AdManager Ads => CurrentAds.Value;

        
        public string BottomPostAd { get; set; }

        public string TopPostAd { get; set; }
        
        public string TopPageAd { get; set; }

        public List<string> ContentAds { get; } = [];

        public List<string> SponsorBanners { get; } = [];

        public string GetFirstContentAd(bool resolveUrls = true)
        {
            var ad = GetFirstItem(ContentAds);
            if (resolveUrls)
                ad = ResolveUrls(ad);
            return ad;
        }

        public string GetFirstSponsorBanner()
        {
            return GetFirstItem(SponsorBanners);
        }

        public List<string> GetShuffledContentAds()
        {
            // shuffle all but first ad
            return Shuffle(ContentAds,1);
        }

        public string GetRandomSponsorBanner(bool fixupSiteRelativeUrls = true)
        {
            var random = Random.Shared;
            var i = random.Next(SponsorBanners.Count);          
            var banner = SponsorBanners[i];
            if (fixupSiteRelativeUrls)
                return ResolveUrls(banner);
            return banner;
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
            manager.ContentAds.AddRange(ReadCollection(root, "ContentAds", "Ad"));
            
            return manager;
        }

        /// <summary>
        /// Saves the current ad configuration to the specified file.
        /// </summary>
        public bool SaveToXml(string saveFilename)
        {
            try
            {
                var path = Path.GetFullPath(saveFilename);
                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("Ads",
                        new XElement("BottomPostAd", new XCData(BottomPostAd ?? string.Empty)),
                        new XElement("TopPostAd", new XCData(TopPostAd ?? string.Empty)),
                        new XElement("TopPageAd", new XCData(TopPageAd ?? string.Empty)),
                        new XElement("SponsorBanners",
                            SponsorBanners.Select(b => new XElement("Banner", new XCData(b ?? string.Empty)))),
                        new XElement("ContentAds",
                            ContentAds.Select(a => new XElement("Ad", new XCData(a ?? string.Empty))))));
                doc.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveAdsFile(string webRootFolder)
        {
            var newAdsFile = Path.Combine(webRootFolder, "admin", "ads.xml");
            if (File.Exists(newAdsFile))
                return newAdsFile;

            return null;
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
       

        private static int GetNumericSuffix(string elementName, string elementPrefix)
        {
            var suffix = elementName[elementPrefix.Length..];
            return int.TryParse(suffix, out var index) ? index : int.MaxValue;
        }


        private static string GetFirstItem(List<string> items)
        {
            return items.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        }

        private static List<string> Shuffle(List<string> items, int skipFirstItemCount = 0)
        {
            var filteredItems = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
            if (filteredItems.Count <= 1)
                return filteredItems;

            List<string> shuffledItems = items;

            if (skipFirstItemCount > 0)
                shuffledItems = filteredItems.Skip(1).ToList();
            
            var random = Random.Shared;

            for (var index = shuffledItems.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (shuffledItems[index], shuffledItems[swapIndex]) = (shuffledItems[swapIndex], shuffledItems[index]);
            }

            return shuffledItems;
        }

        /// <summary>
        /// Resolves site-relative Urls to a fully qualified Url base path.
        /// </summary>
        /// <param name="html">Input Html string</param>
        /// <param name="basePath">Base path to resolve `/` or `~/` to</param>
        /// <returns>Html string with resolved Urls</returns>

        public static string ResolveUrls(string html, string basePath = null)
        {
            if (string.IsNullOrEmpty(html)) return html;

            if (string.IsNullOrEmpty(basePath))
                basePath = wlApp.Configuration.ApplicationBasePath;
            if (string.IsNullOrEmpty(basePath))
                return html;

            basePath = StringUtils.TerminateString(basePath, "/");


            if (!string.IsNullOrWhiteSpace(basePath))
            {
                html = html.Replace("src=\"/", $"src=\"{basePath}")
                           .Replace("src=\"~/", $"src=\"{basePath}")
                           .Replace("href=\"/", $"href=\"{basePath}")
                           .Replace("href=\"~/", $"href=\"{basePath}");                         
            }

            return html;
        }
}
}
