/*
Copyright (c) 2005, 2006, 2007, Schley Andrew Kutz <akutz@lostcreations.com>
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
using System.Globalization;
using System.DirectoryServices;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace Sudowin.Servers.BackEnd
{
	public class SudoServer :	MarshalByRefObject,
		
								Sudowin.Common.ISudoServerBackEnd
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
		public bool AddRemoveUser(
			string userName,
			int which,
			string privilegesGroup )
		{
			// get the domain name and user name
			string[] usr_split = userName.Split( new char[] { '\\' } );
			string usr_dn_part = usr_split[ 0 ];
			string usr_un_part = usr_split[ 1 ];

			// get the domain name and group name
			string[] grp_split = privilegesGroup.Split( new char[] { '\\' } );
			string grp_dn_part = grp_split[ 0 ];
			string grp_gn_part = grp_split[ 1 ];

			DirectoryEntry domain = new DirectoryEntry();

			DirectorySearcher dsearcher = new DirectorySearcher(
				domain, "samAccountName=" + grp_gn_part, null, SearchScope.Subtree );
			SearchResult sr = dsearcher.FindOne();
			DirectoryEntry group = sr.GetDirectoryEntry();

			dsearcher.Filter = "samAccountName=" + usr_un_part;
			sr = dsearcher.FindOne();
			string usr_path = sr.Path;

			bool isAlreadyMember = bool.Parse( Convert.ToString(
				group.Invoke( "IsMember", usr_path ),
				CultureInfo.CurrentCulture ) );

			// add user to privileges group
			if ( which == 1 && !isAlreadyMember )
			{
				group.Invoke( "Add", usr_path );
			}

			// remove user from privileges group
			else if ( which == 0 && isAlreadyMember )
			{
				group.Invoke( "Remove", usr_path );
			}

			// save changes
			group.CommitChanges();

			// cleanup
			group.Dispose();
			domain.Dispose();

			return ( isAlreadyMember );
		} 
	}
}
