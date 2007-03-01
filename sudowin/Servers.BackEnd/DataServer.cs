using System;
using System.Text;
using System.Security;

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
