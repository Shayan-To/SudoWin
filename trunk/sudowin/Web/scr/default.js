var m_width = 700;

function writeTopPart()
{
	document.write(
		'<div id="divSudoForWindowsLogo">' +
			'Sudo for Windows' +
		'</div>' +
		'<div id="divSourceForgeImage">' +
			'<a href="http://sourceforge.net"><img src="http://sflogo.sourceforge.net/sflogo.php?group_id=143653&amp;type=1" style="width: 88px; height: 31px; border: none;" alt="SourceForge.net Logo" /></a>' +
		'</div>' +
		'<div id="divSudoForWindowsSubLogo">' +
			'<a href="http://sourceforge.net/projects/sudowin">project sudowin</a> - ' +
			'<a href="http://www.opensource.org/licenses/bsd-license.php">new bsd license</a>' +
		'</div>' +
		
		'<hr style="clear:both;"/>' +
		
		'<div id="divLinksHeader">' +
			'<a href="index.html">intro</a> . ' +
			'<a href="how.html">how</a> . ' +
			'<a href="plugins.html">plugins</a> . ' +
			'<a href="http://sourceforge.net/pm/task.php?group_project_id=48101&group_id=143653&func=browse">todo</a> . ' +
			'<a href="who.html">who</a>' +
		'</div>' +
		
		'<div id="divDownload">' +
			'-~= download <a href="http://sourceforge.net/project/showfiles.php?group_id=143653&package_id=157780&release_id=426244">0.1.0-r76</a> =~-' +
		'</div>' );
}

function writeDownloadLink()
{
	document.write( '-~= download <a href="http://sourceforge.net/project/showfiles.php?group_id=143653&package_id=157780&release_id=426244">0.1.0-r76</a> =~-' );
}

function writeLinksHeader()
{
	document.write( '<a href="index.html">intro</a> . ' +
		'<a href="how.html">how</a> . ' +
		'<a href="plugins.html">plugins</a> . ' +
		'<a href="http://sourceforge.net/pm/task.php?group_project_id=48101&group_id=143653&func=browse">todo</a> . ' +
		'<a href="who.html">who</a>' );
}

function onPageLoad()
{
	// increase the width of the main div for ie clients
	if ( navigator.userAgent.toLowerCase().indexOf( "msie" ) > -1 )
	{
		m_width = 740;
		document.getElementById( "divBody" ).style.width = m_width + 'px';
	}
	
	window.onresize = onPageResize;
	
	onPageResize();
}

function onPageResize()
{
	// center main div

	// get a reference to the body div
	var o_div_body = document.getElementById( "divBody" );
	
	// hide the outer div wihle it is being set
	o_div_body.style.display = 'none';
	
	// get the width of the window
	var b_is_ie = navigator.appName.indexOf( "Microsoft" ) != -1;
	
	var int_window_width = b_is_ie ? document.body.offsetWidth : window.innerWidth;
	
	// figure out how to position the outer div
	var int_left = int_window_width < m_width ? 0 : ( ( ( int_window_width - m_width ) / 2 ) - ( b_is_ie ? 20 : 25 ) );

	// position the div in the middle of the page
	o_div_body.style.left = int_left + 'px';
	
	// show the outer div again
	o_div_body.style.display = 'block';
}