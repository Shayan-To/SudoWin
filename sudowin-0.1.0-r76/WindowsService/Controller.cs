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
using Sudowin.PublicLibrary;
using System.ComponentModel;
using System.ServiceProcess;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Diagnostics;

namespace Sudowin.WindowsService
{
	/// <summary>
	///		Service controller for the Sudowin service.
	/// </summary>
	public partial class Controller : ServiceBase
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Controller()
		{
			InitializeComponent();
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="args">
		///		
		/// </param>
		protected override void OnStart( string[] args )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod, 
				"entering OnStart" );
			
			// get p to the actual exe
			Uri uri = new Uri( 
				System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase );

			string remote_config_uri = uri.LocalPath + ".config";

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose, 
				"configuring remoting with " + remote_config_uri );
			
			// configure remoting channels and objects
			RemotingConfiguration.Configure( remote_config_uri, true );

			// create the sa object
			DataServer ds = Activator.GetObject( typeof( DataServer ),
				System.Configuration.ConfigurationManager.AppSettings[ "dataServerUri" ] )
				as DataServer;

			// activate the data server object first before any of the
			// sudo clients can do so.  this will cause any exceptions that
			// might get thrown while reading the sudoers data source to
			// cause the service not to start, rather than crashing a sudo
			// ca application.
			ds.Activate();

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStart" );
		}

		/// <summary>
		///		Stops this service.
		/// </summary>
		protected override void OnStop()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int )  EventIds.EnterMethod, 
				"entering OnStop" );
			m_ts.Close();
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStop" );
		}
	}
}
