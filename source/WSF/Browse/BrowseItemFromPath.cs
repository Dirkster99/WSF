namespace WSF.Browse
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using WSF.Enums;
    using WSF.IDs;
    using WSF.Shell.Enums;
    using WSF.Shell.Interop.Dlls;
    using WSF.Shell.Interop.Interfaces.Knownfolders;
    using WSF.Shell.Interop.Interfaces.KnownFolders;
    using WSF.Shell.Interop.Interfaces.ShellFolders;
    using WSF.Shell.Interop.Knownfolders;
    using WSF.Shell.Interop.ResourceIds;
    using WSF.Shell.Interop.ShellFolders;
    using WSF.Shell.Pidl;

    internal class BrowseItemFromPath
    {
        #region fields
////        private bool _IconResourceIdInitialized;
////        private string _IconResourceId;
////
////        private bool _KnownFolderIsInitialized;
////        private bool _ItemTypeIsInitialized;
        #endregion fields

        #region ctors
        /// <summary>
        /// constructor to initialize only <see cref="Path_RAW"/> and
        /// <see cref="PathFileSystem"/> property or
        /// <see cref="PathSpecialItemId"/> property. 
        /// <paramref name="rawPath"/> can be either a real path
        /// or special folder path starting with '::{...}'
        /// </summary>
        /// <param name="rawPath"></param>
        /// <param name="parsingName"></param>
////        /// <param name="itemType"></param>
        protected BrowseItemFromPath(string rawPath,
                                      string parsingName
////                                      ,DirectoryItemFlags itemType = DirectoryItemFlags.Unknown
            )
        {
            Path_RAW = rawPath;

            var pathType = ShellHelpers.IsSpecialPath(parsingName);

            if (pathType == ShellHelpers.SpecialPath.None)
                PathFileSystem = parsingName;
            else
            {
                if (pathType == ShellHelpers.SpecialPath.IsSpecialPath ||
                    pathType == ShellHelpers.SpecialPath.ContainsSpecialPath)
                {
                    IsSpecialParseItem = true;
                    PathSpecialItemId = parsingName;
                }
            }

////            if (itemType != DirectoryItemFlags.Unknown)
////            {
////                ItemType = itemType;
////                _ItemTypeIsInitialized = true;
////            }
        }

        /// <summary>
        /// Parameterized class constructor
        /// </summary>
        /// <param name="pathRaw"></param>
        /// <param name="parseName"></param>
        /// <param name="name"></param>
        /// <param name="labelName"></param>
        /// <param name="specialPathId"></param>
        /// <param name="normPath"></param>
////        /// <param name="parentIdList"></param>
////        /// <param name="relativeChildIdList"></param>
        protected BrowseItemFromPath(string pathRaw,
                                      string parseName,
                                      string name,
                                      string labelName,
                                      string specialPathId,
                                      string normPath
////                                      , IdList parentIdList,
////                                      IdList relativeChildIdList
            )
        {
            if (string.IsNullOrEmpty(specialPathId) == false)
            {
                IsSpecialParseItem = true;
                PathSpecialItemId = specialPathId;
            }

            Name = name;
            ParseName = parseName;
            LabelName = labelName;
            PathFileSystem = normPath;

////            ParentIdList = parentIdList;
////            ChildIdList = relativeChildIdList;

            Path_RAW = pathRaw;
        }

        /// <summary>
        /// Hidden standard constructor
        /// </summary>
        protected BrowseItemFromPath()
        {
        }
        #endregion ctors

        #region properties
        /// <summary>
        /// The name of a folder that should be used for display
        /// (e.g. 'Windows' for 'c:\Windows' or Documents for '::{...}')
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the parse name that was returned from the IShellFolder2 interface.
        /// </summary>
        public string ParseName { get; }

        /// <summary>
        /// Gets a label string that may differ from other naming
        /// strings if the item (eg Drive) supports labeling.
        /// 
        /// String is suitable for display only and should
        /// not be used as index as it is not unique.
        /// </summary>
        public string LabelName { get; }

        /// <summary>
        /// Contains the special path (Guid) Id for a special known folder
        /// or null if this item is not special and cannot be identified
        /// through a seperate GUID.
        /// </summary>
        public string PathSpecialItemId { get; }

        /// <summary>
        /// Indicates whether the parse name is available with a special path reference,
        /// such as, '::{...}'. This type of reference indicates a knownfolder reference
        /// that should be available in <see cref="PathSpecialItemId"/> if this property
        /// returns true.
        /// </summary>
        public bool IsSpecialParseItem { get; }

        /// <summary>
        /// Contains the file system path of this item or null if this
        /// item has no file system representation.
        /// </summary>
        public string PathFileSystem { get; }

////        /// <summary>
////        /// Gets the IdList (if available) that describes the parent
////        /// shell item of this item. This property can be null if this
////        /// shell item does not have a parent (is the Desktop).
////        /// </summary>
////        public IdList ParentIdList { get; }
////
////        /// <summary>
////        /// Gets the IdList (if available) that describes this item
////        /// underneath the parent item.
////        /// 
////        /// This property cannot be null. The <see cref="ParentIdList"/>
////        /// and <see cref="ChildIdList"/> must be processed together
////        /// since this item is otherwise no fully described just using
////        /// one of either properties.
////        /// </summary>
////        public IdList ChildIdList { get; }

////        /// <summary>
////        /// Gets an optional pointer to the default icon resource used when the folder is created.
////        /// This is a null-terminated Unicode string in this form:
////        ///
////        /// Module name, Resource ID
////        /// or null is this information is not available.
////        /// </summary>
////        public string IconResourceId
////        {
////            get
////            {
////                if (_IconResourceIdInitialized == false)
////                {
////                    _IconResourceIdInitialized = true;
////                    _IconResourceId = LoadIconResourceId();
////                }
////
////                return _IconResourceId;
////            }
////        } 
////
////        //// <summary>
////        //// Gets the folders type classification.
////        //// </summary>
////        public DirectoryItemFlags ItemType { get; private set; }
////
////        /// <summary>
////        /// Gets the knownfolder properties if this item represents a knownfolder,
////        /// otherwise null.
////        ///
////        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb773325(v=vs.85).aspx
////        /// </summary>
////        public IKnownFolderProperties KnownFolder { get; private set; }

        /// <summary>
        /// The raw path string that was originally passed in (for debugging only).
        /// </summary>
        public string Path_RAW { get; }
        #endregion properties

        #region methods
////        public void LoadProperties()
////        {
////            if (_KnownFolderIsInitialized == false)
////            {
////                _KnownFolderIsInitialized = true;
////                KnownFolder = LoadKnownFolder();
////            }
////
////            if (_ItemTypeIsInitialized == false)
////            {
////                _ItemTypeIsInitialized = true;
////                ItemType = LoadItemType();
////            }
////
////            if (_IconResourceIdInitialized == false)
////            {
////                _IconResourceIdInitialized = true;
////                _IconResourceId = LoadIconResourceId();
////            }
////        }

        /// <summary>
        /// Initializes the items type flags and path properties.
        /// </summary>
        /// <param name="path">Is either a path reference a la 'C:' or a
        /// special folder path reference a la '::{...}' <seealso cref="KF_IID"/>
        /// for more details.</param>
        /// <returns>Returns a simple pojo type object to initialize
        /// the calling object members.</returns>
        internal static BrowseItemFromPath InitItem(string path)
        {
            if (string.IsNullOrEmpty(path) == true)   // return unknown references
            {
                var ret = new BrowseItemFromPath(path, path);
                ret.Name = path;
                return ret;
            }

            if (path.Length == 38)
            {
                try
                {
                    Guid theGuid;
                    if (Guid.TryParse(path, out theGuid) == true)
                        path = KF_IID.IID_Prefix + path;
                }
                catch {}
            }

            // Return item for root desktop item
            if (string.Compare(path, KF_IID.ID_ROOT_Desktop, true) == 0)
                return InitDesktopRootItem();

            ShellHelpers.SpecialPath isSpecialID = ShellHelpers.IsSpecialPath(path);
            string normPath = null, SpecialPathId = null;
            bool hasPIDL = false;
            IdList parentIdList, relativeChildIdList;

            if (isSpecialID == ShellHelpers.SpecialPath.IsSpecialPath)
            {
                SpecialPathId = path;
                hasPIDL = PidlManager.GetParentIdListFromPath(path, out parentIdList, out relativeChildIdList);
            }
            else
            {
                normPath = Browser.NormalizePath(path);
                hasPIDL = PidlManager.GetParentIdListFromPath(normPath, out parentIdList, out relativeChildIdList);
            }

            if (hasPIDL == false)   // return references that cannot resolve with a PIDL
            {
                var ret = new BrowseItemFromPath(path, path);
                ret.Name = path;
                return ret;
            }

            string parseName = normPath;
            string name = normPath;
            string labelName = null;
            IdList fullIdList = null;

            // Get the IShellFolder2 Interface for the original path item...
            IntPtr fullPidlPtr = default(IntPtr);
            IntPtr ptrShellFolder = default(IntPtr);
            IntPtr parentPIDLPtr = default(IntPtr);
            IntPtr relativeChildPIDLPtr = default(IntPtr);
            try
            {
                // We are asked to build the desktop root item here...
                if (parentIdList == null && relativeChildIdList == null)
                {
                    using (var shellFolder = new ShellFolderDesktop())
                    {
                        parseName = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_FORPARSING);
                        name = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_INFOLDER | SHGDNF.SHGDN_FOREDITING);
                        labelName = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_NORMAL);
                    }
                }
                else
                {
                    fullIdList = PidlManager.CombineParentChild(parentIdList, relativeChildIdList);
                    fullPidlPtr = PidlManager.IdListToPidl(fullIdList);

                    if (fullPidlPtr == default(IntPtr))
                        return null;

                    HRESULT hr = HRESULT.False;

                    if (fullIdList.Size == 1) // Is this item directly under the desktop root?
                    {
                        hr = NativeMethods.SHGetDesktopFolder(out ptrShellFolder);

                        if (hr != HRESULT.S_OK)
                            return null;

                        using (var shellFolder = new ShellFolder(ptrShellFolder))
                        {
                            parseName = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_FORPARSING);
                            name = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_INFOLDER | SHGDNF.SHGDN_FOREDITING);
                            labelName = shellFolder.GetShellFolderName(fullPidlPtr, SHGDNF.SHGDN_NORMAL);
                        }
                    }
                    else
                    {
                        parentPIDLPtr = PidlManager.IdListToPidl(parentIdList);
                        relativeChildPIDLPtr = PidlManager.IdListToPidl(relativeChildIdList);

                        using (var desktopFolder = new ShellFolderDesktop())
                        {
                            hr = desktopFolder.Obj.BindToObject(parentPIDLPtr, IntPtr.Zero,
                                                                typeof(IShellFolder2).GUID, out ptrShellFolder);
                        }

                        if (hr != HRESULT.S_OK)
                            return null;

                        // This item is not directly under the Desktop root
                        using (var shellFolder = new ShellFolder(ptrShellFolder))
                        {
                            parseName = shellFolder.GetShellFolderName(relativeChildPIDLPtr, SHGDNF.SHGDN_FORPARSING);
                            name = shellFolder.GetShellFolderName(relativeChildPIDLPtr, SHGDNF.SHGDN_INFOLDER | SHGDNF.SHGDN_FOREDITING);
                            labelName = shellFolder.GetShellFolderName(relativeChildPIDLPtr, SHGDNF.SHGDN_NORMAL);
                        }
                    }
                }

                if (ShellHelpers.IsSpecialPath(parseName) == ShellHelpers.SpecialPath.None)
                    normPath = parseName;

                return new BrowseItemFromPath(path, parseName, name, labelName,
                                               SpecialPathId, normPath
////                                               ,parentIdList, relativeChildIdList
                                               );
            }
            finally
            {
                PidlManager.ILFree(parentPIDLPtr);
                PidlManager.ILFree(relativeChildPIDLPtr);

                if (fullPidlPtr != default(IntPtr))
                    NativeMethods.ILFree(fullPidlPtr);
            }
        }

        /// <summary>
        /// Class constructor from strings that are commonly exposed by
        /// <see cref="IShellFolder2"/> interfaces. Constructing from these
        /// items can speed up enumeration since we do not need to revisit
        /// each items <see cref="IShellFolder2"/> interfaces.
        /// </summary>
        /// <param name="parseName"></param>
        /// <param name="name"></param>
        /// <param name="labelName"></param>
        /// <returns></returns>
        internal static BrowseItemFromPath InitItem(string parseName,
                                                     string name,
                                                     string labelName)
        {
////            bool hasPIDL = false;
////            IdList parentIdList = null;
////            IdList relativeChildIdList = null;

            string path = parseName;
            string normPath = null, SpecialPathId = null;

            ShellHelpers.SpecialPath isSpecialID = ShellHelpers.IsSpecialPath(path);
            if (isSpecialID == ShellHelpers.SpecialPath.None)
                normPath = parseName;
            else
            {
                SpecialPathId = path;
            }

////            hasPIDL = PidlManager.GetParentIdListFromPath(path, out parentIdList, out relativeChildIdList);
////            if (hasPIDL == false)   // return references that cannot resolve with a PIDL
////            {
////                var ret = new BrowseItemFromPath2(path, path);
////                ret.Name = path;
////                return ret;
////            }

////            IdList fullIdList = null;
////
////            // Get the IShellFolder2 Interface for the original path item...
////            // We are asked to build the desktop root item here...
////            if ((parentIdList == null && relativeChildIdList == null) == false)
////                fullIdList = PidlManager.CombineParentChild(parentIdList, relativeChildIdList);

            return new BrowseItemFromPath(path, parseName, name, labelName,
                                           SpecialPathId, normPath
////                                           ,parentIdList, relativeChildIdList
                                           );
        }

        /// <summary>
        /// Gets an initialized object for the desktop root item.
        /// </summary>
        /// <returns></returns>
        private static BrowseItemFromPath InitDesktopRootItem()
        {
            string root = KF_IID.ID_ROOT_Desktop;
////            DirectoryItemFlags itemType = DirectoryItemFlags.Special | DirectoryItemFlags.DesktopRoot;
            BrowseItemFromPath ret = new BrowseItemFromPath(root, root);

            // Use normal desktop special folder as template for naming and properties
            string specialPath = KF_IID.ID_FOLDERID_Desktop;
            using (var kf = KnownFolderHelper.FromPath(specialPath))
            {
                if (kf == null)
                    return null;

                ////IdList FullPidl = kf.KnownFolderToIdList();
////                ret.KnownFolder = KnownFolderHelper.GetFolderProperties(kf.Obj);
////                ret._KnownFolderIsInitialized = true;
            }

            // A directory we cannot find in file system is by definition VIRTUAL
////            if (ret.KnownFolder.Category == FolderCategory.Virtual)
////                ret.ItemType |= DirectoryItemFlags.Virtual;
////
////            ret.Name = ret.KnownFolder.Name;

            return ret;
        }

