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
using System.IO;
using Sudo.Data;
using System.Text;
using Sudo.Shared;
using System.Security;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.DirectoryServices;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

namespace Sudo.Service
{
	public class Server : MarshalByRefObject, Sudo.Shared.ISudoServer, IDisposable
	{
		#region fields and properties

		/// <summary>
		///		Used to store cached credentials
		///		for a finite amount of time.
		/// </summary>
		private Dictionary<string, SecureString> m_cachd_creds =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Used for synchronizing access to
		///		m_cachd_creds.
		/// </summary>
		private ReaderWriterLock m_cachd_creds_rwl =
			new ReaderWriterLock();

		/// <summary>
		///		Used to store class scope-level timers
		///		that remove cached credentials after a 
		///		finite amount of time.
		/// </summary>
		private Dictionary<string, Timer> m_cachd_creds_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		Used for synchronizing access to
		///		m_cachd_creds_tmrs.
		/// </summary>
		private ReaderWriterLock m_cachd_creds_tmrs_rwl =
			new ReaderWriterLock();

		/// <summary>
		///		Interface used to access sudoers data.
		/// </summary>
		private Sudo.Data.IDataStore m_sudoers_ds;

		/// <summary>
		///		Gets/sets user passwords from/in the credential cache.
		/// </summary>
		/// <remarks>
		///		This property accessor makes a call to the backing
		///		store each time its get and set methods are invoked.
		/// </remarks>
		public string Password
		{
			[EnvironmentPermission( SecurityAction.LinkDemand )]
			[System.Diagnostics.DebuggerStepThrough]
			get
			{
				// get identity of caller
				WindowsIdentity wi = Thread.CurrentPrincipal.Identity as
					WindowsIdentity;

				// used to hold the cached password
				SecureString ss;
				
				m_cachd_creds_rwl.AcquireReaderLock(
					Timeout.Infinite );

				// see if the cache contains a password for 
				// the caller
				bool is_cached;
				try
				{
					is_cached = m_cachd_creds.TryGetValue( wi.Name, out ss );
				}
				finally
				{
					m_cachd_creds_rwl.ReleaseReaderLock();
				}

				// get the non-secure version
				// of the secure string if
				// one was retrieved from the cache
				string password = string.Empty;
				if ( is_cached )
				{
					// get the secure string as a BSTR
					IntPtr p_ss = Marshal.SecureStringToBSTR( ss );

					// copy the BSTR into a managed string
					password = Marshal.PtrToStringBSTR( p_ss );

					// free the BSTR
					Marshal.FreeBSTR( p_ss );
				}

				return ( password );
			}
			[System.Diagnostics.DebuggerStepThrough]
			set
			{
				// get identity of caller
				WindowsIdentity wi = Thread.CurrentPrincipal.Identity as
					WindowsIdentity;

				// get a secure string version of the password
				SecureString ss = new SecureString();
				for ( int x = 0; x < value.Length; ++x )
					ss.AppendChar( value[ x ] );

				m_cachd_creds_rwl.AcquireWriterLock(
					Timeout.Infinite );

				try
				{
					// add the password to the cache
					m_cachd_creds.Add( wi.Name, ss );
				}
				finally
				{
					m_cachd_creds_rwl.ReleaseWriterLock();
				}

				// since there was no exception thrown
				// we must create a timer to expire the
				// cached credentials after a finite
				// amount of time
				Timer t = new Timer(
					new TimerCallback( RemoveCachedCredentials ),
					wi.Name,
					// seconds * 1000 = milliseconds
					m_sudoers_ds.PasswordTimeout * 1000, 
					Timeout.Infinite );

				m_cachd_creds_tmrs_rwl.AcquireWriterLock( Timeout.Infinite );

				// add the timer to the m_cachd_creds_tmrs collection
				try
				{
					m_cachd_creds_tmrs.Add( wi.Name, t );
				}
				// one might want to add a catch block here to dispose
				// of the timer in case there is an error adding
				// it to the collection.  the reason for this would
				// be so that the timer does not unexpectedly fire
				// later on and have nothing to remove.  adding
				// such a trap is unnecessary because if an exception
				// is thrown the Timer t will no longer have any 
				// references and be automatically disposed for us.
				finally
				{
					m_cachd_creds_tmrs_rwl.ReleaseWriterLock();
				}
			}
		}

		/// <summary>
		///		Number of allowed bad password attempts a user has.
		/// </summary>
		public int PasswordTries
		{
			[System.Diagnostics.DebuggerStepThrough]
			get
			{
				return ( m_sudoers_ds.PasswordTries );
			}
		}

		/// <summary>
		///		Number of seconds sudo will cache a user's password.
		/// </summary>
		public int PasswordTimeout
		{
			[System.Diagnostics.DebuggerStepThrough]
			get
			{
				return ( m_sudoers_ds.PasswordTimeout );
			}
		}

		#endregion

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Server()
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
		///		Elevate caller to administrator privileges.
		/// </summary>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		public void BeginElevatePrivileges()
		{
			AddRemoveUser( 1 );
		}

