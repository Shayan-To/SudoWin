/*
Copyright (c) 2005-2008, Schley Andrew Kutz <akutz@lostcreations.com>
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
using System.Security;
using System.Runtime.InteropServices;

namespace Sudowin.Servers.BackEnd
{
	internal class DataServer : MarshalByRefObject
	{
		private SecureString m_session_key = null;

		/// <summary>
		///		This is the value used to verify communications
		///		from the FrontEnd service to the BackEnd service.  
		///		Returns null if this property has not been initialized.
		/// </summary>
		public string SessionKey
		{
			get
			{
				if ( m_session_key == null )
				{
					return ( null );
				}
				else
				{
					IntPtr ps = Marshal.SecureStringToBSTR( m_session_key );
					string skey = Marshal.PtrToStringBSTR( ps );
					Marshal.FreeBSTR( ps );
					return ( skey );
				}
			}
			set
			{
				if ( m_session_key != null )
				{
					m_session_key.Clear();
				}
				else
				{
					m_session_key = new SecureString();
				}
				
				for ( int x = 0; x < value.Length; ++x )
					m_session_key.AppendChar( value[ x ] );
			}
		}

		/// <summary>
		///		Returns null to give this object a lifetime lease.
		/// </summary>
		/// <returns>Returns null to give this object a lifetime lease.</returns>
		public override object InitializeLifetimeService()
		{
			return ( null );
		}
	}
}
