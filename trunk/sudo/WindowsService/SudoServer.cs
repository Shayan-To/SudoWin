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

using Win32;
using System;
using System.IO;
using Sudo.Data;
using System.Text;
using System.Security;
using System.Threading;
using System.Reflection;
using Sudo.PublicLibrary;
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
using System.Configuration;

namespace Sudo.WindowsService
{
	/// <summary>
	///		This is the class that the Sudo Windows service hosts
	///		as the server object that the clients communicate with.
	/// </summary>
	public class SudoServer :	MarshalByRefObject, 
		
								Sudo.PublicLibrary.ISudoServer, 
		
								IDisposable
	{
		/// <summary>
		///		Data server used by the sudo server to
		///		persist information between calls.
		/// </summary>
		private DataServer m_data_server = null;

		/// <summary>
		///		True if the user has exceeded the
		///		number of invalid logon attempts,
		///		false if otherwise.
		/// </summary>
		/// <remmarks>
		///		The value of this property does not
		///		have to be respected by the client, but
		///		the server will not allow the user to
		///		execute sudo if they have exceeded the
		///		invalid logon limit.
		/// </remmarks>
		public bool ExceededInvalidLogonLimit
		{
			get
			{
				// get the user name of the user who invoked sudo
				string un = Thread.CurrentPrincipal.Identity.Name;

				// get the InvalidLogonInfo structure for this user
				InvalidLogonInfo ui = m_data_server.GetInvalidLogonInfo( un );

				return ( ui.TimesExceededInvalidLogonsCount > 
					m_data_server.GetTimesExceededInvalidLogons( un ) - 1 );
			}
		}

		/// <summary>
		///		True if the user's credentials are cached, 
		///		false if otherwise.
		/// </summary>
		/// /// <remmarks>
		///		The value of this property does not
		///		have to be respected by the client, but 
		///		the server will return an immediate 
		///		invalid logon if the client assumes the 
		///		users credentials are cached when they 
		///		are not and passes a null password to 
		///		the Sudo method.
		/// </remmarks>
		public bool AreCredentialsCached
		{
			get
			{
				// get the user name of the user who invoked sudo
				string un = Thread.CurrentPrincipal.Identity.Name;

				return ( m_data_server.GetPassword( un ).Length > 0 );
				// get the InvalidLogonInfo structure for this user
				//InvalidLogonInfo lt = m_data_server.GetInvalidLogonInfo( un );

				//return ( lt.AreCredentialsCached );
			}
		}

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
		///		Adds or removes a user to the privileges
		///		group on the local computer.
		/// </summary>
		/// <param name="userName">
		///		User name to add or remove to the privileges 
		///		group.
		/// </param>
		/// <param name="which">
		///		1 "Add" or 0 "Remove"
		/// </param>
		/// <param name="privilegesGroup">
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use sudo.
		/// </param>
		/// <returns>
		///		If this method was invoked with the which parameter
		///		equal to 1 then this method returns whether or not
		///		the given user name was already a member of the group
		///		that it was supposed to be added to.
		/// 
		///		If this method was invoked with the which parameter
		///		equal to 0 then the return value of this method can
		///		be ignored.
		/// </returns>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		private bool AddRemoveUser( 
			string userName, 
			int which, 
			string privilegesGroup )
		{
			// get the strict user name part of the user name string, 
			// that is the user name without the domain part
			string un_part = userName.Split( new char[] { '\\' } )[ 1 ];

			// get entry for localhost
			DirectoryEntry localhost = new DirectoryEntry( "WinNT://" +
				Environment.MachineName + ",computer" );

			// get entry for the privileges group
			DirectoryEntry privs_group = localhost.Children.Find( privilegesGroup );

			// get entry for user to add
			DirectoryEntry user = localhost.Children.Find(
				un_part, "user" );

			// used for adsi invocations
			object[] path = new object[] { user.Path };

			// find out if the user is already a member
			// of the administrators group
			bool is_member = bool.Parse( Convert.ToString(
				privs_group.Invoke( "IsMember", path ),
				CultureInfo.CurrentCulture ) );

			// add the user to privs group if they
			// are not already a member
			if ( which == 1 && !is_member )
				privs_group.Invoke( "Add", path );
			// remove the member from privs group
			// if they are in the group
			else if ( which == 0 && is_member )
				privs_group.Invoke( "Remove", path );

			// save changes
			privs_group.CommitChanges();

			// cleanup
			user.Dispose();
			privs_group.Dispose();
			localhost.Dispose();

			return( is_member );
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
			// get the fully qualified user name
			string fqun = Thread.CurrentPrincipal.Identity.Name;

			// get the domain and user name parts of the fqun
			Match identity_match = Regex.Match( fqun,
				@"^([^\\]+)\\(.+)$" );
			// domain name
			string dn_part = identity_match.Groups[ 1 ].Value;
			// user name
			string un_part = identity_match.Groups[ 2 ].Value;

			// get the tracked logon information for this user
			InvalidLogonInfo lt = m_data_server.GetInvalidLogonInfo( fqun );
			
			// has the user exceeded their invalid logon limit?
			if ( lt.InvalidLogonsCount ==
				m_data_server.GetInvalidLogons( fqun ) - 1 )
			{
				// sudo result to return
				SudoResultTypes srt;

				// has the user exceeded the limit on the number
				// of times they are allowed to exceed their
				// invalid logon limit by 1?  if so this means that
				// the user is continuing to attempt to run sudo
				// even though they are locket out.  bad user!
				if ( lt.TimesExceededInvalidLogonsCount ==
					m_data_server.GetTimesExceededInvalidLogons( fqun ) )
				{
					// notify the client that this user is locked out
					srt = SudoResultTypes.SudoUserLockedOut;
				}
				// has the user exceeded the limit on the
				// number of times they are allowed to exceed 
				// their invalid logon limit
				else if ( lt.TimesExceededInvalidLogonsCount ==
					m_data_server.GetTimesExceededInvalidLogons( fqun ) - 1 )
				{
					// increment the number of times the user
					// has reached their invalid logon limit
					++lt.TimesExceededInvalidLogonsCount;

					// persist the SudoCallerInformation
					m_data_server.SetInvalidLogonInfo( fqun, lt );

					// schedule the user's lockout for removal
					m_data_server.RemoveInvalidLogonInfo(
						fqun, m_data_server.GetLockoutTimeout( fqun ) );

					// notify the client that this user is locked out
					srt = SudoResultTypes.SudoUserLockedOut;
				}
				// the user has not yet exceeded the number of
				// times they are allowed to exceed their invalid
				// logon limit
				else
				{
					// increment the number of times the user
					// has reached their invalid logon limit
					++lt.TimesExceededInvalidLogonsCount;

					// reset the users invalid logon count
					// to 0 so that they are allowed to try
					// to execute sudo again
					lt.InvalidLogonsCount = 0;

					// persist the SudoCallerInformation
					m_data_server.SetInvalidLogonInfo( fqun, lt );

					srt = SudoResultTypes.TooManyInvalidLogons;
				}

				return ( int ) srt;
			}

			// try to get the cached password for the user
			string cached_password = m_data_server.GetPassword( fqun );
			
			/*
			 * if the user's cached password's length is greater
			 * than zero then it means that a password was cached
			 * for this user and you should set the password that
			 * you are going to use to launch the process to this
			 * cached password.
			 * 
			 * if the user's password is not cached then invoke
			 * LogonUser to validate the credentials.  if
			 * LogonUser fails then increment the user's bad
			 * password attempt count and return an error.
			 */
			if ( cached_password.Length > 0 )
				password = cached_password;
			else
			{
				// define a temporary pointer to reference
				// the users logon handle
				IntPtr hTemp = IntPtr.Zero;

				// attempt to log the user on.
				bool loggedon = Win32.Native.LogonUser( un_part, dn_part, password,
					LogonType.Interactive, LogonProvider.WinNT50, out hTemp );

				// did the user successfully log on?
				if ( loggedon )
				{
					// close the user's logon handle
					Win32.Native.CloseHandle( hTemp );

					// signal that the user's credentials are
					// now cached
					//lt.AreCredentialsCached = true;

					// store the cached password
					m_data_server.SetPassword( fqun, password );

					// persist the SudoCallerInformation
					m_data_server.SetInvalidLogonInfo( fqun, lt );

					// schedule the user's cached logon for
					// removal
					m_data_server.RemoveInvalidLogonInfo(
						fqun, m_data_server.GetLogonTimeout( fqun ) );
				}
				else
				{
					// increment the number of invalid logons
					// for this user
					++lt.InvalidLogonsCount;

					// persist the SudoCallerInformation
					m_data_server.SetInvalidLogonInfo( fqun, lt );

					// schedule the user's invalid logon count
					// for removal
					m_data_server.RemoveInvalidLogonInfo( fqun,
						m_data_server.GetInvalidLogonTimeout( fqun ) );

					// return an error
					return ( int ) SudoResultTypes.InvalidLogon;
				}
			}

			/*
			 * do 4 checks
			 * 
			 * 1) check to see if the command the user is trying
			 * to execute is a built-in shell command.  these are
			 * not yet supported so return an error.
			 * 
			 * 2) check to see if the command the user is trying
			 * to execute is a valid command path.  if not then
			 * return an error.
			 * 
			 * 3) check to see if the user is allowed to execute
			 * the given command in the sudoers file.  if the user
			 * is not allowed to execute the given command then
			 * return an error.
			 * 
			 * 4) is the sudo console application on this machine
			 * signed by the same strong name key as the sudo
			 * windows service?  if not then return an error.
			 */
			if ( IsCommandBuiltin( commandPath ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			if ( !IsCommandPathValid( ref commandPath ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			if ( !m_data_server.IsCommandAllowed( fqun, commandPath, commandArguments ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			if ( !VerifySameSignature( ConfigurationManager.AppSettings[ "consoleApplicationPath" ] ) )
				return ( int ) SudoResultTypes.CommandNotAllowed;

			// get the user token for the user who
			// is executing the sudo client
			IntPtr hUser = IntPtr.Zero;
			
			// get the user's logon token
			QueryUserToken( fqun, ref hUser );

			// get the privileges group this user will
			// join while this process is executed
			string pg = m_data_server.GetPrivilegesGroup( fqun );

			// add the user to the privileges group if they
			// are not already a member of it and record whether
			// or not they are already a member of it
			bool already_member = AddRemoveUser( fqun, 1, pg );

			// start the process
			Process p = CreateProcessAsUser( hUser, password, commandPath, commandArguments );

			// wait for p to exit
			p.WaitForExit();

			// remove the user from the privileges group if 
			// they were not already a member of it
			if ( !already_member )
				AddRemoveUser( fqun, 0, pg );

			return 0;
		}


		private Process CreateProcessAsUser( 
			IntPtr userToken,
			string password,
			string commandPath, 
			string commandArguments )
		{
			SecurityAttributes sa = new SecurityAttributes();
			sa.InheritHandle = false;
			sa.SecurityDescriptor = IntPtr.Zero;
			sa.Length = Marshal.SizeOf( sa );

			StartupInfo si = new StartupInfo();
			si.Desktop = "WinSta0\\Default";
			si.Size = Marshal.SizeOf( si );

			string formatted_cpath = string.Format(
				CultureInfo.CurrentCulture,
				"\"{0}\" -c -p \"{1}\" \"{2}\" {3}",
				ConfigurationManager.AppSettings[ "consoleApplicationPath" ],
				 password,
				commandPath, commandArguments );

			ProcessInformation pi;
			bool started = Native.CreateProcessAsUser(
				userToken, 
				null, 
				formatted_cpath,
				ref sa, ref sa,
				false, 
				( int ) ProcessCreationFlags.CreateNoWindow | ( int ) ProcessPriorityTypes.Normal,
				IntPtr.Zero, null, ref si, out pi );

			if ( !started )
				throw new Win32Exception( Marshal.GetLastWin32Error() );
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
		/// <exception cref="System.ComponentModel.Win32Exception" />
		private void QueryUserToken( string userName, ref IntPtr userTokenHandle )
		{
			// open a handle to the local server
			IntPtr hSvr = WtsApi32.Native.WtsOpenServer( null );

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

					// domainname\username
					string dnun = string.Empty;

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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="otherAssemblyFilePath"></param>
		/// <remarks>
		///		http://blogs.msdn.com/shawnfa/archive/2004/06/07/150378.aspx
		/// </remarks>
		private bool VerifySameSignature( string otherAssemblyFilePath )
		{
			// whether or not both the client and server
			// assemblies have the same strong name signature
			bool same_sig = false;

			// get references to the client and server assemblies
			Assembly client = Assembly.LoadFile( otherAssemblyFilePath );
			Assembly server = Assembly.GetExecutingAssembly();

			// client/server was verified
			bool client_wf = false;
			bool server_wf = false;

			// if both the client and the server assemblies
			// both have valid strong name keys then compare
			// their public key tokens
			if ( StrongNameSignatureVerificationEx(
					client.Location, true, ref client_wf ) &&
				StrongNameSignatureVerificationEx(
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

			return ( same_sig );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="forceVerficiation"></param>
		/// <param name="wasVerified"></param>
		/// <returns></returns>
		/// <remarks>
		///		http://blogs.msdn.com/shawnfa/archive/2004/06/07/150378.aspx
		/// </remarks>
		[DllImport( "mscoree.dll", CharSet = CharSet.Unicode )]
		private static extern bool StrongNameSignatureVerificationEx( 
			string filePath, 
			bool forceVerficiation, 
			ref bool wasVerified );

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
