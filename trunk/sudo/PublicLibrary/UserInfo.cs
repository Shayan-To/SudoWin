using System;
using System.Collections.Generic;
using System.Text;

namespace Sudo.PublicLibrary
{
	/// <summary>
	///		Information about user listed in the sudoers data store.
	/// </summary>
	[Serializable]
	public struct UserInfo
	{
		/// <summary>
		///		Number of invalid logon attempts the
		///		user is allowed.
		/// </summary>
		public int InvalidLogons;

		/// <summary>
		///		Number of times the user has exceeded
		///		their invalid logon attempt limit.
		/// </summary>
		public int TimesExceededInvalidLogons;

		/// <summary>
		///		Number of seconds that the sudo server keeps
		///		track of a user's invalid logon attempts.
		/// </summary>
		public int InvalidLogonTimeout;

		/// <summary>
		///		Number of seconds that a user is locked out
		///		after exceeding their invalid logon attempt
		///		limit.
		/// </summary>
		public int LockoutTimeout;

		/// <summary>
		///		Number of seconds that a
		///		user's valid logon is cached.
		/// </summary>
		public int LogonTimeout;

		/// <summary>
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use sudo.
		/// </summary>
		public string PrivilegesGroup;

		/// <summary>
		///		Whether to log nothing, all failed
		///		actions, all successful actions, or
		///		both.
		/// </summary>
		public LoggingLevelTypes LoggingLevel;
	}
}
