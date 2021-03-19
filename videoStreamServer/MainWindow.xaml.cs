// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: MainWindow.xaml.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using log4net;

namespace videoStreamServer
{
    public partial class MainWindow
    {
        private const string MsgBoxTitle = "RailEssentials";
        private const string CfgPath = "videoStreamServer.json";

        private VideoCapture _capture;
        private bool _captureInProgress;
        private Mat _frame;
        private BitmapSource _recentFrame;
        private Dispatcher _dispatcher;
        private readonly HttpListener _listener = new();
        private readonly List<string> _prefixe = new();
        private Configuration _cfg = null;

        public ILog Log => LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (_capture == null || _capture.Ptr == IntPtr.Zero) return;
            _capture.Retrieve(_frame, 0);
            _dispatcher?.BeginInvoke(new Action(() =>
            {
                ImgWebcam.Source = _frame.ToBitmapSource();
            }), DispatcherPriority.Normal);
        }

        private void SetupWebServer()
        {
            foreach (var it in _prefixe.Where(it => !_listener.Prefixes.Contains(it)))
                _listener.Prefixes.Add(it);
            _listener.Start();
            Run();
        }

        private void HandleRequest(object state)
        {
            _dispatcher?.BeginInvoke(new Action(() =>
            {
                try
                {
                    _recentFrame = ImgWebcam.Source as BitmapSource;
                    if (_recentFrame == null)
                    {
                        Log?.Warn("No image available");
                        return;
                    }
                    var context = (HttpListenerContext)state;
                    context.Response.StatusCode = 200;
                    context.Response.SendChunked = true;
                    var request = context.Request;
                    var response = context.Response;
                    if (request.HttpMethod.ToUpper() != "GET")
                    {
                        Log?.Warn("Method must be GET");
                        return;
                    }
                    response.ContentType = "image/png";
                    response.AddHeader("Date", DateTime.Now.ToString("r"));
                    SaveImage(BitmapFrame.Create(_recentFrame), response.OutputStream);
                    response.OutputStream.Close();
                }
                catch (Exception ex)
                {
                    Log?.Error(ex);
                }
            }), DispatcherPriority.Normal);
        }

        private void SaveImage(BitmapFrame data, Stream saveStream)
        {
            try
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(data);
                using var memoryStream = new MemoryStream();
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                memoryStream.CopyTo(saveStream);
            }
            catch(Exception ex)
            {
                Log?.Error(ex);
            }
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        try
                        {
                            ThreadPool.QueueUserWorkItem(HandleRequest, _listener.GetContext());
                        }
                        catch (Exception)
                        {
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

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow != null)
                _dispatcher = Application.Current.MainWindow.Dispatcher;

            if (!File.Exists(CfgPath))
                MessageBox.Show($"Configuration is missing: {CfgPath}", MsgBoxTitle, MessageBoxButton.OK, MessageBoxImage.Error);

            _cfg = Configuration.Load("videoStreamServer.json", out _);
            _prefixe.AddRange(_cfg.WebServer.Prefixes);

            CvInvoke.UseOpenCL = false;
            try
            {
                _capture = new VideoCapture();
                _capture.ImageGrabbed += ProcessFrame;
            }
            catch (Exception ex)
            {
                Log?.Error(ex);
            }
            _frame = new Mat();
            SetupWebServer();
            ButtonBase_OnClick(null, null);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (_capture == null) return;
            if (_captureInProgress)
            {
                CmdCapture.Content = "Start";
                _capture.Pause();
            }
            else
            {
                CmdCapture.Content = "Stop";
                _capture.Start();
            }
            _captureInProgress = !_captureInProgress;
        }
    }
}