		/// <summary>
		///		Revert caller to normal privileges.
		/// </summary>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		public void EndElevatePrivileges()
		{
			AddRemoveUser( 0 );
		}

		/// <summary>
		///		Adds or removes a user to the administrators
		///		group on the local computer.
		/// </summary>
		/// <param name="function">1 "Add" or 0 "Remove"</param>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		static private void AddRemoveUser( int which )
		{
			// get identity of caller
			WindowsIdentity wi = Thread.CurrentPrincipal.Identity as
				WindowsIdentity;
			string user_name = wi.Name.Split( new char[] { '\\' } )[ 1 ];

			// get entry for localhost
			DirectoryEntry localhost = new DirectoryEntry( "WinNT://" +
				Environment.MachineName + ",computer" );

			// get entry for the admins group
			DirectoryEntry admins = localhost.Children.Find(
				"Administrators", "group" );

			// get entry for user to add
			DirectoryEntry user = localhost.Children.Find(
				user_name, "user" );

			// used for adsi invocations
			object[] path = new object[] { user.Path };

			// find out if the user is already a member
			// of the administrators group
			bool is_member = bool.Parse( Convert.ToString(
				admins.Invoke( "IsMember", path ),
				CultureInfo.CurrentCulture ) );

			// add the user to administrators if they
			// are not already a member
			if ( which == 1 && !is_member )
				admins.Invoke( "Add", path );
			// remove the member from administrators
			// if they are in the group
			else if ( which == 0 && is_member )
				admins.Invoke( "Remove", path );

			// save changes
			admins.CommitChanges();

			// cleanup
			user.Dispose();
			admins.Dispose();
			localhost.Dispose();
		}

		/// <summary>
		///		A dummy method that will force the client
		///		to throw an exception if the caller is not
		///		a member of any of the authorizedGroups 
		///		specified for this server's channel.
		/// </summary>
		public void AuthenticateClient()
		{
			//VerifySameSignature();
		}

		private void VerifySameSignature()
		{
			// whether or not both the client and server
			// assemblies have the same strong name signature
			bool same_sig = false;
			
			// get references to the client and server assemblies
			Assembly client = Assembly.GetEntryAssembly();
			Assembly server = Assembly.GetExecutingAssembly();

			// client/server was verified
			bool client_wf = false;
			bool server_wf = false;

			// if both the client and the server assemblies
			// both have valid strong name keys then compare
			// their public key tokens
			if ( CommonMethods.StrongNameSignatureVerificationEx(
					client.Location, true, ref client_wf ) &&
				CommonMethods.StrongNameSignatureVerificationEx(
					server.Location, true, ref server_wf ) )
			{
				
				// get the client and server public key tokens
				byte[] client_pubkey = client.GetName().GetPublicKeyToken();
				byte[] server_pubkey = server.GetName().GetPublicKeyToken();

				// if the public key tokens are the same size
				// then go deeper and verify them bit by bit
				if ( client_pubkey.Length == server_pubkey.Length )
				{
					// assume both assemblies have the same
					// signature until the bit by bit comparison
					// proves us wrong

					same_sig = true;
					for ( int x = 0; x < client_pubkey.Length && same_sig; ++x )
						if ( client_pubkey[ x ] != server_pubkey[ x ] )
							same_sig = false;
				}
			}

			// if the server and client do not have the same
			// signature then throw an exception
			if ( !same_sig )
				throw ( new Exception( 
					"client and server assemblies are not " +
					"signed by the same private key" ) );
		}

		/// <summary>
		///		Checks to see if the user has the right
		///		to execute the given command with sudo.
		/// </summary>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandSwitches">
		///		Switches the command being executed is using.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		public bool IsCommandAllowed( 
			string commandPath,
			string[] commandSwitches )
		{
			return ( m_sudoers_ds.IsCommandAllowed(
				commandPath, commandSwitches ) );
		}

		/// <summary>
		///		Callback method used by the cached credentials timers.
		/// </summary>
		/// <param name="state">
		///		A key for the m_cachd_creds_tmrs dictionary.  Can cast
		///		into a string.
		/// </param>
		private void RemoveCachedCredentials( object state )
		{
			// get the key
			string key = state as string;

			m_cachd_creds_rwl.AcquireWriterLock( Timeout.Infinite );
			m_cachd_creds_tmrs_rwl.AcquireWriterLock( Timeout.Infinite );

			try
			{
				// expire the cached credentials
				m_cachd_creds.Remove( key );

				// stop the timer and remove it
				m_cachd_creds_tmrs[ key ].Dispose();
				m_cachd_creds_tmrs.Remove( key );
			}
			finally
			{
				m_cachd_creds_rwl.ReleaseWriterLock();
				m_cachd_creds_tmrs_rwl.ReleaseWriterLock();
			}
		}

		#region IDisposable Members

		/// <summary>
		///		Close resources.
		/// </summary>
		public void Dispose()
		{
			m_sudoers_ds.Dispose();
		}

		#endregion
}
}
