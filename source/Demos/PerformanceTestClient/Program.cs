namespace PerformanceTestClient
{
    using WSF;
    using WSF.IDs;
    using WSF.Interfaces;
    using System;
    using System.Collections.Generic;
    using WSF.Enums;

    class Program
    {
        protected Program()
        {
        }

        static void Main(string[] args)
        {
            Console.WriteLine("\n--> Retrieving all sub-directories WITHOUT lazily loading properties:");
            TestAllWinSxsFoldersRetrieval(false);

            Console.WriteLine("\n");
            Console.WriteLine("\n--> Retrieving all sub-directories WITH lazily loading properties:");
            TestAllWinSxsFoldersRetrieval(true);

            Console.ReadKey();
        }

        private static void TestAllWinSxsFoldersRetrieval(bool lazyLoadingProperties)
        {
            var windowsFolder = Browser.Create(KF_IID.ID_FOLDERID_Windows);

            string dirPath = System.IO.Path.Combine(windowsFolder.PathFileSystem, "WinSxs");

            Console.WriteLine("Retrieving all sub-directories from '{0}'...\n", dirPath);

            // List all known folders
            var startTime = DateTime.Now;
            Console.WriteLine("...{0} working on it...\n", startTime);

            List<IDirectoryBrowser> result = new List<IDirectoryBrowser>();
            int i = 0;
            foreach (var item in Browser.GetChildItems(dirPath, null, SubItemFilter.NameOnly, lazyLoadingProperties))
            {
                result.Add(item);
                i++;

                if ((i % 1000) == 0)        // print a little progress indicator
                    Console.Write(".");
            }

            // List all known folders
            var endTime = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("{0} Done retrieving {1} entries.\n", endTime, result.Count);
            Console.WriteLine("After {0:n2} minutes or {1:n2} seconds.\n",
                (endTime - startTime).TotalMinutes, (endTime - startTime).TotalSeconds);
        }
    }
}
