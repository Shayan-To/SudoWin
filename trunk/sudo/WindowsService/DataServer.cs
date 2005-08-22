/*
Copyright (c) 2005, Schley Andrew Kutz <sakutz@gmail.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of Lost Creations nor the names of its contributors may
    be used to endorse or promote products derived from this software without
    specific prior written permission.

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
using Sudo.PublicLibrary;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sudo.WindowsService
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
		///		config file for Sudo.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Interface used to access sudoers data.
		/// </summary>
		private Sudo.Data.IDataStore m_sudoers_ds;

		/// <summary>
		///		Collection of UserCache structures used
		///		to track information about users when they
		///		call sudo.
		/// </summary>
		private Dictionary<string, UserCache> m_ucs =
			new Dictionary<string, UserCache>();

		/// <summary>
		///		Collection of SecureStrings used to persist
		///		the passwords of the users who invoke sudo.
		/// </summary>
		private Dictionary<string, SecureString> m_passwords =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_ucs when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		This mutex is used to synchronize access
		///		to the m_ucs, m_passwords, and m_tmrs collections.
		/// </summary>
		private Mutex m_coll_mtx = new Mutex( false );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public DataServer()
		{
			m_ts.TraceEvent( TraceEventType.Start, 10, "constructing DataServer" );

			// get the sudoers data source connection string
			string sdcs;
			ManagedMethods.GetConfigValue(
				"sudoersDataStoreConnectionString",
				out sdcs );
			m_ts.TraceEvent( TraceEventType.Verbose, 10,
				"sudoersDataStoreConnectionString=" + sdcs );
			if ( sdcs.Length == 0 )
			{
				string msg = "sudodersDataStoreConnectionString must be a " +
					"specified key in the appSettings section of the " +
					"config file";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// get the schema file uri
			string schema_uri;
			ManagedMethods.GetConfigValue( "schemaFileUri", out schema_uri );
			if ( schema_uri.Length == 0 )
			{
				string msg = "schemaFileUri must be a " +
					"specified key in the appSettings section of the " +
					"config file";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// use a sudoers file as the sudoers data store
			m_sudoers_ds = new Sudo.Data.FileClient.FileDataStore()
				as Sudo.Data.IDataStore;

			// open a connection to the sudoers data store
			m_sudoers_ds.Open( sdcs, new Uri( schema_uri ) );

			m_ts.TraceEvent( TraceEventType.Start, 10, "constructed DataServer" );
		}

		/// <summary>
		///		Gets the UserCache for the given
		///		user name.
		/// </summary>
		/// <param name="userName">
		///		User name to look for.
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
		public bool GetUserCache( string userName, ref string password )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetUserCache( string, ref string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},password=", userName );
			
			m_coll_mtx.WaitOne();
			bool isPasswordCached;
			if ( isPasswordCached = m_passwords.ContainsKey( userName ) )
			{
				SecureString ss = m_passwords[ userName ];
				IntPtr ps = Marshal.SecureStringToBSTR( ss );
				password = Marshal.PtrToStringBSTR( ps );
				Marshal.FreeBSTR( ps );
			}
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isPasswordCached={1}", userName, isPasswordCached );
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting GetUserCache( string, ref string )" );

			return ( isPasswordCached );
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
		///		from m_ucs and a SecureString from m_passwords.
		/// </summary>
		/// <param name="state">
		///		User name that is the key to the m_passwords and m_ucs
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

			// if the user has a persisted password clear
			// it and then remove it from the collection
			if ( m_passwords.ContainsKey( un ) )
			{
				m_passwords[ un ].Clear();
				m_passwords.Remove( un );
			}

			// dispose of the timer that caused this
			// callback and remove it from m_tmrs
			m_tmrs[ un ].Dispose();
			m_tmrs.Remove( un );

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Creates a SecureString version of the given
		///		plain-text password in the m_passwords collection
		///		for the given userName.
		/// </summary>
		/// <param name="userName">
		///		User name to create SecureString password for
		///		and to use as the key for the m_passwords collection.
		/// </param>
		/// <param name="password">
		///		Plain-text password to convert into a SecureString.
		/// </param>
		public void SetUserCache( string userName, string password )
		{
			m_coll_mtx.WaitOne();

			SecureString ss = new SecureString();

			for ( int x = 0; x < password.Length; ++x )
					ss.AppendChar( password[ x ] );

			if ( m_passwords.ContainsKey( userName ) )
			{
				m_passwords[ userName ].Clear();
				m_passwords[ userName ] = ss;
			}
			else
			{
				m_passwords.Add( userName, ss );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Gets a Sudo.PublicLibrary.UserInfo structure
		///		from the sudoers data store for the given user name.
		/// </summary>
		/// <param name="userName">
		///		User name to get information for.
		/// </param>
		/// <param name="userInfo">
		///		Sudo.PublicLibrary.UserInfo structure for
		///		the given user name.
		/// </param>
		/// <returns>
		///		True if the UserInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		public bool GetUserInfo( string userName, ref UserInfo userInfo )
		{
			return ( m_sudoers_ds.GetUserInfo( userName, ref userInfo ) );
		}

		/// <summary>
		///		Gets a Sudo.PublicLibrary.CommandInfo structure
		///		from the sudoers data store for the given user name,
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
		///		Sudo.PublicLibrary.CommandInfo structure for
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
			return ( m_sudoers_ds.GetCommandInfo( 
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
