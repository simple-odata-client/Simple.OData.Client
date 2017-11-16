using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if WINDOWS_PHONE
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (Windows Phone)")]
#elif SILVERLIGHT
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (Silverlight)")]
#elif PocketPC
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (Compact)")]
#elif NETSTANDARD2_0
[assembly: AssemblyTitle("Simple.OData.Client (.NET Standard 2.0)")]
#elif PORTABLE
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (Portable)")]
#elif NETFX_CORE
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (WinRT)")]
#elif NET20
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (NET 2.0)")]
#elif NET35
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (NET 3.5)")]
#elif NET40
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter (NET 4.0)")]
#else
[assembly: AssemblyTitle("Simple.OData.Client.V4.Adapter")]
#endif

[assembly: AssemblyDescription("OData V4 adapter for the client library for .NET 4.x, Windows Store, Silverlight 5, Windows Phone 8, Mond for Android and MonoTouch platforms")]
