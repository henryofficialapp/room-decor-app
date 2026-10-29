using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoomDecor.App
{
    /// <summary>
    /// Downloads product photos from the feed's image links, once each, and
    /// keeps them for the session. Photos are shown as supplied, in lists only.
    /// </summary>
    public sealed class ProductImages
    {
        readonly MonoBehaviour host;
        readonly Dictionary<string, Texture2D> loaded = new Dictionary<string, Texture2D>();
        readonly HashSet<string> requested = new HashSet<string>();

        public ProductImages(MonoBehaviour host)
        {
            this.host = host;
        }

        /// <summary>The photo if it has arrived, else null; the first call starts the download.</summary>
        public Texture2D Get(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (loaded.TryGetValue(url, out var texture)) return texture;
            if (requested.Add(url)) host.StartCoroutine(Download(url));
            return null;
        }

        IEnumerator Download(string url)
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    loaded[url] = DownloadHandlerTexture.GetContent(request);
                else
                    Debug.LogWarning($"Product photo failed to load: {url} ({request.error})");
            }
        }
    }
}
