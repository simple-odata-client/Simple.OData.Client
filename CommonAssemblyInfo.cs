using System;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if(DEBUG)
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("Vagif Abilov")]
[assembly: AssemblyProduct("Simple OData Client")]
[assembly: AssemblyCopyright("Copyright © Vagif Abilov 2012-2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if !NETSTANDARD2_0
// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
#endif

[assembly: NeutralResourcesLanguage("en-US")]
[assembly: CLSCompliant(false)]
