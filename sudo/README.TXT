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

NOTES
-----
- The version number of the package is based on the version number of the Release version of Sudo.WindowsService.exe.

- Sudo for Windows currently only supports local users and groups.  Active Directory users and groups will be supported in a near-future release.

- The path to the Windows service log file must exist.

- User names are prefixed "%HOSTNAME%\" in the sudoers data source.

- Administrators do not have sudo privileges by default.  Administrators must be in the sudoers group and in the sudoers data source to have sudo privileges.

- You must be a member of the Administrators group to install Sudo for Windows


FILES INCLUDED
--------------
\Sudoers.xml

\WindowsService\bin\Release\Sudo.WindowsService.exe
\WindowsService\bin\Release\Sudo.WindowsService.exe.config
\WindowsService\bin\Release\WtsApi32.NET.dll
\WindowsService\bin\Release\Sudo.PublicLibrary.dll
\WindowsService\bin\Release\Sudo.AuthorizationModules.dll

\ConsoleApplication\bin\Release\Sudo.ConsoleApplication.exe
\ConsoleApplication\bin\Release\Sudo.ConsoleApplication.exe.config

\Data\FileClient\FileDataStoreSchema_v0.1.xsd


INSTALL
-------

1) Create a local group called "Sudoers" to contain the group of users that will have Sudo privileges.  This group is defined in the service's application configuration file.  Remember, even administrators don't have Sudo privileges by default, the user MUST be in the Sudoers group (or whatever you call it) to be able to invoke sudo.

2) Create the path to the Windows service log file.  This path is defined in the service's application configuration file.

3) Create a directory for the following six files to reside in:

	Sudo.WindowsService.exe
	Sudo.WindowsService.exe.config
	Sudoers.xml
	WtsApi32.NET.dll
	FileDataStoreSchema_v0.1.xsd
	Sudo.PublicLibrary.dll
	Sudo.AuthorizationModules.dll
	
4) Copy the files to the directory you created and register Sudo.PublicLibrary.dll in the GAC.

5) Open a command prompt and change directories to the directory you created.

6) Execute the following command to install the Sudo service:

	%SystemRoot%\Microsoft.NET\Framework\v2.X.XXXXX\installutil.exe Sudo.WindowsService.exe
	
7) Execute the following command to open up the Services control panel:

	services.msc
	
8) Set the Sudo service to start automatically.
	
7) Copy the files "Sudo.ConsoleApplication.exe" and "Sudo.ConsoleApplication.exe.config" to your %SystemRoot%\system32 directory.  Alternatively you could also create a new directory for these files, but if you do be sure to include the new directory in the System PATH variable so that it is executable from any location.  I also recommend renaming the files to "sudo.exe" and "sudo.exe.config" to coincide with its omage.

8) That's it!  You should be able to run any command through Sudo now.  Be sure to read about the Sudoers data source.
