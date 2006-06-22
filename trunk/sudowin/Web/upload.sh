#!/bin/bash

cd "/cygdrive/c/Documents and Settings/akutz/My Documents/Visual Studio 2005/Projects/sudowin/trunk/sudowin/Web"

scp *.html shell.sourceforge.net:sudowin/htdocs/
scp css/* shell.sourceforge.net:sudowin/htdocs/css/
scp scr/* shell.sourceforge.net:sudowin/htdocs/scr/
scp img/* shell.sourceforge.net:sudowin/htdocs/img/
