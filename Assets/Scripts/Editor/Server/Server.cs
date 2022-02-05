using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace Warp
{
    public class Server
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly Thread listenerThread;
        private readonly string staticFilePath;

        public Server(string prefix, string staticFilePath)
        {
            this.staticFilePath = staticFilePath;

            listener.Prefixes.Add(prefix);
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            listener.Start();

            listenerThread = new Thread(Loop);
            listenerThread.Start();
        }

        public void Stop()
        {
            listener.Stop();
            listenerThread.Join();
        }

        private void Loop()
        {
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                Debug.Log("Method: " + context.Request.HttpMethod);
                Debug.Log("LocalUrl: " + context.Request.Url.LocalPath);
                var path = context.Request.Url.LocalPath;
                var res = context.Response;

                if (path.StartsWith("/static/"))
                {
                    var filePath = Path.Combine(staticFilePath, Regex.Replace(path, "^/static/", string.Empty));
                    SendFile(res, filePath);
                }
                else
                {
                    res.StatusCode = (int)HttpStatusCode.NotFound;
                    res.Close();
                }
            }
        }

        private void SendFile(HttpListenerResponse res, string filePath)
        {
            Debug.Log($"Send file at {filePath}");

            if (File.Exists(filePath))
            {
                byte[] content = File.ReadAllBytes(filePath);
                res.OutputStream.Write(content, 0, content.Length);
                res.Close();
            }
            else
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                res.Close();
            }
        }
    }
}
