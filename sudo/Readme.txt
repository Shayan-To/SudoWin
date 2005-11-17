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

\ConsoleApplication\bin\Release\Sudo.ConsoleApplication.exe
\ConsoleApplication\bin\Release\Sudo.ConsoleApplication.exe.config


INSTALL
-------

1) Create a local group called "Sudoers" to contain the group of users that will have Sudo privileges.  This group is defined in the service's application configuration file.

2) Create the path to the Windows service log file.  This path is defined in the service's application configuration file.

3) Create a directory for the following three files to reside in:

	Sudo.WindowsService.exe
	Sudo.WindowsService.exe.config
	Sudoers.xml
	
4) Copy the files to the directory you created.

5) Open a command prompt and change directories to the directory you created.

6) Execute the following command to install the Sudo service:

	%SystemRoot%\Microsoft.NET\Framework\v2.X.XXXXX\installutil.exe Sudo.WindowsService.exe
	
7) Execute the following command to open up the Services control panel:

	services.msc
	
8) Set the Sudo service to start automatically.
	
7) Copy the file "Sudo.ConsoleApplication.exe" to your %SystemRoot%\system32 directory.  Alternatively you could also create a new directory for this file, but if you do be sure to include the new directory in the System PATH variable so that it is executable from any location.  I also renamed the file to "sudo.exe" to coincide with its impetus program.

8) That's it!  You should be able to run any command through Sudo now.  Be sure to read about the Sudoers data source.
