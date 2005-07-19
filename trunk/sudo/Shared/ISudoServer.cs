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
using System.Security.Permissions;

namespace Sudo.Shared
{
	/// <summary>
	///		An interface stub for Sudo.Service.Server.
	/// </summary>
	public interface ISudoServer
	{
		/// <summary>
		///		Gets/sets user passwords from/in the credential cache.
		/// </summary>
		/// <remarks>
		///		This property accessor makes a call to the backing
		///		store each time its get and set methods are invoked.
		/// </remarks>
		string Password
		{
			[EnvironmentPermission( SecurityAction.LinkDemand )]
			get;
			set;
		}

		/// <summary>
		///		Number of allowed bad password attempts a user has.
		/// </summary>
		int PasswordTries
		{
			get;
		}

		/// <summary>
		///		Number of seconds sudo will cache a user's password.
		/// </summary>
		int PasswordTimeout
		{
			get;
		}

		/// <summary>
		///		Elevate caller to administrator privileges.
		/// </summary>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		void BeginElevatePrivileges();

		/// <summary>
		///		Revert caller to normal privileges.
		/// </summary>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		void EndElevatePrivileges();

		/// <summary>
		///		A dummy method that will force the client
		///		to throw an exception if the caller is not
		///		a member of any of the groups authorized
		///		to use this server.
		/// </summary>
		void AuthenticateClient();

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
		bool IsCommandAllowed( 
			string commandPath,
			string[] commandSwitches );
	}
}
