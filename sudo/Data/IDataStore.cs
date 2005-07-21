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

namespace Sudo.Data
{
	/// <summary>
	///		
	/// </summary>
	public interface IDataStore : IDisposable
	{
		/// <summary>
		///		Opens a connection to the sudoers data store.
		/// </summary>
		/// <param name="connectionString">
		///		Connection string used to open a connection
		///		to the sudoers data store.
		/// </param>
		void Open( string connectionString );

		/// <summary>
		///		Opens a connection to the sudoers data store
		///		and validate the data with the given
		///		schema file.
		/// </summary>
		/// <param name="connectionString">
		///		Connection string used to open a connection
		///		to the sudoers data store.
		/// </param>
		/// <param name="schemaFileUri">
		///		Uri of schema file to use to validate the data.
		/// </param>
		void Open( string connectionString, Uri schemaFileUri );

		/// <summary>
		///		Closes the connection to the sudoers data store.
		/// </summary>
		void Close();

		/// <summary>
		///		Checks to see if the user has the right
		///		to execute the given command with sudo.
		/// </summary>
		/// <param name="userName">
		///		Name of the user that is being verified as able
		///		to execute the given command with sudo.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		bool IsCommandAllowed(
			string userName,
			string commandPath, 
			string commandArguments );

		/// <summary>
		///		Gets the number of invalid
		///		logon attempts the user is allowed.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of invalid logon attempts the
		///		user is allowed.
		/// </returns>
		int GetPasswordTries( string userName );
		
		/// <summary>
		///		Gets the number of seconds that a
		///		user's valid logon is cached.
		/// </summary>
		/// <param name="userName">
		///		User name to get data for.
		/// </param>
		/// <returns>
		///		Number of seconds that a user's
		///		valid logon is cached.
		/// </returns>
		int GetPasswordTimeout( string userName );
		
	}
}
