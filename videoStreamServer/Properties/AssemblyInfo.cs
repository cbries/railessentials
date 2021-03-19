// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AssemblyInfo.cs

using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using videoStreamServer;

[assembly: AssemblyTitle(Globals.ApplicationName)]
[assembly: AssemblyDescription(Globals.ApplicationDescription)]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
 [assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany(Globals.Company)]
[assembly: AssemblyProduct(Globals.ApplicationName)]
[assembly: AssemblyCopyright("Copyright © " + Globals.Author + " 2021")]
[assembly: AssemblyTrademark(Globals.ApplicationName)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
