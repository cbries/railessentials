// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AssemblyInfo.cs

using System.Reflection;
using System.Runtime.InteropServices;
using railessentials;

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
[assembly: ComVisible(false)]
[assembly: Guid("f4a7401f-9e67-467a-a657-e046f8885607")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
