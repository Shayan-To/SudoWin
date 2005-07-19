using System;
using System.Collections.Generic;
using System.Text;

namespace Sudo.Service
{
	internal struct PROCESS_INFORMATION
	{
		IntPtr hProcess;
		IntPtr hThread;
		int dwProcessId;
		int dwThreadId;
	}

	[StructLayout( LayoutKind.Sequential )]
	internal struct SECURITY_ATTRIBUTES
	{
		public int nLength;
		public IntPtr lpSecurityDescriptor;
		public bool bInheritHandle;
	}

	[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
	internal struct STARTUPINFO
	{
		Int32 cb;
		string lpReserved;
		string lpDesktop;
		string lpTitle;
		Int32 dwX;
		Int32 dwY;
		Int32 dwXSize;
		Int32 dwYSize;
		Int32 dwXCountChars;
		Int32 dwYCountChars;
		Int32 dwFillAttribute;
		Int32 dwFlags;
		Int16 wShowWindow;
		Int16 cbReserved2;
		Int32 lpReserved2;
		Int32 hStdInput;
		Int32 hStdOutput;
		Int32 hStdError;
	}

	internal enum LOGON_TYPE
	{
		LOGON32_LOGON_INTERACTIVE = 2,
		LOGON32_LOGON_NETWORK,
		LOGON32_LOGON_BATCH,
		LOGON32_LOGON_SERVICE,
		LOGON32_LOGON_UNLOCK = 7,
		LOGON32_LOGON_NETWORK_CLEARTEXT,
		LOGON32_LOGON_NEW_CREDENTIALS
	}

	internal enum LOGON_PROVIDER
	{
		LOGON32_PROVIDER_DEFAULT,
		LOGON32_PROVIDER_WINNT35,
		LOGON32_PROVIDER_WINNT40,
		LOGON32_PROVIDER_WINNT50
	}

	internal class Win32
	{
		[DllImport( "advapi32.dll", SetLastError = true )]
		public static extern bool LogonUser(
			string lpszUsername,
			string lpszDomain,
			string lpszPassword,
			int dwLogonType,
			int dwLogonProvider,
			out IntPtr phToken
			);

		[DllImport( "advapi32.dll", SetLastError = true, CharSet = CharSet.Auto )]
		static extern bool CreateProcessAsUser(
			IntPtr hToken,
			string lpApplicationName,
			[In] StringBuilder lpCommandLine,
			ref SECURITY_ATTRIBUTES lpProcessAttributes,
			ref SECURITY_ATTRIBUTES lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation );
	}
}
