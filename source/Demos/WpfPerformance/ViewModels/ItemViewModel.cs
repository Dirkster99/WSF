namespace WpfPerformance.ViewModels
{
    using System;
    using System.Threading.Tasks;
    using WSF.Interfaces;

    public class ItemViewModel : Base.ViewModelBase, IItemViewModel
    {
        #region fields
        private readonly IDirectoryBrowser _ModelStage1;
        private DateTime _lastRefreshTimeUtc;
        private bool _isLoaded;
        private bool _IsLoading;
        private readonly int _ID;
        #endregion fields

        #region ctors
        /// <summary>
        /// parameterized class constructor
        /// </summary>
        public ItemViewModel(IDirectoryBrowser model, int id)
            : this()
        {
            this._ModelStage1 = model;
            this._ID = id;
        }

        /// <summary>
        /// Hidden class constructor
        /// </summary>
        protected ItemViewModel()
        {
        }
        #endregion ctors

        #region properties
        /// <summary>
        /// Gets a unqie ID within the collection.
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
        }

        /// <summary>
        /// Get Known FolderId or file system Path for this folder.
        /// 
        /// That is:
        /// 1) A knownfolder GUID (if it exists) is shown
        ///    here as default preference over
        ///    
        /// 2) A storage location (if it exists) in the filesystem
        /// </summary>
        public string ItemPath
        {
            get
            {
                if (_ModelStage1 == null)
                    return string.Empty;

                if (string.IsNullOrEmpty(_ModelStage1.SpecialPathId) == false)
                    return _ModelStage1.SpecialPathId;

                return _ModelStage1.PathFileSystem;
            }
        }

        /// <summary>
        /// Gets the name of this folder (without its root path component).
        /// </summary>
        public string ItemName
        {
            get
            {
                if (IsLoaded)
                {
                    if (_ModelStage1 != null)
                    {
                        if (_ModelStage1.KnownFolder != null)
                        {
                            if (string.IsNullOrEmpty(_ModelStage1.KnownFolder.LocalizedName) == false)
                                return _ModelStage1.KnownFolder.LocalizedName;
                        }

                        return _ModelStage1.Name;
                    }
                }
                else
                {
                    return _ModelStage1.Name;
                }

                return string.Empty;
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
                if (_ModelStage1 != null)
                    return _ModelStage1.IconResourceId;

                return null;
            }
        }

        /// <summary>
        /// Gets whether current items are already loaded or not.
        /// </summary>
        public bool IsLoading
        {
            get
            {
                return _IsLoading;
            }

            private set
            {
                if (_IsLoading != value)
                {
                    _IsLoading = value;
                    NotifyPropertyChanged(() => IsLoading);
                }
            }
        }

        /// <summary>
        /// Gets whether current items are already loaded or not.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }

            private set
            {
                if (_isLoaded != value)
                {
                    _isLoaded = value;
                    NotifyPropertyChanged(() => IsLoaded);
                }
            }
        }

        /// <summary>
        /// Gets the Coordinated Universal Time (UTC) of the last load processing
        /// at the ALL/AllNonBindable items collection.
        /// </summary>
        public DateTime LastRefreshTimeUtc
        {
            get
            {
                return _lastRefreshTimeUtc;
            }

            private set
            {
                if (_lastRefreshTimeUtc != value)
                {
                    _lastRefreshTimeUtc = value;
                    NotifyPropertyChanged(() => IsLoaded);
                }
            }
        }
        #endregion properties

        #region methods
        public Task<bool> LoadModel()
        {
            return Task.Run<bool>(() =>
            {
                IsLoaded = false;
                IsLoading = true;
                try
                {
                    if (_ModelStage1.IsFullyInitialized == false)
                    {
                        _ModelStage1.LoadProperties();

                        NotifyPropertyChanged(() => ItemName);
                        NotifyPropertyChanged(() => ItemPath);
                        NotifyPropertyChanged(() => IconResourceId);

                        LastRefreshTimeUtc = DateTime.Now;
                        System.Console.WriteLine("Model {0} loaded.", _ID);
                    }
                }
                catch
                {
                    return false;
                }
                finally
                {
                    IsLoading = false;
                    IsLoaded = true;
                }

                return IsLoaded;
            });
        }

        public Task<bool> UnlodLoadModel()
        {
            return Task.Run<bool>(() =>
            {
                if (IsLoaded == true)
                {
                    IsLoading = true;
                    try
                    {
                        Console.WriteLine("Simulating UnloadModel in id: {0}", _ID);
                    }
                    finally
                    {
                        IsLoaded = false;
                        IsLoading = false;
                    }

                    return true;
                }

                return false;
            });
        }
        #endregion methods
    }
}
