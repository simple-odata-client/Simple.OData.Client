using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if WINDOWS_PHONE
[assembly: AssemblyTitle("Simple.OData.Client (Windows Phone)")]
#elif SILVERLIGHT
[assembly: AssemblyTitle("Simple.OData.Client (Silverlight)")]
#elif PocketPC
[assembly: AssemblyTitle("Simple.OData.Client (Compact)")]
#elif NETSTANDARD2_0
[assembly: AssemblyTitle("Simple.OData.Client (.NET Standard 2.0)")]
#elif PORTABLE
[assembly: AssemblyTitle("Simple.OData.Client (Portable)")]
#elif NETFX_CORE
[assembly: AssemblyTitle("Simple.OData.Client (WinRT)")]
#elif NET20
[assembly: AssemblyTitle("Simple.OData.Client (NET 2.0)")]
#elif NET35
[assembly: AssemblyTitle("Simple.OData.Client (NET 3.5)")]
#elif NET40
[assembly: AssemblyTitle("Simple.OData.Client (NET 4.0)")]
#else
[assembly: AssemblyTitle("Simple.OData.Client")]
#endif

[assembly: AssemblyDescription("OData client library for .NET 4.x, Windows Store, Silverlight 5, Windows Phone 8, Mond for Android and MonoTouch platforms")]
