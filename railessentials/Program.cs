// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Program.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using ecoslib;
using ecoslib.Entities;
using ecoslib.Sniffer;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.ClientHandler;

namespace railessentials
{
    internal partial class Program
    {
        public static string PathToCfg = @"railessentials.json";

        public static Configuration Cfg { get; set; } = new();
        private static ClientHandler.ClientHandler ClientHandler { get; } = new();
        private static Metadata Metadata { get; } = new();
        private static Sniffer _sniffer;
        private static WebServer _webServer;

        private static bool _startImportUi;
        private static bool _startBrowser;
        private static string _startBrowserWorkspace = string.Empty;

        public class Logger : ILogger
        {
            public ILog Log => LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public static JArray GetThemeData()
        {
            var fullPath = Path.Combine(Cfg.Theme.Root, Cfg.Theme.Name + ".json");
            if (!File.Exists(fullPath)) return null;

            try
            {
                var cnt = File.ReadAllText(fullPath, Encoding.UTF8);
                return JArray.Parse(cnt);
            }
            catch (Exception ex)
            {
                _sniffer?.Logger?.Log?.Error("Theme", ex);
            }

            return null;
        }

        public static bool LoadMetadata(string workspaceName, bool forceLoad, bool sendClientUpdates)
        {
            if (string.IsNullOrEmpty(workspaceName)) return false;

            var ns = Globals.Workspace;

            if (string.IsNullOrEmpty(ns))
            {
                Globals.Workspace = Cfg.RecentWorkspace;
                ns = Cfg.RecentWorkspace;
            }

            if (!ns.Equals(workspaceName, StringComparison.OrdinalIgnoreCase))
            {
                Globals.Workspace = workspaceName;
                Cfg.RecentWorkspace = workspaceName;
                var saveRes = Configuration.Save(Cfg, out var errorMessage);
                if (!saveRes)
                    ClientHandler?.SendDebugMessage($"Load of workspace {workspaceName} has trouble: {errorMessage}");

                var wsPath = Path.Combine(Globals.RootWorkspace, Globals.Workspace);
                if (!Directory.Exists(wsPath))
                    Directory.CreateDirectory(wsPath);
            }
            else
            {
                if (!forceLoad)
                    return true;
            }

            Globals.Workspace = workspaceName;
            Globals.RootWorkspace = Cfg.RootWorkspace;

            Metadata.LoadMetamodel(Globals.GetCfgDataPath("Metamodel"));
            Metadata.LoadRoutes(Globals.GetCfgDataPath("Routes"), true);
            Metadata.LoadLocomotives(Globals.GetCfgDataPath("Locomotives"));
            Metadata.LoadLocomotivesDurations(Globals.GetCfgDataPath("LocomotivesDurations"));
            Metadata.LoadFeedbacks(Globals.GetCfgDataPath("FbEvents"));
            Metadata.LoadOccData(Globals.GetCfgDataPath("Occ"), true);

            if (sendClientUpdates)
            {
                ClientHandler?.SendModelToClients(railessentials.ClientHandler.ClientHandler.ModelType.UpdateOcc);
                ClientHandler?.SendModelToClients(railessentials.ClientHandler.ClientHandler.ModelType.UpdateRoutes);
                ClientHandler?.SendModelToClients(railessentials.ClientHandler.ClientHandler.ModelType.UpdateFeedbacks);
                ClientHandler?.SendModelToClients(railessentials.ClientHandler.ClientHandler.ModelType.UpdateLocomotivesData);
                ClientHandler?.SendModelToClients(railessentials.ClientHandler.ClientHandler.ModelType.UpdateLocomotivesDurationsData);
            }

            return true;
        }

