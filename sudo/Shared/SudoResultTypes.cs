using System;
using System.Collections.Generic;
using System.Text;

namespace Sudo.Shared
{
	/// <summary>
	///		Result types of ISudoServer.Sudo.
	/// </summary>
	public enum SudoResultTypes : int
	{
		/// <summary>
		///		Success
		/// </summary>
		SudoK,

		/// <summary>
		///		Unknown error
		/// </summary>
		SudoH,

		/// <summary>
		///		Could not open handle to windows terminal
		///		server session manager.
		/// </summary>
		ErrorOpeningWts,

		/// <summary>
		///		Given command path is not allowed.
		/// </summary>
		CommandNotAllowed,

		/// <summary>
		///		User has entered a bad username / password
		///		combination.
		/// </summary>
		InvalidLogon,

		/// <summary>
		///		User has exceeded the number of allowed
		///		invalid logon attempts in a row.
		/// </summary>
		TooManyInvalidLogons,
		
		/// <summary>
		///		User has exceeded the number of allowed
		///		invalid logon attempts within the server's
		///		configured lock out period and will now
		///		be locked out from sudo for a given amount
		///		of time.
		/// </summary>
		SudoUserLockedOut,
	}
}
