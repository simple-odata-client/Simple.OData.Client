using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if WINDOWS_PHONE
[assembly: AssemblyTitle("GXOData.Client (Windows Phone)")]
#elif SILVERLIGHT
[assembly: AssemblyTitle("GXOData.Client (Silverlight)")]
#elif PocketPC
[assembly: AssemblyTitle("GXOData.Client (Compact)")]
#elif NETSTANDARD2_0
[assembly: AssemblyTitle("GXOData.Client (.NET Standard 2.0)")]    
#elif PORTABLE
[assembly: AssemblyTitle("GXOData.Client (Portable)")]
#elif NETFX_CORE
[assembly: AssemblyTitle("GXOData.Client (WinRT)")]
#elif NET20
[assembly: AssemblyTitle("GXOData.Client (NET 2.0)")]
#elif NET35
[assembly: AssemblyTitle("GXOData.Client (NET 3.5)")]
#elif NET40
[assembly: AssemblyTitle("GXOData.Client (NET 4.0)")]
#else
[assembly: AssemblyTitle("GXOData.Client")]
#endif

[assembly: AssemblyDescription("OData client library for .NET 4.x, Windows Store, Silverlight 5, Windows Phone 8, Mond for Android and MonoTouch platforms")]

[assembly: InternalsVisibleTo("Simple.OData.Client.Dynamic")]
[assembly: InternalsVisibleTo("Simple.OData.Client.V3.Adapter")]
[assembly: InternalsVisibleTo("Simple.OData.Client.V4.Adapter")]
[assembly: InternalsVisibleTo("Simple.OData.Client.UnitTests")]
[assembly: InternalsVisibleTo("Simple.OData.Client.IntegrationTests")]
[assembly: InternalsVisibleTo("Simple.OData.Client.TestUtils")]
[assembly: InternalsVisibleTo("Simple.OData.Client.Tests.Core")]
[assembly: InternalsVisibleTo("WebApiOData.V3.Samples.Tests")]
[assembly: InternalsVisibleTo("WebApiOData.V4.Samples.Tests")]
