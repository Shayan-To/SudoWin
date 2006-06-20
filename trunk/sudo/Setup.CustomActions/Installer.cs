/*
Copyright (c) 2005, 2006, Schley Andrew Kutz <akutz@lostcreations.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of l o s t c r e a t i o n s nor the names of its 
    contributors may be used to endorse or promote products derived from this 
    software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.ComponentModel;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Text.RegularExpressions;

namespace Sudo.Setup.CustomActions
{
	[RunInstaller( true )]
	public partial class Installer : System.Configuration.Install.Installer
	{
		public Installer()
		{
			InitializeComponent();
		}

		public override void Install( System.Collections.IDictionary stateSaver )
		{
			base.Install( stateSaver );

			// create a group called Sudoers on the local machine if it
			// does not exist
			DirectoryEntry de = new DirectoryEntry( string.Format( "WinNT://{0},computer",
				Environment.MachineName ) );
			DirectoryEntry grp = null;
			try
			{
				grp = de.Children.Find( "Sudoers", "group" );
			}
			catch
			{
			}

			if ( grp == null )
			{
				grp = de.Children.Add( "Sudoers", "group" );
				grp.Properties[ "description" ].Value = "Members in this group have the required " +
					"privileges to initiate secure communication channels with the sudo server.";
				grp.CommitChanges();
			}

			grp.Close();
			de.Close();

			// TODO: ask what users should be sudoers and add them to the group and sudoers.xml file

			#region Edit the Sudo.WindowsService.exe.config file

			const string XpathTranslateFormat =
				"translate({0},'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')";

			string target_dir = this.Context.Parameters[ "TargetDir" ];
			string connectionString = string.Format( @"{0}Server\Sudo.WindowsService.exe.config",
				target_dir );

			// throw an exception if the xml file is not found
			if ( !System.IO.File.Exists( connectionString ) )
				throw new System.IO.FileNotFoundException(
					"xml file not found", connectionString );

			// create a xmlreadersettings object
			// to specify how to read in the file
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.CloseInput = true;
			xrs.IgnoreComments = false;

			// read in the file
			XmlReader xr = XmlReader.Create( connectionString, xrs );

			// load the xml reader into the xml document.
			XmlDocument xml_doc = new XmlDocument();
			xml_doc.Load( xr );

			// close the xmlreader
			xr.Close();

			// create the namespace manager using the xml file name table
			XmlNamespaceManager xml_ns_mgr = new XmlNamespaceManager( xml_doc.NameTable );

			// if there is a default namespace specified in the
			// xml file then it needs to be added to the namespace
			// manager so the xpath queries will work
			Regex ns_rx = new Regex(
				@"xmlns\s{0,}=\s{0,}""([\w\d\.\:\/\\]{0,})""",
				RegexOptions.Multiline | RegexOptions.IgnoreCase );
			Match ns_m = ns_rx.Match( xml_doc.InnerXml );

			// add the default namespace
			string default_ns = ns_m.Groups[ 1 ].Value;
			xml_ns_mgr.AddNamespace( "d", default_ns );

			// build the query, find the node, set the value
			string user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'authorizationPluginConnectionString'" ) );
			XmlNode node = xml_doc.SelectSingleNode( user_xpq, xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "value" ].Value = string.Format( @"{0}Server\sudoers.xml",
					target_dir );

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'authorizationPluginSchemaFileUri'" ) );
			node = xml_doc.SelectSingleNode( user_xpq, xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "value" ].Value = string.Format( @"{0}Server\XmlAuthorizationPluginSchema_v0.1.xsd",
					target_dir );

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'callbackApplicationPath'" ) );
			node = xml_doc.SelectSingleNode( user_xpq, xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "value" ].Value = string.Format( @"{0}Callback\Sudo.CallbackApplication.exe",
					target_dir );

			// <source name="traceSrc" switchValue="ActivityTracing, Verbose">
			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:source[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'traceSrc'" ) );
			node = xml_doc.SelectSingleNode( user_xpq, xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "switchValue" ].Value = "Error";

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'traceListener'" ) );
			node = xml_doc.SelectSingleNode( user_xpq, xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "initializeData" ].Value = string.Format( @"{0}Server\service.log",
					target_dir );

			// save it back to the file
			xml_doc.Save( connectionString );

			#endregion
		}

		public override void Rollback( System.Collections.IDictionary savedState )
		{
			// delete the Sudoers group on the local machine if it exists
			DirectoryEntry de = new DirectoryEntry( string.Format( "WinNT://{0},computer",
				Environment.MachineName ) );
			DirectoryEntry grp = null;
			try
			{
				grp = de.Children.Find( "Sudoers", "group" );
			}
			catch
			{
			}

			if ( grp != null )
			{
				de.Children.Remove( grp );
				grp.Close();
			}
			de.Close();

			base.Rollback( savedState );

			// remove the installation directory
			//string target_dir = this.Context.Parameters[ "TargetDir" ];
			//if ( Directory.Exists( target_dir ) )
			//	Directory.Delete( target_dir );
		}

		public override void Uninstall( System.Collections.IDictionary savedState )
		{
			// delete the Sudoers group on the local machine if it exists
			DirectoryEntry de = new DirectoryEntry( string.Format( "WinNT://{0},computer",
				Environment.MachineName ) );
			DirectoryEntry grp = null;
			try
			{
				grp = de.Children.Find( "Sudoers", "group" );
			}
			catch
			{
			}

			if ( grp != null )
			{
				de.Children.Remove( grp );
				grp.Close();
			}
			de.Close();
			
			base.Uninstall( savedState );

			// remove the installation directory
			//string target_dir = this.Context.Parameters[ "TargetDir" ];
			//if ( Directory.Exists( target_dir ) )
			//	Directory.Delete( target_dir );
		}
	}
}