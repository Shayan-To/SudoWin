using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Sudo.Clients.Gui
{
	static class Program
	{
		/// <summary>
		///		The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( new MainForm() );
		}
	}
}