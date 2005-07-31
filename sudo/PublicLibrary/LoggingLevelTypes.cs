using System;
using System.Collections.Generic;
using System.Text;

namespace Sudo.PublicLibrary
{
	/// <summary>
	///		Specifies different levels of logging for the
	///		sudo server.
	/// </summary>
	[Serializable]
	public enum LoggingLevelTypes : int
	{
		/// <summary>
		///		Log nothing.
		/// </summary>
		None = 0,

		/// <summary>
		///		Log events when they succeed.
		/// </summary>
		Success = 1,

		/// <summary>
		///		Log events if they fail.
		/// </summary>
		Failure = 2,

		/// <summary>
		///		Log an event when it succeeds or fails.
		/// </summary>
		Both = 3,
	}
}
