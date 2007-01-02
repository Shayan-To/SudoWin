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
using System.Data;
using Sudowin.Common;
using Sudowin.Plugins;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.ComponentModel;
using System.ServiceProcess;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace Sudowin.Server
{
	/// <summary>
	///		Service controller for the Sudowin service.
	/// </summary>
	public partial class Controller : ServiceBase
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.Server.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Controller()
		{
			InitializeComponent();
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="args">
		///		
		/// </param>
		protected override void OnStart( string[] args )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod, 
				"entering OnStart" );
			
			// get the path to the actual service executable
			Uri uri = new Uri( 
				System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase );

			string remote_config_uri = uri.LocalPath + ".config";

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose, 
				"configuring remoting with " + remote_config_uri );
				
			// configure remoting channels and objects
			RemotingConfiguration.Configure( remote_config_uri, true );

			// load the plugins
			PluginConfigurationSchema pcs = new PluginConfigurationSchema();
			pcs.ReadXml( ConfigurationManager.AppSettings[ "pluginConfigurationUri" ] );
			LoadPlugins( pcs, "authenticationPlugin" );
			LoadPlugins( pcs, "authorizationPlugin" );
			LoadPlugins( pcs, "credentialsCachePlugin" );
			
			// create the sa object
			//DataServer ds = Activator.GetObject( typeof( DataServer ),
			//	System.Configuration.ConfigurationManager.AppSettings[ "dataServerUri" ] )
			//	as DataServer;

			// activate the data server object first before any of the
			// sudo clients can do so.  this will cause any exceptions that
			// might get thrown while reading the sudoers data source to
			// cause the service not to start, rather than crashing a sudo
			// client application.
			// ds.Activate();

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStart" );
		}

		/// <summary>
		///		Stops this service.
		/// </summary>
		protected override void OnStop()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int )  EventIds.EnterMethod, 
				"entering OnStop" );
			m_ts.Close();
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStop" );
		}
		
		private void LoadPlugins( PluginConfigurationSchema pluginConfigSchema, string pluginType )
		{
			// determine the plugin type
			DataRowCollection drc = null;
			switch ( pluginType )
			{
				case "authenticationPlugin" :
				{
					drc = pluginConfigSchema.authenticationPlugin.Rows;
					break;
				}
				case "authorizationPlugin" :
				{
					drc = pluginConfigSchema.authorizationPlugin.Rows;
					break;
				}
				case "credentialsCachePlugin" :
				{
					drc = pluginConfigSchema.credentialsCachePlugin.Rows;
					break;
				}
				default:
				{
					// do nothing
					break;
				}
			}
			
			int x = 0;
			
			// activate the plugin assemblies
			foreach ( DataRow r in drc )
			{
				string assem_str = Convert.ToString( 
					r[ "assemblyString" ], CultureInfo.CurrentCulture );
				
			 	Regex rx =
					new Regex( @"^(?<assembly>.+)\.(?<class>[^,]+),(?<assemblyVersion>.+)$",
					RegexOptions.IgnoreCase );
				Match m = rx.Match( assem_str );
				if ( !m.Success )
				{
					string msg = string.Format( CultureInfo.CurrentCulture,
						"assemblyString is not properly formatted, {0}", assem_str );
					m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
					throw new System.Configuration.ConfigurationErrorsException( msg );
				}
				
				bool plugin_enabled = Convert.ToBoolean( r[ "enabled" ] );
				if ( plugin_enabled )
				{
					//
					// register the plugin as a remoting object -- the plugins
					// will have the following uri formats:
					//
					// pluginTypeXX.rem 
					// 
					// where the XX is plugin index (0 based) in its section 
					// in the plugin configuration file.  for example, the 2nd 
					// authorization plugin's uri would be:
					//
					// authorizationPlugin01.rem
					//
					Type t = Type.GetType( assem_str, true, true );
					string uri = string.Format( "{0}{0:d2}.rem", pluginType, x );
					RemotingConfiguration.RegisterWellKnownServiceType( t, uri, 
						( WellKnownObjectMode ) Enum.Parse( typeof( WellKnownObjectMode ),
							Convert.ToString( r[ "serverType" ], CultureInfo.CurrentCulture ) ) );
					
					// get a reference to the remoting object we just created
					IPlugin ip = Activator.GetObject( typeof( IPlugin ), uri ) as IPlugin;

					// activate the remoting object first before any of the sudo clients
					// do so through a sudo invocation.  this will cause any exceptions
					// that might get thrown in the plugin's construction to do so now,
					// causing this service not to start.  it is better that the sudowin
					// service fail outright than have a client application crash later
					ip.Activate();
				}

				++x;
			}
		}
	}
}
