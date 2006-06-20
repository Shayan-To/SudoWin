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

namespace Sudo.PublicLibrary
{
	/// <summary>
	///		Result types of ISudoServer.Sudo.
	/// </summary>
	[Serializable]
	public enum SudoResultTypes : int
	{
		/// <summary>
		///		Ok
		/// </summary>
		SudoK = 0,

		/// <summary>
		///		Given command path is not allowed.
		/// </summary>
		CommandNotAllowed = 1,

		/// <summary>
		///		User has entered a bad username / password
		///		combination.
		/// </summary>
		InvalidLogon = 2,

		/// <summary>
		///		User has exceeded the number of allowed
		///		invalid logon attempts in a row.
		/// </summary>
		TooManyInvalidLogons = 3,
		
		/// <summary>
		///		User has exceeded the number of allowed
		///		invalid logon attempts within the server's
		///		configured lock out period and will now
		///		be locked out from sudo for a given amount
		///		of time.
		/// </summary>
		LockedOut = 4,
	}
}