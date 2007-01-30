@ECHO OFF

REM
REM get ready for release
REM
REM usage: grfr.bat VERSION REVISION
REM
REM description: this script exports a copy of
REM   sudowin's source code, zips it up, copies
REM   and renames the setup files, zips them,
REM   and finally ftp's them to sourceforge
REM

set /p VERSION=ver: 
set /p REVISION=rev: 

REM
REM do not proceed if arguments are missing
REM
IF NOT DEFINED VERSION GOTO REQUIRED_ARGS_MISSING
IF NOT DEFINED REVISION GOTO REQUIRED_ARGS_MISSING

:BEGIN

set RELEASESDIR=r:\projects\sudowin\releases
set EXPORTDIR=r:\projects\sudowin\releases\sudowin
set TRUNKDIR=r:\projects\sudowin\trunk
set UPLOAD=1
set DELETE_WHEN_DONE=1

REM
REM delete the old export directory if it exists
REM
IF EXIST %EXPORTDIR% (
	rmdir /Q /S  %EXPORTDIR%
)

REM
REM export a copy of the source code
REM
svn export -r %REVISION% %TRUNKDIR%\sudowin\ %EXPORTDIR%

REM
REM zip the source code up
REM
7z a -tzip -mx=9 -mm=Deflate -md=32k %RELEASESDIR%\sudowin-src-%VERSION%-r%REVISION%.zip %EXPORTDIR%

REM
REM remove the exported copy of the source code
REM
rmdir /Q /S %EXPORTDIR%

REM
REM copy the setup files into the release dir
REM
copy %TRUNKDIR%\sudowin\setup\release\* %RELEASESDIR%\

REM
REM make a copy of the setup.msi file
REM
copy %RELEASESDIR%\setup.msi %RELEASESDIR%\sudowin-bin-%VERSION%-r%REVISION%.msi

REM
REM renamed the setup.exe file
REM
move %RELEASESDIR%\setup.exe %RELEASESDIR%\sudowin-bin-%VERSION%-r%REVISION%.exe

REM
REM zip up sudowin-bin-%VERSION%-r%RELEASE%.exe and setup.msi
REM
7z a -tzip -mx=9 -mm=Deflate -md=32k %RELEASESDIR%\sudowin-bin-%VERSION%-r%REVISION%.exe.zip %RELEASESDIR%\sudowin-bin-%VERSION%-r%REVISION%.exe %RELEASESDIR%\setup.msi

REM
REM remove sudowin-bin-%VERSION%-r%RELEASE%.exe and setup.msi
REM
del /Q %RELEASESDIR%\sudowin-bin-%VERSION%-r%REVISION%.exe 
del /Q %RELEASESDIR%\setup.msi

REM
REM only upload the files if the UPLOAD variable is set to 1
REM
IF %UPLOAD% EQU 1 (
	ftp -s:sf_ftp_cmds.txt upload.sourceforge.net
)

:UPLOAD

GOTO END

:REQUIRED_ARGS_MISSING

ECHO usage: grfr.bat VERSION REVISION
GOTO END

:END

IF %DELETE_WHEN_DONE% EQU 1 (
	del /Q %RELEASESDIR%\*
)