using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-In title and Guid are extracted from the following two attributes
[assembly: AssemblyTitle("Mitsuba")]
[assembly: Guid("611eea18-e64b-46a8-a9c4-b0d96574d9cf")]

// Plug-in Description Attributes - all of these are optional
[assembly: PlugInDescription(DescriptionType.Address, "119 Ferris Place\n14850 Ithaca, NY")]
[assembly: PlugInDescription(DescriptionType.Country, "USA")]
[assembly: PlugInDescription(DescriptionType.Email, "wenzel@cs.cornell.edu")]
[assembly: PlugInDescription(DescriptionType.Phone, "123.456.7890")]
[assembly: PlugInDescription(DescriptionType.Fax, "987.654.3210")]
[assembly: PlugInDescription(DescriptionType.Organization, "Mitsuba Renderer")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "http://www.mitsuba-renderer.org")]
[assembly: PlugInDescription(DescriptionType.WebSite, "http://www.mitsuba-renderer.org")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDescription("Mitsuba")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Mitsuba Renderer")]
[assembly: AssemblyProduct("Mitsuba")]
[assembly: AssemblyCopyright("Copyright ©  2012")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyFileVersion("1.0.0.0")]
