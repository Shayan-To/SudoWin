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
using System.Security;
using System.Diagnostics;
using System.Configuration;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Sudo.Shared
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// 
	/// The following values will be used if they are
	/// defined in the hosting processes config file.
	/// 
	/// - alloweBadPasswordAttempts - the number of
	///		times a user is allowed to retry a password
	/// 
	///		default - 3
	/// 
	/// - interactionMethod - how sudo interacts with the
	///		user
	/// 
	///		default - forms
	/// 
	/// </remarks>
	public class Client
	{
		/// <summary>
		///		This class is not instantiable.
		/// </summary>
		private Client()
		{
		}

		/// <summary>
		///		Executes sudo.
		/// </summary>
		/// <param name="sudoServer"></param>
		/// <param name="commandName">
		///		Command that will be executed with sudo.
		/// </param>
		/// <param name="commandArguments">
		///		Arguemnts of command that will be executed with sudo.
		/// </param>
		/// <param name="interactionMethod">
		///		Method used to send messages and request 
		///		input to and from the user.
		/// </param>
		/// <remarks>
		///		commandArguments is a space delimited string.
		/// 
		///		interactionMethod is currently limited to 
		///		the command line and windows forms.
		/// </remarks>
		static public void Sudo(
			ISudoServer sudoServer,	
			string commandName,
			string commandArguments )
		{
			// number of times a user is allowed to
			// retry after a bad password attempt
			//
			// try to get this from the host process's
			// config file.  if that fails set the
			// number of allowed attempts to 3.
			int allowed_bad_password_attempts;

			GetConfigValue( "allowedBadPasswordAttempts",
				out allowed_bad_password_attempts );
			
			if ( allowed_bad_password_attempts == -1 )
				allowed_bad_password_attempts = 3;
			

			// how sudo interacts with the user
			//
			// try to get this from the host process's
			// config file.  if that fails set the
			// number of allowed attempts to 'Forms'
			InteractionMethodTypes interaction_method;

			GetConfigValue( "interactionMethod",
				out interaction_method );

			if ( interaction_method == 0 )
				interaction_method = InteractionMethodTypes.Forms;

			#region early checks
			//
			// performs some early checks to insure that
			// the user's experience is as smooth as
			// possible
			//
			// - make sure the user is a valid sudoer
			//
			// - make sure the command the user is
			//   trying to execute is a 1) valid command and
			//   2)they  have permission to execute it
			//

			// the AuthenticateClient() method will throw
			// an exception if the WindowsIdentity of this
			// process is not a member of any of the groups
			// authorized to use this server.
			try
			{
				sudoServer.AuthenticateClient();
			}
			catch ( RemotingException e )
			{
				string error_message = string.Empty;

				// access is denied
				if ( Regex.IsMatch( e.Message, "access is denied", 
					RegexOptions.IgnoreCase ) )
				{
					error_message = "access denied";
				}
				// ipc server is not running
				if ( Regex.IsMatch( e.Message, "cannot find the file",
					RegexOptions.IgnoreCase ) )
				{
					error_message = "sudo service down";
				}

				// if there was no error message specified
				// the just use the system message
				if ( error_message.Length == 0 )
					error_message = e.Message;

				WriteOutput( 
					error_message, OutputMessageTypes.Error, interaction_method );
				return;
			}

			// validate the command the user is trying to execute
			if ( commandName.Length == 0 )
			{
				WriteOutput( "no command specified",
					OutputMessageTypes.Error, interaction_method );
				return;
			}
			else
			{
				// built in commands are not supported yet
				if ( IsBuiltinCommand( commandName ) )
				{
					WriteOutput( "builtin commands not supported yet",
						OutputMessageTypes.Error, interaction_method );
					return;
				}
				
				// check to see if the command is a valid
				// file or builtin windows command
				if ( !IsValidCommand( ref commandName ) )
				{
					WriteOutput( "command not found",
						OutputMessageTypes.Error, interaction_method );
					return;
				}
			}

			#endregion

			// get the caller's identity
			WindowsIdentity wi = WindowsIdentity.GetCurrent();
			string[] user_parts = wi.Name.Split( new char[] { '\\' } );

			// initialze the process start info
			ProcessStartInfo psi = new ProcessStartInfo();

			// initialize the credentials used to
			// run the process
			psi.Domain = user_parts[ 0 ];
			psi.UserName = user_parts[ 1 ];

			// if this caller's password is cached
			// on the server then use the cached version
			string password = sudoServer.Password;
			bool pwd_is_cached = password.Length > 0;
			if ( pwd_is_cached )
				psi.Password = GetSecureString( password );

			// command name and arguments
			psi.FileName = commandName;
			psi.Arguments = commandArguments;
			
			psi.UseShellExecute = false;
			psi.LoadUserProfile = true;

			// 0 = success	
			int error_code = -1;

			// number of times the given user/password
			// combo has received an 'access denied' when
			// used to try to start the process
			int bad_password_attempts = 0;

			// keep trying to start the process until the
			// error code is no longer -1
			while ( error_code < 0 )
			{
				try
				{
					// start the process - if it is successful it will
					// it will set the password to the password used
					// to start the process
					StartProcess(
						psi, sudoServer, interaction_method, ref password );

					// cache the password if it was not cached
					if ( !pwd_is_cached )
						sudoServer.Password = password;

					// the process was started without throwing
					// an exception.  we don't have to try to
					// start it anymore.
					error_code = 0;
				}
				catch ( System.ComponentModel.Win32Exception e )
				{
					switch ( e.NativeErrorCode )
					{
						// unknown user/password
						case 1326:
						{
							// if the number of bad password 
							// attempts has exceeded the number
							// of allowed attempts then set the
							// error code to 5
							if ( bad_password_attempts == 2 )
								error_code = 1326;
							// user gets another chance
							else
							{
								// increment number of bad attempts
								++bad_password_attempts;

								// kill the existing password
								psi.Password = null;

								// notify the user
								WriteOutput(
									"invalid user name / password combination",
									OutputMessageTypes.Error,
									interaction_method );
							}
							break;
						}
						default:
						{
							error_code = e.ErrorCode;
							break;
						}
					}
				}
			}

			// if there was an error then notify the user
			if ( error_code != 0 )
			{
				string message = string.Empty;

				switch ( error_code )
				{
					// access denied
					case 1326:
					{
						message =
							"too many invalid user name / password attempts";
						break;
					}
					default:
					{
						message = string.Format(
							CultureInfo.CurrentCulture,
							"win32 error - {0}",
							error_code );
						break;
					}
				}

				WriteOutput( message,
					OutputMessageTypes.Error,
					interaction_method );
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
		static private bool IsValidCommand( ref string cmdName )
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
		///		Tests all the executable file extensions on
		///		the command path parameter in order to determine
		///		whether the given command name is a valid 
		///		executable without the extension on the end.
		/// </summary>
		/// <param name="commandPath"></param>
		/// <returns></returns>
		static private string TestFileExtensions( string commandPath )
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

		static bool IsBuiltinCommand( string cmdName )
		{
			return ( Regex.IsMatch( cmdName,
				"(cd)|(dir)|(type)", 
				RegexOptions.IgnoreCase ) );
		}

		static private void StartProcess(
			ProcessStartInfo startInfo,
			ISudoServer sudoServer,
			InteractionMethodTypes interactionMethod,
			ref string password )
		{
			// if the password was not cached
			// the prompt the user for it
			if ( startInfo.Password == null )
			{
				// decide whether to prompt for
				// a password with a form or
				// read the password in from
				// the command line
				switch ( interactionMethod )
				{
					case InteractionMethodTypes.Forms:
					{
						// display the inputbox that
						// asks a user for their password
						InputBox ib = new InputBox();
						DialogResult dr = ib.ShowDialog();
						if ( dr == DialogResult.OK )
							password = ib.Password;
						ib.Dispose();

						// get the password from the inputbox,
						// make a secure string out of it, and
						// stick it in the startInfo's password
						// property
						startInfo.Password = GetSecureString( password );
						break;
					}
					case InteractionMethodTypes.Console:
					{
						if ( startInfo.Password == null )
							startInfo.Password = new SecureString();

						// read the password in from the command
						// line char by char, not displaying the
						// password of course.  append all the
						// chars, save the newline, to the startInfo's
						// Password, which is a secure string
						Console.WriteLine();
						Console.Write( "password: " );
						ConsoleKeyInfo cki = Console.ReadKey( true );
						do
						{
							startInfo.Password.AppendChar( cki.KeyChar );
							cki = Console.ReadKey( true );
						} while ( cki.Key != ConsoleKey.Enter );
						break;
					}
				}
			}

			// elevate the user to administrator privileges
			sudoServer.BeginElevatePrivileges();

			// start the process
			try
			{
				Process.Start( startInfo );
			}
			finally
			{
				// revert the user to standard privileges
				sudoServer.EndElevatePrivileges();
			}
		}

		static private SecureString GetSecureString( string plain )
		{
			SecureString ss = new SecureString();
			for ( int x = 0; x < plain.Length; ++x )
				ss.AppendChar( plain[ x ] );
			return( ss );
		}

		static private void WriteOutput(
			string output,
			OutputMessageTypes outputMessageType,
			InteractionMethodTypes interactionMethod )
		{
			// decide whether to output messages to
			// to console or with message boxes
			switch ( interactionMethod )
			{
				case InteractionMethodTypes.Console:
				{
					string format = string.Empty;

					switch ( outputMessageType )
					{
						case OutputMessageTypes.Error:
						{
							format = "error - {0}";
							break;
						}
						case OutputMessageTypes.Standard:
						{
							format = "{0}";
							break;
						}
					}

					Console.WriteLine();
					Console.WriteLine( format, output );
					break;
				}
				case InteractionMethodTypes.Forms:
				{
					// get the type of message box
					// icon to display
					MessageBoxIcon mbi = 0;

					switch ( outputMessageType )
					{
						case OutputMessageTypes.Error:
						{
							mbi = MessageBoxIcon.Error;
							break;
						}
						case OutputMessageTypes.Standard:
						{
							mbi = MessageBoxIcon.Information;
							break;
						}
					}

					// create a bogus form so it
					// can own the message box
					Form f = new Form();
					f.ShowInTaskbar = false;

					// show the message box
					MessageBox.Show( f, output, "sudo", 
						MessageBoxButtons.OK, mbi );

					f.Dispose();
					break;
				}
				default:
				{
					//do nothing
					break;
				}
			}
		}

		/// <summary>
		///		Get an string value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be be the key value.  Empty string
		///		if not found.
		///	</param>
		static void GetConfigValue( 
			string keyName,
			out string keyValue )
		{
			// if the key is defined in the config file 
			// then get it else return an empty string
			if ( Array.IndexOf( ConfigurationManager.AppSettings.AllKeys,
				keyName ) > -1 )
				keyValue = ConfigurationManager.AppSettings[ keyName ];
			// empty string
			else
				keyValue = string.Empty;
		}

		/// <summary>
		///		Get an integer value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed integer.
		///		-1 if not found or cannot parse.
		///	</param>
		static void GetConfigValue( 
			string keyName, 
			out int keyValue )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				keyValue = 0;
			else
			{
				if ( !int.TryParse( temp, out keyValue ) )
					keyValue = -1;
			}
		}

		/// <summary>
		///		Get an InteractionMethodType value from 
		///		the host process's config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed InteractionMethodType.
		///		0 if not found or cannot parse.
		///	</param>
		static void GetConfigValue(
			string keyName,
			out InteractionMethodTypes interactionMethod )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				interactionMethod = 0;
			else
			{
				try
				{
					interactionMethod = ( InteractionMethodTypes )
						Enum.Parse( typeof( InteractionMethodTypes ),
						temp, true );
				}
				catch
				{
					interactionMethod = 0;
				}
			}
		}
	}
}
