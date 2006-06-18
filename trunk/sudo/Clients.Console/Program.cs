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
using System.Text;
using System.Reflection;
using Sudo.PublicLibrary;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Sudo.ConsoleApplication
{
	class Program
	{
		static void Main( string[] args )
		{
			switch ( args.Length )
			{
				// print the help menu
				case 0:
				{
					PrintHelpMenu();
					break;
				}
				// -h
				// -v
				// %cmd%
				case 1:
				{
					if ( Regex.IsMatch( args[ 0 ], @"^--?(h|\?|(help))$" ) )
						PrintHelpMenu();
					else if ( Regex.IsMatch( args[ 0 ], @"^--?(v|(version))$" ) )
						PrintVersion();
					else
						InvokeSudo( string.Empty, args[ 0 ], string.Empty );
					break;
				}
				// %cmd% %arg1%
				case 2:
				{
					InvokeSudo( string.Empty, args[ 0 ], args[ 1 ] );
					break;
				}
				// -p %pwd% %cmd%
				// %cmd% %arg1% %arg2%
				case 3:
				{
					if ( Regex.IsMatch( args[ 0 ], @"^--?(p|(password))$" ) )
					{
						InvokeSudo( args[ 1 ], args[ 2 ], string.Empty );
					}
					else
					{
						InvokeSudo( string.Empty, args[ 0 ], 
							args[ 1 ] + " " + args[ 2 ] );
					}
					break;
				}
				// -c -p %pwd% %cmd%
				// -p %pwd% %cmd% %arg1%
				// %cmd% %arg1% %arg2% %arg3%
				case 4:
				{
					if ( Regex.IsMatch( args[ 0 ], @"^--?(c|(current-context))$" ) &&
						Regex.IsMatch( args[ 1 ], @"^--?(p|(password))$" ) )
					{
						CreateProcessLoadProfile( args[ 2 ], args[ 3 ], string.Empty );
					}
					else if ( Regex.IsMatch( args[ 0 ], @"^--?(p|(password))$" ) )
					{
						InvokeSudo( args[ 1 ], args[ 2 ], args[ 3 ] );
					}
					else
					{
						InvokeSudo( string.Empty, args[ 0 ], 
							args[ 1 ] + " " + args[ 2 ] + " " + args[ 3 ]);
					}
					break;
				}
				// args.Length > 4
				default:
				{
					if ( Regex.IsMatch( args[ 0 ], @"^--?(c|(current-context))$" ) &&
						Regex.IsMatch( args[ 1 ], @"^--?(p|(password))$" ) )
					{
						CreateProcessLoadProfile( args[ 2 ], args[ 3 ], 
							string.Join( " ", args, 4, args.Length - 4 ) );
					}
					else if ( Regex.IsMatch( args[ 0 ], @"^--?(p|(password))$" ) )
					{
						InvokeSudo( args[ 1 ], args[ 2 ], 
							string.Join( " ", args, 3, args.Length - 3 ) );
					}
					else
					{
						InvokeSudo( string.Empty, args[ 0 ],
							string.Join( " ", args, 1, args.Length - 1 ) );
					}
					break;
				}
			}
		}

		/// <summary>
		///		Creates a new process with commandPath and commandArguments
		///		and loads the executing user's profile when doing so.
		/// </summary>
		/// <param name="password">
		///		Password of the executing user.
		/// </param>
		/// <param name="commandPath">
		///		Command path to create new process with.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments used with commandPath.
		/// </param>
		private static void CreateProcessLoadProfile(
			string password,
			string commandPath,
			string commandArguments )
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.FileName = commandPath;
			psi.Arguments = commandArguments;

			// MUST be false when specifying credentials
			psi.UseShellExecute = false;

			// MUST be true so that the user's profile will
			// be loaded and any new group memberships will
			// be respected
			psi.LoadUserProfile = true;

			// get the domain and user name parts of the current
			// windows identity
			Match identity_match = Regex.Match( 
				WindowsIdentity.GetCurrent().Name,
				@"^([^\\]+)\\(.+)$" );
			// domain name
			string dn = identity_match.Groups[ 1 ].Value;
			// user name
			string un = identity_match.Groups[ 2 ].Value;

			// only set the domain if it is an actual domain and
			// not the name of the local machine, i.e. a local account
			// invoking sudo
			if ( !Regex.IsMatch( dn,
				Environment.MachineName, RegexOptions.IgnoreCase ) )
			{
				psi.Domain = dn;
			}

			psi.UserName = un;

			// transform the plain-text password into a
			// SecureString so that the ProcessStartInfo class
			// can use it
			psi.Password = new System.Security.SecureString();
			for ( int x = 0; x < password.Length; ++x )
				psi.Password.AppendChar( password[ x ] );

			Process.Start( psi );
		}

		private static void InvokeSudo( 
			string password, 
			string commandPath, 
			string commandArguments )
		{
			#region configure remoting

			// get path to the actual exe
			Uri uri = new Uri(
				Assembly.GetExecutingAssembly().GetName().CodeBase );

			// configure remoting channels and objects
			RemotingConfiguration.Configure( uri.LocalPath + ".config", true );

			// get the server object that is used to elevate
			// privleges and act as a backend store for
			// caching credentials

			// get an array of the registered well known client urls
			WellKnownClientTypeEntry[] wkts =
				RemotingConfiguration.GetRegisteredWellKnownClientTypes();

			// loop through the list of well known clients until
			// the SudoServer object is found
			ISudoServer iss = null;
			for ( int x = 0; x < wkts.Length && iss == null; ++x )
			{
				iss = Activator.GetObject( typeof( ISudoServer ),
					wkts[ x ].ObjectUrl ) as ISudoServer;
			}

			#endregion

			// how sudo interacts with the user
			//
			// try to get this from the host process's
			// config file.  if that fails set the
			// number of allowed attempts to 'Gui'
			UIModeTypes ui_mode;
			ManagedMethods.GetConfigValue( "uiMode",
				out ui_mode );
			if ( ui_mode == 0 )
				ui_mode = UIModeTypes.Gui;

			// holds the result of the sudo invocation
			SudoResultTypes srt;

			do
			{
				if ( iss.ExceededInvalidLogonLimit )
				{
					WriteOutput( "multiple invalid logon limit exceeded " +
						"- temporary lockout enforced",
						OutputMessageTypes.Error, ui_mode );
					srt = SudoResultTypes.LockedOut;
				}
				else
				{
					// if the password was passed into this program as
					// a command line argument or if the user's credentials
					// are cached then do not bother asking the user for
					// their password
					password = password.Length > 0 || iss.AreCredentialsCached ?
						string.Empty : GetPassword( ui_mode );

					// invoke sudo
					srt = iss.Sudo( password, commandPath, commandArguments );

					// set the password to an empty string.  this is not for
					// security as one might think but rather so if the result
					// of Sudo was InvalidLogon the next iteration through this
					// loop will prompt the user for their password instead
					// of using this password known to be invalid
					password = string.Empty;

					switch ( srt )
					{
						case SudoResultTypes.InvalidLogon:
						{
							WriteOutput( "invalid logon",
								OutputMessageTypes.Error, ui_mode );
							break;
						}
						case SudoResultTypes.TooManyInvalidLogons:
						{
							WriteOutput( "invalid logon limit exceeded",
								OutputMessageTypes.Error, ui_mode );
							break;
						}
						case SudoResultTypes.CommandNotAllowed:
						{
							WriteOutput( "command not allowed",
								OutputMessageTypes.Error, ui_mode );
							break;
						}
						case SudoResultTypes.LockedOut:
						{
							WriteOutput( "multiple invalid logon limit exceeded " +
								"- temporary lockout enforced",
								OutputMessageTypes.Error, ui_mode );
							break;
						}
					}
				}
			} while ( srt == SudoResultTypes.InvalidLogon );
		}

		private static void PrintVersion()
		{
			Console.WriteLine();
			Console.WriteLine( "sudo for windows by akutz@lostcreations.com" );
			Console.WriteLine( "{0}",
				Assembly.GetExecutingAssembly().GetName().Version.ToString( 4 ) );
			Console.WriteLine();
		}

		private static void PrintHelpMenu()
		{
			string menu = @"
usage: sudo [OPTION]... [COMMAND] [ARGUMENTS]
executes the COMMAND and its ARGUMENTS in the security context of a user's
assigned privileges group

    -p, --password            password of the user executing sudo

                              if this option is not specified and
                              the user's credentials are not cached
                              a password prompt will appear requesting
                              that the user enter their password

    -v, --version             displays sudo version number
    -h, --help                displays this menu
    -?, --help                displays this menu

    examples:

      sudo c:\program files\microsoft visual studio 8\common7\ide\devenv.exe

        will launch visual studio .net 2005 with administrative privileges

      sudo -p mypassword cmd

        will suppress sudo's password prompt using 'mypassword' instead
        and launch the windows command shell with administrative privileges


";

			Console.WriteLine( menu );
		}

		static private string GetPassword( UIModeTypes uiMode )
		{
			// password for this method to return
			string password = string.Empty;

			// decide whether to prompt for
			// a password with a form or
			// read the password in from
			// the command line
			switch ( uiMode )
			{
				case UIModeTypes.Gui:
				{
					// display the inputbox that
					// asks a user for their password
					// then copy the password to this method's
					// ref parameter, password
					InputBox ib = new InputBox();
					DialogResult dr = ib.ShowDialog();
					if ( dr == DialogResult.OK )
						password = ib.Password;
					ib.Dispose();
					break;
				}
				case UIModeTypes.CommandLine:
				{
					// read the password in from the command
					// line char by char, not displaying the
					// password of course.  append all the
					// chars, save the newline, to a stringbuffer
					// and then copy the buffer to this method's
					// ref parameter, password
					System.Text.StringBuilder pwd =
						new System.Text.StringBuilder( 100 );
					Console.WriteLine();
					Console.Write( "password: " );
					ConsoleKeyInfo cki = Console.ReadKey( true );
					do
					{
						pwd.Append( cki.KeyChar );
						cki = Console.ReadKey( true );
					} while ( cki.Key != ConsoleKey.Enter );
					password = pwd.ToString();
					break;
				}
			}

			return ( password );
		}

		static private void WriteOutput(
			string output,
			OutputMessageTypes outputMessageType,
			UIModeTypes interactionMethod )
		{
			// decide whether to output messages to
			// to console or with message boxes
			switch ( interactionMethod )
			{
				case UIModeTypes.CommandLine:
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
				case UIModeTypes.Gui:
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
	}
}
