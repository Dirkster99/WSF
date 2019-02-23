namespace WSF.Browse
{
    using WSF.Enums;
    using WSF.Interfaces;
    using System;
    using WSF.Shell.Pidl;
    using System.Text;
    using WSF.Shell.Interop.Interfaces.Knownfolders;
    using WSF.Shell.Interop.ResourceIds;
    using System.IO;
    using System.Diagnostics;
    using WSF.Shell.Interop.Interfaces.KnownFolders;
    using WSF.IDs;

    /// <summary>
    /// Implements a light weight Windows Shell Browser class that can be used
    /// to model and browse the shell tree structure of the Windows (7-10) Shell.
    /// </summary>
    internal sealed partial class DirectoryBrowser : IDirectoryBrowser
    {
        #region fields
        private object resolvePropsLock = new object();
        private bool _IconResourceIdInitialized;
        private string _IconResourceId;

        private bool _KnownFolderIsInitialized;
        private IKnownFolderProperties _KnownFolder;

        private object _resolvePidlsLock = new object();
        private bool _PIDLs_Initialized;
        private IdList _ParentIdList;
        private IdList _ChildIdList;

        private bool _ItemTypeIsInitialized;
        private DirectoryItemFlags _ItemType;
        #endregion fields

        #region ctors
        /// <summary>
        /// Class constructor
        /// </summary>
        public DirectoryBrowser(BrowseItemFromPath itemModel)
        {
            PathRAW = itemModel.Path_RAW;

            Name = itemModel.Name;
            ParseName = itemModel.ParseName;
            Label = itemModel.LabelName;
            SpecialPathId = itemModel.PathSpecialItemId;
            IsSpecialParseItem = itemModel.IsSpecialParseItem;
            PathFileSystem = itemModel.PathFileSystem;

            // Get PathShell
            if (string.IsNullOrEmpty(itemModel.PathSpecialItemId) == false)
                PathShell = itemModel.PathSpecialItemId;
            else
            {
                if (string.IsNullOrEmpty(itemModel.PathFileSystem) == false)
                    PathShell = itemModel.PathFileSystem;
                else
                {
                    PathShell = itemModel.Name;
                }
            }

            // Get FullName
            if (string.IsNullOrEmpty(itemModel.PathFileSystem) == false)
                FullName = itemModel.PathFileSystem;
            else
            {
                if (string.IsNullOrEmpty(itemModel.PathSpecialItemId) == false)
                    FullName = itemModel.PathSpecialItemId;
                else
                {
                    FullName = itemModel.Name;
                }
            }
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copyThis"></param>
        public DirectoryBrowser(DirectoryBrowser copyThis)
        {
            if (copyThis == null)
                return;

            _ItemTypeIsInitialized = copyThis._ItemTypeIsInitialized;
            _ItemType = copyThis._ItemType;

            _KnownFolderIsInitialized = copyThis._KnownFolderIsInitialized;
            _KnownFolder = copyThis._KnownFolder;

            _IconResourceIdInitialized = copyThis._IconResourceIdInitialized;
            _IconResourceId = copyThis._IconResourceId;

            Name = copyThis.Name;
            ParseName = copyThis.ParseName;
            Label = copyThis.Label;
            PathRAW = copyThis.PathRAW;
            PathShell = copyThis.PathShell;

            _PIDLs_Initialized = copyThis._PIDLs_Initialized;
            _ChildIdList = copyThis._ChildIdList;
            _ParentIdList = copyThis._ParentIdList;

            SpecialPathId = copyThis.SpecialPathId;
            IsSpecialParseItem = copyThis.IsSpecialParseItem;

            PathFileSystem = copyThis.PathFileSystem;

            FullName = copyThis.FullName;
        }
        #endregion ctors

        #region properties
        /// <summary>
        /// Gets the (localized) name of an item.
        /// </summary>
        public string Name { get; }

        public string ParseName { get; }

        /// <summary>
        /// Gets a label string that may differ from other naming
        /// strings if the item (eg Drive) supports labeling.
        /// 
        /// String is suitable for display only and should
        /// not be used as index as it is not unique.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets the logical path of a directory item.
        /// The logical path can differ from the physical storage location path
        /// because there are some special folder items
        /// that have no storage location (e.g. 'This PC')
        /// and a special folder can be identified:
        /// 
        /// 1) with GUID based strings (see KF_IID) or
        /// 2) via its path ('C:\Windows')
        /// 
        /// 3) via its name ('G:\')
        ///    A device that is not ready (eg: DVD drive without DVD)
        ///    will have neither a special id nor a filesystem path.
        /// </summary>
        public string PathShell { get; }

        /// <summary>
        /// Contains the special path GUID if this item is a special shell space item.
        /// </summary>
        public string SpecialPathId { get; }

        public bool IsSpecialParseItem { get; }

        /// <summary>
        /// Gets the filesystem path (e.g. 'C:\') if this item has a dedicated
        /// or associated storage location in the file system.
        /// </summary>
        public string PathFileSystem { get; }

        /// <summary>
        /// Gets the IdList (if available) that describes the full
        /// shell path for this item)
        /// </summary>
        public IdList ParentIdList
        {
            get
            {
                LoadPidls();
                return _ParentIdList;
            }
        }

        /// <summary>
        /// Gets the IdList (if available) that describes the full
        /// shell path for this item)
        /// </summary>
        public IdList ChildIdList
        {
            get
            {
                LoadPidls();
                return _ChildIdList;
            }
        }

        /// <summary>
        /// Gets an optional pointer to the default icon resource used when the folder is created.
        /// This is a null-terminated Unicode string in this form:
        ///
        /// Module name, Resource ID
        /// or null is this information is not available.
        /// </summary>
        public string IconResourceId
        {
            get
            {
                lock (resolvePropsLock)
                {
                    if (_IconResourceIdInitialized == false)
                    {
                        _IconResourceIdInitialized = true;
                        _IconResourceId = LoadIconResourceId();
                    }
                }

                return _IconResourceId;
            }
        }

        //// <summary>
        //// Gets the folders type classification.
        //// </summary>
        public DirectoryItemFlags ItemType
        {
            get
            {
                lock (resolvePropsLock)
                {
                    if (_ItemTypeIsInitialized == false)
                    {
                        _ItemTypeIsInitialized = true;
                        _ItemType = LoadItemType();
                    }
                }

                return _ItemType;
            }
        }

        /// <summary>
        /// Gets the knownfolder properties if this item represents a knownfolder,
        /// otherwise null.
        ///
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb773325(v=vs.85).aspx
        /// </summary>
        public IKnownFolderProperties KnownFolder
        {
            get
            {
                lock (resolvePropsLock)
                {
                    if (_KnownFolderIsInitialized == false)
                    {
                        _KnownFolderIsInitialized = true;
                        _KnownFolder = LoadKnownFolder();
                    }
                }

                return _KnownFolder;
            }
        }

        /// <summary>
        /// Gets whether all properties have been fully resolved or not.
        /// 
        /// Properties like <see cref="KnownFolder"/>, <see cref="IconResourceId"/>,
        /// or <see cref="ItemType"/> can be loaded lazily
        /// - call <see cref="LoadProperties"/> method to complete this process
        /// before using these properties.
        /// </summary>
        public bool IsFullyInitialized
        {
            get
            {
                return _IconResourceIdInitialized
                    && _ItemTypeIsInitialized
                    && _KnownFolderIsInitialized;
            }
        }

        /// <summary>
        /// Gets the raw string that was used to construct this object.
        /// This property is for debug purposes only and should not be
        /// visible in any UI or ViewModel.
        /// </summary>
        public string PathRAW { get; }

        /// <summary>
        /// Get Known file system Path  or FolderId for this folder.
        /// 
        /// That is:
        /// 1) A storage location (if it exists) in the filesystem
        /// 
        /// 2) A knownfolder GUID (if it exists) is shown
        ///    here as default preference over
        ///    
        /// </summary>
        public string FullName { get; }
        #endregion properties

        #region methods
        /// <summary>
        /// Resolves remaining properties if <see cref="IsFullyInitialized"/> indicates
        /// missing values (useful for lazy initialization).
        /// </summary>
        public void LoadProperties()
        {
            lock (resolvePropsLock)
            {
                if (_KnownFolderIsInitialized == false)
                {
                    _KnownFolderIsInitialized = true;
                    _KnownFolder = LoadKnownFolder();
                }

                if (_ItemTypeIsInitialized == false)
                {
                    _ItemTypeIsInitialized = true;
                    _ItemType = LoadItemType();
                }

                if (_IconResourceIdInitialized == false)
                {
                    _IconResourceIdInitialized = true;
                    _IconResourceId = LoadIconResourceId();
                }
            }
        }

        /// <summary>
        /// Determines if this item refers to an existing path in the filesystem or not.
        /// </summary>
        /// <returns></returns>
        public bool DirectoryPathExists()
        {
            if (string.IsNullOrEmpty(PathFileSystem) == true)
                return false;

            // strings empty and Name with same value usually indicates non-existing item
            if (string.IsNullOrEmpty(PathFileSystem) == true &&
                string.IsNullOrEmpty(SpecialPathId) == true && Name == PathRAW)
                return false;

            // Check if this folder or drive item exists in file system
            if ((ItemType & DirectoryItemFlags.FileSystemDirectory) != 0)
            {
                bool isPath = false;

                try
                {
                    isPath = System.IO.Directory.Exists(PathFileSystem);
                }
                catch
                {
                }

                return isPath;
            }

            return false;
        }

        #region ICloneable
        public object Clone()
        {
            return new DirectoryBrowser(this);
        }
        #endregion ICloneable

        /// <summary>
        /// Standard ToString() function to support internal debugging.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder itemFlags = new StringBuilder();

            Array values = Enum.GetValues(typeof(DirectoryItemFlags));

            foreach (DirectoryItemFlags val in values)
            {
                if ((ItemType & val) != 0)
                    itemFlags.Append(itemFlags.Length == 0 ? val.ToString() : " | " + val.ToString());
            }

            return string.Format("Name: '{0}',PathRAW: '{1}', SpecialPathId: '{2}', PathFileSystem '{3}', Flags: '{4}'",
                Name, PathRAW, SpecialPathId, PathFileSystem, itemFlags);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(IDirectoryBrowser other)
        {
            if (other == null)
                return false;

            if (KnownFolder != null && other.KnownFolder != null)
            {
                if (KnownFolder.FolderId == other.KnownFolder.FolderId)
                    return true;
            }

            if (string.Compare(this.SpecialPathId, other.SpecialPathId, true) != 0 ||
                string.Compare(this.PathFileSystem, other.PathFileSystem, true) != 0)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            var otherbrowser = obj as IDirectoryBrowser;

            if (otherbrowser == null)
                return false;

            return Equals(otherbrowser);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            if (PathShell == null)
                return 0;

            return PathShell.GetHashCode();
        }

        /// <summary>
        /// Compares a given parse name with the parse names known in this object.
        /// 
        /// Considers case insensitive string matching for:
        /// 1> SpecialPathId
        ///   1.2> PathRAW (if SpecialPathId fails and CLSID may have been used to create this)
        ///
        /// 3> PathFileSystem
        /// </summary>
        /// <param name="parseName">True is a matching parse name was found and false if not.</param>
        /// <returns></returns>
        public bool EqualsParseName(string parseName)
        {
            if (string.IsNullOrEmpty(parseName) == true)
                return false;

            bool KF_SpecialId = false;
            if (string.IsNullOrEmpty(SpecialPathId) == false)
            {
                KF_SpecialId = string.Compare(SpecialPathId, parseName, true) == 0;

                if (KF_SpecialId == false)
                {
                    // There are some corner cases where knownfolder special id is sometimes based on
                    // a CLSID (as in case of this PC) and the resulting knownfolder_id is different
                    // from the initial CLSID -> Lets try to cover this one here
                    if (string.Compare(PathRAW, parseName, true) == 0)
                        KF_SpecialId = true;
                }
            }

            if (KF_SpecialId == false)
            {
                if (string.IsNullOrEmpty(PathFileSystem) == false)
                    return string.Compare(PathFileSystem, parseName, true) == 0;
            }
            else
                return true;

            return false;
        }

        public void LoadPidls()
        {
            lock (_resolvePidlsLock)
            {
                if (_PIDLs_Initialized == true)
                    return;

                _PIDLs_Initialized = true;
                IdList parentIdList, relativeChildIdList;
                bool hasPIDL = PidlManager.GetParentIdListFromPath(PathShell, out parentIdList, out relativeChildIdList);
                if (hasPIDL == true)
                {
                    _ParentIdList = parentIdList;
                    _ChildIdList = relativeChildIdList;
                }
            }
        }

        private string LoadIconResourceId()
        {
            string filename = null; // Get Resoure Id for desktop root item
            int index = -1;

            lock (resolvePropsLock)
            {
                if (_KnownFolderIsInitialized == false)
                {
                    _KnownFolderIsInitialized = true;
                    _KnownFolder = LoadKnownFolder();
                }
            }

            if (KnownFolder == null)
                return null;

            bool isKFIconResourceIdValid = false;
            if (KnownFolder != null)
                isKFIconResourceIdValid = KnownFolder.IsIconResourceIdValid();

            if (isKFIconResourceIdValid == false)
            {
                IdList pidl = null;
                LoadPidls();

                if (ChildIdList != null || ParentIdList != null)
                    pidl = PidlManager.CombineParentChild(ParentIdList, ChildIdList);
                else
                    pidl = IdList.Create();

                if (IconHelper.GetIconResourceId(pidl, out filename, out index))
                {
                    // Store filename and index for Desktop Root ResourceId
                    return string.Format("{0},{1}", filename, index);
                }
            }
            else
            {
                return KnownFolder.IconResourceId;
            }

            return null;
        }

        private IKnownFolderProperties LoadKnownFolder()
        {
            if (this.IsSpecialParseItem)
                return Browser.FindKnownFolderByFileSystemPath(this.SpecialPathId);
            else
            {
                if (string.IsNullOrEmpty(this.ParseName) == false)
                    return Browser.FindKnownFolderByFileSystemPath(this.ParseName);
                else
                    return Browser.FindKnownFolderByFileSystemPath(this.Name);
            }
        }

        private DirectoryItemFlags LoadItemType()
        {
            DirectoryItemFlags itemType = DirectoryItemFlags.Unknown;

            if (string.IsNullOrEmpty(PathFileSystem) == false)
            {
                var pathIsTypeOf = Browser.IsTypeOf(PathFileSystem);

                if (pathIsTypeOf == Enums.PathType.FileSystemPath)
                {
                    // TODO XXX Always evaluate on NormPath???
                    try
                    {
                        bool pathExists = false;
                        try
                        {
                            pathExists = System.IO.File.Exists(PathFileSystem);
                        }
                        catch { }

                        if (pathExists)
                        {
                            itemType |= DirectoryItemFlags.FileSystemFile;

                            if (PathFileSystem.EndsWith(".zip"))
                                itemType |= DirectoryItemFlags.DataFileContainer;
                        }
                    }
                    catch { }

                    // See if this is a directory if it was not a file...
                    if ((itemType & DirectoryItemFlags.FileSystemFile) == 0)
                    {
                        // Does this directory exist in file system ?
                        try
                        {
                            bool pathExists = false;
                            try
                            {
                                pathExists = System.IO.Directory.Exists(PathFileSystem);
                            }
                            catch
                            {
                                // Catch this in case string contains blah blah blah :-)
                            }

                            if (pathExists == true)
                            {
                                itemType |= DirectoryItemFlags.FileSystemDirectory;

                                // This could be a reference to a drive
                                DirectoryInfo d = new DirectoryInfo(PathFileSystem);
                                if (d.Parent == null)
                                    itemType |= DirectoryItemFlags.Drive;
                            }
                            else
                            {
                                // Neither a regular directory nor a regular file
                                // -> Most likely a folder inside a zip file data container
                                if (PathFileSystem.Contains(".zip"))
                                {
                                    itemType |= DirectoryItemFlags.DataFileContainerFolder;
                                }

                                // -> Lets get its name for display if its more than empty
                                string displayName = System.IO.Path.GetFileName(PathFileSystem);
                            }
                        }
                        catch (Exception exp)
                        {
                            // Catch and output this in debug in case we've missed a trivial check
                            Debug.WriteLine(exp.Message);
                        }
                    }
                }
            }

            if (KnownFolder != null)
            {
                if (KnownFolder.Category == FolderCategory.Virtual)
                    itemType |= DirectoryItemFlags.Virtual;
            }

            if (IsSpecialParseItem)
            {
                itemType |= DirectoryItemFlags.Special;

                // Check for very common known special directory reference
                if (KF_IID.ID_FOLDERID_Desktop.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Desktop;
                else
                if (KF_IID.ID_FOLDERID_Documents.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Documents;
                else
                if (KF_IID.ID_FOLDERID_Downloads.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Downloads;
                else
                if (KF_IID.ID_FOLDERID_Music.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Music;
                else
                if (KF_IID.ID_FOLDERID_Pictures.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Pictures;
                else
                if (KF_IID.ID_FOLDERID_Videos.Equals(SpecialPathId, StringComparison.InvariantCultureIgnoreCase))
                    itemType |= DirectoryItemFlags.Videos;
            }


            return itemType;
        }
        #endregion methods
    }
}
