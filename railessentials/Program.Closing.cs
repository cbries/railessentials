// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Program.Closing.cs

using System.Runtime.InteropServices;

namespace railessentials
{
    partial class Program
    {
        private static bool _isclosing;

        #region Win32 

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        private delegate bool SetConsoleCtrlEventHandler(CtrlTypes sig);
        private enum CtrlTypes
        {
            CtrlCEvent = 0,
            CtrlBreakEvent,
            CtrlCloseEvent,
            CtrlLogoffEvent = 5,
            CtrlShutdownEvent
        }

        #endregion

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            StopServices();

            switch (ctrlType)
            {
                case CtrlTypes.CtrlCEvent:
                    //Console.WriteLine("CTRL+C received!");
                    _isclosing = true;
                    break;

                case CtrlTypes.CtrlBreakEvent:
                    //Console.WriteLine("CTRL+BREAK received!");
                    _isclosing = true;
                    break;

                case CtrlTypes.CtrlCloseEvent:
                    //Console.WriteLine("Program being closed!");
                    _isclosing = true;
                    break;

                case CtrlTypes.CtrlLogoffEvent:
                case CtrlTypes.CtrlShutdownEvent:
                    //Console.WriteLine("User is logging off!");
                    _isclosing = true;
                    break;
            }

            return true;
        }

        private static void StopServices()
        {
            _webServer?.Stop();

            ClientHandler?.SaveAll();

            _sniffer?.Stop();
        }
    }
}