        private static void ParseArguments(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0) return;
                for (var i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];
                    switch (arg.Trim().ToLower())
                    {
                        case "--cfg":
                        case "-cfg":
                            {
                                PathToCfg = args[i + 1];
                                ++i;
                            }
                            break;

                        case "--importer":
                        case "-importer":
                            {
                                _startImportUi = true;
                            }
                            break;

                        case "--browser":
                        case "-browser":
                            {
                                _startBrowser = true;
                            }
                            break;
                    }
                }
            }
            catch
            {
                Usage();
            }
        }

        private static void Usage()
        {
            Console.WriteLine("railessentials.exe [-cfg | --cfg PathTo\\ecoslibServerNet.json] [--browser] [--importer]");
            Environment.Exit(0);
        }

        private static void RunApplication(out bool runRailEssentials)
        {
            runRailEssentials = false;
            Console.WriteLine("Start importer...");
            var application = new System.Windows.Application();
            var winImporter = new Importer.Ui.Importer(Cfg);
            application.Run(winImporter);
            Console.WriteLine("Importer closed");
            if (winImporter.ChkLoadAfterImport.IsChecked != null)
                runRailEssentials = winImporter.ChkLoadAfterImport.IsChecked.Value;
            if (winImporter.IsCanceled)
                return;
            _startBrowserWorkspace = winImporter.ImportedWorkspace;
        }

        [STAThread]
        private static void Main(string[] args)
        {
            ParseArguments(args);

            Cfg = Configuration.Load(PathToCfg, out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine($"{errorMessage}");
                return;
            }

            if (_startImportUi)
            {
                RunApplication(out var runRailEssentials);
                if (!runRailEssentials)
                    return;

                if (!string.IsNullOrEmpty(_startBrowserWorkspace))
                {
                    Cfg.RecentWorkspace = _startBrowserWorkspace;
                    Globals.Workspace = Cfg.RecentWorkspace;
                }
            }

            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

            if (!string.IsNullOrEmpty(Cfg.RecentWorkspace))
                Globals.Workspace = Cfg.RecentWorkspace;

            Console.WriteLine($"Try to connect to: {Cfg.Ecos}");

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            LoadMetadata(Globals.Workspace, true, false);

            if (Cfg.SimulationMode)
                Metadata.LoadEcosDataForSimulation(Cfg.SimulationData);

            _sniffer = new Sniffer(Cfg.Ecos.Ip, Cfg.Ecos.Port)
            {
                Logger = new Logger(),
                SaveOnEveryPlanfieldChange = Cfg.SaveOnEveryPlanfieldChange,
                InitializeDelay = Cfg.InitializeDelay,
                IsSimulationMode = Cfg.SimulationMode
            };

            if (Cfg.SimulationMode)
            {
                _sniffer?.Logger?.Log?.Warn("*************** Simulation: ACTIVATED");

                // currently only supported in simulation mode, when
                // ECoS is offline/switch-off, when deserialized, then
                // no view is registered and no switch states or other 
                // replies from the ECoS are recognized

                var dp = _sniffer.GetDataProvider();
                dp.Parse(Metadata.EcosData);

                var dp88 = _sniffer.GetDataProviderS88();
                dp88.Parse(Metadata.EcosData);
            }

            _sniffer.DataReceived += SnifferOnDataReceived;

            _sniffer.Start(Cfg.SimulationMode);

            if (_sniffer.IsStarted)
            {
                _sniffer.GetWsServer().GetCfg().ServerIp = Cfg.WsServer.GetIpAddress();
                _sniffer.GetWsServer().GetCfg().ServerPort = Cfg.WsServer.Port;
                _sniffer.StartWsServer();

                ClientHandler.Initialize(_sniffer, Metadata, GetThemeData(), new ClientHandlerCfg
                {
                    ThemeName = Cfg.Theme.Name
                });
            }

            _webServer = new WebServer(_sniffer, Cfg.WebServer.Prefixes);
#if DEBUG
            Cfg.WebServer.HttpRoot = Cfg.WebServer.HttpRootDebug;
            Cfg.WebServer.HttpRootFallback = Cfg.WebServer.HttpRootDebug;
#endif
            if (!string.IsNullOrEmpty(Cfg.WebServer.HttpRoot) && Directory.Exists(Cfg.WebServer.HttpRoot))
            {
                _webServer.RootDir = Cfg.WebServer.HttpRoot;
            }
            else if (!string.IsNullOrEmpty(Cfg.WebServer.HttpRootFallback) && Directory.Exists(Cfg.WebServer.HttpRootFallback))
            {
                _webServer.RootDir = Cfg.WebServer.HttpRootFallback;
            }
            else
            {
                _sniffer?.Logger?.Log?.Warn($"HttpRoot is physically missing. Use direct file:// access!");
            }
            _webServer.Run();

            if (_startBrowser)
            {
                var pref = Cfg.WebServer.Prefixes.First();
                var url = pref;
                if (!string.IsNullOrEmpty(Globals.Workspace))
                    url += $"?workspace={Globals.Workspace}";
                Process.Start(url);
            }

            while (!_isclosing)
                Thread.Sleep(250);
        }

        private static JObject _recentS88JObject;
        private static JObject _recentGeneralJObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="area"></param>
        /// <param name="recent"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private static bool IsAreaChanged(EcosAreaT area, JObject recent, JObject current)
        {
            var areaName = string.Empty;

            switch (area)
            {
                case EcosAreaT.EcosBase:
                    areaName = "ecosbase";
                    break;
                case EcosAreaT.Locomotives:
                    areaName = "locomotives";
                    break;

                case EcosAreaT.Accessories:
                    areaName = "accessories";
                    break;

                case EcosAreaT.Feedbacks:
                    areaName = "feedbacks";
                    break;
            }

            var o0 = recent?[areaName] as JArray;
            var o1 = current?[areaName] as JArray;
            if (o0 == null || o1 == null) return false;
            return !JToken.DeepEquals(o0, o1);
        }

        public enum EcosAreaT
        {
            EcosBase,
            Locomotives,
            Accessories,
            Feedbacks
        }

        private static void SnifferOnDataReceived(object sender, IDataProvider dataProvider, bool forceChange = false)
        {
            if (sender == null) return;
            if (dataProvider == null) return;

            var changeAvailable = false;

            var ecosbaseChanged = false;
            var locomotivesChanged = false;
            var accessoriesChanged = false;
            var feedbacksChanged = false;

            switch (dataProvider.Mode)
            {
                case DataModeT.S88:
                    {
                        _recentS88JObject ??= dataProvider.ToJson();
                        var obj = dataProvider.ToJson();
                        changeAvailable = !JToken.DeepEquals(_recentS88JObject, obj);
                        if (changeAvailable)
                        {
                            feedbacksChanged = IsAreaChanged(EcosAreaT.Feedbacks, _recentGeneralJObject, obj);

                            _recentS88JObject = obj;

                            ClientHandler?.HandleFeedbackData();
                        }
                    }
                    break;

                case DataModeT.General:
                    {
                        _recentGeneralJObject ??= dataProvider.ToJson();
                        var obj = dataProvider.ToJson();
                        changeAvailable = !JToken.DeepEquals(_recentGeneralJObject, obj);
                        if (changeAvailable)
                        {
                            ecosbaseChanged = IsAreaChanged(EcosAreaT.EcosBase, _recentGeneralJObject, obj);
                            locomotivesChanged = IsAreaChanged(EcosAreaT.Locomotives, _recentGeneralJObject, obj);
                            accessoriesChanged = IsAreaChanged(EcosAreaT.Accessories, _recentGeneralJObject, obj);

                            _recentGeneralJObject = obj;
                        }
                    }
                    break;
            }

            if (!forceChange)
                if (!changeAvailable) return;

            var mergedObject = new JObject
            {
                ["ecosbase"] = _recentGeneralJObject?["ecosbase"] ?? new JObject(),
                ["ecosbaseChanged"] = ecosbaseChanged,
                ["locomotives"] = _recentGeneralJObject?["locomotives"] ?? new JObject(),
                ["locomotivesChanged"] = locomotivesChanged,
                ["accessories"] = _recentGeneralJObject?["accessories"] ?? new JObject(),
                ["accessoriesChanged"] = accessoriesChanged,
                ["feedbacks"] = _recentS88JObject?["feedbacks"] ?? new JObject(),
                ["feedbacksChanged"] = feedbacksChanged
            };

            if (forceChange)
            {
                mergedObject["ecosbaseChanged"] = ecosbaseChanged;
                mergedObject["locomotivesChanged"] = locomotivesChanged;
                mergedObject["accessoriesChanged"] = accessoriesChanged;
                mergedObject["feedbacksChanged"] = feedbacksChanged;
            }

            var recentJsonStr = Metadata.EcosData.ToString(Formatting.None);
            var currentJsonStr = mergedObject.ToString(Formatting.None);
            if (!recentJsonStr.Equals(currentJsonStr))
            {
                Metadata.EcosData = mergedObject;

                dataProvider.ResetAllChangedStates();

                ClientHandler?.HandleEcosData();
            }
        }
    }
}
