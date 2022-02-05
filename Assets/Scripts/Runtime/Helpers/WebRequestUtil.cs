using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace Warp
{
    public static class WebRequestUtil
    {
        public static byte[] DownloadBytes(string url)
        {
            var req = WebRequest.Create(url);
            var res = req.GetResponse();
            var memory = new MemoryStream();
            res.GetResponseStream().CopyTo(memory);
            return memory.ToArray();
        }

        public static string DownloadText(string url)
        {
            var req = WebRequest.Create(url);
            var res = req.GetResponse();
            var reader = new StreamReader(res.GetResponseStream());
            return reader.ReadToEnd();
        }
    }
}