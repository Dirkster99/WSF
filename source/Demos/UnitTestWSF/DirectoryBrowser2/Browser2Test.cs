namespace UnitTestWSF.DirectoryBrowser2
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WSF;
    using WSF.Enums;
    using WSF.IDs;
    using WSF.Interfaces;
    using System.Linq;
    using System.Collections.Generic;

    [TestClass]
    public class Browser2Test
    {
        /// <summary>
        /// Testing SysDefault with DirectoryExists in ShellBrowser
        /// </summary>
        [TestMethod]
        public void TestSysDefault()
        {
            var dir = Browser2.SysDefault;

            Assert.IsTrue(dir != null);

            Assert.IsTrue(Browser2.IsTypeOf(dir.PathFileSystem) == PathType.FileSystemPath);

            IDirectoryBrowser2[] pathItems;
            Assert.IsTrue(Browser2.DirectoryExists(dir.PathFileSystem, out pathItems));

            Assert.IsTrue(pathItems != null);
            Assert.IsTrue(pathItems.Length > 0);
        }

        /// <summary>
        /// Tests whether Shellbrowser can parse a Windows Shell Path (sequence of item names)
        /// and whether its existance can be verified based on a string path.
        /// </summary>
        [TestMethod]
        public void TestWinShellPath()
        {
            IDirectoryBrowser2 libraries = Browser2.Create(KF_IID.ID_FOLDERID_Libraries);
            IDirectoryBrowser2 music = null;
            Assert.IsTrue(libraries != null);

            foreach (var item in Browser2.GetChildItems(libraries.SpecialPathId))
            {
                // This test fails if folder was not resolved as being a known folder
                // All items under libraries (UsersLibrariesFolder) should be knownfolders
                // (CameraRollLibrary, DocumentsLibrary, MusicLibrary, PicturesLibrary, SavedPicturesLibrary, VideosLibrary)
                Assert.IsTrue(item.KnownFolder != null);

                if (string.Compare(item.SpecialPathId, KF_ParseName_IID.MusicLibrary, true) == 0)
                    music = item;
            }

            Assert.IsTrue(music != null);

            // 
            // String evaluates to "Libraries\Music" (item.LocalizedName) on an English system
            string WindowsShellPath = string.Format("{0}\\{1}", libraries.Name, music.Name);

            // Check if we can retrieve items for this through dedicated method
            IDirectoryBrowser2[] winPath = Browser2.GetWinShellPathItems(WindowsShellPath);

            Assert.IsTrue(winPath != null);
            Assert.IsTrue(winPath.Length == 2);

            Assert.IsTrue(Browser2.IsTypeOf(WindowsShellPath) == PathType.WinShellPath);

            // Check to see if we can retrieve items for this through DirectoryExists
            IDirectoryBrowser2[] pathItems;
            bool exists = Browser2.DirectoryExists(WindowsShellPath, out pathItems);

            Assert.IsTrue(pathItems != null);
            Assert.IsTrue(pathItems.Length == 2);
        }

        [TestMethod]
        public void TestWinShellPath2()
        {
            IDirectoryBrowser2 userFiles = Browser2.Create(KF_IID.ID_FOLDERID_UsersFiles);
            Assert.IsTrue(userFiles != null);

            HashSet<string> KFs = new HashSet<string>();

            KFs.Add(KF_ParseName_IID.Searches.ToUpper());
            KFs.Add(KF_ParseName_IID.Links.ToUpper());
            KFs.Add(KF_ParseName_IID.OneDrive.ToUpper());
            KFs.Add(KF_ParseName_IID.Contacts.ToUpper());
            KFs.Add(KF_ParseName_IID.My_Pictures.ToUpper());
            KFs.Add(KF_ParseName_IID.My_Videos.ToUpper());
            KFs.Add(KF_ParseName_IID.My_Music.ToUpper());
            KFs.Add(KF_ParseName_IID.Downloads.ToUpper());
            KFs.Add(KF_ParseName_IID.Personal.ToUpper());
            KFs.Add(KF_ParseName_IID.SavedGames.ToUpper());

            foreach (var item in Browser2.GetChildItems(userFiles.SpecialPathId))
            {
                // This test fails if folder was not resolved as being a known folder
                // All items under libraries (UsersLibrariesFolder) should be knownfolders
                if (item.KnownFolder != null)
                {
                    if (string.IsNullOrEmpty(item.KnownFolder.ParsingName) == false)
                    {
                        string search = item.KnownFolder.ParsingName.ToUpper();
                        bool bFound = false;

                        bFound = KFs.Contains(search);

                        // If this tests fails it might just mean that we've encountered a
                        // knownfolder that was not installed and running at the time this was written
                        // So, failing this test may not be a big deal
                        // (it really depends on the folder - is it listed above (? extend list) or not)
                        Assert.IsTrue(bFound);
                    }
                }
            }
        }

        /// <summary>
        /// Tests whether Shellbrowser can parse a Paths with '\' separators
        /// in normalized form and non-normalized form.
        /// </summary>
        [TestMethod]
        public void TestGetDirectories()
        {
            string testRefPath = @"c:\windows\win32";
            string testPath1 = @"c:windows\win32";

            // Test Path normalization
            var normPath = Browser2.NormalizePath(testPath1);
            Assert.IsTrue(string.Compare(testRefPath, normPath, true) == 0);

            // Test getting directories for normalized and unnormalized paths
            var dirNormItems = Browser2.GetDirectories(normPath);
            Assert.IsTrue(dirNormItems.Length == 3);

            var dirItems = Browser2.GetDirectories(testPath1);
            Assert.IsTrue(dirItems.Length == 3);

            for (int i = 0; i < dirItems.Length; i++)
            {
                Assert.IsTrue(string.Compare(dirItems[i], dirNormItems[i], true) == 0);
            }
        }

        [TestMethod]
        public void TestGetFileSystemPathItems()
        {
            var windowsDir = Browser2.Create(KF_IID.ID_FOLDERID_Windows);
            Assert.IsTrue( windowsDir.DirectoryPathExists() );

            // Test getting directories for normalized and unnormalized paths
            var dirWindowsStringItems = Browser2.GetDirectories(windowsDir.PathFileSystem);

            var dirWindowsItems = Browser2.GetFileSystemPathItems(windowsDir.PathFileSystem);

            Assert.IsTrue(dirWindowsItems.Length == dirWindowsStringItems.Length);

            for (int i = 0; i < dirWindowsItems.Length; i++)
            {
                // The name of a drive can be its label (eg name of 'c:\' drive may be 'Windows')
                // So, we need to explicitely evaluate the PathFileSystem to compare this to strings
                var dirs = Browser2.GetDirectories(dirWindowsItems[i].PathFileSystem);

                Assert.IsTrue(string.Compare(dirs[dirs.Length-1],
                                             dirWindowsStringItems[i], true) == 0);
            }
        }

        /// <summary>
        /// Test whether a string path can be rooted under desktop or ThisPC item.
        /// </summary>
        [TestMethod]
        public void TestFindRoot()
        {
            var sysDefault = Browser2.SysDefault;

            IDirectoryBrowser2[] pathItems;
            bool exists = Browser2.DirectoryExists(sysDefault.PathFileSystem, out pathItems);

            Assert.IsTrue(pathItems != null);
            bool pathIsRouted = false;
            var rootedPath = Browser2.FindRoot(pathItems, sysDefault.PathFileSystem, out pathIsRouted);

            Assert.IsTrue(pathIsRouted);

            // Expectation:
            // The rooted Path should contain 'ThisPC' + drive
            // while the sysDefault should be just a drive 'C:\'
            Assert.IsTrue(pathItems.Length < rootedPath.Length);
        }

        /// <summary>
        /// Tests an alternative way for re-rooting items into the windows shell system.
        /// </summary>
        [TestMethod]
        public void TestPathItemsAsIdList()
        {
            var windowsDir = Browser2.Create(KF_IID.ID_FOLDERID_Windows);
            Assert.IsTrue(windowsDir.DirectoryPathExists());

            var items = Browser2.PathItemsAsIdList(windowsDir);

            IDirectoryBrowser2[] pathItems;
            bool exists = Browser2.DirectoryExists(windowsDir.PathFileSystem, out pathItems);

            Assert.IsTrue(items != null);
            Assert.IsTrue(items.Count > 0);

            foreach (var item in items)
            {
                var dirItem = Browser2.Create(item);
                Assert.IsTrue(dirItem != null);
            }

            // Expectation:
            // The rooted Path should contain 'ThisPC' + drive
            // while the sysDefault should be just a drive 'C:\'
            Assert.IsTrue(pathItems.Length < items.Count);

            // See if FindRoot agrees with what we got
            var rootedItems = Browser2.FindRoot(windowsDir);
            Assert.IsTrue(rootedItems.Length == items.Count);
        }

        /// <summary>
        /// Tests whether a given directory browser object can be converted into a
        /// sequence of parse names to describe the orginal location.
        /// </summary>
        [TestMethod]
        public void TestPathItemsAsParseNames()
        {
            var windowsDir = Browser2.Create(KF_IID.ID_FOLDERID_Windows);
            Assert.IsTrue(windowsDir.DirectoryPathExists());

            var browseItems = Browser2.PathItemsAsIdList(windowsDir);
            var items = Browser2.PathItemsAsParseNames(windowsDir);

            // Expectation: Both lists hold the same path using a different format
            // So, there length should be equal
            Assert.IsTrue(items != null);
            Assert.IsTrue(items.Count > 0);
            Assert.IsTrue(items.Count == browseItems.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var dirItem = Browser2.Create(items[i]);

                Assert.IsTrue(dirItem != null);
            }
        }

        /// <summary>
        /// Test if a file system parent of a file system path can be determined.
        /// </summary>
        [TestMethod]
        public void TestIsParentOf()
        {
            var windowsDir = Browser2.Create(KF_IID.ID_FOLDERID_Windows);
            Assert.IsTrue(windowsDir.DirectoryPathExists());

            var dirStrings = Browser2.GetDirectories(windowsDir.PathFileSystem);

            string pathExt;
            Assert.IsTrue(Browser2.IsParentPathOf(dirStrings[0], windowsDir.PathFileSystem,
                                                      out pathExt));

            // Alternative 1) way to check for parent relationship between 2 paths
            var result = Browser2.IsCurrentPath(dirStrings[0], windowsDir.PathFileSystem);
            Assert.IsTrue(result == PathMatch.PartialSource);

            // Alternative 2) way to check for parent relationship between 2 paths
            result = Browser2.IsCurrentPath(windowsDir.PathFileSystem, dirStrings[0]);
            Assert.IsTrue(result == PathMatch.PartialTarget);

            // Check if 2 paths are the same
            result = Browser2.IsCurrentPath(windowsDir.PathFileSystem, windowsDir.PathFileSystem);
            Assert.IsTrue(result == PathMatch.CompleteMatch);

            // Check if 2 paths are completely unrelated
            result = Browser2.IsCurrentPath(@"X:\Data\MyPath", @"Y:\Data\MyPath");
            Assert.IsTrue(result == PathMatch.Unrelated);
        }

        /// <summary>
        /// Test if we can join two paths based on <see cref="IDirectoryBrowser"/> and
        /// string path representation.
        /// </summary>
        [TestMethod]
        public void TestFindCommonRoot()
        {
            var windowsDir = Browser2.Create(KF_IID.ID_FOLDERID_Windows);
            Assert.IsTrue(windowsDir.DirectoryPathExists());

            var dirStrings = Browser2.GetDirectories(windowsDir.PathFileSystem);

            string pathExt;
            Assert.IsTrue(Browser2.IsParentPathOf(dirStrings[0], windowsDir.PathFileSystem,
                                                      out pathExt));

            // Alternative 1) way to check for parent relationship between 2 paths
            var result = Browser2.IsCurrentPath(dirStrings[0], windowsDir.PathFileSystem);
            Assert.IsTrue(result == PathMatch.PartialSource);

            var currentPath = Browser2.FindRoot(windowsDir);

            var items = Browser2.FindCommonRoot(currentPath, dirStrings[0], out pathExt);

            Assert.IsTrue(items > 0);
            Assert.IsTrue(pathExt == string.Empty);

            var dirstringsItems = Browser2.FindRoot(Browser2.Create(dirStrings[0]));

            items = Browser2.FindCommonRoot(dirstringsItems, windowsDir.PathFileSystem , out pathExt);

            Assert.IsTrue(items > 0);
            Assert.IsTrue(string.IsNullOrEmpty(pathExt) == false);

            var dirstringsList = dirstringsItems.ToList();

            bool bresult = Browser2.ExtendPath(ref dirstringsList, pathExt);

            Assert.IsTrue(bresult);
            Assert.IsTrue(dirstringsItems.Length < dirstringsList.Count);
        }
    }
}
