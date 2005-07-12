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
using System.Text;
using System.Security;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.DirectoryServices;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

namespace Sudo.Service
{
	public class Server : MarshalByRefObject, Sudo.Shared.ISudoServer
	{
		/// <summary>
		///		Collection used to store credential sets
		///		for a limited time.
		/// </summary>
		private Dictionary<string, SecureString> m_cached_credentials =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Reader/writer lock for the credentials collection.
		/// </summary>
		private ReaderWriterLock m_cached_credentials_rwl =
			new ReaderWriterLock();

		/// <summary>
		///		Gets/sets user passwords from/in the credential cache.
		/// </summary>
		/// <remarks>
		///		This property accessor makes a call to the backing
		///		store each time its get and set methods are invoked.
		/// </remarks>
		public string Password
		{
			get
			{
				// get identity of caller
				WindowsIdentity wi = Thread.CurrentPrincipal.Identity as
					WindowsIdentity;

				// used to hold the cached password
				SecureString ss;
				
				m_cached_credentials_rwl.AcquireReaderLock(
					Timeout.Infinite );

				// see if the cache contains a password for 
				// the caller
				bool is_cached;
				try
				{
					is_cached = m_cached_credentials.TryGetValue( wi.Name, out ss );
				}
				finally
				{
					m_cached_credentials_rwl.ReleaseReaderLock();
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
			set
			{
				// get identity of caller
				WindowsIdentity wi = Thread.CurrentPrincipal.Identity as
					WindowsIdentity;

				// get a secure string version of the password
				SecureString ss = new SecureString();
				for ( int x = 0; x < value.Length; ++x )
					ss.AppendChar( value[ x ] );

				m_cached_credentials_rwl.AcquireWriterLock(
					Timeout.Infinite );

				try
				{
					// add the password to the cache
					m_cached_credentials.Add( wi.Name, ss );
				}
				finally
				{
					m_cached_credentials_rwl.ReleaseWriterLock();
				}
			}
		}

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Server()
		{
			
		}

		/// <summary>
		///		Elevate caller to administrator privileges.
		/// </summary>
		public void BeginElevatePrivileges()
		{
			AddRemoveUser( 1 );
		}

		/// <summary>
		///		Revert caller to normal privileges.
		/// </summary>
		public void EndElevatePrivileges()
		{
			AddRemoveUser( 0 );
		}

		/// <summary>
		///		Adds or removes a user to the administrators
		///		group on the local computer.
		/// </summary>
		/// <param name="function">1 "Add" or 0 "Remove"</param>
		private void AddRemoveUser( int which )
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
			// dummy method
		}
	}
}
