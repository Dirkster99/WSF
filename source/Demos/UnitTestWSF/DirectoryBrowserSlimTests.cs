namespace UnitTestWSF
{
/***
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WSF;
    using WSF.IDs;
    using WSF.Shell.Enums;
    using WSF.Shell.Interop.Interfaces.KnownFolders;
    using WSF.Shell.Interop.Knownfolders;
    using WSF.Shell.Pidl;

    [TestClass]
    public class DirectoryBrowserSlimTests
    {
        [TestMethod]
        public void CanGetDesktopChildFolders()
        {
            var Items = Browser.GetChildItems(KF_IID.ID_FOLDERID_Desktop).ToList();
            Items.Sort((x, y) => string.Compare(x.Name, y.Name, true));

            var SlimItems = Browser.GetSlimChildItems(KF_IID.ID_FOLDERID_Desktop).ToList();
            SlimItems.Sort((x, y) => string.Compare(x.LabelName, y.LabelName, true));

            for (int i = 0; i < SlimItems.Count; i++)
            {
                Assert.IsTrue(string.Compare(SlimItems[i].Name, Items[i].Name) == 0);
            }

            KnownFolderManagerClass knownFolderManager = new KnownFolderManagerClass();

            for (int i = 0; i < SlimItems.Count; i++)
            {
                Assert.IsTrue(string.Compare(SlimItems[i].Name, Items[i].Name) == 0);

                if (string.IsNullOrEmpty(SlimItems[i].SpecialParseNameId) == false)
                {
                    IntPtr fullpidl = PidlManager.IdListToPidl(SlimItems[i].idListFullItem);
                    try
                    {
                        IKnownFolderNative iknownFolder;
                        HRESULT hr = knownFolderManager.FindFolderFromIDList(fullpidl, out iknownFolder);

                        string knownFolderId = null;
                        if (hr == HRESULT.S_OK)
                            knownFolderId = string.Format("{0}{1}{2}", "::{", iknownFolder.GetId(), "}");

                        if (Items[i].SpecialPathId != null && knownFolderId != null)
                            Assert.IsTrue(string.Compare(Items[i].SpecialPathId, knownFolderId, true) == 0);
                        else
                            Assert.IsTrue(Items[i].SpecialPathId == null && knownFolderId == null);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(fullpidl);
                    }
                }
            }
        }

        [TestMethod]
        public void CanGetThisPCChildFolders()
        {
            var Items = Browser.GetChildItems(KF_IID.ID_FOLDERID_ComputerFolder).ToList();
            Items.Sort((x, y) => string.Compare(x.Name, y.Name, true));

            var SlimItems = Browser.GetSlimChildItems(KF_IID.ID_FOLDERID_ComputerFolder).ToList();
            SlimItems.Sort((x, y) => string.Compare(x.LabelName, y.LabelName, true));

            for (int i = 0; i < SlimItems.Count; i++)
            {
                Assert.IsTrue(string.Compare(SlimItems[i].Name, Items[i].Name) == 0);
            }
        }
    }
***/
}
