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
using Sudo.Shared;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sudo.Service
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
		///		Collection of UserInfo structures used
		///		to store persistent information about
		///		users who invoke sudo.
		/// </summary>
		private Dictionary<string, UserInfo> m_user_infos =
			new Dictionary<string, UserInfo>();

		/// <summary>
		///		Used to synchronize access to m_user_infos
		///		for asynchronous methods.
		/// </summary>
		private ReaderWriterLock m_user_infos_rwl =
			new ReaderWriterLock();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_user_infos when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_user_infos_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		Used to synchronize access to m_user_infos_tmrs
		///		for asynchronous methods.
		/// </summary>
		private ReaderWriterLock m_user_infos_tmrs_rwl =
			new ReaderWriterLock();

		/// <summary>
		///		Default constructor.
		/// </summary>
		public DataServer()
		{
			// get the sudoers data source connection string
			string sudoers_ds_cnxn_string;
			CommonMethods.GetConfigValue(
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
			CommonMethods.GetConfigValue( "schemaFileUri", out schema_uri );

			// use a sudoers file as the sudoers data store
			m_sudoers_ds = new Sudo.Data.FileClient.FileDataStore()
				as Sudo.Data.IDataStore;

			// open a connection to the sudoers data store
			m_sudoers_ds.Open( sudoers_ds_cnxn_string, new Uri( schema_uri ) );
		}

		/// <summary>
		///		Gets a userInfo structure for the given
		///		user name.
		/// </summary>
		/// <param name="userName">
		///		User name to look for.
		/// </param>
		/// <returns>
		///		If the userInfo structure for the given
		///		user name exists in the cache then this
		///		method will return that user name.
		/// 
		///		If the userInfo structure for the given
		///		user name does not yet exist in the cache
		///		a new userInfo structure will be returned.
		/// </returns>
		public UserInfo GetUserInfo( string userName )
		{
			// userInfo structure this method will return
			UserInfo ui;

			// whether or not the userInfo structure
			// with the userName parameter for its key
			// is already is the m_user_infos collection
			bool is_cached = false;

			m_user_infos_rwl.AcquireReaderLock( Timeout.Infinite );

			try
			{
				is_cached = m_user_infos.TryGetValue( userName, out ui );
			}
			finally
			{
				m_user_infos_rwl.ReleaseReaderLock();
			}

			if ( is_cached )
				return ( ui );
			else
				return ( new UserInfo( 0, false ) );
		}

		/// <summary>
		///		If the user info for the given user name
		///		does not already exist in the user info
		///		collection then the it is added.
		/// 
		///		If the user info for the given user name
		///		does already exist in the the user info
		///		collection then the existing data is updated.
		/// </summary>
		/// <param name="userName">
		///		User name to set the user info for.
		/// </param>
		/// <param name="userInfo">
		///		User info to set.
		/// </param>
		public void SetUserInfo( string userName, UserInfo userInfo )
		{
			// whether or not the userInfo structure
			// with the userName parameter for its key
			// is already is the m_user_infos collection
			bool is_cached = false;

			m_user_infos_rwl.AcquireReaderLock( Timeout.Infinite );
			try
			{
				is_cached = m_user_infos.ContainsKey( userName );
			}
			finally
			{
				m_user_infos_rwl.ReleaseReaderLock();
			}

			if ( is_cached )
				m_user_infos[ userName ] = userInfo;
			else
			{
				m_user_infos_rwl.AcquireWriterLock( Timeout.Infinite );
				try 
				{
					m_user_infos.Add( userName, userInfo );
				}
				finally
				{
					m_user_infos_rwl.ReleaseWriterLock();
				}
			}
		}

		/// <summary>
		///		Remove the userInfo structure from the collection.
		/// </summary>
		/// <param name="userName">
		///		User name that is the key of the item in the collection
		///		to remove.
		/// </param>
		/// <param name="secondsUntil">
		///		Number of seconds to wait until the item is removed.
		/// </param>
		public void RemoveUserInfo( string userName, int secondsUntil )
		{
			// create a timer to remove the userInfo structure
			// from the collection after so many seconds
			Timer t = new Timer( new TimerCallback( RemoveUserInfoCallback ),
				userName, secondsUntil * 1000, Timeout.Infinite );

			// add the timer to the timers collection
			m_user_infos_tmrs_rwl.AcquireWriterLock( Timeout.Infinite );
			try
			{
				m_user_infos_tmrs.Add( userName, t );
			}
			finally
			{
				m_user_infos_tmrs_rwl.ReleaseWriterLock();
			}
		}

		/// <summary>
		///		Callback method that removes a userInfo structure
		///		from m_user_infos
		/// </summary>
		/// <param name="state">
		///		User name string.
		/// </param>
		private void RemoveUserInfoCallback( object state )
		{
			// case this callback's state as a user name string
			string un = state as string;

			// remove the userInfo structure from
			// the userInfos collection
			m_user_infos_rwl.AcquireWriterLock( Timeout.Infinite );
			try
			{
				m_user_infos.Remove( un );
			}
			finally
			{
				m_user_infos_rwl.ReleaseWriterLock();
			}

			// shutdown the timer and remove it from
			// the timers collection
			m_user_infos_tmrs_rwl.AcquireWriterLock( Timeout.Infinite );
			try
			{
				m_user_infos_tmrs[ un ].Dispose();
				m_user_infos_tmrs.Remove( un );
			}
			finally
			{
				m_user_infos_tmrs_rwl.ReleaseWriterLock();
			}
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
		public int GetPasswordTries( string userName )
		{
			return ( m_sudoers_ds.GetPasswordTries( userName ) );
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
		public int GetPasswordTimeout( string userName )
		{
			return ( m_sudoers_ds.GetPasswordTimeout( userName ) );
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
