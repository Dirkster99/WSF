namespace UnitTestWSF
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WSF.Enums;
    using System.Reflection;
    using WSF;

    [TestClass]
    public class ZipFilesTests2
    {
        /// <summary>
        /// Gets the location of a zip file to support unit tests with it.
        /// </summary>
        /// <returns></returns>
        private string GetTestFileLocation()
        {
            string assemplyPath = Assembly.GetExecutingAssembly().Location;
            string assemplyDir = System.IO.Path.GetDirectoryName(assemplyPath);

            return System.IO.Path.Combine(assemplyDir, "Resources\\New folder.zip");
        }

        /// <summary>
        /// Attempt to browse the directory content of a zip file stored in the
        /// Resources section of this unit test project.
        /// </summary>
        [TestMethod]
        public void TestOpenZipFile()
        {
            string resourcePath = GetTestFileLocation();

            Assert.IsTrue(System.IO.File.Exists(resourcePath));

            var zipFile = Browser2.Create(resourcePath);

            Assert.IsTrue(zipFile != null);
            Assert.IsTrue((zipFile.ItemType & DirectoryItemFlags.DataFileContainer) != 0);
            Assert.IsTrue((zipFile.ItemType & DirectoryItemFlags.FileSystemFile) != 0);
            //Assert.IsTrue(zipFile.PathType == PathHandler.FileSystem);

            Assert.IsTrue(zipFile.Name == "New folder.zip");

            Assert.IsTrue(zipFile.PathFileSystem == zipFile.PathShell);
            Assert.IsTrue(string.Compare(zipFile.PathFileSystem, resourcePath, true) == 0);
        }
    }
}
