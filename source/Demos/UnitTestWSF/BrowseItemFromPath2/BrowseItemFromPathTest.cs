namespace UnitTestWSF.BrowseItemFromPath
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using WSF.Browse;
    using WSF.Enums;
    using WSF.IDs;
    using WSF.Shell.Interop.Knownfolders;

    [TestClass]
    public class BrowseItemFromPathTest
    {
        /// <summary>
        /// Verify that we can build a <see cref="BrowseItemFromPath"/> object
        /// for a default drive.
        /// 
        /// Note that the properties SpecialPathId and PathFileSystem are expected to
        /// contain different path information.
        /// </summary>
        [TestMethod]
        public void GetDefaultDrive()
        {
            // Get the default drive's path
            var drivePath = new System.IO.DirectoryInfo(Environment.SystemDirectory).Root.Name;
            var driveInfoPath = new System.IO.DriveInfo(drivePath);

            Assert.IsTrue(drivePath != null);

            var drive = BrowseItemFromPath.InitItem(drivePath);
////            drive.LoadProperties();

            Assert.IsTrue(drive != null);

            Assert.IsTrue(drivePath.Equals(drive.PathFileSystem, StringComparison.InvariantCulture));

            Assert.IsFalse(string.IsNullOrEmpty(drive.Name));
            //Assert.IsTrue(string.Compare(drive.Name, driveInfoPath.VolumeLabel, true) == 0);
            Assert.IsTrue(string.Compare(drive.ParseName, driveInfoPath.Name, true) == 0);
            Assert.IsFalse(string.IsNullOrEmpty(drive.LabelName));

            Assert.IsFalse(drive.IsSpecialParseItem);
            Assert.IsTrue(string.IsNullOrEmpty(drive.PathSpecialItemId));

            Assert.IsTrue(string.Compare(drive.ParseName, drive.PathFileSystem, true) == 0);

////            Assert.IsTrue((drive.ItemType & DirectoryItemFlags.FileSystemDirectory) != 0);
////            Assert.IsTrue((drive.ItemType & DirectoryItemFlags.Drive) != 0);

////            Assert.IsTrue(drive.ParentIdList.Size > 0);
////            Assert.IsTrue(drive.ChildIdList.Size > 0);

////            Assert.IsFalse(string.IsNullOrEmpty(drive.IconResourceId));
        }

        /// <summary>
        /// Verify that we can build a <see cref="BrowseItemFromPath"/> object
        /// for a special directory.
        /// 
        /// Note that the properties SpecialPathId and PathFileSystem are expected to
        /// contain different path information.
        /// 
        /// Use the PathShell property if you want to navigate to parents
        /// or children and keep a system wide unique reference.
        /// </summary>
        [TestMethod]
        public void GetDirectory()
        {
            string dirPath = null;

            using (var kf = KnownFolderHelper.FromCanonicalName("Windows"))
            {
                Assert.IsTrue(kf != null);
                dirPath = kf.GetPath();
            }

            Assert.IsTrue(dirPath != null);

            // Lets test the directory browser object with that path
            var dir = BrowseItemFromPath.InitItem(dirPath);
////            dir.LoadProperties();

            Assert.IsTrue(dir != null);

            Assert.IsTrue(dir.IsSpecialParseItem == false);
            Assert.IsTrue(string.IsNullOrEmpty(dir.PathSpecialItemId));
////            Assert.IsTrue(dir.KnownFolder != null);

            // This item should have the Guid of the 'Windows' special folder
            var dirFolderGuid = new Guid(KF_ID.ID_FOLDERID_Windows);
////            Assert.IsTrue(dirFolderGuid == dir.KnownFolder.FolderId);
            Assert.IsTrue(string.Compare(dirPath, dir.PathFileSystem, true) == 0);

            Assert.IsFalse(string.IsNullOrEmpty(dir.Name));
            Assert.IsTrue(string.Compare(dir.Name, dir.LabelName) == 0);
            Assert.IsTrue(string.Compare(dir.ParseName, dir.PathFileSystem) == 0);

////            Assert.IsTrue((dir.ItemType & DirectoryItemFlags.FileSystemDirectory) != 0);

            // Not Special as far as parsing name is concerned but a KnownFolder it is.
////            Assert.IsTrue((dir.ItemType & DirectoryItemFlags.Special) == 0);

////            Assert.IsTrue(dir.ParentIdList.Size > 0);
////            Assert.IsTrue(dir.ChildIdList.Size > 0);

////            Assert.IsFalse(string.IsNullOrEmpty(dir.IconResourceId));
        }

////        [TestMethod]
////        public void TestPathShell()
////        {
////            var dir = Browser.DesktopDirectory;
////
////            Assert.IsTrue(string.IsNullOrEmpty(dir.PathShell) == false);
////            Assert.IsTrue(string.Compare(dir.SpecialPathId, dir.PathShell, true) == 0);
////
////            var sysDefault = Browser.SysDefault;
////            Assert.IsTrue(string.IsNullOrEmpty(sysDefault.PathShell) == false);
////            Assert.IsTrue(string.Compare(sysDefault.PathFileSystem, sysDefault.PathShell, true) == 0);
////
////        }
////
////        [TestMethod]
////        public void TestFullName()
////        {
////            var dir = Browser.DesktopDirectory;
////
////            Assert.IsTrue(string.IsNullOrEmpty(dir.FullName) == false);
////            Assert.IsTrue(string.Compare(dir.FullName, dir.PathFileSystem, true) == 0);
////
////            var sysDefault = Browser.SysDefault;
////            Assert.IsTrue(string.IsNullOrEmpty(sysDefault.FullName) == false);
////            Assert.IsTrue(string.Compare(sysDefault.PathFileSystem, sysDefault.FullName, true) == 0);
////
////            var thisPC = Browser.MyComputer;
////            Assert.IsTrue(string.IsNullOrEmpty(thisPC.FullName) == false);
////            Assert.IsTrue(string.Compare(thisPC.FullName, thisPC.SpecialPathId, true) == 0);
////        }

        /// <summary>
        /// Verify that we can build a <see cref="BrowseItemFromPath"/> object
        /// for a special known folder item that does NOT have a drirectory in
        /// in the file system.
        /// 
        /// Note that the properties SpecialPathId and PathFileSystem are expected to
        /// contain different path information.
        /// 
        /// Use the PathShell property if you want to navigate to parents
        /// or children and keep a system wide unique reference.
        /// </summary>
        [TestMethod]
        public void GetThisPC()
        {
            string id = null;

            using (var kf = KnownFolderHelper.FromCanonicalName("MyComputerFolder"))
            {
                Assert.IsTrue(kf != null);
                id = kf.GetId();
            }

            Assert.IsTrue(id != null);        // file system representation

            // Lets test the directory browser object with that path
            var specialItem = BrowseItemFromPath.InitItem(KF_ParseName_IID.MyComputerFolder);
////            specialItem.LoadProperties();

            Assert.IsTrue(specialItem != null);

            Assert.IsTrue(specialItem.IsSpecialParseItem == true);
            Assert.IsTrue(string.IsNullOrEmpty(specialItem.PathSpecialItemId) == false);

////            Assert.IsTrue(specialItem.KnownFolder != null);
////            Assert.IsTrue(string.Compare(KF_ParseName_IID.MyComputerFolder, specialItem.KnownFolder.ParsingName, true) == 0);
////            Assert.IsTrue(string.Compare(specialItem.ParseName, specialItem.KnownFolder.ParsingName, true) == 0);

            // This item should be the Guid of the 'This PC' special folder
////            Assert.IsTrue(specialItem.KnownFolder.FolderId == new Guid(KF_ID.ID_FOLDERID_ComputerFolder));

            Assert.IsTrue(string.IsNullOrEmpty(specialItem.PathFileSystem));

            Assert.IsFalse(string.IsNullOrEmpty(specialItem.Name));
            Assert.IsTrue(string.Compare(specialItem.LabelName, specialItem.Name) == 0);

////            Assert.IsTrue((specialItem.ItemType & DirectoryItemFlags.FileSystemDirectory) == 0);
////            Assert.IsTrue((specialItem.ItemType & DirectoryItemFlags.SpecialDesktopFileSystemDirectory) != 0);

            // Not Special as far as parsing name is concerned but a KnownFolder it is.
////            Assert.IsTrue((specialItem.ItemType & DirectoryItemFlags.Special) != 0);

////            Assert.IsTrue(specialItem.ParentIdList.Size == 0);
////            Assert.IsTrue(specialItem.ChildIdList.Size > 0);

////            Assert.IsFalse(string.IsNullOrEmpty(specialItem.IconResourceId));
        }

        /// <summary>
        /// Verify that we can build a <see cref="IDirectoryBrowser"/> object
        /// for a special known folder item that does HAVE a drirectory in
        /// in the file system.
        /// 
        /// Note that the properties SpecialPathId and PathFileSystem are expected to
        /// contain different path information.
        /// 
        /// Use the PathShell property if you want to navigate to parents
        /// or children and keep a system wide unique reference.
        /// </summary>
        [TestMethod]
        public void GetMusic()
        {
            var music = BrowseItemFromPath.InitItem(KF_IID.ID_FOLDERID_Music);
////            music.LoadProperties();

            Assert.IsTrue(music != null);
////            Assert.IsFalse(string.IsNullOrEmpty(music.IconResourceId));
////            Assert.IsTrue(music.IconResourceId.IndexOf(',') > 0);

            Assert.IsFalse(string.IsNullOrEmpty(music.Name));
            Assert.IsFalse(string.IsNullOrEmpty(music.PathFileSystem));

            Assert.IsTrue(music.IsSpecialParseItem);
            Assert.IsTrue(string.Compare(music.PathSpecialItemId, KF_IID.ID_FOLDERID_Music, true) == 0);

////            Assert.IsTrue(music.KnownFolder != null);
////            Assert.IsTrue(string.Compare(music.PathSpecialItemId, music.KnownFolder.ParsingName, true) != 0);
////            Assert.IsTrue(music.KnownFolder.FolderId == new Guid(KF_ID.ID_FOLDERID_Music));

////            Assert.IsTrue((music.ItemType & DirectoryItemFlags.Special) != 0);
////            Assert.IsTrue((music.ItemType & DirectoryItemFlags.FileSystemDirectory) != 0);

////            Assert.IsTrue(music.ParentIdList != null);
////            Assert.IsTrue(music.ParentIdList.Size >= 1);

////            Assert.IsTrue(music.ChildIdList != null);
////            Assert.IsTrue(music.ParentIdList.Size >= 1);
        }

/***

        [TestMethod]
        public void GetDriveEquality()
        {
            // Get the default drive's path
            var drivePath = new DirectoryInfo(Environment.SystemDirectory).Root.Name;
            var driveInfoPath = new System.IO.DriveInfo(drivePath);

            Assert.IsTrue(drivePath != null);

            var drive = Browser.Create(drivePath);
            var drive1 = Browser.Create(drivePath);

            Assert.IsTrue(drive != null);
            Assert.IsTrue(drive1 != null);

            Assert.IsTrue(drive.Equals(drive1));

            Assert.IsTrue(drive.ParentIdList.Equals(drive.ParentIdList));
            Assert.IsTrue(drive.ChildIdList.Equals(drive.ChildIdList));
        }

        [TestMethod]
        public void GetDirectoryEquality()
        {
            string dirPath = null;

            using (var kf = KnownFolderHelper.FromCanonicalName("Windows"))
            {
                Assert.IsTrue(kf != null);
                dirPath = kf.GetPath();
            }

            Assert.IsTrue(dirPath != null);

            // Lets test the directory browser object with that path
            var dir = Browser.Create(dirPath);
            var dir1 = Browser.Create(dirPath);

            Assert.IsTrue(dir != null);
            Assert.IsTrue(dir1 != null);

            Assert.IsTrue(dir.Equals(dir1));

            //Assert.IsTrue(dir.ParentIdList.Equals(dir1.ParentIdList));
            //Assert.IsTrue(dir.ChildIdList.Equals(dir1.ChildIdList));
        }

        [TestMethod]
        public void GetThisPCEquality()
        {
            string dirPath = null, id = null, pathShell = null;

            using (var kf = KnownFolderHelper.FromCanonicalName("MyComputerFolder"))
            {
                Assert.IsTrue(kf != null);
                dirPath = kf.GetPath();
                id = kf.GetId();
                pathShell = kf.GetPathShell();
            }

            Assert.IsTrue(dirPath == null);    // Test a special folder that has no
            Assert.IsTrue(id != null);        // file system representation
            Assert.IsTrue(pathShell != null);
            Assert.IsTrue(pathShell != id);

            // Lets test the directory browser object with that path
            var specialItem = Browser.Create(pathShell);
            var specialItem1 = Browser.Create(pathShell);

            Assert.IsTrue(specialItem != null);
            Assert.IsTrue(specialItem1 != null);

            Assert.IsTrue(specialItem.Equals(specialItem1));

            //Assert.IsTrue(specialItem.ParentIdList.Equals(specialItem1.ParentIdList));
            //Assert.IsTrue(specialItem.ChildIdList.Equals(specialItem1.ChildIdList));
        }

        [TestMethod]
        public void GetMusicEquality()
        {
            var music = Browser.Create(KF_IID.ID_FOLDERID_Music);
            var music1 = Browser.Create(KF_IID.ID_FOLDERID_Music);

            Assert.IsTrue(music != null);
            Assert.IsTrue(music1 != null);

            Assert.IsTrue(music.Equals(music1));

            //Assert.IsTrue(music.ParentIdList.Equals(music1.ParentIdList));
            //Assert.IsTrue(music.ChildIdList.Equals(music1.ChildIdList));
        }


        [TestMethod]
        public void GetInEquality()
        {
            // Get the default drive's path
            var drivePath = new DirectoryInfo(Environment.SystemDirectory).Root.Name;
            var driveInfoPath = new System.IO.DriveInfo(drivePath);
            var drive = Browser.Create(drivePath);

            // Lets test the directory browser object with that path
            string dirPath = null;
            using (var kf = KnownFolderHelper.FromCanonicalName("Windows"))
            {
                dirPath = kf.GetPath();
            }
            var dir = Browser.Create(dirPath);

            // Get This PC and Music SpecialItem
            var specialItem = Browser.Create(KF_IID.ID_FOLDERID_ComputerFolder);
            var music = Browser.Create(KF_IID.ID_FOLDERID_Music);

            Assert.IsFalse(drive.Equals(dir));
            Assert.IsFalse(drive.Equals(specialItem));
            Assert.IsFalse(drive.Equals(music));

            Assert.IsFalse(dir.Equals(drive));
            Assert.IsFalse(dir.Equals(specialItem));
            Assert.IsFalse(dir.Equals(music));

            Assert.IsFalse(specialItem.Equals(dir));
            Assert.IsFalse(specialItem.Equals(drive));
            Assert.IsFalse(specialItem.Equals(music));

            Assert.IsFalse(music.Equals(dir));
            Assert.IsFalse(music.Equals(drive));
            Assert.IsFalse(music.Equals(specialItem));
        }
***/
    }
}
