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
using Sudowin.Common;

namespace Sudowin.AuthorizationPlugins
{
	/// <summary>
	///		IAuthorizationPlugin defines the interface for all classes that 
	///		are designed to operate as an authorization plugin for Sudowin.  
	///		The sudo server uses authorization plugins to verify that a user 
	///		is allowed to execute a given command with elevated privileges.
	/// </summary>
	public interface IAuthorizationPlugin : IDisposable
	{
		/// <summary>
		///		Opens a connection to the authorization data source 
		///		and validates the data source with the given schema 
		///		file.
		/// </summary>
		/// <param name="connectionString">
		///		Connection string used to open a connection
		///		to the authorization data source.
		/// </param>
		/// <param name="schemaFileUri">
		///		Uri of schema file to use to validate the data source.
		/// </param>
		void Open( string connectionString, Uri schemaFileUri );

		/// <summary>
		///		Closes the connection to the authorization data source.
		/// </summary>
		void Close();

		/// <summary>
		///		Gets a Sudowin.Common.UserInfo structure
		///		from the authorization source for the given user name.
		/// </summary>
		/// <param name="userName">
		///		User name to get information for.
		/// </param>
		/// <param name="userInfo">
		///		Sudowin.Common.UserInfo structure for
		///		the given user name.
		/// </param>
		/// <returns>
		///		True if the UserInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		bool GetUserInfo( string userName, ref UserInfo userInfo );

		/// <summary>
		///		Gets a Sudowin.Common.CommandInfo structure
		///		from the authorization source for the given user name,
		///		command path, and command arguments.
		/// </summary>
		/// <param name="username">
		///		User name to get information for.
		/// </param>
		/// <param name="commandPath">
		///		Command path to get information for.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments to get information for.
		/// </param>
		/// <param name="commandInfo">
		///		Sudowin.Common.CommandInfo structure for
		///		the given user name, command path, and command 
		///		arguments.
		/// </param>
		/// <returns>
		///		True if the CommandInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		bool GetCommandInfo(
			string username,
			string commandPath,
			string commandArguments,
			ref CommandInfo commandInfo );
	}
}