////        private string LoadIconResourceId()
////        {
////            string filename = null; // Get Resoure Id for desktop root item
////            int index = -1;
////
////            bool isKFIconResourceIdValid = false;
////            if (KnownFolder != null)
////                isKFIconResourceIdValid = KnownFolder.IsIconResourceIdValid();
////
////            if (isKFIconResourceIdValid == false)
////            {
////                IdList pidl = null;
////                if (ChildIdList != null || ParentIdList != null)
////                    pidl = PidlManager.CombineParentChild(ParentIdList, ChildIdList);
////                else
////                    pidl = IdList.Create();
////
////                if (IconHelper.GetIconResourceId(pidl, out filename, out index))
////                {
////                    // Store filename and index for Desktop Root ResourceId
////                    return string.Format("{0},{1}", filename, index);
////                }
////            }
////            else
////            {
////                return KnownFolder.IconResourceId;
////            }
////
////            return null;
////        }
////
////        private IKnownFolderProperties LoadKnownFolder()
////        {
////            if (this.IsSpecialParseItem)
////                return Browser2.FindKnownFolderByFileSystemPath(this.PathSpecialItemId);
////            else
////            {
////                if (string.IsNullOrEmpty(this.ParseName) == false)
////                    return Browser2.FindKnownFolderByFileSystemPath(this.ParseName);
////                else
////                    return Browser2.FindKnownFolderByFileSystemPath(this.Name);
////            }
////        }
////
////        private DirectoryItemFlags LoadItemType()
////        {
////            DirectoryItemFlags itemType = DirectoryItemFlags.Unknown;
////
////            if (string.IsNullOrEmpty(PathFileSystem) == false)
////            {
////                var pathIsTypeOf = Browser.IsTypeOf(PathFileSystem);
////
////                if (pathIsTypeOf == Enums.PathType.FileSystemPath)
////                {
////                    // TODO XXX Always evaluate on NormPath???
////                    try
////                    {
////                        bool pathExists = false;
////                        try
////                        {
////                            pathExists = System.IO.File.Exists(PathFileSystem);
////                        }
////                        catch { }
////
////                        if (pathExists)
////                        {
////                            itemType |= DirectoryItemFlags.FileSystemFile;
////
////                            if (PathFileSystem.EndsWith(".zip"))
////                                itemType |= DirectoryItemFlags.DataFileContainer;
////                        }
////                    }
////                    catch { }
////
////                    // See if this is a directory if it was not a file...
////                    if ((itemType & DirectoryItemFlags.FileSystemFile) == 0)
////                    {
////                        // Does this directory exist in file system ?
////                        try
////                        {
////                            bool pathExists = false;
////                            try
////                            {
////                                pathExists = System.IO.Directory.Exists(PathFileSystem);
////                            }
////                            catch { }
////
////                            if (pathExists == true)
////                            {
////                                itemType |= DirectoryItemFlags.FileSystemDirectory;
////
////                                // This could be a reference to a drive
////                                DirectoryInfo d = new DirectoryInfo(PathFileSystem);
////                                if (d.Parent == null)
////                                    itemType |= DirectoryItemFlags.Drive;
////                            }
////                            else
////                            {
////                                // Neither a regular directory nor a regular file
////                                // -> Most likely a folder inside a zip file data container
////                                if (PathFileSystem.Contains(".zip"))
////                                {
////                                    itemType |= DirectoryItemFlags.DataFileContainerFolder;
////                                }
////
////                                // -> Lets get its name for display if its more than empty
////                                string displayName = System.IO.Path.GetFileName(PathFileSystem);
////                            }
////                        }
////                        catch (Exception exp)
////                        {
////                            Debug.WriteLine(exp.Message);
////                        }
////                    }
////                }
////            }
////
////            if (KnownFolder != null)
////            {
////                if (KnownFolder.Category == FolderCategory.Virtual)
////                    itemType |= DirectoryItemFlags.Virtual;
////            }
////
////            if (IsSpecialParseItem)
////            {
////                itemType |= DirectoryItemFlags.Special;
////
////                // Check for very common known special directory reference
////                if (KF_IID.ID_FOLDERID_Desktop.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Desktop;
////                else
////                if (KF_IID.ID_FOLDERID_Documents.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Documents;
////                else
////                if (KF_IID.ID_FOLDERID_Downloads.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Downloads;
////                else
////                if (KF_IID.ID_FOLDERID_Music.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Music;
////                else
////                if (KF_IID.ID_FOLDERID_Pictures.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Pictures;
////                else
////                if (KF_IID.ID_FOLDERID_Videos.Equals(PathSpecialItemId, StringComparison.InvariantCultureIgnoreCase))
////                    itemType |= DirectoryItemFlags.Videos;
////            }
////
////
////            return itemType;
////        }
        #endregion methods
    }
}
