Copyright (c) 2005, 2006, 2007, Schley Andrew Kutz <akutz@lostcreations.com>
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

UPGRADING SUDO FOR WINDOWS
--------------------------

0.1.1-r95 to 0.2.0-r

	- Make a copy of your sudoers.xml file.  This is located at INSTALLDIR\
	Servers\sudoers.xml
	
	- Use "Add/Remove Programs" or the original installer to remove the
	0.1.1-r95 version of Sudo for Windows.  Be sure to choose "No" when
	the uninstaller asks if you would like to remove the old Sudoers group.
	
	- Install Sudo for Windows 0.2.0-r
	
	- Copy your old sudoers.xml file back to INSTALLDIR\Server\sudoers.xml.
	
	- Restart the Sudowin service.

0.1.0-r76 to 0.1.1-r95
	
	- Make a copy of your sudoers.xml file.  This is located at INSTALLDIR\
	Server\sudoers.xml.
	
	- Use "Add/Remove Programs" or the original installer to remove the 
	0.1.0-r76 version of Sudo for Windows.
	
	- Install 0.1.1-r95.
	
	- Copy your old sudoers.xml file back to INSTALLDIR\Server\sudoers.xml.
	
	- Restart the Sudowin service.
	
	- Add the desired users back into the Sudoers group on the local computer.
	
	- Log out and back into your computer.
	
	- That's it!  I hope to make this easier in the future.  The installer 
	that comes with Visual Studio .NET 2005 sucks for doing upgrade deploymnents.


POST INSTALLATION STEPS
-----------------------
- You should edit the sudoers.xml file so that the users you want to have sudo
privileges are in the file.  This file is located at INSTALLDIR\Server\sudoers.xml.

- You need to add the users you want to be able to communicatie with the sudo 
server to the local user group "Sudoers".  These users will have log out and 
back into the computer before their new group membership will take effect.

QUESTIONS
---------

For any questions please visit http://sudowin.sourceforge.net