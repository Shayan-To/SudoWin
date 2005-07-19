using System;
using System.Text;
using Sudo.Shared;
using System.Security;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Sudo.Shared
{
	public class CommonMethods
	{
		/// <summary>
		///		Verifies a strong name signature.
		/// </summary>
		/// <param name="assemblyPath">
		///		Fully qualified path of the assembly
		///		to be verified.
		/// </param>
		/// <param name="forceVerification">
		///		True to bypass the skip verification list.
		///		False will cause wasVerified to be set.
		/// </param>
		/// <param name="wasVerified">
		///		True if forceVerification is false and
		///		the assembly was verified.  False if
		///		forceVerification is true and the assembly
		///		was on the skip verification list.
		/// </param>
		/// <returns>
		///		True if verified, false if otherwise.
		/// </returns>
		[DllImport( "mscoree.dll", CharSet = CharSet.Unicode )]
		public static extern bool StrongNameSignatureVerificationEx(
			string assemblyPath, 
			bool forceVerification, 
			ref bool wasVerified );

		/// <summary>
		///		Static class.  Do not instantiate.
		/// </summary>
		private CommonMethods()
		{
		}

		static public SecureString GetSecureString( string plain )
		{
			SecureString ss = new SecureString();
			for ( int x = 0; x < plain.Length; ++x )
				ss.AppendChar( plain[ x ] );
			return ( ss );
		}

		/// <summary>
		///		Get an string value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be be the key value.  Empty string
		///		if not found.
		///	</param>
		static public void GetConfigValue(
			string keyName,
			out string keyValue )
		{
			// if the key is defined in the config file 
			// then get it else return an empty string
			if ( Array.IndexOf( ConfigurationManager.AppSettings.AllKeys,
				keyName ) > -1 )
				keyValue = ConfigurationManager.AppSettings[ keyName ];
			// empty string
			else
				keyValue = string.Empty;
		}

		/// <summary>
		///		Get an integer value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed integer.
		///		-1 if not found or cannot parse.
		///	</param>
		static public void GetConfigValue(
			string keyName,
			out int keyValue )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				keyValue = 0;
			else
			{
				if ( !int.TryParse( temp, out keyValue ) )
					keyValue = -1;
			}
		}

		/// <summary>
		///		Get an InteractionMethodType value from 
		///		the host process's config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed InteractionMethodType.
		///		0 if not found or cannot parse.
		///	</param>
		static public void GetConfigValue(
			string keyName,
			out UIModeTypes interactionMethod )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				interactionMethod = 0;
			else
			{
				try
				{
					interactionMethod = ( UIModeTypes )
						Enum.Parse( typeof( UIModeTypes ),
						temp, true );
				}
				catch
				{
					interactionMethod = 0;
				}
			}
		}
	}
}
