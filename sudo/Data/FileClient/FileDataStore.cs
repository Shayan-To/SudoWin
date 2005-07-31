/*
Copyright (c) 2005, Schley Andrew Kutz <sakutz@gmail.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of Lost Creations nor the names of its contributors may
    be used to endorse or promote products derived from this software without
    specific prior written permission.

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
*/

using System;
using System.Xml;
using System.Data;
using System.Text;
using System.Xml.Schema;
using Sudo.PublicLibrary;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sudo.Data.FileClient
{
	/// <summary>
	///		Used to access sudoer information stored in a xml
	///		file that adheres to the FileDataStoreSchema.
	/// </summary>
	public class FileDataStore : IDataStore
	{
		/// <summary>
		///		FileDataStore boolean that allows
		///		bool to be null.
		/// </summary>
		private enum FdsBool : short
		{
			False = 0,
			True = 1,
			Null = 2,
		}

		/// <summary>
		///		Format for translating a value in an xpath
		///		query into all lowercase.
		/// </summary>
		const string XpathTranslateFormat = 
			"translate({0},'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')";

		/// <summary>
		///		For parsing the sudoers xml file.
		/// </summary>
		private XmlDocument m_xml_doc = new XmlDocument();

		/// <summary>
		///		For resolving namespaces in the sudoers file
		///		so that xpath queries will work.
		/// </summary>
		private XmlNamespaceManager m_namespace_mgr;

		/// <summary>
		///		Default constructor.
		/// </summary>
		public FileDataStore()
		{
		}

		/// <summary>
		///		Gets the xml node for the given user name.
		/// </summary>
		/// <param name="userName">
		///		The xpath query in this method uses this value
		///		to search for a xml node of type user with a name
		///		attribute value equal to the value of this parameter.
		/// </param>
		/// <returns>
		///		Xml node with the name attribute equal to
		///		the value of the userName parameter of this
		///		method.
		/// </returns>
		private XmlNode FindUserNode( string userName )
		{
			// find the user in the sudoers file.  to do this
			// we first build a xpath query which will look for
			// the user with the given user name
			string user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:user[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'" + userName + "'" ) );

			// find the user's node in the sudoers file.  if the 
			// user is not found then the query will return null 
			XmlNode user_node = m_xml_doc.SelectSingleNode(
				user_xpq, m_namespace_mgr );

			return ( user_node );
			
		}

		#region IDataStore Members

		/// <summary>
		///		Opens a connection to the sudoers data store
		///		and validate the data with the given
		///		schema file.
		/// </summary>
		/// <param name="connectionString">
		///		Connection string used to open a connection
		///		to the sudoers data store.
		/// </param>
		/// <param name="schemaFileUri">
		///		Uri of schema file to use to validate the data.
		/// </param>
		public void Open( string connectionString, Uri schemaFileUri )
		{
			// throw an exception if the sudoers file is not found
			if ( !System.IO.File.Exists( connectionString ) )
				throw new System.IO.FileNotFoundException(
					"sudoers file not found", connectionString );
			
			// create a xmlreadersettings object
			// to specify how to read in the sudoers file
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.CloseInput = true;
			xrs.IgnoreComments = true;

			// if a schema file uri was specified
			// then enable validation against it
			// when the sudoers file is read
			if ( schemaFileUri != null )
			{
				xrs.Schemas.Add( null, schemaFileUri.AbsoluteUri );
				xrs.ValidationType = ValidationType.Schema;
			}
			
			// read in the sudoers file
			XmlReader xr = XmlReader.Create( connectionString, xrs );

			// load the xml reader into the 
			// sudoers xml document.
			m_xml_doc.Load( xr );
		
			// close the xmlreader
			xr.Close();

			// create the namespace manager using the sudoers file name table
			m_namespace_mgr = new XmlNamespaceManager( m_xml_doc.NameTable );

			// if there is a default namespace specified in the
			// xml file then it needs to be added to the namespace
			// manager so the xpath queries will work
			Regex ns_rx = new Regex(
				@"xmlns\s{0,}=\s{0,}""([\w\d\.\:\/\\]{0,})""",
				RegexOptions.Multiline | RegexOptions.IgnoreCase );
			Match ns_m = ns_rx.Match( m_xml_doc.InnerXml );

			// if the default namespace declaration was found then
			// add the default namespace to the namespace manager
			if ( ns_m.Success )
			{
				string default_ns = ns_m.Groups[ 1 ].Value;
				// add the default namespace
				m_namespace_mgr.AddNamespace( "d", default_ns );
			}
		}

		/// <summary>
		///		Opens the sudoers xml file.
		/// </summary>
		/// <param name="connectionString">
		///		Fully qualified path to the sudoers xml file.
		/// </param>
		public void Open( string connectionString )
		{
			Open( connectionString, null );
		}
		
		/// <summary>
		///		Present for compliance with IDataStore.
		/// </summary>
		public void Close()
		{
			// do nothing
		}

		/// <summary>
		///		Gets a Sudo.PublicLibrary.UserInfo structure
		///		from the sudoers data store for the given user name.
		/// </summary>
		/// <param name="userName">
		///		User name to get information for.
		/// </param>
		/// <returns>
		///		Sudo.PublicLibrary.UserInfo structure for
		///		the given user name.
		/// </returns>
		public UserInfo GetUserInfo( string userName )
		{
			UserInfo ui = new UserInfo();

			// temp values
			int itv;
			string stv;
			LoggingLevelTypes llttv;

			// get the user node for this user
			XmlNode unode = FindUserNode( userName );

			if ( unode == null )
				return ( ui );

			GetUserAttributeValue( unode, true, "invalidLogons", out itv );
			ui.InvalidLogons = itv;

			GetUserAttributeValue( unode, true, "timesExceededInvalidLogons", out itv );
			ui.TimesExceededInvalidLogons = itv;

			GetUserAttributeValue( unode, true, "invalidLogonTimeout", out itv );
			ui.InvalidLogonTimeout = itv;

			GetUserAttributeValue( unode, true, "lockoutTimeout", out itv );
			ui.LockoutTimeout = itv;

			GetUserAttributeValue( unode, true, "logonTimeout", out itv );
			ui.LogonTimeout = itv;

			GetUserAttributeValue( unode, true, "privilegesGroup", out stv );
			ui.PrivilegesGroup = stv;

			GetUserAttributeValue( unode, true, "loggingLevel", out llttv );
			ui.LoggingLevel = llttv;
			
			return ( ui );
		}

		/// <summary>
		///		Gets a Sudo.PublicLibrary.CommandInfo structure
		///		from the sudoers data store for the given user name,
		///		command path, and command arguments.
		/// </summary>
		/// <param name="username">
		///		User name to get information for.
		/// </param>
		/// <param name="commandPath">
		///		Command path to get information for.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments to get information for.
		/// </param>
		/// <returns>
		///		Sudo.PublicLibrary.CommandInfo structure for
		///		the given user name, command path, and command 
		///		arguments.
		/// </returns>
		public CommandInfo GetCommandInfo(
			string username,
			string commandPath,
			string commandArguments )
		{
			CommandInfo ci = new CommandInfo();

			// find the user node
			XmlNode u_node = FindUserNode( username );

			if ( u_node == null )
				return ( ci );

			// find the command node
			XmlNode c_node = FindCommandNode( u_node, commandPath, commandArguments );

			if ( c_node == null )
				return ( ci );

			ci.IsCommandAllowed = IsCommandAllowed( u_node, c_node, commandArguments );

			LoggingLevelTypes llttv;
			GetCommandAttributeValue( u_node, true, c_node, "loggingLevel", out llttv );

			return ( ci );
		}

		/// <summary>
		///		Checks to see if the user has the right
		///		to execute the given command with sudo.
		/// </summary>
		/// <param name="userNode">
		///		User node that represents the user that
		///		invoked sudo.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		private XmlNode FindCommandNode(
			XmlNode userNode,
			string commandPath,
			string commandArguments )
		{
			/*
			 * if the user is not disabled we need to discover whether
			 * or not the command they are trying to sudo is allowed.
			 * to do this we must look in 4 potential locations in
			 * the following order
			 * 
			 * 1) commands node local to user
			 * 2) commandGroupRefs node local to user
			 * 3) commands node local to user's parent user group
			 * 4) commandGroupRefs node local to user's parent user group
			 * 
			 * ex. if the command is found in step 1 its allowances
			 * will be decided in step 1 and the result will be returned.
			 * the check only falls to the next step if the command is not
			 * found in a previous step.  in other words, if we reach
			 * step 2 then it means we did not find the command in step 1.
			 * 
			 * define a xml node that will point to the
			 * node that represents the command the user
			 * is attempting to use sudo to execute
			 */
			XmlNode cmd_node = null;

			// 1) commands node local to user
			XmlNode local_cmds = userNode.SelectSingleNode(
				"d:commands", m_namespace_mgr );
			if ( local_cmds != null && local_cmds.HasChildNodes )
				cmd_node = FindCommandNode( local_cmds, commandPath );
			
			// 2) commandGroupRefs node local to user
			if ( cmd_node == null )
			{
				XmlNode local_cmd_refs = userNode.SelectSingleNode(
					"d:commandGroupRefs", m_namespace_mgr );
				if ( local_cmd_refs != null && local_cmd_refs.HasChildNodes )
				{
					cmd_node = FindCommandNode( userNode,
						local_cmd_refs, commandPath, commandArguments );
				}
			}
			
			// 3) commands node local to user's parent user group
			if ( cmd_node == null )
			{
				XmlNode parent_cmds = userNode.ParentNode.ParentNode.SelectSingleNode(
					"d:commands", m_namespace_mgr );

				if ( parent_cmds != null && parent_cmds.HasChildNodes )
					cmd_node = FindCommandNode( parent_cmds, commandPath );
			}

			// 4) commandGroupRefs node local to user's parent user group
			if ( cmd_node == null )
			{
				XmlNode parent_cmd_refs = userNode.ParentNode.ParentNode.SelectSingleNode(
					"d:commandGroupRefs", m_namespace_mgr );
				if ( parent_cmd_refs != null && parent_cmd_refs.HasChildNodes )
				{
					cmd_node = FindCommandNode( userNode,
							parent_cmd_refs, commandPath, commandArguments );
				}
			}
			return ( cmd_node );
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		///		Present for compliance with IDataStore.
		/// </summary>
		public void Dispose()
		{
			// do nothing
		}

		#endregion

		/// <summary>
		///		Examines the attributes of a command
		///		node and determines whether or not
		///		the user is allowed to execute the
		///		given command with sudo.
		/// </summary>
		/// <param name="userNode">
		///		User node that represents the user that
		///		invoked sudo.
		/// </param>
		/// <param name="commandNode">
		///		Command node that represents the command
		///		the user is attempting to execute with sudo.
		///	</param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		private bool IsCommandAllowed(
			XmlNode userNode,
			XmlNode commandNode,
			string commandArguments )
		{
			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// is the user enabled?
			FdsBool user_enabled;
			GetUserAttributeValue(
				userNode, false, "enabled", out user_enabled );

			if ( user_enabled == FdsBool.False )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// are all commands allowed?
			FdsBool all_cmds_allwd;
			GetUserAttributeValue(
				userNode, false, "allowAllCommands", out all_cmds_allwd );

			if ( all_cmds_allwd == FdsBool.True )
				return ( true );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// is the command enabled?
			//
			// pass a null value for the userNode parameter so that
			// the we won't look past the command node's immediate
			// parent for this attribute value
			FdsBool cmd_enabled;
			GetCommandAttributeValue(
				null, false, commandNode, "enabled", out cmd_enabled );
			if ( cmd_enabled == FdsBool.False )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// if the command is enabled then check to see if
			// it is being executed within a valid timeframe
			DateTime cmd_st, cmd_et;
			GetCommandAttributeValue(
				userNode, true, commandNode, "startTime", out cmd_st );
			GetCommandAttributeValue(
				userNode, true, commandNode, "endTime", out cmd_et );

			DateTime now = DateTime.Now;
			if ( !( now.TimeOfDay >= cmd_st.TimeOfDay &&
				now.TimeOfDay <= cmd_et.TimeOfDay ) )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// well, if we made it this far it means the user is
			// allowed to execute the command
			return ( true );
		}

		/// <summary>
		///		Searches for a command group node that has
		///		a name attribute equal to the name of
		///		the command group reference name being
		///		searches for.
		/// </summary>
		/// <param name="commandGroupRefsParent">
		///		Node that has commandGroupRef nodes as children.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <param name="wasCommandFound">
		///		Whether or not the command groups that the
		///		reference pointed to contained the command
		///		node that represents the command path being
		///		searched for.
		/// </param>
		/// <returns>
		///		The return value of this method is only
		///		significant if the out parameter wasCommandFound
		///		is set to true.
		/// 
		///		True if the command is allowed, false if it is not.
		/// </returns>
		private XmlNode FindCommandNode(
			XmlNode userNode,
			XmlNode commandGroupRefsParent,
			string commandPath,
			string commandArguments )
		{
			XmlNode cmd_node = null;

			// for each command group reference build a xpath
			// query and then get the command group
			foreach ( XmlNode cmd_ref in commandGroupRefsParent.ChildNodes )
			{
				string cmdgrp_xpq = string.Format(
					CultureInfo.CurrentCulture,
					@"//d:commandGroup[{0} = {1}]",
					string.Format( CultureInfo.CurrentCulture,
						XpathTranslateFormat, "@name" ),
					string.Format( CultureInfo.CurrentCulture,
						XpathTranslateFormat,
						"'" + cmd_ref.Attributes[ "commandGroupName" ].Value + "'" ) );

				// search for the command group
				XmlNode cmdgrp = m_xml_doc.SelectSingleNode(
					cmdgrp_xpq, m_namespace_mgr );

				// if the command group is found then search
				// the command group for the command
				if ( cmdgrp != null && cmdgrp.HasChildNodes )
					cmd_node = FindCommandNode( cmdgrp, commandPath );

				// if the command is found determine if it is
				// allowed to be executed
				if ( cmd_node != null )
					break;
			}

			return ( cmd_node );
		}

		/// <summary>
		///		Searches for a command node that has
		///		a path attribute equal to the path of
		///		of the command that the user is attempting
		///		to execute with sudo.
		/// </summary>
		/// <param name="commandNodeParent">
		///		Node that has command nodes as children.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <returns></returns>
		private XmlNode FindCommandNode(
			XmlNode commandNodeParent,
			string commandPath )
		{
			// build the query used to look for the command node in
			// the current node context with the given command path
			string xpath_command_query = string.Format(
				CultureInfo.CurrentCulture,
				@"d:command[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@path" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'" + commandPath + "'" ) );

			// look for the command node
			XmlNode cmd_node = commandNodeParent.SelectSingleNode(
				xpath_command_query, m_namespace_mgr );

			return ( cmd_node );
		}

		#region GetUserAttributeValue +4

		/// <summary>
		///		Gets the value of a user attribute.
		/// </summary>
		/// <param name="userNode">
		///		User node to get the attribute value from.
		/// </param>
		/// <param name="checkDefaults">
		///		Whether or not to look at the default settings
		///		for this attribute if it cannot be found at
		///		a lower level.
		/// </param>
		/// <param name="attributeName">
		///		Name of the attribute value to get.
		/// </param>
		/// <param name="attributeValue">
		///		Value of the of the attribute.
		/// </param>
		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out string attributeValue )
		{
			// is this setting defined on the actual user node?
			if ( userNode.Attributes[ attributeName ] != null )
			{
				attributeValue = userNode.Attributes[ attributeName ].Value;
			}
			// is this setting defined on the userGroup node
			// that this user belongs to?
			else if ( userNode.ParentNode.ParentNode.Attributes[ attributeName ] != null )
			{
				attributeValue = 
					userNode.ParentNode.ParentNode.Attributes[ attributeName ].Value;
			}
			// go to the default settings
			else if ( checkDefaults )
			{
				attributeValue =
					userNode.OwnerDocument.DocumentElement.Attributes[ attributeName ].Value;
			}
			else
				attributeValue = string.Empty;
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out FdsBool attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				FdsBool.Null :
				( FdsBool ) Enum.Parse( typeof( FdsBool ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out LoggingLevelTypes attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				0 :
				( LoggingLevelTypes ) Enum.Parse( typeof( LoggingLevelTypes ), temp_value, true );

		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out int attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				-1 :
				int.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out DateTime attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				DateTime.MinValue :
				DateTime.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		#endregion

		#region GetCommandAttributeValue +4

		/// <summary>
		///		Gets the value of a command attribute.
		/// </summary>
		/// <param name="attributeName">
		///		Name of the attribute value to get.
		/// </param>
		/// <param name="userNode">
		///		User node that the command node is physically
		///		under or logically under by way of a command
		///		group reference.
		/// </param>
		/// <param name="commandNode">
		///		Command node to get the attribute value from.
		/// </param>
		/// <param name="attributeValue">
		///		Value of the of the attribute.
		/// </param>
		/// <remarks>
		///		Only use this method to get attribute values that
		///		are also set at the default level since this method
		///		will travel up to the default level to look for
		///		the attribute value if it cannot find it at a lower
		///		level.
		/// </remarks>
		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out string attributeValue )
		{
			// is this setting defined on the actual command node?
			if ( commandNode.Attributes[ attributeName ] != null )
			{
				attributeValue = commandNode.Attributes[ attributeName ].Value;
			}
			// is this setting defined on the commandGroup node
			// that this command belongs to?
			else if ( commandNode.ParentNode.Attributes[ attributeName ] != null )
			{
				attributeValue = commandNode.ParentNode.Attributes[ attributeName ].Value;
			}
			// look at the user and then up from there for the attribute value
			else if ( userNode != null )
			{
				GetUserAttributeValue(
					userNode, checkDefaults, attributeName, out attributeValue );
			}
			else
			{
				attributeValue = string.Empty;
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out FdsBool attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				FdsBool.Null :
				( FdsBool ) Enum.Parse( typeof( FdsBool ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out LoggingLevelTypes attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue(
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				0 :
				( LoggingLevelTypes ) Enum.Parse( typeof( LoggingLevelTypes ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out int attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				-1 :
				int.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out DateTime attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				DateTime.MinValue :
				DateTime.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		#endregion
	}
}
