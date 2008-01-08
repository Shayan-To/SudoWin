using System;
using System.Runtime.InteropServices;
using System.DirectoryServices;

namespace Sudowin.Common
{
    public class DirectoryFinder
    {

        private DirectoryFinder()
        {
        }

        /// <summary>
        /// Exception safe replacement for DirectoryEntries.Find. If will catch the COMException
        /// and return null if entry not found
        /// </summary>
        /// <param name="entries">Directory collection to search</param>
        /// <param name="name">Name of item to search for</param>
        /// <returns>Entry if found, or null if not found</returns>
        public static DirectoryEntry Find(DirectoryEntries entries, string name)
        {
            return DirectoryFinder.Find(entries, name, null);
        }

        /// <summary>
        /// Exception safe replacement for DirectoryEntries.Find. If will catch the COMException
        /// and return null if entry not found
        /// </summary>
        /// <param name="entries">Directory collection to search</param>
        /// <param name="name">Name of item to search for</param>
        /// <param name="schemaClassName">Name of schema class</param>
        /// <returns>Entry if found, or null if not found</returns>
        public static DirectoryEntry Find(DirectoryEntries entries, string name, string schemaClassName)
        {
            try
            {
                DirectoryEntry entry = entries.Find(name, schemaClassName);
                return entry;
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode == unchecked((int)0x800708AD) /*The user name could not be found.*/
                 || ex.ErrorCode == unchecked((int)0x80005004) /*An unknown directory object was requested.*/ )
                {
                    return null;
                }
            }
            return null;
        }

    }
}
