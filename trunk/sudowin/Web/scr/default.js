var m_width = 700;

function writeDownloadLink()
{
	document.write( '-~= download <a href="http://sourceforge.net/project/showfiles.php?group_id=143653&package_id=157780&release_id=426244">0.1.0-r76</a> =~-' );
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