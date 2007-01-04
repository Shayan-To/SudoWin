/*
Copyright (c) 2005, 2006, 2007, Schley Andrew Kutz <akutz@lostcreations.com>
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
using System.Globalization;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Lifetime;

namespace Sudowin.Plugins
{
	public class Plugin : MarshalByRefObject, IPlugin
	{
		/// <summary>
		///		This class is not meant to be directly instantiated.
		/// </summary>
		protected Plugin()
		{
			
		}

		/// <summary>
		///		This will always return a lifetime lease for plugins
		///		that operate as singletons.  Plugins that operate as
		///		singlecall objects will simply ignore this.
		/// </summary>
		/// <returns>
		///		This object's lease.
		/// </returns>
		/// <remarks>
		///		See http://msdn2.microsoft.com/en-us/library/23bk23zc.aspx 
		///		for more information on leases.
		/// </remarks>
		public override object InitializeLifetimeService()
		{
			// i have to talk with someone who knows more about remoting
			// and leases than me.  for some reason this is not working.
			//
			/*if ( ServerType == "Singleton" )
			{
				ILease lease = base.InitializeLifetimeService() as ILease;
				if ( lease.CurrentState == LeaseState.Initial )
				{
					lease.InitialLeaseTime = TimeSpan.FromSeconds( PluginServerLifetime );
					//lease.SponsorshipTimeout = TimeSpan.FromMinutes( 2 );
					//lease.RenewOnCallTime = TimeSpan.FromSeconds( 2 );
				}
				return ( lease );
			}
			else
			{
				return ( base.InitializeLifetimeService() );
			}*/
			
			return ( null );
		}
		
		/// <summary>
		///		The sudowin service's plugin configuration.
		/// </summary>
		private PluginConfigurationSchema m_pcs = null;

		/// <summary>
		///		The sudowin service's plugin configuration.
		/// </summary>
		private PluginConfigurationSchema PluginConfigSchema
		{
			get
			{
				if ( m_pcs == null )
				{
					m_pcs = new PluginConfigurationSchema();
					m_pcs.ReadXml( ConfigurationManager.AppSettings[ "pluginConfigurationUri" ] );
				}
				
				return ( m_pcs );
			}
		}
		
		/// <summary>
		///		Returns this plugin's configuration string if it
		///		is defined in the plugin configuration file;
		///		otherwise null.
		/// </summary>
		private string m_plugin_connection_string = null;

		/// <summary>
		///		Returns this plugin's configuration string if it
		///		is defined in the plugin configuration file;
		///		otherwise null.
		/// </summary>
		protected string PluginConnectionString
		{
			get
			{
				if ( m_plugin_connection_string == null )
				{
					m_plugin_connection_string = 
						GetStringValue( PluginConfigSchema.plugin[ PluginIndex ][ "connectionString" ], null );
				}
				return ( m_plugin_connection_string );
			}
		}

		/// <summary>
		///		Returns this plugin's schema uri if it
		///		is defined in the plugin configuration file;
		///		otherwise null.
		/// </summary>
		private string m_plugin_schema_uri = null;

		/// <summary>
		///		Returns this plugin's schema uri if it
		///		is defined in the plugin configuration file;
		///		otherwise null.
		/// </summary>
		protected string PluginSchemaUri
		{
			get
			{
				if ( m_plugin_schema_uri == null )
				{
					m_plugin_schema_uri =
						GetStringValue( PluginConfigSchema.plugin[ PluginIndex ][ "schemaUri" ], null );
				}
				return ( m_plugin_schema_uri );
			}
		}

		/// <summary>
		///		Returns this plugin's server type if it
		///		is defined in the plugin configuration file;
		///		otherwise SingleCall.
		/// </summary>
		private string m_server_type = null;

		/// <summary>
		///		Returns this plugin's server type if it
		///		is defined in the plugin configuration file;
		///		otherwise SingleCall.
		/// </summary>
		protected string ServerType
		{
			get
			{
				if ( m_server_type == null )
				{
					m_server_type =
						GetStringValue( PluginConfigSchema.plugin[ PluginIndex ][ "serverType" ], "SingleCall" );
				}
				return ( m_server_type );
			}
		}

		/// <summary>
		///		Returns this plugin's server lifetime if it
		///		is defined in the plugin configuration file;
		///		otherwise 0.
		/// </summary>
		private int m_plugin_server_lifetime = -1;

		/// <summary>
		///		Returns this plugin's server lifetime if it
		///		is defined in the plugin configuration file;
		///		otherwise 0.
		/// </summary>
		protected int PluginServerLifetime
		{
			get
			{
				if ( m_plugin_server_lifetime == -1 )
				{
					m_plugin_server_lifetime =
						GetInt32Value( PluginConfigSchema.plugin[ PluginIndex ][ "serverLifetime" ], 0 );
				}
				return ( m_plugin_server_lifetime );
			}
		}

		/// <summary>
		///		The 0-based index of the plugin in the plugin configuration file.
		/// </summary>
		private int m_plugin_index = -1;
		
		/// <summary>
		///		The 0-based index of the plugin in the plugin configuration file.
		/// </summary>
		protected int PluginIndex
		{
			get
			{
				if ( m_plugin_index == -1 )
				{
					string uri = System.Runtime.Remoting.RemotingServices.GetObjectUri( this );
					string index = Regex.Match( uri, @"^.*(?<index>\d{2})\.rem$", 
						RegexOptions.IgnoreCase ).Groups[ "index" ].Value;
					m_plugin_index = Convert.ToInt32( index );
				}
				return ( m_plugin_index );
			}
		}
		
		/// <summary>
		///		Gets a string value from a DB value and returns the
		///		given defaultValue if the give value is DBNull.
		/// </summary>
		/// <param name="value">The DB value to convert to a string.</param>
		/// <param name="defaultValue">The value to return if the given value is DBNull.</param>
		/// <returns>
		///		If value is not DBNull then that value converted to a string;
		///		otherwise defaultValue.
		/// </returns>
		private string GetStringValue( object value, string defaultValue )
		{
			return ( value is DBNull ? defaultValue : Convert.ToString( value, CultureInfo.CurrentCulture ) );
		}

		/// <summary>
		///		Gets an integer value from a DB value and returns the
		///		given defaultValue if the give value is DBNull.
		/// </summary>
		/// <param name="value">The DB value to convert to an integer.</param>
		/// <param name="defaultValue">The value to return if the given value is DBNull.</param>
		/// <returns>
		///		If value is not DBNull then that value converted to an integer;
		///		otherwise defaultValue.
		/// </returns>
		private int GetInt32Value( object value, int defaultValue )
		{
			return ( value is DBNull ? defaultValue : Convert.ToInt32( value ) );
		}
		
		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		public virtual void Activate()
		{
			this.Activate( string.Empty );
		}

		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		/// <param name="activationData">
		///		A plugin designer can pass data into the plugin from the plugin configuration
		///		file by passing the data into the plugin as a string formatted variable.
		/// </param>
		public virtual void Activate( string activationData )
		{
			
		}
	}
}
