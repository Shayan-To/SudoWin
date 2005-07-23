using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Configuration.Install;

namespace Sudo.WindowsService
{
	/// <summary>
	///		Project installer class for the Sudo Windows service.
	/// </summary>
	[RunInstaller( true )]
	public partial class ProjectInstaller : Installer
	{
		/// <summary>
		///		Default constructor.
		/// </summary>
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}