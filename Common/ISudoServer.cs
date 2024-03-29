/*
Copyright (c) 2005-2008, Schley Andrew Kutz <akutz@lostcreations.com>
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
using System.Security;
using System.Security.Permissions;

namespace Sudowin.Common
{
	/// <summary>
	///		An interface stub for Sudowin.Service.Server.
	/// </summary>
	public interface ISudoServer
	{
		/// <summary>
		///		Invokes sudo on the given command path.
		/// </summary>
		/// <param name="password">
		///		Password of user invoking Sudowin.
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
		///		An integer that can be cast as a 
		///		SudoResultsTypes value.
		/// </returns>
		SudoResultTypes Sudo(
			string password,
			string commandPath,
			string commandArguments );

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
		bool ExceededInvalidLogonLimit
		{
			get;
		}

		/// <summary>
		///		True if the user's credentials are cached, 
		///		false if otherwise.
		/// </summary>
		/// /// <remmarks>
		///		The value of this property does not
		///		have to be respected by the client, but
		///		the server will return an immediate
		///		invalid logon if the client assumes the
		///		users credentials are cached when they 
		///		are not and passes a null password to 
		///		the Sudo method.
		/// </remmarks>
		bool AreCredentialsCached
		{
			get;
		}

		/// <summary>
		///		This is a dummy property.  It enables
		///		clients to trap an exception that will occur
		///		if they do not have permissions to talk to
		///		the server.
		/// </summary>
		bool IsConnectionOpen
		{
			get;
		}
	}
}
