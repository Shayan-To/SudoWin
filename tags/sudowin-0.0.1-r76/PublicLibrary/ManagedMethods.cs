using System;
using System.Text;
using System.Security;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;

namespace Sudowin.PublicLibrary
{
	/// <summary>
	///		Provides managed methods common to this .NET solution.
	/// </summary>
	public class ManagedMethods
	{
		/// <summary>
		///		Static class.  Do not instantiate.
		/// </summary>
		[DebuggerHidden]
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
		[DebuggerHidden]
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
		[DebuggerHidden]
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

		[DebuggerHidden]
		[Conditional( "TRACE" )]
		static public void TraceWrite(
			string userName,
			string messageFormat,
			params string[] args )
		{
			// get the first method that does not have
			// the same declaring type of the declaring type
			// of this method
			MethodBase mb_this = MethodBase.GetCurrentMethod();
			Type t_this = mb_this.DeclaringType;
			int sf_offset = 1;
			MethodBase mb_caller;
			StackFrame sf;
			do
			{
				sf = new StackFrame( sf_offset );
				mb_caller = sf.GetMethod();
				++sf_offset;
			}
			while ( mb_caller.DeclaringType == mb_this.DeclaringType );

			string msg = string.Format( CultureInfo.CurrentCulture,
				"{0:yyyy/MM/dd HH:mm:ss},{1},{2}.{3},\"{4}\"",
				DateTime.Now,
				userName,
				mb_caller.DeclaringType,
				mb_caller.Name,
				string.Format( CultureInfo.CurrentCulture,
					messageFormat, args ) );

			Trace.WriteLine( msg );
		}
	}
}
