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
using System.Reflection;
using Sudo.PublicLibrary;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;

namespace Sudo.ConsoleApplication
{
	class Program
	{
		static void Main( string[] args )
		{
			#region configure remoting

			// get path to the actual exe
			Uri uri = new Uri(
				Assembly.GetExecutingAssembly().GetName().CodeBase );

			// configure remoting channels and objects
			RemotingConfiguration.Configure( uri.LocalPath + ".config" );

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

			// get the command path to sudo
			string cmd_path =
				args.Length < 1 ?
				string.Empty : args[ 0 ];

			// get the command arguments to sudo
			string cmd_args =
				args.Length < 2 ?
				string.Empty :
				string.Join( " ", args, 1, args.Length - 1 );

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
					srt = SudoResultTypes.SudoUserLockedOut;
				}
				else
				{
					// get the user's password if the user 
					// does not have cached credentials on
					// the server
					string password = iss.AreCredentialsCached ? 
						string.Empty : GetPassword( ui_mode );

					// invoke sudo
					srt = ( SudoResultTypes )
						iss.Sudo( password, cmd_path, cmd_args );

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
						case SudoResultTypes.SudoUserLockedOut:
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
