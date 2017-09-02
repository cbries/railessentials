using System;
using System.IO;
using CefSharp;

namespace RailwayEssentialWeb.Cef
{
    class LocalResourceHandler : IResourceHandler
    {
        private string _mimeType;
        private MemoryStream _stream;

        public void Cancel()
        {

        }

        public bool CanGetCookie(Cookie cookie)
        {
            return true;
        }

        public bool CanSetCookie(Cookie cookie)
        {
            return true;
        }

        public void Dispose()
        {

        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            responseLength = _stream == null ? 0 : _stream.Length;
            redirectUrl = null;

            response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            response.StatusText = "OK";
            response.MimeType = _mimeType;
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            // The 'host' portion is entirely ignored by this scheme handler.
            var uri = new Uri(request.Url);
            var file = uri.AbsolutePath;

            if (!string.IsNullOrEmpty(file))
                file = file.Replace("%20", " ");

            if (File.Exists(file))
            {
                Byte[] bytes = File.ReadAllBytes(file);
                _stream = new MemoryStream(bytes);
                switch (Path.GetExtension(file))
                {
                    case ".html":
                    case ".htm":
                        _mimeType = "text/html";
                        break;
                    case ".css":
                        _mimeType = "text/css";
                        break;
                    case ".js":
                        _mimeType = "text/javascript";
                        break;
                    case ".png":
                        _mimeType = "image/png";
                        break;
                    case ".jpg":
                    case ".jpeg":
                        _mimeType = "image/jpeg";
                        break;
                    case ".gif":
                        _mimeType = "image/gif";
                        break;
                    case ".woff":
                        _mimeType = "application/font-woff";
                        break;
                    case ".eot":
                        _mimeType = "application/vnd.ms-fontobject";
                        break;
                    case ".ttf":
                        _mimeType = "application/font-ttf";
                        break;
                    case ".otf":
                        _mimeType = "application/font-otf";
                        break;
                    case ".svg":
                        _mimeType = "image/svg+xml";
                        break;
                    case ".appcache":
                    case ".manifest":
                        _mimeType = "text/cache-manifest";
                        break;
                    default:
                        _mimeType = "application/octet-stream";
                        break;
                }
                callback.Continue();
                return true;
            }
            return false;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            //Dispose the callback as it's an unmanaged resource, we don't need it in this case
            callback.Dispose();

            if (_stream == null)
            {
                bytesRead = 0;
                return false;
            }

            //Data out represents an underlying buffer (typically 32kb in size).
            var buffer = new byte[dataOut.Length];
            bytesRead = _stream.Read(buffer, 0, buffer.Length);

            dataOut.Write(buffer, 0, buffer.Length);

            return bytesRead > 0;
        }
    }
}
