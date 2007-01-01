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

namespace Sudowin.Plugins.Authorization
{
	public class AuthorizationPlugin : IAuthorizationPlugin
	{
		#region IAuthorizationPlugin Members

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
		public virtual void Open( string connectionString, Uri schemaFileUri )
		{
			throw new Exception( "This method must be overriden." );
		}

		/// <summary>
		///		Closes the connection to the authorization data source.
		/// </summary>
		public virtual void Close()
		{
			throw new Exception( "This method must be overriden." );
		}

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
		public virtual bool GetUserInfo( string userName, ref Sudowin.Common.UserInfo userInfo )
		{
			throw new Exception( "This method must be overriden." );
		}

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
		public virtual bool GetCommandInfo( string username, string commandPath, string commandArguments, ref Sudowin.Common.CommandInfo commandInfo )
		{
			throw new Exception( "This method must be overriden." );
		}

		#endregion

		#region IPlugin Members

		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		public virtual void Activate()
		{
			
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		///		Free resources.
		/// </summary>
		public virtual void Dispose()
		{
			throw new Exception( "This method must be overriden." );
		}

		#endregion
	}
}
