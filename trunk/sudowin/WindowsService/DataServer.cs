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
using System.Security;
using System.Threading;
using Sudowin.PublicLibrary;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Sudowin.WindowsService
{
	/// <summary>
	///		Class instantiated as a Singleton object used to 
	///		store and access persistent information about sudoers
	///		between calls to the sudo sa.
	/// </summary>
	internal class DataServer : MarshalByRefObject
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Interface used to access the authorization data source.
		/// </summary>
		private Sudowin.AuthorizationPlugins.IAuthorizationPlugin m_auth_ds;

		/// <summary>
		///		Collection of UserCache structures used
		///		to track information about users when they
		///		call Sudowin.
		/// </summary>
		private Dictionary<string, UserCache> m_ucs =
			new Dictionary<string, UserCache>();

		/// <summary>
		///		Collection of SecureStrings used to persist
		///		the passphrases of the users who invoke Sudowin.
		/// </summary>
		private Dictionary<string, SecureString> m_passphrases =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_ucs when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		This mutex is used to synchronize access
		///		to the m_ucs, m_passphrases, and m_tmrs collections.
		/// </summary>
		private Mutex m_coll_mtx = new Mutex( false );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public DataServer()
		{
			m_ts.TraceEvent( TraceEventType.Start, 10, "constructing DataServer" );

			// get the authorization plugin connection string
			string authz_cnxn_str;
			ManagedMethods.GetConfigValue( 
				"authorizationPluginConnectionString",
				out authz_cnxn_str );
			m_ts.TraceEvent( TraceEventType.Verbose, 10,
				"authorizationPluginConnectionString=" + authz_cnxn_str );
			if ( authz_cnxn_str.Length == 0 )
			{
				string msg = "sudodersDataStoreConnectionString must be a " +
					"specified key in the appSettings section of the " +
					"config file";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// get the authorization plugin schema file uri
			string authz_schema_uri;
			ManagedMethods.GetConfigValue( "authorizationPluginSchemaFileUri", out authz_schema_uri );
			//if ( authz_schema_uri.Length == 0 )
			//{
			//	string msg = "authorizationPluginSchemaFileUri must be a " +
			//		"specified key in the appSettings section of the " +
			//		"config file";
			//	m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
			//	throw new System.Configuration.ConfigurationErrorsException( msg );
			//}

			// get and parse the authorization plugin uri
			string authz_plugin_uri;
			ManagedMethods.GetConfigValue( "authorizationPluginAssembly", out authz_plugin_uri );
			if ( authz_plugin_uri.Length == 0 )
			{
				string msg = "authorizationPluginAssembly must be a " +
					"specified key in the appSettings section of the " +
					"config file";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}
			Regex rx_authz_plugin_uri = new Regex( "^(?<type>[^,]+),(?<assembly>.+)$", RegexOptions.IgnoreCase );
			Match m_authz_plugin_uri = rx_authz_plugin_uri.Match( authz_plugin_uri );
			if ( !m_authz_plugin_uri.Success )
			{
				string msg = "authorizationPluginAssembly is not " +
					"properly formatted";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// load the authorization plugin
			Assembly authz_plugin_assem = Assembly.Load( m_authz_plugin_uri.Groups[ "assembly" ].Value );
			m_auth_ds = authz_plugin_assem.CreateInstance( m_authz_plugin_uri.Groups[ "type" ].Value )
				as Sudowin.AuthorizationPlugins.IAuthorizationPlugin;
			if ( m_auth_ds == null )
			{
				string msg = "there was an error in loading the specified " +
					"authorizationPluginAssembly";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// open a connection to the authorization plugin's data source
			m_auth_ds.Open( authz_cnxn_str, new Uri( authz_schema_uri ) );

			m_ts.TraceEvent( TraceEventType.Start, 10, "constructed DataServer" );
		}

		/// <summary>
		///		Gets the UserCache for the given
		///		user name.
		/// </summary>
		/// <param name="userName">
		///		User name to look for.
		/// </param>
		/// <param name="userCache">
		///		Reference to returned UserCache structure.
		/// </param>
		/// <returns>
		///		If the UserCache structure for the given
		///		user name exists in the cache then this
		///		method will return the information for that
		///		user name.
		/// 
		///		If the UserCache structure for the given 
		///		user name does not exist in the cache then this 
		///		method will return a new UserCache structure.
		/// </returns>
		[DebuggerHidden]
		public bool GetUserCache( string userName, ref UserCache userCache )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetUserCache( string, ref UserCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},userCache=", userName );

			m_coll_mtx.WaitOne();
			bool isUserCacheCached = m_ucs.TryGetValue( userName, out userCache );
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isUserCacheCached={1}", userName, isUserCacheCached );
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting GetUserCache( string, ref UserCache )" );

			return ( isUserCacheCached );
		}

		[DebuggerHidden]
		public bool GetUserCache( string userName, ref string passphrase )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetUserCache( string, ref string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},passphrase=", userName );
			
			m_coll_mtx.WaitOne();
			bool ispassphraseCached;
			if ( ispassphraseCached = m_passphrases.ContainsKey( userName ) )
			{
				SecureString ss = m_passphrases[ userName ];
				IntPtr ps = Marshal.SecureStringToBSTR( ss );
				passphrase = Marshal.PtrToStringBSTR( ps );
				Marshal.FreeBSTR( ps );
			}
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, ispassphraseCached={1}", userName, ispassphraseCached );
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting GetUserCache( string, ref string )" );

			return ( ispassphraseCached );
		}

		/// <summary>
		///		If the UserCache for the given user name
		///		does not already exist in the UserCache
		///		collection then the it is added.
		/// 
		///		If the UserCache for the given user name
		///		does already exist in the the UserCache
		///		collection then the existing data is updated.
		/// </summary>
		/// <param name="userName">
		///		User name to set the UserCache for.
		/// </param>
		/// <param name="userCache">
		///		UserCache to set.
		/// </param>
		[DebuggerHidden]
		public void SetUserCache( 
			string userName, 
			UserCache userCache )
		{
			m_coll_mtx.WaitOne();
			
			// whether or not the userCache structure
			// with the userName parameter for its key
			// is already is the m_ucs collection
			bool is_cached = m_ucs.ContainsKey( userName );
			
			if ( is_cached )
				m_ucs[ userName ] = userCache;
			else
				m_ucs.Add( userName, userCache );

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Expire the UserCache structure for the given userName
		///		in the number of seconds defined in secondsUntil.
		/// </summary>
		/// <param name="userName">
		///		User name that is the key of the item in the collection
		///		to remove.
		/// </param>
		/// <param name="secondsUntil">
		///		Number of seconds to wait until the UserCache is expired.
		/// </param>
		public void ExpireUserCache( string userName, int secondsUntil )
		{
			m_coll_mtx.WaitOne();

			// if the timers collection already contains a timer
			// for this user then change when it is supposed to fire
			if ( m_tmrs.ContainsKey( userName ) )
			{
				m_tmrs[ userName ].Change( 
					secondsUntil * 1000, Timeout.Infinite );
			}
			// add a new timer and a new changed value
			else
			{
				m_tmrs.Add( userName, new Timer(
					new TimerCallback( ExpireUserCacheCallback ),
					userName, secondsUntil * 1000, Timeout.Infinite ) );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Callback method that removes a UserCache structure
		///		from m_ucs and a SecureString from m_passphrases.
		/// </summary>
		/// <param name="state">
		///		User name that is the key to the m_passphrases and m_ucs
		///		collections with the objects that are to be removed.
		/// </param>
		private void ExpireUserCacheCallback( object state )
		{
			m_coll_mtx.WaitOne();

			// cast this callback's state as 
			// a user name string
			string un = state as string;

			// if the user has a UserCache structure
			// in the collection then remove it
			if ( m_ucs.ContainsKey( un ) )
				m_ucs.Remove( un );

			// if the user has a persisted passphrase clear
			// it and then remove it from the collection
			if ( m_passphrases.ContainsKey( un ) )
			{
				m_passphrases[ un ].Clear();
				m_passphrases.Remove( un );
			}

			// dispose of the timer that caused this
			// callback and remove it from m_tmrs
			m_tmrs[ un ].Dispose();
			m_tmrs.Remove( un );

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Creates a SecureString version of the given
		///		plain-text passphrase in the m_passphrases collection
		///		for the given userName.
		/// </summary>
		/// <param name="userName">
		///		User name to create SecureString passphrase for
		///		and to use as the key for the m_passphrases collection.
		/// </param>
		/// <param name="passphrase">
		///		Plain-text passphrase to convert into a SecureString.
		/// </param>
		public void SetUserCache( string userName, string passphrase )
		{
			m_coll_mtx.WaitOne();

			SecureString ss = new SecureString();

			for ( int x = 0; x < passphrase.Length; ++x )
					ss.AppendChar( passphrase[ x ] );

			if ( m_passphrases.ContainsKey( userName ) )
			{
				m_passphrases[ userName ].Clear();
				m_passphrases[ userName ] = ss;
			}
			else
			{
				m_passphrases.Add( userName, ss );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Gets a Sudowin.PublicLibrary.UserInfo structure
		///		from the authorization plugin for the given user name.
		/// </summary>
		/// <param name="userName">
		///		User name to get information for.
		/// </param>
		/// <param name="userInfo">
		///		Sudowin.PublicLibrary.UserInfo structure for
		///		the given user name.
		/// </param>
		/// <returns>
		///		True if the UserInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		public bool GetUserInfo( string userName, ref UserInfo userInfo )
		{
			return ( m_auth_ds.GetUserInfo( userName, ref userInfo ) );
		}

		/// <summary>
		///		Gets a Sudowin.PublicLibrary.CommandInfo structure
		///		from the authorization plugin for the given user name,
		///		command p, and command arguments.
		/// </summary>
		/// <param name="username">
		///		User name to get information for.
		/// </param>
		/// <param name="commandPath">
		///		Command p to get information for.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments to get information for.
		/// </param>
		/// <param name="commandInfo">
		///		Sudowin.PublicLibrary.CommandInfo structure for
		///		the given user name, command p, and command 
		///		arguments.
		/// </param>
		/// <returns>
		///		True if the CommandInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		public bool GetCommandInfo(
			string username,
			string commandPath,
			string commandArguments,
			ref CommandInfo commandInfo )
		{
			return ( m_auth_ds.GetCommandInfo( 
				username, commandPath, commandArguments, ref commandInfo ) );
		}

		/// <summary>
		///		Dummy method used by the windows service to
		///		activate this object before anyone else does.
		/// </summary>
		public void Activate()
		{
		}
	}
}
