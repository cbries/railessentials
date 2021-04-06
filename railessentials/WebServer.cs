// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: WebServer.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ecoslib.Sniffer;
using Utilities;

namespace railessentials
{
    public class WebServer
    {
        public string RootDir { get; set; }
        public string DefaultIndex { get; set; } = "index.html";
        private readonly HttpListener _listener = new();
        private Configuration _cfg;
        private readonly Sniffer _sniffer;
        private readonly List<string> _prefixe;

        public WebServer(
            Configuration cfg,
            Sniffer snifferCtx)
        {
            _cfg = cfg;
            _sniffer = snifferCtx;
            _prefixe = _cfg.WebServer.Prefixes;

            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            foreach (var it in _prefixe)
                _listener.Prefixes.Add(it);

            _listener.Start();
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                _sniffer?.Logger?.Log.Info($"Webserver started: {string.Join(", ", _prefixe)}");

                try
                {
                    while (_listener.IsListening)
                    {
                        try
                        {
                            ThreadPool.QueueUserWorkItem(HandleRequest, _listener.GetContext());
                        }
                        catch (Exception ex)
                        {
                            _sniffer?.Logger?.Log?.Warn("Listener canceled", ex);

                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        private void FillWithSubstitutions(string pathToFile)
        {
            if (string.IsNullOrEmpty(pathToFile)) return;
            if (!File.Exists(pathToFile)) return;
            try
            {
                var listOfWorkspacesHtml = string.Empty;
                var wsRoot = Globals.RootWorkspace;
                var listOfWs = new List<string>();
                if (!string.IsNullOrEmpty(wsRoot))
                {
                    foreach (var it in Directory.GetDirectories(wsRoot, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (string.IsNullOrEmpty(it)) continue;
                        listOfWs.Add(Path.GetFileName(it).Trim());
                    }

                    foreach (var it in listOfWs)
                    {
                        if (it.Equals(Program.Cfg.RecentWorkspace, StringComparison.OrdinalIgnoreCase))
                            listOfWorkspacesHtml += $"<option selected>{it}</option>\n";
                        else
                            listOfWorkspacesHtml += $"<option>{it}</option>\n";
                    }
                }

                var subst = new Dictionary<string, string> {
                    { "{{WORKSPACE}}", Globals.Workspace },
                    { "{{Author}}", Globals.Author },
                    { "{{ApplicationName}}", Globals.ApplicationName },
                    { "{{ApplicationDescription}}", Globals.ApplicationDescription },
                    { "{{LIST_OF_WORKSPACES}}", listOfWorkspacesHtml}
                };

#if DEBUG
                subst.Add("{{COMMENT_START_RELEASE_BUILD}}", string.Empty);
                subst.Add("{{COMMENT_END_RELEASE_BUILD}}", string.Empty);
                subst.Add("{{COMMENT_START_DEBUG_BUILD}}", "<!--");
                subst.Add("{{COMMENT_END_DEBUG_BUILD}}", "-->");
#else
                subst.Add("{{COMMENT_START_RELEASE_BUILD}}", "<!--");
                subst.Add("{{COMMENT_END_RELEASE_BUILD}}", "-->");
                subst.Add("{{COMMENT_START_DEBUG_BUILD}}", string.Empty);
                subst.Add("{{COMMENT_END_DEBUG_BUILD}}", string.Empty);
#endif

                if(_cfg?.Theme != null)
                {
                    if(string.IsNullOrEmpty(_cfg.Theme.PlanBackground))
                        subst.Add("{{BACKGROUND_COLOR}}", string.Empty);
                    else
                        subst.Add("{{BACKGROUND_COLOR}}", $"background-color: {_cfg.Theme.PlanBackground};");
                }

                var cnt = File.ReadAllText(pathToFile, Encoding.UTF8);

                foreach (var it in subst)
                    cnt = cnt.Replace(it.Key, it.Value);

                if (!AddWebcams(ref cnt))
                {
                    cnt = cnt.Replace("{{WEBCAM CODE HTML}}", string.Empty);
                    cnt = cnt.Replace("{{WEBCAM CODE JS}}", string.Empty);
                }

                File.WriteAllText(pathToFile, cnt, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        private bool AddWebcams(ref string html)
        {
            var webcams = _cfg?.Webcams;
            if (webcams == null) return false;
            var codeHtml = string.Empty;
            var codeJs = string.Empty;
            for (var i = 0; i < webcams.Count; ++i)
            {
                var itWebcam = webcams[i];
                if (itWebcam == null) continue;
                var webcamHtml = $@"<div id=""webcam{i}"" class=""videoStream"" style=""position: absolute; top: {itWebcam.Y}px; left: {itWebcam.X}px;""></div>";
                codeHtml += webcamHtml + "\n";

                codeJs += $"$('#webcam{i}').videoStream({{" +
                        $"url: \"{itWebcam.Url}\"," +
                        $"width: {itWebcam.Width}," +
                        $"height: {itWebcam.Height}," +
                        $"fps: {itWebcam.Fps}," +
                        $"stopOnFailure: true," +
                        $"caption: \"{itWebcam.Caption}\"," +
                        $"moved: function(data) {{ changeSetting({{ mode: \"webcam\", cmd: \"geometry\", value: data}}); }}" +
                    "}); ";
            }
            html = html.Replace("{{WEBCAM CODE HTML}}", codeHtml);
            html = html.Replace("{{WEBCAM CODE JS}}", codeJs);
            return true;
        }

        private void HandleRequest(object state)
        {
            try
            {
                var context = (HttpListenerContext)state;

                context.Response.StatusCode = 200;
                context.Response.SendChunked = true;

                var request = context.Request;
                var response = context.Response;

                var q = request.QueryString;
                if (q.HasKeys())
                {
                    var workspace = q.Get("workspace");
                    if (!string.IsNullOrEmpty(workspace))
                        Program.LoadMetadata(workspace, false, true);
                }

                var fileName = request.Url.LocalPath.Trim().TrimStart('/');
                if (string.IsNullOrEmpty(fileName))
                    fileName = DefaultIndex;

                _sniffer?.Logger?.Log.Debug($"Got {request.HttpMethod} request for: {fileName}");

                if (request.HttpMethod.ToUpper() != "GET")
                {
                    SendErrorResponse(response, 405, "Method must be GET");
                    return;
                }

                var fullFilePath = Path.Combine(RootDir, fileName).Replace("/", "\\");
                var decodedFullFilePath = Uri.UnescapeDataString(fullFilePath);
                var lastIdx = decodedFullFilePath.LastIndexOf("?r=", StringComparison.OrdinalIgnoreCase);
                if (lastIdx != -1)
                    decodedFullFilePath = decodedFullFilePath.Substring(0, lastIdx).Trim();
                if (!File.Exists(decodedFullFilePath))
                {
                    SendErrorResponse(response, 404, $"File not found: {decodedFullFilePath}");
                    return;
                }

                if (decodedFullFilePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var idx = decodedFullFilePath.LastIndexOf(".html", StringComparison.OrdinalIgnoreCase);
                        var noExtPath = decodedFullFilePath.Substring(0, idx).Trim();
                        var generatedPath = $"{noExtPath}__generated.html";
                        File.Copy(decodedFullFilePath, generatedPath, true);
                        FillWithSubstitutions(generatedPath);
                        decodedFullFilePath = generatedPath;
                    }
                    catch
                    {
                        // ignore
                    }
                }

                using (var fileStream = File.OpenRead(decodedFullFilePath))
                {
                    response.ContentType = MimeTypesUtilities.GetMimeType(decodedFullFilePath);
                    response.ContentLength64 = new FileInfo(decodedFullFilePath).Length;
                    //response.AddHeader(
                    //    "Content-Disposition",
                    //    "Attachment; filename=\"" + Path.GetFileName(decodedFullFilePath) + "\"");
                    response.AddHeader("Date", DateTime.Now.ToString("r"));
                    response.AddHeader("Last-Modified", File.GetLastWriteTime(decodedFullFilePath).ToString("r"));

                    /*
                        <meta http-equiv="cache-control" content="max-age=0" />
                        <meta http-equiv="cache-control" content="no-store" />
                        <meta http-equiv="expires" content="-1" />
                        <meta http-equiv="expires" content="Tue, 01 Jan 1980 1:00:00 GMT" />
                        <meta http-equiv="pragma" content="no-cache" />
                     */
                    response.AddHeader("cache-control", "max-age=0");
                    response.AddHeader("cache-control", "no-store");
                    response.AddHeader("expires", "-1");
                    response.AddHeader("expires", "Tue, 01 Jan 1980 1:00:00 GMT");
                    response.AddHeader("pragma", "no-cache");

                    try
                    {
                        fileStream.CopyTo(response.OutputStream);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                response.OutputStream.Close();
            }
            catch (Exception)
            {
                // Client disconnected or some other error - ignored for this example
            }
        }

        private void SendErrorResponse(HttpListenerResponse response, int statusCode, string statusResponse)
        {
            response.ContentLength64 = 0;
            response.StatusCode = statusCode;
            response.StatusDescription = statusResponse;
            response.OutputStream.Close();
            _sniffer?.Logger?.Log.Warn($"*** Sent error: {statusCode} {statusResponse}");
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener?.Close();
        }
    }
}
