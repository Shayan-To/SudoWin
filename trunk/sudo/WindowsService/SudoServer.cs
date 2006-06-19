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

using Win32;
using System;
using System.IO;
using System.Text;
using System.Security;
using System.Threading;
using System.Reflection;
using Sudo.PublicLibrary;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.ComponentModel;
using System.DirectoryServices;
using Sudo.AuthorizationPlugins;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

namespace Sudo.WindowsService
{
	/// <summary>
	///		This is the class that the Sudo Windows service hosts
	///		as the sa object that the clients communicate with.
	/// </summary>
	public class SudoServer :	MarshalByRefObject, 
		
								Sudo.PublicLibrary.ISudoServer, 
		
								IDisposable
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudo.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Data sa used by the sudo sa to
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
				m_ts.TraceEvent( TraceEventType.Start, 10, 
					"entering get_ExceededInvalidLogonLimit" );

				// declare this method's return value
				bool hasExceeded;

				string un = Thread.CurrentPrincipal.Identity.Name;

				UserCache uc = new UserCache();
				UserInfo ui = new UserInfo();

				if ( m_data_server.GetUserCache( un, ref uc ) &&
					m_data_server.GetUserInfo( un, ref ui ) )
				{
					hasExceeded = uc.TimesExceededInvalidLogonCount >=
						ui.TimesExceededInvalidLogons;
				}
				else
				{
					hasExceeded = false;
				}

				m_ts.TraceEvent( TraceEventType.Verbose, 10,
					"{0}, hasExceeded={1}", un, hasExceeded );
				m_ts.TraceEvent( TraceEventType.Stop, 10,
					"exiting get_ExceededInvalidLogonLimit" );

