namespace WSF.Browse
{
    using System;
    using System.Runtime.InteropServices;
    using WSF.Shell.Enums;
    using WSF.Shell.Interop.Interfaces.KnownFolders;
    using WSF.Shell.Interop.Knownfolders;
    using WSF.Shell.Pidl;

    /// <summary>
    /// Implements a simple PoCo type class that can be quickly enumerated
    /// and be used to view a large list of objects.
    /// 
    /// The large list of objects case can be handled by 
    /// </summary>
    public class DirectoryBrowserSlim
    {
        private static KnownFolderManagerClass knownFolderManager = new KnownFolderManagerClass();

        #region ctors
        /// <summary>
        /// Parameterized class constructor
        /// </summary>
        public DirectoryBrowserSlim(int parId,
                                    string parItemPath,

                                    string parParseName,
                                    string parName,
                                    string parLabelName,
                                    Shell.Pidl.IdList pidlFullList,
                                    Shell.Pidl.IdList apidlIdList)
            : this()
        {
            ID = parId;
            ItemPath = parItemPath;
            Name = parName;
            ParseName = parParseName;
            LabelName = parLabelName;

            ParentIdList = pidlFullList;
            ItemIdList = apidlIdList;
        }

        /// <summary>
        /// Hidden class constructor
        /// </summary>
        protected DirectoryBrowserSlim()
        {
        }
        #endregion ctors

        #region properties
        /// <summary>
        /// Gets an id for this item
        /// (the generator of the item should ensure uniqueness
        /// for the overall collection).
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets the path of the parent item.
        /// 
        /// Thats the path/special path id that was used to generate this item.
        /// </summary>
        public string ItemPath { get; }

        /// <summary>
        /// Gets the Name of this item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parse name of this item.
        /// </summary>
        public string ParseName { get; }

        /// <summary>
        /// Gets the label string (for usage in UI) of this item.
        /// </summary>
        public string LabelName { get; }

        /// <summary>
        /// Gets the IdList (PIDL) of the parent item.
        /// </summary>
        public IdList ParentIdList { get; }

        /// <summary>
        /// Gets the relative IdList (PIDL) of this item.
        /// </summary>
        public IdList ItemIdList { get; }

        /// <summary>
        /// Gets the combined IdList (PIDL) of the parent and this item.
        /// </summary>
        public IdList idListFullItem
        {
            get
            {
                return PidlManager.Combine(ParentIdList, ItemIdList);
            }
        }

        /// <summary>
        /// Contains the special path GUID if this item is a special shell item.
        /// 
        /// The ParseName refers to one of these:
        /// http://andif888.blogspot.com/2015/05/known-folder-ids-in-windows-10-path-and.html
        /// </summary>
        public string SpecialParseNameId
        {
            get
            {
                if (Browser.IsTypeOf(ParseName) == Enums.PathType.SpecialFolder)
                    return ParseName;

                return null;
            }
        }

        /// <summary>
        /// Contains the special path GUID if this item is a special shell space item.
        /// </summary>
        public string FileSystemPath
        {
            get
            {
                if (Browser.IsTypeOf(ParseName) == Enums.PathType.FileSystemPath)
                    return ParseName;

                return null;
            }
        }
        #endregion properties

        #region methods
        /// <summary>
        /// Gets the KnownfolderId of this object or null if it is not a knownfolder.
        /// </summary>
        /// <returns></returns>
        public string GetKnownFolderId()
        {
            // Resolve filesystem path (eg: 'C:\Windows')
            if (Browser.IsTypeOf(ParseName) == Enums.PathType.FileSystemPath)
            {
                var dir = Browser.FindKnownFolderByFileSystemPath(ParseName);
                if (dir != null)
                    return dir.SpecialPathId;

                return null;
            }

            // Resolve special path id (eg: '::{21EC2020-3AEA-1069-A2DD-08002B30309D}')
            IntPtr fullpidl = default(IntPtr);
            try
            {
                IKnownFolderNative iknownFolder;

                try
                {
                    knownFolderManager.FindFolderFromPath(ParseName, 0, out iknownFolder);
                    if (iknownFolder != null)
                    {
                        string ret = string.Format("{0}{1}{2}", "::{", iknownFolder.GetId(), "}");
                        return ret;
                    }
                }
                catch
                {
                    // throws exception if we are evaluating a device status = 'not ready'
                    // (eg.: CD Drive without CD inserted)
                }

                fullpidl = PidlManager.IdListToPidl(idListFullItem);
                HRESULT hr = knownFolderManager.FindFolderFromIDList(fullpidl, out iknownFolder);

                if (hr == HRESULT.S_OK)
                {
                    string ret = string.Format("{0}{1}{2}", "::{", iknownFolder.GetId(), "}");
                    return ret;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Marshal.FreeCoTaskMem(fullpidl);
            }
        }
        #endregion methods
    }
}
