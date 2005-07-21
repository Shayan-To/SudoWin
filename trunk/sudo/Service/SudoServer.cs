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
using Sudo.Service.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.DirectoryServices;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

namespace Sudo.Service
{
	public class SudoServer : MarshalByRefObject, Sudo.Shared.ISudoServer, IDisposable
	{
		/// <summary>
		///		Data server used by the sudo server to
		///		persist information between calls.
		/// </summary>
		private DataServer m_data_server = null;

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SudoServer()
		{
			m_data_server = Activator.GetObject( typeof( DataServer ),
				System.Configuration.ConfigurationManager.AppSettings[ "dataServerUri" ] )
				as DataServer;
		}

		/// <summary>
		///		Adds or removes a user to the administrators
		///		group on the local computer.
		/// </summary>
		/// <param name="function">1 "Add" or 0 "Remove"</param>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
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
		///		Invokes sudo on the given command path.
		/// </summary>
		/// <param name="password">
		///		Password of user invoking sudo.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command that
		///		sudo is being invoked on.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments of the command that
		///		sudo is being invoked on.
		/// </param>
		/// <returns>
		///		An integer that can be cast as a 
		///		SudoResultsTypes value.
		/// </returns>
		public int Sudo(
			string password,
			string commandPath,
			string commandArguments )
		{
			// password cannot be null
			if ( password == null )
				return ( int ) SudoResultTypes.InvalidLogon;
			// ... or an empty string
			else if ( password.Length == 0 )
				return ( int ) SudoResultTypes.InvalidLogon;

			// commandPath cannot be null
			if ( commandPath == null )
				return ( int ) SudoResultTypes.CommandNotAllowed;
			// ... or an empty string
			else if ( commandPath.Length == 0 )
				return ( int ) SudoResultTypes.CommandNotAllowed;
			// ... or a built-in command
			else if ( IsCommandBuiltin( commandPath ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;
			// ... or an invalid path
			else if ( !IsCommandPathValid( ref commandPath ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			// get the user name of the user who invoked sudo
			string un = Thread.CurrentPrincipal.Identity.Name;

			// get the user info structure for this user
			UserInfo ui = m_data_server.GetUserInfo( un );

			// check to see if the user has exceeded
			// the allowed number of bad password attempts
			// in a given time period.  if they have then
			// start a timer that will expire their user
			// information after 3 minutes and then return
			// an error to the client.
			if ( ui.InvalidLogons == m_data_server.GetPasswordTries( un ) )
			{
				m_data_server.RemoveUserInfo( un, 180 );
				return ( int ) SudoResultTypes.TooManyInvalidLogons;
			}
			
			// check to see if the user's credentials are
			// cached.  if they are not then invoke
			// LogonUser to validate the credentials.  if
			// LogonUser fails then increment the user's bad
			// password attempt count and return an error.
			if ( !ui.AreCredentialsCached )
			{
				// define a temporary pointer to reference
				// the users logon handle
				IntPtr hTemp = IntPtr.Zero;

				Match m = Regex.Match( un, @"(.+)\\(.+)" );

				string dn_part = m.Groups[ 1 ].Value;
				string un_part = m.Groups[ 2 ].Value;
				
				// attempt to log the user on.
				bool loggedon = Win32.Native.LogonUser( un_part, dn_part, password,
					LogonType.Interactive, LogonProvider.WinNT50, out hTemp );

				// if the user successfully logged on then
				// close the temporary user handle, toggle the
				// switch that says their credentials are cached
				// and cause their userInfo structure to expire
				// in however many seconds their PasswordTimeout is
				if ( loggedon )
				{
					Win32.Native.CloseHandle( hTemp );
					ui.AreCredentialsCached = true;

					// persist the userInfo structure for this user
					m_data_server.SetUserInfo( un, ui );

					m_data_server.RemoveUserInfo( 
						un, m_data_server.GetPasswordTimeout( un ) );
				}
				else
				{
					// increment the number of invalid logons
					// for this user
					++ui.InvalidLogons;

					// persist the userInfo structure for this user
					m_data_server.SetUserInfo( un, ui );

					// return an error
					return ( int ) SudoResultTypes.InvalidLogon;
				}
			}

			// check to see if the command the user is
			// trying to execute is allowed.  if the command is
			// not allowed then return an error.
			if ( !m_data_server.IsCommandAllowed( un, commandPath, commandArguments ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			// get the user token for the user who
			// is executing the sudo client
			IntPtr hUser = IntPtr.Zero;
			
			// get the user's logon token
			QueryUserToken( un, ref hUser );
			
			// elevate the user's privileges
			AddRemoveUser( 1 );

			// start the process
			StartProcess( hUser, commandPath, commandArguments );

			// reduce the user's privileges
			AddRemoveUser( 0 );

			return ( int ) SudoResultTypes.SudoK;
		}

		private Process StartProcess( 
			IntPtr userToken, 
			string commandPath, 
			string commandArguments )
		{
			SecurityAttributes sa = new SecurityAttributes();
			sa.InheritHandle = false;
			sa.SecurityDescriptor = ( IntPtr ) 0;
			sa.Length = Marshal.SizeOf( sa );

			StartupInfo si = new StartupInfo();
			si.Desktop = "WinSta0\\Default";
			si.Size = Marshal.SizeOf( si );

			ProcessInformation pi;
			bool started = Native.CreateProcessAsUser(
				userToken, 
				null, 
				commandPath + " " + commandArguments, 
				ref sa, ref sa,
				false, 
				( int ) ProcessCreationFlags.CreateNewConsole | ( int ) ProcessPriorityTypes.Normal,
				IntPtr.Zero, null, ref si, out pi );

			if ( !started )
				return ( null );
			else
			{
				// get a managed reference to the process
				Process p = Process.GetProcessById( pi.ProcessId );
				
				// free the unmanaged handles
				WtsApi32.Native.CloseHandle( pi.Thread );
				WtsApi32.Native.CloseHandle( pi.Process );

				return ( p );
			}
		}

		/// <summary>
		///		Gets the logon token of the user who is
		///		executing the sudo client.
		/// </summary>
		/// <param name="userName">
		///		User name to get token for.
		/// </param>
		/// <param name="userTokenHandle">
		///		User logon token.
		/// </param>
		private void QueryUserToken( string userName, ref IntPtr userTokenHandle )
		{
			// get a handle to the local server
			IntPtr hSvr = WtsApi32.Native.WtsOpenServer( null );//( IntPtr ) WtsApi32.Native.WtsCurrentServerHandle;

			try
			{
				// enumerate the sessions on the terminal server
				WtsApi32.WtsSessionInfo[] wsis = WtsApi32.Managed.WtsEnumerateSessions( hSvr );

				for ( int x = 0; x < wsis.Length && userTokenHandle == IntPtr.Zero; ++x )
				{
					string un = string.Empty;	// user name
					string dn = string.Empty;	// domain name

					// get the user name
					WtsApi32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						WtsApi32.WtsQueryInfoTypes.WtsUserName,
						out un );

					// get the domain name
					WtsApi32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						WtsApi32.WtsQueryInfoTypes.WtsDomainName,
						out dn );

					string dnun = string.Empty;	// domain\user name

					// if both the user name and domain name
					// were successfully retrieved then
					// format them into dn\un
					if ( un.Length > 0 && dn.Length > 0 )
					{
						dnun = string.Format( CultureInfo.CurrentCulture,
							"{0}\\{1}", dn, un );
					}

					// if dnun matches the name of the user whose
					// security principal is attached to the current thread,
					// i.e. the user who executed the remote client, then
					// grab the user token, he/she's our guy/gal.
					if ( dnun == userName )
					{
						WtsApi32.Native.WtsQueryUserToken(
							wsis[ x ].SessionId, ref userTokenHandle );
					}
				}
			}
			finally
			{
				if ( hSvr != IntPtr.Zero )
					WtsApi32.Native.WtsCloseServer( hSvr );
			}
		}

		/// <summary>
		///		Checks to see if the given command name
		///		exists exactly as entered, as entered with
		///		known executable file extensions on the end,
		///		or somewhere in one of the directories 
		///		specified in the environment variable %PATH%.
		/// </summary>
		/// <param name="cmdName">
		///		Command to check.  If this method returns true
		///		this parameter will be set to the fully
		///		qualified path of the command.
		/// </param>
		/// <returns>
		///		True if the command exists and false if it does not.
		/// </returns>
		private bool IsCommandPathValid( ref string cmdName )
		{
			// method scope var used to hold
			// the results of tests to see if
			// the given command exists somewhere
			bool cmd_exists = false;

			// if cmdName contains a slash or a backslash
			// then test the existence of the command exactly
			// as entered and with the executable file
			// extensions and immediately return the results
			// of this test.
			if ( cmdName.Contains( "\\" ) ||
				cmdName.Contains( "/" ) )
			{
				// if the command exists *exactly* as entered
				// then return true immediately
				cmd_exists = File.Exists( cmdName );

				// if the command does not exist exactly as
				// entered then test it with known executable
				// file extensions appended to the end
				if ( !cmd_exists )
					cmd_exists = TestFileExtensions( cmdName ).Length > 0;
			}
			// at this point we must check to see
			// if the command exists in one of the
			// path directories
			else
			{
				string path = Environment.GetEnvironmentVariable( "path" );

				// create an array to hold the path directories
				string[] path_dirs = path.Split( new char[] { ';' } );

				// loop through the directories specified in
				// in %PATH% checking if the command name
				// exists in one of those directories.  the 
				// loop will break if the end of the %PATH%
				// directories array is reached or if the 
				// command is found.
				for ( int x = 0; x < path_dirs.Length && !cmd_exists; ++x )
				{
					string path_dir = path_dirs[ x ];

					// if the directory does not end with a
					// trailing slash then we must add one
					if ( !Regex.IsMatch( path_dir, @"^.+(\\|/)$" ) )
					{
						// if backslashes are used then append
						// a backslash to the end of the path
						if ( path_dir.IndexOf( '\\' ) > -1 )
							path_dir += "\\";
						// append a slash to the end of the path
						else
							path_dir += "/";
					}

					// set the fully qualified command path
					cmdName = path_dir + cmdName;
					cmdName = TestFileExtensions( cmdName );
					cmd_exists = cmdName.Length > 0;
				}
			}

			return ( cmd_exists );
		}

		/// <summary>
		///		Tests the given command path to see if
		///		the command is a command built in to
		///		command.com.
		/// </summary>
		/// <param name="commandName">
		///		Command to test.
		/// </param>
		/// <returns>
		///		True if the command is builtin, false if otherwise.
		/// </returns>
		private bool IsCommandBuiltin( string commandName )
		{
			return ( Regex.IsMatch( commandName,
				"(cd)|(dir)|(type)",
				RegexOptions.IgnoreCase ) );
		}

		/// <summary>
		///		Tests all the executable file extensions on
		///		the command path parameter in order to determine
		///		whether the given command name is a valid 
		///		executable without the extension on the end.
		/// </summary>
		/// <param name="commandPath">
		///		Command to test.
		/// </param>
		/// <returns>
		///		If a command with an executable file extension was
		///		found then this method returns the fully qualified path 
		///		to the command with the correct file extension 
		///		appended to the end.
		/// 
		///		If no command was found this method returns
		///		an empty string.
		/// </returns>
		private string TestFileExtensions( string commandPath )
		{
			// used to exit the below for loop early
			// if the fully qualified command path is found
			bool cmd_exists = false;

			// used to hold the fully qualified command path with
			// an executable extension tacked on at the end
			string cmd_path_with_ext = string.Empty;

			// check all the possible executable extensions
			for ( int x = 0; x < 4 && !cmd_exists; ++x )
			{
				switch ( x )
				{
					case 0:
						{
							cmd_path_with_ext = commandPath + ".exe";
							break;
						}
					case 1:
						{
							cmd_path_with_ext = commandPath + ".bat";
							break;
						}
					case 2:
						{
							cmd_path_with_ext = commandPath + ".cmd";
							break;
						}
					case 3:
						{
							cmd_path_with_ext = commandPath + ".lnk";
							break;
						}
				}

				cmd_exists = File.Exists( cmd_path_with_ext );
			}

			// if the command exists then return
			// the fully qualified path to it else
			// return an empty string
			if ( cmd_exists )
				return ( cmd_path_with_ext );
			else
				return ( string.Empty );
		}

		#region IDisposable Members

		/// <summary>
		///		Close resources.
		/// </summary>
		public void Dispose()
		{
			// do nothing
		}

		#endregion
	}
}
