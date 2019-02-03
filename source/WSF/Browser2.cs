namespace WSF
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using WSF.Browse;
    using WSF.Enums;
    using WSF.IDs;
    using WSF.Interfaces;
    using WSF.Shell.Enums;
    using WSF.Shell.Interop;
    using WSF.Shell.Interop.Dlls;
    using WSF.Shell.Interop.Interfaces.Knownfolders;
    using WSF.Shell.Interop.Interfaces.ShellFolders;
    using WSF.Shell.Interop.Knownfolders;
    using WSF.Shell.Interop.ShellFolders;
    using WSF.Shell.Pidl;

    /// <summary>
    /// Implements core API type methods and properties that are used to interact
    /// with Windows Shell System Items (folders and known folders).
    /// </summary>
    public static class Browser2
    {
        #region fields
        /// <summary>
        /// Filtering these folders since they are not too useful here
        /// and cause odd exception in later processing steps ...
        ///
        /// SyncSetupFolder, SyncResultsFolder, ConflictFolder
        /// AppUpdatesFolder, ChangeRemoveProgramsFolder, SyncCenterFolder
        /// </summary>
        private static readonly HashSet<Guid> _filterKF = new HashSet<Guid>()
        {
            new Guid(KF_ID.ID_FOLDERID_SyncSetupFolder),
            new Guid(KF_ID.ID_FOLDERID_SyncResultsFolder),
            new Guid(KF_ID.ID_FOLDERID_ConflictFolder),
            new Guid(KF_ID.ID_FOLDERID_AppUpdates),
            new Guid(KF_ID.ID_FOLDERID_ChangeRemovePrograms),
            new Guid(KF_ID.ID_FOLDERID_SyncManagerFolder),
            new Guid(KF_ID.ID_FOLDERID_LocalizedResourcesDir),
        };
        #endregion fields

        #region ctors
        /// <summary>
        /// Static constructor
        /// </summary>
        static Browser2()
        {
            KnownFileSystemFolders = new Dictionary<string, IKnownFolderProperties>();

            LocalKFs = new Dictionary<Guid, IKnownFolderProperties>();
        }
        #endregion  ctors

        #region properties
        /// <summary>
        /// Gets the default system drive - usually 'C:\'.
        /// </summary>
        /// <returns></returns>
        public static IDirectoryBrowser2 SysDefault
        {
            get
            {
                try
                {
                    var drive = new DirectoryInfo(Environment.SystemDirectory).Root.Name;
                    return Create(drive);
                }
                catch
                {
                    return Create(@"C:\");
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IDirectoryBrowser2"/> interface
        /// for a user's desktop folder.
        /// </summary>
        public static IDirectoryBrowser2 DesktopDirectory
        {
            get
            {
                return Create(KF_IID.ID_FOLDERID_Desktop);
            }
        }

        /// <summary>
        /// Gets the <see cref="IDirectoryBrowser"/> interface
        /// for a current system user's directory and known folder item.
        /// </summary>
        public static IDirectoryBrowser2 CurrentUserDirectory
        {
            get
            {
                return Create(KF_IID.ID_FOLDERID_Profile);
            }
        }

        /// <summary>
        /// Gets the interface for a user's 'This PC' (virtual folder).
        /// 
        /// This item usually lists Mounted drives (eg: 'C:\'),
        /// and frequently used special folders like: Desktop, Music, Video, Downloads etc..
        /// </summary>
        public static IDirectoryBrowser2 MyComputer
        {
            get
            {
                return Create(KF_IID.ID_FOLDERID_ComputerFolder);
            }
        }

        /// <summary>
        /// Gets the <see cref="IDirectoryBrowser2"/> interface
        /// for the Public Documents folder (%PUBLIC%\Documents).
        /// </summary>
        public static IDirectoryBrowser2 PublicDocuments
        {
            get
            {
                try
                {
                    return Create(KF_IID.ID_FOLDERID_PublicDocuments);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the interface for the Network
        /// (virtual folder, Legacy: My Network Places).
        /// </summary>
        public static IDirectoryBrowser2 Network
        {
            get
            {
                return Create(KF_IID.ID_FOLDERID_NetworkFolder);
            }
        }

        /// <summary>
        /// Gets the interface
        /// for the Rycycle Bin (virtual folder).
        /// </summary>
        public static IDirectoryBrowser2 RecycleBin
        {
            get
            {
                return Create(KF_IID.ID_FOLDERID_RecycleBinFolder);
            }
        }

        /// <summary>
        /// Contains a collection of known folders with a file system folder.
        /// This collection is build on program start-up.
        /// </summary>
        private static Dictionary<string, IKnownFolderProperties> KnownFileSystemFolders { get; }

        private static Dictionary<Guid, IKnownFolderProperties> LocalKFs { get; set; }
        #endregion properties

        #region methods
        /// <summary>Creates a new object that implements the
        /// <see cref="IDirectoryBrowser"/> interface. The created
        /// instance is based on the given parameter, which can be
        /// a path based string (e.g: 'c:\') or a special folder
        /// based string (e.g: '::{...}').
        /// 
        /// The <see cref="IDirectoryBrowser"/> object is always created
        /// and returned unless the given string parameter is null or empty
        /// which results in an <see cref="System.ArgumentNullException"/>
        /// Exception being thrown.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static IDirectoryBrowser2 Create(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) == true)
                throw new System.ArgumentNullException("path cannot be null or empty");

            try
            {
                var itemModel = BrowseItemFromPath2.InitItem(fullPath);
                itemModel.LoadProperties();

                return new DirectoryBrowser2(itemModel);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Exception in Create method '{0}' on '{1}'", exc.Message, fullPath);

                return null;
            }
        }


        /// <summary>Creates a new object that implements the
        /// <see cref="IDirectoryBrowser2"/> interface from a
        /// <paramref name="parseName"/>, <paramref name="name"/>,
        /// and <paramref name="labelName"/>.
        /// 
        /// This method is a short-cut to by-pass additional Windows Shell API
        /// queries for this items. An enumeration of Windows Shell items, for
        /// instance, can already result into all three items, so we by-pass
        /// the IShellFolder query stuff to speed up processing in this situation.
        /// 
        /// The created instance is based on the given <paramref name="parseName"/>
        /// parameter, which can be a path based string (e.g: 'c:\') or
        /// a special folder based string (e.g: '::{...}').
        /// 
        /// The <see cref="IDirectoryBrowser2"/> object is always created
        /// and returned unless the given <paramref name="parseName"/>
        /// parameter is null or empty which results in an <see cref="System.ArgumentNullException"/>
        /// Exception being thrown.
        /// </summary>
        /// <param name="parseName"></param>
        /// <param name="labelName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IDirectoryBrowser2 Create(string parseName,
                                                string name,
                                                string labelName)
        {
            if (string.IsNullOrEmpty(parseName) == true)
                throw new System.ArgumentNullException("path cannot be null or empty");

            try
            {
                var itemModel = BrowseItemFromPath2.InitItem(parseName, name, labelName);
                itemModel.LoadProperties();

                return new DirectoryBrowser2(itemModel);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Exception in Create method '{0}' on '{1}'", exc.Message, parseName);

                return null;
            }
        }

        /// <summary>
        /// Gets an enumeration of all childitems below the
        /// <paramref name="folderParseName"/> item.
        /// </summary>
        /// <param name="folderParseName">Is the parse name that should be used to emit child items for.</param>
        /// <param name="searchMask">Optional name of an item that should be filtered
        /// in case insensitive fashion when searching for a certain child rather than all children.</param>
        /// <param name="itemFilter">Specify wether to filter only on names or on names and ParseNames</param>
        /// <returns>returns each item as <see cref="IDirectoryBrowser2"/> object</returns>
        public static IEnumerable<IDirectoryBrowser2> GetChildItems(string folderParseName,
                                                                   string searchMask = null,
                                                                   SubItemFilter itemFilter = SubItemFilter.NameOnly)
        {
            if (string.IsNullOrEmpty(folderParseName) == true)
                yield break;

            // Defines the type of items that we want to retieve below the item passed in
            const SHCONTF flags = SHCONTF.FOLDERS | SHCONTF.INCLUDEHIDDEN | SHCONTF.FASTITEMS;

            //  Get the desktop root folder.
            IntPtr enumerator = default(IntPtr);
            IntPtr pidlFull = default(IntPtr);
            IntPtr ptrFolder = default(IntPtr);
            IShellFolder2 iFolder = null;
            IEnumIDList enumIDs = null;
            IntPtr ptrStr = default(IntPtr);      // Fetch parse name for this item
            try
            {
                HRESULT hr;

                if (KF_IID.ID_FOLDERID_Desktop.Equals(folderParseName, StringComparison.InvariantCultureIgnoreCase))
                    hr = NativeMethods.SHGetDesktopFolder(out ptrFolder);
                else
                {
                    pidlFull = ShellHelpers.PidlFromParsingName(folderParseName);

                    if (pidlFull == default(IntPtr)) // 2nd chance try known folders
                    {
                        using (var kf = KnownFolderHelper.FromPath(folderParseName))
                        {
                            if (kf != null)
                                kf.Obj.GetIDList((uint)KNOWN_FOLDER_FLAG.KF_NO_FLAGS, out pidlFull);
                        }
                    }

                    if (pidlFull == default(IntPtr))
                        yield break;

                    using (var desktopFolder = new ShellFolderDesktop())
                    {
                        hr = desktopFolder.Obj.BindToObject(pidlFull, IntPtr.Zero,
                                                            typeof(IShellFolder2).GUID,
                                                            out ptrFolder);
                    }
                }

                if (hr != HRESULT.S_OK)
                    yield break;

                if (ptrFolder != IntPtr.Zero)
                    iFolder = (IShellFolder2)Marshal.GetTypedObjectForIUnknown(ptrFolder, typeof(IShellFolder2));

                if (iFolder == null)
                    yield break;

                //  Create an enumerator and enumerate over each item.
                hr = iFolder.EnumObjects(IntPtr.Zero, flags, out enumerator);

                if (hr != HRESULT.S_OK)
                    yield break;

                // Convert enum IntPtr to interface
                enumIDs = (IEnumIDList)Marshal.GetTypedObjectForIUnknown(enumerator, typeof(IEnumIDList));

                if (enumIDs == null)
                    yield break;

                FilterMask filter = null;
                if (searchMask != null)
                    filter = new FilterMask(searchMask);

                uint fetched, count = 0;
                IntPtr apidl = default(IntPtr);

                // Allocate memory to convert parsing names into .Net strings efficiently below
                ptrStr = Marshal.AllocCoTaskMem(NativeMethods.MAX_PATH * 2 + 4);
                Marshal.WriteInt32(ptrStr, 0, 0);
                StringBuilder strbuf = new StringBuilder(NativeMethods.MAX_PATH);
                //var desktop = Browser2.DesktopDirectory;

                // Get one item below root item at a time and process by getting its display name
                // PITEMID_CHILD: The ITEMIDLIST is an allocated child ITEMIDLIST relative to
                // a parent folder, such as a result of IEnumIDList::Next.
                // It contains exactly one SHITEMID structure (https://docs.microsoft.com/de-de/windows/desktop/api/shtypes/ns-shtypes-_itemidlist)
                for (; enumIDs.Next(1, out apidl, out fetched) == HRESULT.S_OK; count++)
                {
                    if (fetched <= 0)  // End this loop if no more items are available
                        break;

                    try
                    {
                        string name = string.Empty;
                        bool bFilter = false;
                        if (iFolder.GetDisplayNameOf(apidl, SHGDNF.SHGDN_INFOLDER | SHGDNF.SHGDN_FOREDITING, ptrStr) == HRESULT.S_OK)
                        {
                            NativeMethods.StrRetToBuf(ptrStr, default(IntPtr),
                                                      strbuf, NativeMethods.MAX_PATH);

                            name = strbuf.ToString();
                        }

                        // Skip this item if search parameter is set and this appears to be a non-match
                        if (filter != null)
                        {
                            if (filter.MatchFileMask(name) == false)
                            {
                                if (itemFilter == SubItemFilter.NameOnly) // Filter items on Names only
                                    continue;

                                bFilter = true;
                            }
                        }

                        string parseName = string.Empty;
                        if (iFolder.GetDisplayNameOf(apidl, SHGDNF.SHGDN_FORPARSING, ptrStr) == HRESULT.S_OK)
                        {
                            NativeMethods.StrRetToBuf(ptrStr, default(IntPtr),
                                                      strbuf, NativeMethods.MAX_PATH);

                            parseName = strbuf.ToString();
                        }

                        // Skip this item if search parameter is set and this appears to be a non-match
                        if (filter != null)
                        {
                            if (filter.MatchFileMask(parseName) == false && bFilter == true)
                                continue;
                        }

                        string labelName = string.Empty;
                        if (iFolder.GetDisplayNameOf(apidl, SHGDNF.SHGDN_NORMAL, ptrStr) == HRESULT.S_OK)
                        {
                            NativeMethods.StrRetToBuf(ptrStr, default(IntPtr),
                                                      strbuf, NativeMethods.MAX_PATH);

                            labelName = strbuf.ToString();
                        }

                        IdList apidlIdList = PidlManager.PidlToIdlist(apidl);

                        yield return Create(parseName, name, labelName);
                    }
                    finally
                    {
                        apidl = PidlManager.FreeCoTaskMem(apidl);
                    }
                }
            }
            finally
            {
                if (enumIDs != null)
                    Marshal.ReleaseComObject(enumIDs);

                if (enumerator != default(IntPtr))
                    Marshal.Release(enumerator);

                if (iFolder != null)
                    Marshal.ReleaseComObject(iFolder);

                if (ptrFolder != default(IntPtr))
                    Marshal.Release(ptrFolder);

                ptrStr = PidlManager.FreeCoTaskMem(ptrStr);
            }
        }

        /// <summary>
        /// Parses the first two characters of a given path to determine its type.
        /// Paths that are shorter than 2 characters are classified <see cref="PathType.Unknown"/>.
        /// 
        /// Paths with 2 or more characters having no File Fystem or Special Folder signature
        /// are clasified as <seealso cref="PathType.WinShellPath"/>.
        /// 
        /// Returns false for strings 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static PathType IsTypeOf(string input)
        {
            if (string.IsNullOrEmpty(input))
                return PathType.Unknown;

            if (input.Length < 2)
                return PathType.Unknown;

            // Could be drive based like 'c:\Windows' or a network share like '\\MyServer\share'
            if ((char.ToLower(input[0]) >= 'a' && char.ToLower(input[0]) <= 'z' &&   // Drive based file system path
                 input[1] == ':') ||
                 (char.ToLower(input[0]) == '\\' && char.ToLower(input[1]) <= '\\'))  // UNC file system path
                return PathType.FileSystemPath;

            // Could be something like '::{Guid}' which is usually a known folder's path
            if (input[0] == ':' && input[1] == ':')
                return PathType.SpecialFolder;

            // Could be something like 'Libraries\Music'
            return PathType.WinShellPath;
        }

        /// <summary>
        /// Determines if a directory (special or not) exists at the givem path
        /// (path can be formatted as special path KF_IDD) and returns <paramref name="pathItems"/>
        /// if path was a sequence of Windows shell named items (eg 'Libraries\Music').
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathItems"></param>
        /// <returns>Returns true if item has a filesystem path otherwise false.</returns>
        public static bool DirectoryExists(string path,
                                           out IDirectoryBrowser2[] pathItems)
        {
            pathItems = null;

            if (string.IsNullOrEmpty(path))
                return false;

            if (ShellHelpers.IsSpecialPath(path) == ShellHelpers.SpecialPath.IsSpecialPath)
            {
                try
                {
                    // translate KF_IID into file system path and check if it exists
                    string fs_path = KnownFolderHelper.GetKnownFolderPath(path);

                    if (fs_path != null)
                    {
                        bool exists = System.IO.Directory.Exists(fs_path);

                        if (exists)
                            pathItems = GetFileSystemPathItems(fs_path);

                        return exists;
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            }
            else
            {
                if (path.Length < 2)
                    return false;

                try
                {
                    if ((path[0] == '\\' && path[1] == '\\') || path[1] == ':')
                    {
                        bool exists = System.IO.Directory.Exists(path);

                        if (exists)
                            pathItems = GetFileSystemPathItems(path);

                        return exists;
                    }

                    if (path.Length > 1)
                        path = path.TrimEnd('\\');

                    // Try to resolve an abstract Windows Shell Space description like:
                    // 'Libraries/Documents' (valid in a localized fashion only)
                    pathItems = GetWinShellPathItems(path);

                    // This path exists as sequence of localized names of windows shell items
                    if (pathItems != null)
                        return true;
                }
                catch
                {
                    // Something went wrong so we signal that we cannot resolve this one...
                    return false;
                }

                return false;
            }
        }

        /// <summary>
        /// Converts a given existing file system path string into a sequence
        /// of <see cref="IDirectoryBrowser2"/> items or null if path cannot be resolved.
        /// </summary>
        /// <param name="fs_path">The file system path to be resolved.</param>
        /// <param name="bFindKF">Determines if known folder should be looked up
        /// even if given folder is a normal string such as (eg.: 'C:\Windows\').
        /// Set this parameter only if you are sure that you need it as it will
        /// have a performance impact on the time required to generate the object.
        /// </param>
        /// <returns></returns>
        public static IDirectoryBrowser2[] GetFileSystemPathItems(string fs_path,
                                                                  bool bFindKF = false)
        {
            try
            {
                var dirs = GetDirectories(fs_path);
                var dirItems = new IDirectoryBrowser2[dirs.Length];
                string currentPath = null;
                for (int i = 0; i < dirItems.Length; i++)
                {
                    if (currentPath == null)
                        currentPath = dirs[0];
                    else
                        currentPath = System.IO.Path.Combine(currentPath, dirs[i]);

                    dirItems[i] = Create(currentPath);
                }

                return dirItems;
            }
            catch
            {
                // Lets make sure we can recover from errors
                return null;
            }
        }

        /// <summary>
        /// Converts a given existing Windows shell Path string
        /// (eg 'Libraries\Music') into a sequence of <see cref="IDirectoryBrowser"/>
        /// items or null if path cannot be resolved.
        /// </summary>
        /// <param name="path">The Windows Shell Path to be resolved.</param>
        /// <returns></returns>
        public static IDirectoryBrowser2[] GetWinShellPathItems(string path)
        {
            IDirectoryBrowser2[] pathItems = null;
            try
            {
                string[] pathNames = GetDirectories(path);

                if (pathNames == null)
                    return null;

                if (pathNames.Length == 0)
                    return null;

                pathItems = new IDirectoryBrowser2[pathNames.Length];

                string parentPath = KF_IID.ID_FOLDERID_Desktop;
                for (int i = 0; i < pathItems.Length; i++)
                {
                    if (i > 0)
                        parentPath = pathItems[i - 1].PathShell;

                    var subList = GetChildItems(parentPath, pathNames[i]);
                    if (subList.Any())
                    {
                        pathItems[i] = subList.First();
                    }
                    else
                        return null;
                }

                return pathItems;
            }
            catch
            {
                // Lets make sure we can recover from errors
                return null;
            }
        }

        /// <summary>
        /// Split the current folder in an array of sub-folder names and return it.
        /// </summary>
        /// <returns>Returns a string array of su-folder names (including drive) or null if there are no sub-folders.</returns>
        public static string[] GetDirectories(string folder)
        {
            if (string.IsNullOrEmpty(folder) == true)
                return null;

            folder = NormalizePath(folder);

            string[] dirs = null;

            try
            {
                dirs = folder.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                if (dirs != null)
                {
                    if (dirs[0].Length == 2)       // Normalizing Drive representation
                    {                             // from 'C:' to 'C:\'
                        if (dirs[0][1] == ':')   // to ensure correct processing
                            dirs[0] += '\\';    // since 'C:' is technically invalid(!)
                    }
                }
            }
            catch
            {
            }

            return dirs;
        }

        /// <summary>
        /// Make sure that a path reference does actually work with
        /// <see cref="System.IO.DirectoryInfo"/> by replacing 'C:' by 'C:\'.
        /// </summary>
        /// <param name="dirOrfilePath"></param>
        /// <returns></returns>
        public static string NormalizePath(string dirOrfilePath)
        {
            if (string.IsNullOrEmpty(dirOrfilePath) == true)
                return dirOrfilePath;

            // The dirinfo constructor will not work with 'C:' but does work with 'C:\'
            if (dirOrfilePath.Length < 2)
                return dirOrfilePath;

            if (dirOrfilePath.Length == 2)
            {
                if (dirOrfilePath[dirOrfilePath.Length - 1] == ':')
                    return dirOrfilePath + System.IO.Path.DirectorySeparatorChar;
            }

            if (dirOrfilePath.Length == 3)
            {
                if (dirOrfilePath[dirOrfilePath.Length - 2] == ':' &&
                    dirOrfilePath[dirOrfilePath.Length - 1] == System.IO.Path.DirectorySeparatorChar)
                    return dirOrfilePath;

                if (dirOrfilePath[1] == ':')
                    return "" + dirOrfilePath[0] + dirOrfilePath[1] +
                                System.IO.Path.DirectorySeparatorChar + dirOrfilePath[2];
                else
                    return dirOrfilePath;
            }

            // Insert a backslash in 3rd character position if not already present
            // C:Temp\myfile -> C:\Temp\myfile
            if (dirOrfilePath.Length >= 3)
            {
                if (char.ToUpper(dirOrfilePath[0]) >= 'A' && char.ToUpper(dirOrfilePath[0]) <= 'Z' &&
                    dirOrfilePath[1] == ':' &&
                    dirOrfilePath[2] != '\\')
                {
                    dirOrfilePath = dirOrfilePath.Substring(0, 2) + "\\" + dirOrfilePath.Substring(2);
                }
            }

            // This will normalize directory and drive references into 'C:' or 'C:\Temp'
            if (dirOrfilePath[dirOrfilePath.Length - 1] == System.IO.Path.DirectorySeparatorChar)
                dirOrfilePath = dirOrfilePath.Trim(System.IO.Path.DirectorySeparatorChar);

            return dirOrfilePath;
        }

        /// <summary>
        /// Tries to determine whether there is a known folder associated with this
        /// path or not.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IKnownFolderProperties FindKnownFolderByFileSystemPath(string path)
        {
            if (KnownFileSystemFolders.Count == 0)
                LoadAllKFs();

            IKnownFolderProperties matchedItem = null;
            KnownFileSystemFolders.TryGetValue(path.ToUpper(), out matchedItem);

            return matchedItem;
        }

        /// <summary>
        /// Gets a strongly-typed collection of all registered known folders that have
        /// an associated file system path.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<Guid, IKnownFolderProperties> GetAllKFs()
        {
            // Should this method be thread-safe?? (It'll take a while
            // to get a list of all the known folders, create the managed wrapper
            // and return the read-only collection.
            var pathList = new Dictionary<Guid, IKnownFolderProperties>();
            uint count;
            IntPtr folders = IntPtr.Zero;

            try
            {
                KnownFolderManagerClass knownFolderManager = new KnownFolderManagerClass();
                var result = knownFolderManager.GetFolderIds(out folders, out count);

                if (count > 0 && folders != IntPtr.Zero)
                {
                    // Loop through all the KnownFolderID elements
                    for (int i = 0; i < count; i++)
                    {
                        // Read the current pointer
                        IntPtr current = new IntPtr(folders.ToInt64() + (Marshal.SizeOf(typeof(Guid)) * i));

                        // Convert to Guid
                        Guid knownFolderID = (Guid)Marshal.PtrToStructure(current, typeof(Guid));

                        if (_filterKF.Contains(knownFolderID))
                            continue;

                        try
                        {
                            using (var nativeKF = KnownFolderHelper.FromKnownFolderGuid(knownFolderID))
                            {
                                var kf = KnownFolderHelper.GetFolderProperties(nativeKF.Obj);

                                // Add to our collection if it's not null (some folders might not exist on the system
                                // or we could have an exception that resulted in the null return from above method call
                                if (kf != null)
                                {
                                    pathList.Add(kf.FolderId, kf);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                if (folders != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(folders);
            }

            return pathList;
        }

        private static void LoadAllKFs()
        {
            if (LocalKFs.Count == 0)
            {
                LocalKFs = GetAllKFs();

                foreach (var item in LocalKFs.Values)
                {
                    // Make known FolderId and SpecialParseNameId available in one collection
                    KnownFileSystemFolders.Add(item.FolderId.ToString("B").ToUpper(), item);

                    KnownFileSystemFolders.Add(KF_IID.IID_Prefix + item.FolderId.ToString("B").ToUpper(), item);

                    if (Browser2.IsTypeOf(item.ParsingName) == PathType.SpecialFolder)
                    {
                        KnownFileSystemFolders.Add(item.ParsingName.ToUpper(), item);

                        if (string.IsNullOrEmpty(item.RelativePath) == false &&
                            string.IsNullOrEmpty(item.ParsingName) == false)
                        {
                            int idx = item.ParsingName.LastIndexOf("\\");
                            if (idx > 0)
                            {
                                string relPath = item.ParsingName.Substring(0, idx);
                                relPath += "\\" + item.RelativePath;
                                KnownFileSystemFolders.Add(relPath.ToUpper(), item);
                            }
                        }

                    }

                    if (string.IsNullOrEmpty(item.Path) == false)
                    {
                        try
                        {
                            // It is possible to have more than one known folder point at one
                            // file system location - but this implementation still handles
                            // unique file locations and associated folders
                            IKnownFolderProperties val = null;
                            if (KnownFileSystemFolders.TryGetValue(item.Path.ToUpper(), out val) == false)
                                KnownFileSystemFolders.Add(item.Path.ToUpper(), item);
                        }
                        catch
                        {
                            // swallow errors beyond this point
                        }
                    }
                }
            }
        }
        #endregion methods
    }
}
