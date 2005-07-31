using System;
using System.Collections.Generic;
using System.Text;

namespace Sudo.PublicLibrary
{
	/// <summary>
	///		Information about a command listed in the sudoers data store
	///		as it relates to the user who is attempting to sudo the command.
	/// </summary>
	[Serializable]
	public struct CommandInfo
	{
		/// <summary>
		///		True if the user is allowed to sudo the command; 
		///		false if otherwise.
		/// </summary>
		public bool IsCommandAllowed;

		/// <summary>
		///		Whether to log nothing, all failed
		///		actions, all successful actions, or
		///		both.
		/// </summary>
		public LoggingLevelTypes LoggingLevel;
	}
}
