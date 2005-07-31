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
	///		between calls to the sudo server.
	/// </summary>
	internal class DataServer : MarshalByRefObject
	{ 
		/// <summary>
		///		Interface used to access sudoers data.
		/// </summary>
		private Sudo.Data.IDataStore m_sudoers_ds;

		/// <summary>
		///		Collection of InvalidLogonInfo structures used
		///		to track information about invalid logon attempts
		///		made by users when they call sudo.
		/// </summary>
		private Dictionary<string, InvalidLogonInfo> m_ilis =
			new Dictionary<string, InvalidLogonInfo>();

		/// <summary>
		///		Collection of SecureStrings used to persist
		///		the passwords of the users who invoke sudo.
		/// </summary>
		private Dictionary<string, SecureString> m_passwords =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_ilis when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		This mutex is used to synchronize access
		///		to the m_ilis, m_passwords, and m_tmrs collections.
		/// </summary>
		private Mutex m_coll_mtx = new Mutex( false );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public DataServer()
		{
			// get the sudoers data source connection string
			string sudoers_ds_cnxn_string;
			ManagedMethods.GetConfigValue(
				"sudoersDataStoreConnectionString",
				out sudoers_ds_cnxn_string );
			
			// if the sudodersDataStoreConnectionString key is
			// not specified in the config file then throw
			// an exception
			if ( sudoers_ds_cnxn_string.Length == 0 )
				throw new System.Configuration.ConfigurationErrorsException(
					"sudodersDataStoreConnectionString must be a " +
					"specified key in the appSettings section of the " +
					"config file" );

			// if a schema file uri was specified in the
			// config file get the file uri
			string schema_uri;
			ManagedMethods.GetConfigValue( "schemaFileUri", out schema_uri );

			// use a sudoers file as the sudoers data store
			m_sudoers_ds = new Sudo.Data.FileClient.FileDataStore()
				as Sudo.Data.IDataStore;

			// open a connection to the sudoers data store
			m_sudoers_ds.Open( sudoers_ds_cnxn_string, new Uri( schema_uri ) );
		}

		/// <summary>
		///		Gets the invalid logon info for the
		///		given user name.
		/// </summary>
		/// <param name="userName">
		///		User name to look for.
		/// </param>
		/// <returns>
		///		If the InvalidLogonInfo structure for the given
		///		user name exists in the cache then this
		///		method will return the information for that
		///		user name.
		/// 
		///		If the InvalidLogonInfo structure for the given 
		///		user name does not exist in the cache then this 
		///		method will return a new InvalidLogonInfo structure.
		/// </returns>
		[DebuggerHidden]
		public InvalidLogonInfo GetInvalidLogonInfo( string userName )
		{
			// InvalidLogonInfo structure this method will return
			InvalidLogonInfo ili;

			// get the InvalidLogonInfo structure with the given
			// user name as the collection's key
			m_coll_mtx.WaitOne();
			bool is_cached = m_ilis.TryGetValue( userName, out ili );
			m_coll_mtx.ReleaseMutex();

			if ( is_cached )
				return ( ili );
			else
				return ( new InvalidLogonInfo() );
		}

		/// <summary>
		///		If the InvalidLogonInfo for the given user name
		///		does not already exist in the InvalidLogonInfo
		///		collection then the it is added.
		/// 
		///		If the InvalidLogonInfo for the given user name
		///		does already exist in the the InvalidLogonInfo
		///		collection then the existing data is updated.
		/// </summary>
		/// <param name="userName">
		///		User name to set the InvalidLogonInfo for.
		/// </param>
		/// <param name="invalidLogonInfo">
		///		InvalidLogonInfo to set.
		/// </param>
		[DebuggerHidden]
		public void SetInvalidLogonInfo( 
			string userName, 
			InvalidLogonInfo invalidLogonInfo )
		{
			m_coll_mtx.WaitOne();
			
			// whether or not the invalidLogonInfo structure
			// with the userName parameter for its key
			// is already is the m_ilis collection
			bool is_cached = m_ilis.ContainsKey( userName );
			
			if ( is_cached )
				m_ilis[ userName ] = invalidLogonInfo;
			else
				m_ilis.Add( userName, invalidLogonInfo );

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Remove the InvalidLogonInfo structure from the collection.
		/// </summary>
		/// <param name="userName">
		///		User name that is the key of the item in the collection
		///		to remove.
		/// </param>
		/// <param name="secondsUntil">
		///		Number of seconds to wait until the item is removed.
		/// </param>
		public void RemoveInvalidLogonInfo( string userName, int secondsUntil )
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
					new TimerCallback( RemoveUserInfoCallback ),
					userName, secondsUntil * 1000, Timeout.Infinite ) );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Callback method that removes a invalidLogonInfo structure
		///		from m_ilis
		/// </summary>
		/// <param name="state">
		///		User name string.
		/// </param>
		private void RemoveUserInfoCallback( object state )
		{
			m_coll_mtx.WaitOne();

			// cast this callback's state as 
			// a user name string
			string un = state as string;

			// remove the invalidLogonInfo structure 
			// for the given user name from
			// m_ilis
			m_ilis.Remove( un );

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
		///		Gets a plain-text version of the user's password
		///		from the m_passwords collection.
		/// </summary>
		/// <param name="userName">
		///		User name to get the password for and to use
		///		as the key for the m_passwords collection.
		/// </param>
		/// <returns>
		///		If the password exists in the collection a plain-text 
		///		version of the users password that is persisted as a 
		///		SecureString.
		/// 
		///		If the password does not exist an empty string.
		/// </returns>
		public string GetPassword( string userName )
		{
			m_coll_mtx.WaitOne();

			string p = string.Empty;

			if ( m_passwords.ContainsKey( userName ) )
			{
				SecureString ss = m_passwords[ userName ];

				IntPtr ps = Marshal.SecureStringToBSTR( ss );
				p = Marshal.PtrToStringBSTR( ps );
				Marshal.FreeBSTR( ps );
			}

			m_coll_mtx.ReleaseMutex();

			return ( p );
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
		public void SetPassword( string userName, string password )
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
		///		Gets the number of invalid
		///		logon attempts the user is allowed.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of invalid logon attempts the
		///		user is allowed.
		/// </returns>
		public int GetInvalidLogons( string userName )
		{
			return m_sudoers_ds.GetInvalidLogons( userName );
		}

		/// <summary>
		///		Gets the number of times the user
		///		has exceeded their invalid logon
		///		attempt limit.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of times the user has exceeded 
		///		their invalid logon attempt limit.
		/// </returns>
		public int GetTimesExceededInvalidLogons( string userName )
		{
			return m_sudoers_ds.GetTimesExceededInvalidLogons( userName );
		}

		/// <summary>
		///		Gets the number of seconds that the sudo
		///		server keeps track of a user's invalid
		///		logon attempts.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of seconds that the sudo server
		///		keeps track of a user's invalid logon
		///		attempts.
		/// </returns>
		public int GetInvalidLogonTimeout( string userName )
		{
			return m_sudoers_ds.GetInvalidLogonTimeout( userName );
		}

		/// <summary>
		///		Get's the number of seconds that a user
		///		is locked out after exceeding their
		///		invalid logon attempt limit.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of seconds that a user is locked
		///		out after exceed their invalid logon
		///		attempt limit.
		/// </returns>
		public int GetLockoutTimeout( string userName )
		{
			return m_sudoers_ds.GetLockoutTimeout( userName );
		}

		/// <summary>
		///		Gets the number of seconds that a
		///		user's valid logon is cached.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of seconds that a user's
		///		valid logon is cached.
		/// </returns>
		public int GetLogonTimeout( string userName )
		{
			return m_sudoers_ds.GetLogonTimeout( userName );
		}

		/// <summary>
		///		Gets the name of the group that possesses
		///		the same privileges that the user will
		///		when they use sudo.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use sudo.
		/// </returns>
		public string GetPrivilegesGroup( string userName )
		{
			return m_sudoers_ds.GetPrivilegesGroup( userName );
		}

		/// <summary>
		///		Checks to see if the user has the right
		///		to execute the given command with sudo.
		/// </summary>
		/// <param name="userName">
		///		Name of the user that is being verified as able
		///		to execute the given command with sudo.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		public bool IsCommandAllowed(
			string userName,
			string commandPath,
			string commandArguments )
		{
			return ( m_sudoers_ds.IsCommandAllowed( userName, commandPath, commandArguments ) );
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
