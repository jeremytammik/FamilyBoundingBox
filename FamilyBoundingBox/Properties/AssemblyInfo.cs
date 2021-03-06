using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "FamilyBoundingBox" )]
[assembly: AssemblyDescription( "Revit C# .NET add-in determining family bounding box" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "FamilyBoundingBox Revit C# .NET Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2017 (C) Kevin Lau and Jeremy Tammik, Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
//
// History:
//
// 2017-03-15 2017.0.0.0 initial implementation
// 2017-07-20 2017.0.0.1 tighter bounding box calculation using FamilyInstance information
//
[assembly: AssemblyVersion( "2017.0.0.1" )]
[assembly: AssemblyFileVersion( "2017.0.0.1" )]