				return ( hasExceeded );
			}
		}

		/// <summary>
		///		True if the user's credentials are cached, 
		///		false if otherwise.
		/// </summary>
		/// <remmarks>
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
				m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterPropertyGet,
					"entering get_AreCredentialsCached" );

				// declare this method's return value
				bool areCached = false;

				string un = Thread.CurrentPrincipal.Identity.Name;
				string p = string.Empty;
				areCached = m_data_server.GetUserCache( un, ref p );
				p = string.Empty;

				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, areCached={1}", un, areCached );
				m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitPropertyGet,
					"exiting get_AreCredentialsCached" );

				return ( areCached );
			}
		}

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SudoServer()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterConstructor,
				"constructing SudoServer" );

			string dsuri = ConfigurationManager.AppSettings[ "dataServerUri" ];
			
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"getting reference to SAO, m_data_server={0}", dsuri );

			m_data_server = Activator.GetObject( typeof( DataServer ), dsuri ) as DataServer;

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitConstructor,
				"constructed SudoServer" );
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
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering AddRemoveUser( string, int, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"{0}, which={1}, privilegesGroup={2}",
				userName, which, privilegesGroup );
			
			// remove the domain from the userName string
			string un_part = userName.Split( new char[] { '\\' } )[ 1 ];

			// get the directory entries for the localhost, the privileges
			// group, and the user
			DirectoryEntry localhost = new DirectoryEntry( "WinNT://" +
				Environment.MachineName + ",computer" );
			DirectoryEntry group = localhost.Children.Find( privilegesGroup );
			DirectoryEntry user = localhost.Children.Find( un_part, "user" );

			// used for adsi calls
			object[] path = new object[] { user.Path };

			bool isAlreadyMember = bool.Parse( Convert.ToString(
				group.Invoke( "IsMember", path ),
				CultureInfo.CurrentCulture ) );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isAlreadyMember={1}",
				userName, isAlreadyMember );

			// add user to privileges group
			if ( which == 1 && !isAlreadyMember )
			{
				group.Invoke( "Add", path );

				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, added user to privileges group",
					userName );
			}

			// remove user from privileges group
			else if ( which == 0 && isAlreadyMember )
			{
				group.Invoke( "Remove", path );

				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, removed user from privileges group",
					userName );
			}

			// save changes
			group.CommitChanges();

			// cleanup
			user.Dispose();
			group.Dispose();
			localhost.Dispose();

			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting AddRemoveUser( string, int, string )" );

			return( isAlreadyMember );
		}

		/// <summary>
		///		Invokes sudo on the given command p.
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
		///		A SudoResultTypes value.
		/// </returns>
		public SudoResultTypes Sudo(
			string password,
			string commandPath,
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering Sudo( string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"password=,commandPath={0},commandArguments={1}",
				commandPath, commandArguments );

			string un = Thread.CurrentPrincipal.Identity.Name;

			// check to see if the user is present in the sudoers data store
			UserInfo ui = new UserInfo();
			if ( !m_data_server.GetUserInfo( un, ref ui ) )
			{
				m_ts.TraceEvent( TraceEventType.Information, ( int ) EventIds.Information,
					"{0}, user not in sudoers data store", un );
				
				return ( LogResult( un, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed ) );
			}

			// make sure the user has not exceeded any invalid logon limits
			UserCache uc = new UserCache();
			if ( m_data_server.GetUserCache( un, ref uc ) && 
				uc.InvalidLogonCount >= ui.InvalidLogons - 1 )
			{
				return ( LogResult( un, ui.LoggingLevel,
					GetLimitDetails( un, ref ui, ref uc ) ) );
			}

			// check to see if the user has a cached password
			if ( !m_data_server.GetUserCache( un, ref password ) )
			{
				// validate the users logon credentials
				if ( !LogonUser( un, password, ref ui, ref uc ) )
				{
					return ( LogResult( un, ui.LoggingLevel,
						SudoResultTypes.InvalidLogon ) );
				}
			}
			
			// verify the command being sudoed
			if ( !VerifyCommand( un, ref commandPath, commandArguments ) )
			{
				return ( LogResult( un, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed ) );
			}

			// verify that this service and the sudo console app
			// are both signed with the same strong name key
			if ( !VerifySameSignature( un,
				ConfigurationManager.AppSettings[ "callbackApplicationPath" ] ) )
			{
				return ( LogResult( un, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed ) );
			}

			// sudo the command for the user
			Sudo( un, password, ui.PrivilegesGroup, commandPath, commandArguments );

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting Sudo( string, string, string )" );

			return ( LogResult( un, ui.LoggingLevel, 
				SudoResultTypes.SudoK ) );
		}

		private void Sudo(
			string userName, 
			string password, 
			string privilegesGroup,
			string commandPath, 
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering Sudo( string, string, string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},password=,privilegesGroup={1},commandPath={2},commandArguments={3}",
				userName, privilegesGroup, commandPath, commandArguments );

			// get the user's logon token
			IntPtr hUser = IntPtr.Zero;
			QueryUserToken( userName, ref hUser );

			// add the user to the group and record if they
			// were already a member of the group
			bool am = AddRemoveUser( userName, 1, privilegesGroup );

			// create the callback process and wait for it to exit so the user is
			// not removed from the privileges group before the indended process starts
			Process p = null;
			if ( CreateProcessAsUser( hUser, password, commandPath, commandArguments, ref p ) )
			{
				p.WaitForExit();
			}

			// remove the user from the group if they were not already a member
			if ( !am )
				AddRemoveUser( userName, 0, privilegesGroup );

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting Sudo( string, string, string, string, string )" );
		}

		private SudoResultTypes LogResult(
			string userName,
			LoggingLevelTypes loggingLevel,
			SudoResultTypes sudoResultType )
		{
			if ( loggingLevel != LoggingLevelTypes.None )
			{
				EventLogEntryType elet = 0;

				switch ( sudoResultType )
				{
					case SudoResultTypes.CommandNotAllowed:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.InvalidLogon:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.LockedOut:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.SudoK:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.SuccessAudit;
						break;
					}
					case SudoResultTypes.TooManyInvalidLogons:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
				}

				EventLog.WriteEntry( "Sudo",
					string.Format( CultureInfo.CurrentCulture,
						"{0} - {1}", userName, sudoResultType ),
					elet,
					( int ) sudoResultType );
			}

			return ( sudoResultType );
		}

		private bool VerifyCommand(
			string userName,
			ref string commandPath,
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering VerifyCommand( string, ref string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},commandPath={1},commandArguments={2}",
				userName, commandPath, commandArguments );
			
			CommandInfo ci = new CommandInfo();

			// declare this method's return value
			bool isCommandVerified =

				!IsShellCommand( commandPath )

				&&

				IsCommandPathValid( ref commandPath )

				&&

				m_data_server.GetCommandInfo(
					userName,
					commandPath,
					commandArguments,
					ref ci )

				&&

				ci.IsCommandAllowed;

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isCommandVerified={1}", userName, isCommandVerified );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting VerifyCommand( string, ref string, string )" );

			return ( isCommandVerified );
		}

		private bool LogonUser( 
			string userName,
			string password, 
			ref UserInfo userInfo,
			ref UserCache userCache )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering LogonUser( string, string, ref UserInfo, ref UserCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},password=,userInfo=,userCache=", userName);

			// get the domain and user name parts of the userName
			Match m = Regex.Match( userName, @"^([^\\]+)\\(.+)$" );
			string dn_part = m.Groups[ 1 ].Value;
			string un_part = m.Groups[ 2 ].Value;

			// get and parse the authentication plugin uri
			string authn_plugin_uri;
			ManagedMethods.GetConfigValue( "authenticationPluginAssembly", out authn_plugin_uri );
			if ( authn_plugin_uri.Length == 0 )
			{
				string msg = "authenticationPluginAssembly must be a " +
					"specified key in the appSettings section of the " +
					"config file";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}
			Regex rx_authn_plugin_uri = new Regex( "^(?<type>[^,]+),(?<assembly>.+)$", RegexOptions.IgnoreCase );
			Match m_authn_plugin_uri = rx_authn_plugin_uri.Match( authn_plugin_uri );
			if ( !m_authn_plugin_uri.Success )
			{
				string msg = "authenticationPluginAssembly is not " +
					"properly formatted";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// load the authentication plugin
			Assembly authn_plugin_assem = Assembly.Load( m_authn_plugin_uri.Groups[ "assembly" ].Value );
			Sudo.AuthenticationPlugins.IAuthenticationPlugin authn_ds = 
				authn_plugin_assem.CreateInstance( m_authn_plugin_uri.Groups[ "type" ].Value )
				as Sudo.AuthenticationPlugins.IAuthenticationPlugin;
			if ( authn_ds == null )
			{
				string msg = "there was an error in loading the specified " +
					"authenticationPluginAssembly";
				m_ts.TraceEvent( TraceEventType.Critical, 10, msg );
				throw new System.Configuration.ConfigurationErrorsException( msg );
			}

			// verify the user's credentials
			bool logonSuccessful = authn_ds.VerifyCredentials( dn_part, un_part, password );

			if ( logonSuccessful )
			{
				// cache the user's password and set it's expiration date
				m_data_server.SetUserCache( userName, password );
				m_data_server.ExpireUserCache( userName, userInfo.LogonTimeout );
			}
			else
			{
				++userCache.InvalidLogonCount;

				// cache the user's InvalidLogonCount and set it's expiration date
				m_data_server.SetUserCache( userName, userCache );
				m_data_server.ExpireUserCache( userName, userInfo.InvalidLogonTimeout );
			}

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, logonSuccessful={1}", userName, logonSuccessful );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting LogonUser( string, ref string, string )" );

			return ( logonSuccessful );
		}

		private SudoResultTypes GetLimitDetails( 
			string userName, 
			ref UserInfo userInfo, 
			ref UserCache userCache )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetLimitDetails( string, ref UserInfo, ref UserCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},userInfo=,userCache=", userName );

			// declare this method's return value
			SudoResultTypes sudoResult;

			// let the user know they have been locked out
			if ( userCache.TimesExceededInvalidLogonCount >=
				userInfo.TimesExceededInvalidLogons )
			{
				sudoResult = SudoResultTypes.LockedOut;
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, previously locked out", userName );
			}
			
			// lock the user out and set their lockout expiration
			else if ( userCache.TimesExceededInvalidLogonCount >=
				userInfo.TimesExceededInvalidLogons - 1 )
			{
				++userCache.TimesExceededInvalidLogonCount;

				m_data_server.SetUserCache( userName, userCache );
				m_data_server.ExpireUserCache( userName, userInfo.LockoutTimeout );

				sudoResult = SudoResultTypes.LockedOut;
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, locked out", userName );
			}

			// move the user 1 step closer to being locked out
			else
			{
				++userCache.TimesExceededInvalidLogonCount;

				// reset the user's invalid logon count so their
				// next call to sudo does not immediately result
				// in an SudoResultTypes.TooManyInvalidLogons
				userCache.InvalidLogonCount = 0;

				m_data_server.SetUserCache( userName, userCache );
				sudoResult = SudoResultTypes.TooManyInvalidLogons;
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, {1} invalid attempts away from being locked out", 
					userName, 
					userInfo.TimesExceededInvalidLogons - userCache.TimesExceededInvalidLogonCount );
			}

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, sudoResult={1}", userName, sudoResult );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting GetLimitDetails( string, ref UserInfo, ref UserCache )" );

			return ( sudoResult );
		}

		private bool CreateProcessAsUser( 
			IntPtr userToken,
			string password,
			string commandPath, 
			string commandArguments,
			ref Process newProcess )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering CreateProcessAsUser( IntPtr, string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userToken=,password=,commandPath={0},commandArguments={1}", 
				commandPath, commandArguments );

			// needed to create a new process
			SecurityAttributes sa = new SecurityAttributes();
			sa.InheritHandle = false;
			sa.SecurityDescriptor = IntPtr.Zero;
			sa.Length = Marshal.SizeOf( sa );

			// bind the new process to the interactive desktop
			StartupInfo si = new StartupInfo();
			si.Desktop = "WinSta0\\Default";
			si.Size = Marshal.SizeOf( si );

			// build a formatted command path to call the Sudo.ConsoleApplication with
			string fcp = string.Format(
				CultureInfo.CurrentCulture,
				
				// i took this out for now until i decide whether or not
				// i want to bother with command line switches in the callback 
				// application
				//"\"{0}\" -c -p \"{1}\" \"{2}\" {3}",
				
				"\"{0}\"  \"{1}\" \"{2}\" {3}",
				ConfigurationManager.AppSettings[ "callbackApplicationPath" ],
				 password,
				commandPath, commandArguments );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"formatted command path={0}",
				
				// see the last comment as to why this is commented out
				//Regex.Replace( fcp, @"\-p ""([^""]*)""", "-p" ) );
				
				Regex.Replace( fcp, @"  ""([^""]*)""", "" ) );

			ProcessInformation pi;
			bool newProcessCreated = Native.CreateProcessAsUser(
				userToken,
				null,
				fcp,
				ref sa, ref sa,
				false,
				( int ) ProcessCreationFlags.CreateNoWindow | ( int ) ProcessPriorityTypes.Normal,
				IntPtr.Zero, null, ref si, out pi );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"processCreated={0}" + ( newProcessCreated ? "" : ", win32error={1}" ), 
				newProcessCreated, 
				newProcessCreated ? 0 : Marshal.GetLastWin32Error() );

			if ( newProcessCreated )
			{
				// get a managed reference to the process
				newProcess = Process.GetProcessById( pi.ProcessId );

				// free the unmanaged handles
				WtsApi32.Native.CloseHandle( pi.Thread );
				WtsApi32.Native.CloseHandle( pi.Process );
			}

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting CreateProcessAsUser( IntPtr, string, string, string )" );

			return ( newProcessCreated );
		}

		/// <summary>
		///		Retrieves the logon token for the user that is 
		///		logged into the computer with a user name that is
		///		equal to the parameter userName.
		/// </summary>
		/// <param name="userName">
		///		User name to get token for.
		/// </param>
		/// <param name="userToken">
		///		User logon token.
		/// </param>
		/// <returns>
		///		True if the token was retrieved, otherwise false.
		/// </returns>
		private bool QueryUserToken( string userName, ref IntPtr userToken )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering QueryUserToken( string, ref IntPtr )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},userToken=", userName );

			// open a handle to the localhost
			IntPtr hSvr = WtsApi32.Native.WtsOpenServer( null );

			// get a list of the sessions on the localhost
			WtsApi32.WtsSessionInfo[] wsis;
			try
			{
				wsis = WtsApi32.Managed.WtsEnumerateSessions( hSvr );
			}
			catch ( Win32Exception e )
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Error,
					"{0}, WtsEnumerateSessions FAILED, Win32Error={1}",
					userName, e.ErrorCode );
				m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
					"exiting QueryUserToken( userName, ref IntPtr" );
				return ( false );
			}

			// check all the sessions on the server to get the logon token
			// of the user that has the same user name as the userName parameter
			for ( int x = 0; x < wsis.Length && userToken == IntPtr.Zero; ++x )
			{
				// declare 2 strings to hold the user name and domain name
				string un = string.Empty, dn = string.Empty;

				// compare the session's user name with the userName
				// parameter and get the logon token if they are equal
				if ( WtsApi32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						WtsApi32.WtsQueryInfoTypes.WtsUserName,
						out un )

					&&

					WtsApi32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						WtsApi32.WtsQueryInfoTypes.WtsDomainName,
						out dn )

					&&

					( ( dn + "\\" + un ) == userName ) )
				{
					WtsApi32.Native.WtsQueryUserToken(
						wsis[ x ].SessionId, ref userToken );
				}
			}

			if ( hSvr != IntPtr.Zero )
				WtsApi32.Native.WtsCloseServer( hSvr );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, tokenRetrieved={1}", userName, userToken != IntPtr.Zero );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting QueryUserToken( userName, ref IntPtr" );
			
			return ( userToken != IntPtr.Zero );
		}

		/// <summary>
		///		Checks to see if the given command name
		///		exists exactly as entered, as entered with
		///		known executable file extensions on the end,
		///		or somewhere in one of the directories 
		///		specified in the environment variable %PATH%.
		/// </summary>
		/// <param name="commandPath">
		///		Command to check.  If this method returns true
		///		this parameter will be set to the fully
		///		qualified p of the command.
		/// </param>
		/// <returns>
		///		True if the command exists, otherwise false.
		/// </returns>
		private bool IsCommandPathValid( ref string commandPath )
		{
			// declare this method's return value
			bool isValid = false;

			// check to see if commandPath exists as entered or 
			// as entered with any of the known executable file
			// extensions appended to it
			if ( commandPath.Contains( "\\" ) ||
				commandPath.Contains( "/" ) )
			{
				// check to see if the commandPath exists
				if ( !( isValid = File.Exists( commandPath ) ) )
				{
					// check to see if commandPath exists with any of the
					// known executable file extensions appended to it
					isValid = ( commandPath = TestFileExtensions( commandPath ) ).Length > 0;
				}
			}
			
			// check to see if commandPath exists in any of the folders
			// listed in the PATH environment variable
			else
			{
				string p = Environment.GetEnvironmentVariable( "PATH" );
				string[] pdirs = p.Split( new char[] { ';' } );

				for ( int x = 0; x < pdirs.Length && !isValid; ++x )
				{
					string pd = pdirs[ x ];

					// add a trailing slash to the path directory
					// if it does not have one
					if ( !Regex.IsMatch( pd, @"^.+(\\|/)$" ) )
					{
						// add the appropriate type of slash,
						// i.e. a slash or a backslash
						if ( pd.IndexOf( '\\' ) > -1 )
							pd += "\\";
						else
							pd += "/";
					}

					// check to see if commandPath exists with
					// the current path directory prepended to it
					commandPath = pd + commandPath;

					commandPath = TestFileExtensions( commandPath );
					isValid = commandPath.Length > 0;
				}
			}

			return ( isValid );
		}

		/// <summary>
		///		Tests the given commandPath to see if
		///		the command is a Windows shell command.
		/// </summary>
		/// <param name="commandPath">
		///		Command to test.
		/// </param>
		/// <returns>
		///		True if the command is a shell command, otherwise false.
		/// </returns>
		private bool IsShellCommand( string commandPath )
		{
			return ( Regex.IsMatch( commandPath,
				"(cd)|(dir)|(type)",
				RegexOptions.IgnoreCase ) );
		}

		/// <summary>
		///		Tests all the executable file extensions on
		///		the command p parameter in order to determine
		///		whether the given command name is a valid 
		///		executable without the extension on the end.
		/// </summary>
		/// <param name="commandPath">
		///		Command to test.
		/// </param>
		/// <returns>
		///		If a command with an executable file extension was
		///		found then this method returns the fully qualified p 
		///		to the command with the correct file extension 
		///		appended to the end.
		/// 
		///		If no command was found this method returns
		///		an empty string.
		/// </returns>
		private string TestFileExtensions( string commandPath )
		{
			// declare this method's return value
			string withExtension = string.Empty;

			// declare a bool that is true of the
			// command exists with the given file
			// extension
			bool ce = false;

			// test all the possible executable extensions
			for ( int x = 0; x < 4 && !ce; ++x )
			{
				switch ( x )
				{
					case 0:
					{
						withExtension = commandPath + ".exe";
						break;
					}
					case 1:
					{
						withExtension = commandPath + ".bat";
						break;
					}
					case 2:
					{
						withExtension = commandPath + ".cmd";
						break;
					}
					case 3:
					{
						withExtension = commandPath + ".lnk";
						break;
					}
				}

				m_ts.TraceEvent( TraceEventType.Verbose, 10, withExtension );

				// set the the return value to an empty string
				// if the file does not exist and this is the
				// last iteration of this loop
				if ( !( ce = File.Exists( withExtension ) ) )
					withExtension = string.Empty;
			}

			return ( withExtension );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="otherAssemblyFilePath"></param>
		/// <remarks>
		///		http://blogs.msdn.com/shawnfa/archive/2004/06/07/150378.aspx
		/// </remarks>
		private bool VerifySameSignature( 
			string userName,
			string otherAssemblyFilePath )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering VerifySameSignature( string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"{0}, otherAssemblyFilePath={1}", userName, otherAssemblyFilePath );

			// declare this method's return value
			bool isVerified = false;

			// load the Sudo.ConsoleApplication assembly
			Assembly ca = Assembly.LoadFile( otherAssemblyFilePath );

			// get a reference to the Sudo.WindowsService assembly
			Assembly sa = Assembly.GetExecutingAssembly();

			// declare 2 bools to hold the results of both the
			// server and the client private key verficiation tests
			bool ca_wv = false; 
			bool sa_wv = false;

			// verify that the console application and
			// the service application are both signed
			// by the same private key
			if ( isVerified = ( StrongNameSignatureVerificationEx(
					ca.Location, true, ref ca_wv ) &&
				StrongNameSignatureVerificationEx(
					sa.Location, true, ref sa_wv ) ) )
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"private keys verified={0}", isVerified );

				// get the ca and sa public key tokens
				byte[] ca_pubkey = ca.GetName().GetPublicKeyToken();
				byte[] sa_pubkey = sa.GetName().GetPublicKeyToken();

				// verify that both public key tokens are the same length
				if ( ca_pubkey.Length == sa_pubkey.Length )
				{
					// verify that each bit of the the public key tokens
					for ( int x = 0; x < ca_pubkey.Length && isVerified; ++x )
					{
						isVerified = ca_pubkey[ x ] == sa_pubkey[ x ];
					}

					m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
						"public key tokens verified={0}", isVerified );
				}
			}
			else
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"private keys not verified" );
			}

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"key signatures verified={0}", isVerified );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting VerifySameSignature( string, string )" );

			return ( isVerified );
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
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterDispose,
				"entering Dispose" );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitDispose,
				"exiting Dispose" );
		}

		#endregion
	}
}
