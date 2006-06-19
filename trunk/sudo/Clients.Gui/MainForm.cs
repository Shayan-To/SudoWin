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
using System.IO;
using System.Text;
using System.Drawing;
using System.Reflection;
using Sudo.PublicLibrary;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Security.Principal;

namespace Sudo.Clients.Gui
{
	public partial class MainForm : Form
	{
		private ISudoServer m_isudo_server;

		public MainForm()
		{
			InitializeComponent();

			#region configure remoting

			// get path to the actual exe
			Uri uri = new Uri(
				Assembly.GetExecutingAssembly().GetName().CodeBase );

			// configure remoting channels and objects
			RemotingConfiguration.Configure( uri.LocalPath + ".config", true );

			// get the server object that is used to elevate
			// privleges and act as a backend store for
			// caching credentials

			// get an array of the registered well known client urls
			WellKnownClientTypeEntry[] wkts =
				RemotingConfiguration.GetRegisteredWellKnownClientTypes();

			// loop through the list of well known clients until
			// the SudoServer object is found
			for ( int x = 0; x < wkts.Length && m_isudo_server == null; ++x )
			{
				m_isudo_server = Activator.GetObject( typeof( ISudoServer ),
					wkts[ x ].ObjectUrl ) as ISudoServer;
			}

			#endregion

			// get the current user's account icon if they have one
			string user_icon_path = string.Format( "{0}\\{1}\\{2}.bmp",
				Environment.GetEnvironmentVariable( "AllUsersProfile" ),
				@"Application Data\Microsoft\User Account Pictures",
				WindowsIdentity.GetCurrent().Name.Split( new char[] { '\\' } )[ 1 ] );
			
			// load the user's account icon if they have one, otherwise
			// just load a random icon from the standard location.
			if ( File.Exists( user_icon_path ) )
			{
				m_picbox_user_icon.Load( user_icon_path );
			}
			else
			{
				string icon_directory_path = string.Format( "{0}\\{1}",
					Environment.GetEnvironmentVariable( "AllUsersProfile" ),
					@"Application Data\Microsoft\User Account Pictures\Default Pictures" );
				if ( Directory.Exists( icon_directory_path ) )
				{
					string[] icon_file_paths = Directory.GetFiles( icon_directory_path );
					Random r = new Random();
					int icon_file_paths_index = r.Next( 0, icon_file_paths.Length - 1 );
					m_picbox_user_icon.Load( icon_file_paths[ icon_file_paths_index ] );
				}
			}

			// display the file being sudoed
			string[] args = Environment.GetCommandLineArgs();
			FileInfo sudoed_cmd = new FileInfo( args[ 1 ] );
			m_lbl_sudoed_cmd.Text = sudoed_cmd.Name;
			m_picbox_sudoed_cmd.Image = 
				Icon.ExtractAssociatedIcon( sudoed_cmd.FullName ).ToBitmap();
		}

		private void btnOk_Click( object sender, EventArgs e )
		{
			string password = m_txtbox_password.Text;
			
		}

		private void btnCancel_Click( object sender, EventArgs e )
		{
			Application.Exit();
		}
	}
}