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
using Sudowin.Common;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Sudowin.Clients.Console
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
				// args.Length >= 4
				default:
				{
					if ( Regex.IsMatch( args[ 0 ], @"^--?(p|(password))$" ) )
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

		private static void InvokeSudo( 
			string password, 
			string commandPath, 
			string commandArguments )
		{
			System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController( "Sudowin" );
			if ( sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped )
			{
				System.Console.WriteLine();
				System.Console.WriteLine( "Sudowin service is stopped" );
				return;
			}

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

			bool is_sudo_server_comm_link_open = false;
			try
			{
				is_sudo_server_comm_link_open = iss.IsConnectionOpen;
			}
			catch
			{
			}

			if ( !is_sudo_server_comm_link_open )
			{
				System.Console.WriteLine( "Sudowin service is dead to you" );
				return;
			}

			// holds the result of the sudo invocation
			SudoResultTypes srt;

			do
			{
				if ( iss.ExceededInvalidLogonLimit )
				{
					System.Console.WriteLine( "Locked out" );
					srt = SudoResultTypes.LockedOut;
				}
				else
				{
					// if the password was passed into this program as
					// a command line argument or if the user's credentials
					// are cached then do not bother asking the user for
					// their password
					password = password.Length > 0 || iss.AreCredentialsCached ?
						string.Empty : GetPassword();

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
							System.Console.WriteLine( "Invalid logon attempt" );
							break;
						}
						case SudoResultTypes.TooManyInvalidLogons:
						{
							System.Console.WriteLine( "Invalid logon limit exceeded" );
							break;
						}
						case SudoResultTypes.CommandNotAllowed:
						{
							System.Console.WriteLine( "Command not allowed" );
							break;
						}
						case SudoResultTypes.LockedOut:
						{
							System.Console.WriteLine( "Locked out" );
							break;
						}
					}
				}
			} while ( srt == SudoResultTypes.InvalidLogon );
		}

		private static void PrintVersion()
		{
			System.Console.WriteLine();
			System.Console.WriteLine( "sudo for windows by akutz at lostcreations dot com" );
			System.Console.WriteLine( "{0}",
				Assembly.GetExecutingAssembly().GetName().Version.ToString( 4 ) );
			System.Console.WriteLine();
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

        will launch visual studio .net 2005 with elevated privileges

      sudo -p mypassword cmd

        will suppress sudo's password prompt using 'mypassword' instead
        and launch the windows command shell with elevated privileges


";

			System.Console.WriteLine( menu );
		}

		static private string GetPassword()
		{
			// password for this method to return
			string password = string.Empty;

			System.Text.StringBuilder pwd =
				new System.Text.StringBuilder( 100 );
			System.Console.WriteLine();
			System.Console.Write( "Please enter your password: " );
			ConsoleKeyInfo cki = System.Console.ReadKey( true );
			do
			{
				pwd.Append( cki.KeyChar );
				cki = System.Console.ReadKey( true );
			} while ( cki.Key != ConsoleKey.Enter );
			password = pwd.ToString();
					
			return ( password );
		}
	}
}
