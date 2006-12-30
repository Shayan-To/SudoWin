#!/bin/bash

cd "/cygdrive/r/projects/sudowin/trunk/sudowin/Web"

scp *.html shell.sourceforge.net:sudowin/htdocs/
scp css/* shell.sourceforge.net:sudowin/htdocs/css/
scp scr/* shell.sourceforge.net:sudowin/htdocs/scr/
scp img/* shell.sourceforge.net:sudowin/htdocs/img/
