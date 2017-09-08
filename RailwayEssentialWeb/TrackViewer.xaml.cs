/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using RailwayEssentialCore;
using RailwayEssentialWeb.Cef;

namespace RailwayEssentialWeb
{
    public partial class TrackViewer : ITrackViewer, ITrackViewerZoom
    {
        private string _url;

        private readonly SynchronizationContext _ctx;

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;

                if (!string.IsNullOrEmpty(_url))
                    Browser.Load(_url);
            }
        }

        public IWebGenerator WebGenerator { get; set; }
        #region ITrackViewerZoom

        public double ZoomLevel
        {
            get => Browser.ZoomLevel;
            set { Browser.ZoomLevel = value; }
        }

        public double ZoomLevelIncrement => Browser.ZoomLevelIncrement;

        #endregion

        public TrackViewer()
        {
            var settings = new CefSettings { RemoteDebuggingPort = 1234 };
            // for some reason, performance sucks w/ the gpu enabled
            //settings.CefCommandLineArgs.Add("disable-gpu", "1");
            //settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            if(!CefSharp.Cef.IsInitialized)
                CefSharp.Cef.Initialize(settings);

            InitializeComponent();

            _ctx = SynchronizationContext.Current;

            Browser.ResourceHandlerFactory = new LocalResourceHandlerFactory();
            Browser.MenuHandler = new MenuHandler();

            Browser.BrowserSettings.FileAccessFromFileUrls = CefState.Enabled;
            Browser.BrowserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            Browser.BrowserSettings.WebSecurity = CefState.Disabled;
            //Browser.BrowserSettings.WebGl = CefSharp.CefState.Disabled;

            Browser.IsBrowserInitializedChanged += BrowserOnIsBrowserInitializedChanged;
            Browser.LoadError += BrowserOnLoadError;
            Browser.ConsoleMessage += BrowserOnConsoleMessage;
            Browser.LoadingStateChanged += BrowserOnLoadingStateChanged;
            Browser.FrameLoadEnd += BrowserOnFrameLoadEnd;

            _jsCallback = new TrackViewerJsCallback();

            Browser.RegisterJsObject("railwayEssentialCallback", _jsCallback);

            settings.Dispose();
        }

        private void BrowserOnConsoleMessage(object sender, ConsoleMessageEventArgs arg)
        {
            Trace.WriteLine($"<TackViewer> {arg.Line}: {arg.Message}");
        }

        private void BrowserOnFrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            _ctx.Send(state =>
            {
                if (args.Frame.IsMain)
                {
                    if (Browser.IsBrowserInitialized)
                    {
                        var vm = DataContext as ITrackWindow;
                        if (vm != null)
                            vm.ViewerReady();
                    }
                }
            }, new object());
        }

        private void BrowserOnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            //Trace.WriteLine("Loading state: " + args.IsLoading);
        }

        public void Reload()
        {
            if (_ctx == null)
                return;

            _ctx.Send(state =>
            {
                if (Browser != null && Browser.IsBrowserInitialized)
                    Browser.Reload();
            }, new object());
        }

        private void BrowserOnIsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (Url != null)
                Browser.Address = Url.Replace(" ", "%20");
        }

        private void BrowserOnLoadError(object sender, LoadErrorEventArgs loadErrorEventArgs)
        {
            //Trace.WriteLine("<error> " + loadErrorEventArgs.ErrorCode);
            //Trace.WriteLine("<error> " + loadErrorEventArgs.ErrorText);
            //Trace.WriteLine("<error> " + loadErrorEventArgs.FailedUrl);
        }

        private void Browser_OnInitialized(object sender, EventArgs e)
        {
        }

        private void TrackViewer_OnInitialized(object sender, EventArgs e)
        {
        }

        private void TrackViewer_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = DataContext as ITrackWindow;
            if (vm != null)
                vm.PromoteViewer(this);
        }

        #region ITrackViewer

        private readonly ITrackViewerJsCallback _jsCallback;

        public ITrackViewerJsCallback JsCallback => _jsCallback;

        public void ExecuteJs(string scriptCode)
        {
            _ctx.Send(state =>
            {
                if (string.IsNullOrEmpty(scriptCode))
                    return;

                if (Browser == null || !Browser.IsBrowserInitialized)
                    return;

                Browser.ExecuteScriptAsync(scriptCode.Trim());

            }, new object());
        }

        public void SetUrl(string url)
        {
            Url = url.Replace(" ", "%20");
        }

        public void Load()
        {
            Browser.Address = Url.Replace(" ", "%20");
            Browser.Reload();
        }

        public async Task<bool> Print(string targetFilename)
        {
            return await Browser.PrintToPdfAsync(targetFilename, new PdfPrintSettings
            {
                MarginType = CefPdfPrintMarginType.Custom,
                MarginBottom = 10,
                MarginTop = 10,
                MarginLeft = 15,
                MarginRight = 15
            });
        }

        #endregion

    }
}
