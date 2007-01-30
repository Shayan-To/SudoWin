/*
Copyright (c) 2005, 2006, 2007, Schley Andrew Kutz <akutz@lostcreations.com>
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
using Sudowin.Common;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Sudowin.CallbackApplication
{
	class Program
	{
		static void Main( string[] args )
		{
			try
			{
				// enclose arguments with quotes that have spaces in them -- this bit
				// 'o code was intended to fix the problem with msi files and passing
				// unescaped paths -- i would have preferred a more elegant solution
				// using Array.ForEach<T>, but alas, this method does not properly set
				// the values back inside the array
				string cmd_args = string.Empty;
				if ( args.Length > 2 )
				{
					string[] arr_cmd_args = new string[ args.Length - 2 ];
					Array.Copy( args, 2, arr_cmd_args, 0, args.Length - 2 );
					for ( int x = 0; x < arr_cmd_args.Length; ++x )
					{
						if ( arr_cmd_args[ x ].Contains( " " ) && arr_cmd_args[ x ][ 0 ] != '"' )
							arr_cmd_args[ x ] = string.Format( "\"{0}\"", arr_cmd_args[ x ] );
					}
					cmd_args = string.Join( " ", arr_cmd_args );
				}

				CreateProcessLoadProfile( args[ 0 ], args[ 1 ], cmd_args );
			}
			catch ( Exception e )
			{
				string msg = string.Format( CultureInfo.CurrentCulture,
					"I do apologize, but I seem to have lost my pants.  Until I find them, " +
					"be a good fellow or madam and send the following error to the author of " +
					"this dashingly handsome program.{0}{0}" +
					"Message: {1}" +
					"Stacktrace: {2}",
					Environment.NewLine,
					e.Message, e.StackTrace );
				Console.WriteLine( msg );
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
	}
}
