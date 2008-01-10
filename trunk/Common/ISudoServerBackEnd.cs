using System;
using System.Security.Permissions;

namespace Sudowin.Common
{
	public interface ISudoServerBackEnd
	{
		/// <summary>
		///		Adds or removes a user to the privileges group.
		/// </summary>
		/// <param name="userName">
		///		User name to add or remove to the privileges 
		///		group.  This name should be in the format:
		/// 
		///			DOMAIN\USERNAME
		/// </param>
		/// <param name="which">
		///		1 "Add" or 0 "Remove"
		/// </param>
		/// <param name="privilegesGroup">
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use Sudowin.
		/// </param>
		/// <returns>
		///		If this method was invoked with the which parameter
		///		equal to 1 then this method returns whether or not
		///		the given user name was already a member of the group
		///		that it was supposed to be added to.
		/// 
		///		If this method was invoked with the which parameter
		///		equal to 0 then the return value of this method can
		///		be ignored.
		/// </returns>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		bool AddRemoveUser(
			string userName,
			int which,
			string privilegesGroup );
	}
}
