//===============================================================================
// Microsoft patterns & practices Enterprise Library
// Security Application Block
//===============================================================================
// Copyright ? Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

[assembly : AssemblyTitle("Enterprise Library Security Application Block")]
[assembly : AssemblyDescription("Enterprise Library Security Application Block")]
[assembly : AssemblyVersion("4.0.0.926")]
[assembly : Instrumented(@"root\EnterpriseLibrary")]
[assembly : WmiConfiguration(@"root\EnterpriseLibrary", HostingModel = ManagementHostingModel.Decoupled, IdentifyLevel = false)]
[assembly : AllowPartiallyTrustedCallers]
[assembly : SecurityTransparent]
