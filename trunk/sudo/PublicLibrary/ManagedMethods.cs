using System;
using System.Text;
using System.Security;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Sudo.PublicLibrary
{
	/// <summary>
	///		Provides managed methods common to this .NET solution.
	/// </summary>
	public class ManagedMethods
	{
		/// <summary>
		///		Static class.  Do not instantiate.
		/// </summary>
		private ManagedMethods()
		{
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
		///		Gets an UIModeTypes value from 
		///		the host process's config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed UIModeTypes.
		///		0 if not found or cannot parse.
		///	</param>
		static public void GetConfigValue(
			string keyName,
			out UIModeTypes uiModeTypesValue )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				uiModeTypesValue = 0;
			else
			{
				try
				{
					uiModeTypesValue = ( UIModeTypes )
						Enum.Parse( typeof( UIModeTypes ),
						temp, true );
				}
				catch
				{
					uiModeTypesValue = 0;
				}
			}
		}
	}
}
