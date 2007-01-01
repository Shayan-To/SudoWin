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
using System.Text;
using Sudowin.Common;
using System.Security;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sudowin.Plugins.CredentialsCache.LocalServer
{
	public class LocalServerCredentialsCachePlugin : Sudowin.Plugins.CredentialsCache.CredentialsCachePlugin
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Collection of CredentialsCache structures used
		///		to track information about users.
		/// </summary>
		private Dictionary<string, CredentialsCache> m_ccs =
			new Dictionary<string, CredentialsCache>();

		/// <summary>
		///		Collection of SecureStrings used to persist
		///		the passphrases of the users who invoke Sudowin.
		/// </summary>
		private Dictionary<string, SecureString> m_passphrases =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_ccs when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		This mutex is used to synchronize access
		///		to the m_ccs, m_passphrases, and m_tmrs collections.
		/// </summary>
		private Mutex m_coll_mtx = new Mutex( false );

		public override bool GetCache( string userName, ref CredentialsCache credCache )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetCache( string, ref CredentialsCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},credCache=", userName );

			m_coll_mtx.WaitOne();
			bool is_passphrase_cached;
			string passphrase = string.Empty;
			if ( is_passphrase_cached = m_passphrases.ContainsKey( userName ) )
			{
				SecureString ss = m_passphrases[ userName ];
				IntPtr ps = Marshal.SecureStringToBSTR( ss );
				passphrase = Marshal.PtrToStringBSTR( ps );
				Marshal.FreeBSTR( ps );
			}
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, is_passphrase_cached={1}", userName, is_passphrase_cached );
		
			m_coll_mtx.WaitOne();
			bool is_cred_cache_cached = m_ccs.TryGetValue( userName, out credCache );
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, is_cred_cache_cached={1}", userName, is_cred_cache_cached );

			// put the retrieved passphrase back into the credentials 
			// cache structure
			credCache.Passphrase = passphrase;

			return ( is_passphrase_cached && is_cred_cache_cached );
		}

		public override void SetCache( string userName, CredentialsCache credCache )
		{
			m_coll_mtx.WaitOne();

			// securely store the passphrase
			SecureString ss = new SecureString();
			for ( int x = 0; x < credCache.Passphrase.Length; ++x )
				ss.AppendChar( credCache.Passphrase[ x ] );

			if ( m_passphrases.ContainsKey( userName ) )
			{
				m_passphrases[ userName ].Clear();
				m_passphrases[ userName ] = ss;
			}
			else
			{
				m_passphrases.Add( userName, ss );
			}

			// remove the passphrase from the credentials cache 
			// structure that is to be stored
			credCache.Passphrase = string.Empty;

			// store the credentials cache structure
			if ( m_ccs.ContainsKey( userName ) )
			{
				m_ccs[ userName ] = credCache;
			}
			else
			{
				m_ccs.Add( userName, credCache );
			}

			m_coll_mtx.ReleaseMutex();
		}

		public override void ExpireCache( string userName, int seconds )
		{
			m_coll_mtx.WaitOne();

			// if the timers collection already contains a timer
			// for this user then change when it is supposed to fire
			if ( m_tmrs.ContainsKey( userName ) )
			{
				m_tmrs[ userName ].Change(
					seconds * 1000, Timeout.Infinite );
			}
			// add a new timer and a new changed value
			else
			{
				m_tmrs.Add( userName, new Timer(
					new TimerCallback( ExpireUserCacheCallback ),
					userName, seconds * 1000, Timeout.Infinite ) );
			}

			m_coll_mtx.ReleaseMutex();
		}

		private void ExpireUserCacheCallback( object state )
		{
			m_coll_mtx.WaitOne();

			// cast this callback's state as 
			// a user name string
			string un = state as string;

			// if the user has a UserCache structure
			// in the collection then remove it
			if ( m_ccs.ContainsKey( un ) )
				m_ccs.Remove( un );

			// if the user has a persisted passphrase clear
			// it and then remove it from the collection
			if ( m_passphrases.ContainsKey( un ) )
			{
				m_passphrases[ un ].Clear();
				m_passphrases.Remove( un );
			}

			// dispose of the timer that caused this
			// callback and remove it from m_tmrs
			m_tmrs[ un ].Dispose();
			m_tmrs.Remove( un );

			m_coll_mtx.ReleaseMutex();
		}
	}
}
