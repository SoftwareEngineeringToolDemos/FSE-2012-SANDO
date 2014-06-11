﻿using ABB.SrcML;
using ABB.SrcML.VisualStudio.SrcMLService;
using EnvDTE80;
using Microsoft.Practices.Unity;
using Sando.Core.QueryRefomers;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.SearchContracts;
using Sando.Indexer;
using Sando.Indexer.Searching.Criteria;
using Sando.Recommender;
using Sando.UI.Base;
using Sando.UI.View;
using Sando.UI.View.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Sando.UI.ViewModel
{
    public class SearchViewModel : BaseViewModel, ISearchResultListener
    {
        #region Properties

        private IndexedFile _indexedFile;
        private bool _isBrowseButtonEnabled;
        private String _searchStatus;
        private Visibility _progressBarVisibility;
        private SearchManager _searchManager;

        public ICommand AddIndexFolderCommand
        {
            get;
            set;
        }

        public ICommand RemoveIndexFolderCommand
        {
            get;
            set;
        }

        public ICommand ApplyCommand
        {
            get;
            set;
        }

        public ICommand CancelCommand
        {
            get;
            set;
        }

        public ICommand SearchCommand
        {
            get;
            set;
        }

        public ICommand ResetCommand
        {
            get;
            set;
        }

        public ObservableCollection<IndexedFile> IndexedFiles
        {
            get;
            set;
        }

        public List<IndexedFile> ModifiedIndexedFile
        {
            get;
            set;
        }

        public IndexedFile SelectedIndexedFile
        {
            get
            {
                return this._indexedFile;
            }
            set
            {
                this._indexedFile = value;
                OnPropertyChanged("SelectedIndexedFile");

                if (null != this._indexedFile)
                {
                    this.IsBrowseButtonEnabled = true;
                }
                else
                {
                    this.IsBrowseButtonEnabled = false;
                }
            }
        }

        public bool IsBrowseButtonEnabled
        {
            get
            {
                return this._isBrowseButtonEnabled;
            }
            set
            {
                this._isBrowseButtonEnabled = value;
                OnPropertyChanged("IsBrowseButtonEnabled");
            }
        }

        public Visibility ProgressBarVisibility
        {
            get
            {
                return this._progressBarVisibility;
            }
            set
            {
                this._progressBarVisibility = value;
                OnPropertyChanged("ProgressBarVisibility");
            }
        }

        public String SearchStatus
        {
            get
            {
                return this._searchStatus;
            }
            set
            {
                this._searchStatus = value;
                OnPropertyChanged("SearchStatus");
            }
        }


        public ObservableCollection<AccessWrapper> AccessLevels
        {
            get;
            set;
        }

        public ObservableCollection<ProgramElementWrapper> ProgramElements
        {
            get;
            set;
        }

        #endregion

        public SearchViewModel()
        {
            this.ModifiedIndexedFile = new List<IndexedFile>();
            this.IndexedFiles = new ObservableCollection<IndexedFile>();
            this.ProgramElements = new ObservableCollection<ProgramElementWrapper>();
            this.AccessLevels = new ObservableCollection<AccessWrapper>();

            this.AddIndexFolderCommand = new RelayCommand(AddIndexFolder);
            this.RemoveIndexFolderCommand = new RelayCommand(RemoveIndexFolder);
            this.ApplyCommand = new RelayCommand(Apply);
            this.CancelCommand = new RelayCommand(Cancel);
            this.SearchCommand = new RelayCommand(Search);
            this.ResetCommand = new RelayCommand(Reset);

            this.IsBrowseButtonEnabled = false;
            this.ProgressBarVisibility = Visibility.Collapsed;

            InitAccessLevels();
            InitProgramElements();

            this.RegisterSrcMLService();
            this.RegisterSolutionEvents();

            this._searchManager = SearchManagerFactory.GetUserInterfaceSearchManager();
            this._searchManager.AddListener(this);

            this.InitializeIndexedFile();

        }

        /// <summary>
        /// Used by Browse button in the user interface
        /// </summary>
        /// <param name="path"></param>
        public void SetIndexFolderPath(String path)
        {
            if (null != this.SelectedIndexedFile)
            {
                this.SelectedIndexedFile.FilePath = path;
                if (this.SelectedIndexedFile.OperationStatus == IndexedFile.Status.Normal)
                {
                    this.SelectedIndexedFile.OperationStatus = IndexedFile.Status.Modified;
                }

                IndexedFile file = this.ModifiedIndexedFile.Find((f) => f.GUID == this.SelectedIndexedFile.GUID);
                if (null != file)
                {
                    if(file.OperationStatus == IndexedFile.Status.Normal)
                        file.OperationStatus = IndexedFile.Status.Modified;
                }
            }
        }

        #region Command Methods

        private void AddIndexFolder(object param)
        {
            IndexedFile file = new IndexedFile();
            file.FilePath = "C:\\";
            file.OperationStatus = IndexedFile.Status.Add;

            this.AddIndexFolder(file);
        }

        private void RemoveIndexFolder(object param)
        {
            if (null != this.SelectedIndexedFile)
            {

                IndexedFile file = this.ModifiedIndexedFile.Find((f) => this.SelectedIndexedFile.GUID == f.GUID);
                if (null != file)
                {
                    file.OperationStatus = IndexedFile.Status.Remove;
                }


                int index = this.IndexedFiles.IndexOf(this.SelectedIndexedFile);

                if (index > 0)
                {
                    if (index != this.IndexedFiles.Count - 1)
                    {
                        this.SelectedIndexedFile = this.IndexedFiles[index + 1];
                    }
                    else
                    {
                        this.SelectedIndexedFile = this.IndexedFiles[index - 1];
                    }
                    
                }
                else if (index == 0)
                {
                    this.SelectedIndexedFile = null;
                }

                
                this.IndexedFiles.RemoveAt(index);
            }
        }

        private void Apply(object param)
        {
            ISrcMLGlobalService srcMlService = null;
            try
            {
                srcMlService = ServiceLocator.Resolve<ISrcMLGlobalService>();
            }
            catch (ResolutionFailedException resFailed)
            {
                //TODO:Should not ignore exception here.
            }

            if (null != srcMlService)
            {
                foreach (var file in this.ModifiedIndexedFile)
                {

                    if (file.OperationStatus == IndexedFile.Status.Remove)
                    {
                        srcMlService.RemoveDirectoryFromMonitor(file.FilePath);
                    }
                    else if (file.OperationStatus == IndexedFile.Status.Add)
                    {
                        AddDirectoryToMonitor(srcMlService, file);
                    }
                    else if (file.OperationStatus == IndexedFile.Status.Modified)
                    {

                        srcMlService.RemoveDirectoryFromMonitor(file.FilePath);

                        AddDirectoryToMonitor(srcMlService, file);
                    }
                }
            }


            Synchronize();
            
        }

        private void Cancel(object param)
        {
            foreach (var file in this.ModifiedIndexedFile)
            {
                if (file.OperationStatus == IndexedFile.Status.Add)
                {
                    this.IndexedFiles.Remove(GetFileFromIndexedFiles(file.GUID));
                }
                else if (file.OperationStatus == IndexedFile.Status.Remove)
                {
                    this.IndexedFiles.Add(file);
                }
                else if (file.OperationStatus == IndexedFile.Status.Modified)
                {
                    GetFileFromIndexedFiles(file.GUID).FilePath = file.FilePath;
                }
            }

            Synchronize();

        }

        private void Search(object param)
        {
            BeginSearch(param.ToString());
        }

        private void Reset(object param)
        {
            var srcMlService = ServiceLocator.Resolve<ISrcMLGlobalService>();
            var indexer = ServiceLocator.Resolve<DocumentIndexer>();
            if (srcMlService == null)
            {
                MessageBox.Show("Could not reset the index.", "Failed to Reset", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                srcMlService.Reset();
                indexer.AddDeletionFile();
                MessageBox.Show("Restart this Visual Studio Instance to complete the index reset.", "Restart to Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Private Methods

        private void AddIndexFolder(IndexedFile file)
        {
            this.IndexedFiles.Add(file);
            this.SelectedIndexedFile = file;

            IndexedFile backupFile = new IndexedFile();
            backupFile.FilePath = this.SelectedIndexedFile.FilePath;
            backupFile.OperationStatus = this.SelectedIndexedFile.OperationStatus;
            backupFile.GUID = this.SelectedIndexedFile.GUID;
            this.ModifiedIndexedFile.Add(backupFile);
        }


        private IndexedFile GetFileFromIndexedFiles(Guid guid)
        {

            IndexedFile result = null;

            foreach (var indexedFile in this.IndexedFiles)
            {
                if (indexedFile.GUID == guid)
                {

                    result = indexedFile;

                }
            }

            return result;
        }

        private void Synchronize()
        {

            this.ModifiedIndexedFile.Clear();

            foreach (var file in this.IndexedFiles)
            {
                file.OperationStatus = IndexedFile.Status.Normal;
                this.ModifiedIndexedFile.Add(file);
            }

        }

        private void AddDirectoryToMonitor(ISrcMLGlobalService srcMlService, IndexedFile file)
        {

            
            foreach (IndexedFile addFile in this.IndexedFiles)
            {

                if (addFile.GUID == file.GUID)
                {
                    try
                    {
                        srcMlService.AddDirectoryToMonitor(addFile.FilePath);
                    }
                    catch (DirectoryScanningMonitorSubDirectoryException cantAdd)
                    {
                        MessageBox.Show("Sub-directories of existing directories cannot be added - " + cantAdd.Message, "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (ForbiddenDirectoryException cantAdd)
                    {
                        MessageBox.Show(cantAdd.Message, "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

            }
            
        }

        private bool IsPathEqual(String indexedFilePath, String eventFilePath)
        {
            return Path.GetFullPath(indexedFilePath + "\\") == Path.GetFullPath(eventFilePath);
        }

        private void InitProgramElements()
        {
            ProgramElements = new ObservableCollection<ProgramElementWrapper>
                {
                    new ProgramElementWrapper(true, ProgramElementType.Class),
                    new ProgramElementWrapper(false, ProgramElementType.Comment),
                    new ProgramElementWrapper(true, ProgramElementType.Custom),
                    new ProgramElementWrapper(true, ProgramElementType.Enum),
                    new ProgramElementWrapper(true, ProgramElementType.Field),
                    new ProgramElementWrapper(true, ProgramElementType.Method),
                    new ProgramElementWrapper(true, ProgramElementType.MethodPrototype),
                    new ProgramElementWrapper(true, ProgramElementType.Property),
                    new ProgramElementWrapper(true, ProgramElementType.Struct),
                    new ProgramElementWrapper(true, ProgramElementType.XmlElement)
                    //new ProgramElementWrapper(true, ProgramElementType.TextLine)
                };
        }

        private void InitAccessLevels()
        {
            AccessLevels = new ObservableCollection<AccessWrapper>
                {
                    new AccessWrapper(true, AccessLevel.Private),
                    new AccessWrapper(true, AccessLevel.Protected),
                    new AccessWrapper(true, AccessLevel.Internal),
                    new AccessWrapper(true, AccessLevel.Public)
                };
        }

        private void RegisterSrcMLService()
        {
            ISrcMLGlobalService srcMLService = ServiceLocator.Resolve<ISrcMLGlobalService>();

            if (srcMLService.IsUpdating)
            {
                this.ProgressBarVisibility = Visibility.Visible;
            }

            srcMLService.DirectoryAdded += (sender, e) =>
            {

                IndexedFile file = new IndexedFile();
                file.FilePath = e.Directory;
                file.OperationStatus = IndexedFile.Status.Normal;

                Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    bool equals = false;
                    foreach (IndexedFile currentFile in this.IndexedFiles)
                    {
                        
                        if (IsPathEqual(currentFile.FilePath, file.FilePath))
                        {
                            equals = true;
                            break;
                        }
                        
                    }

                    if (!equals)
                    {
                        this.AddIndexFolder(file);
                    }

                }), null);

            };

            srcMLService.DirectoryRemoved += (sender, e) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {

                    IndexedFile toRemoveFile = null;

                    foreach (IndexedFile file in this.IndexedFiles)
                    {

                        if (IsPathEqual(file.FilePath, e.Directory))
                        {
                            toRemoveFile = file;
                            break;
                        }


                    }

                    if (null != toRemoveFile)
                    {
                        this.IndexedFiles.Remove(toRemoveFile);
                        toRemoveFile = this.ModifiedIndexedFile.Find((f) => f.GUID == toRemoveFile.GUID);
                        if (null != toRemoveFile)
                        {
                            this.ModifiedIndexedFile.Remove(toRemoveFile);
                        }
                            
                    }


                }), null);
            };

            srcMLService.UpdateArchivesStarted += (sender, args) =>
            {
                this.ProgressBarVisibility = Visibility.Visible;
            };

            srcMLService.UpdateArchivesCompleted += (sender, args) =>
            {
                this.ProgressBarVisibility = Visibility.Collapsed;
            };
        }

        private void RegisterSolutionEvents()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            if (dte != null)
            {
                dte.Events.SolutionEvents.BeforeClosing += () =>
                {

                    Application.Current.Dispatcher.Invoke(new Action(delegate()
                    {

                        //clear the state
                        this.IndexedFiles.Clear();
                        this.ModifiedIndexedFile.Clear();
                        this.SelectedIndexedFile = null;

                        this.SearchStatus = String.Empty;

                    }));

                };
            }
        }

        private void InitializeIndexedFile()
        {
            ISrcMLGlobalService srcMLService = ServiceLocator.Resolve<ISrcMLGlobalService>();

            if (null != srcMLService.MonitoredDirectories)
            {
                foreach (String filePath in srcMLService.MonitoredDirectories)
                {
                    bool isEqual = false;
                    foreach (IndexedFile indexedFile in this.IndexedFiles)
                    {

                        if (IsPathEqual(indexedFile.FilePath, filePath))
                        {
                            isEqual = true;
                            break;
                        }

                    }

                    if (!isEqual)
                    {
                        IndexedFile file = new IndexedFile();
                        file.FilePath = filePath.TrimEnd("\\".ToCharArray());
                        file.OperationStatus = IndexedFile.Status.Normal;
                        this.AddIndexFolder(file);
                    }


                }
            }
            
        }

        #endregion

        #region Search

        private void SearchAsync(String text, SimpleSearchCriteria searchCriteria)
        {
            var searchWorker = new BackgroundWorker();
            searchWorker.DoWork += SearchWorker_DoWork;
            var workerSearchParams = new WorkerSearchParameters { Query = text, Criteria = searchCriteria };
            searchWorker.RunWorkerAsync(workerSearchParams);
        }

        private class WorkerSearchParameters
        {
            public SimpleSearchCriteria Criteria { get; set; }
            public String Query { get; set; }
        }

        void SearchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var searchParams = (WorkerSearchParameters)e.Argument;
            _searchManager.Search(searchParams.Query, searchParams.Criteria);
        }

        private void BeginSearch(string searchString)
        {

            this.BeforeSearch(this, new EventArgs());

            AddSearchHistory(searchString);

            SimpleSearchCriteria Criteria = new SimpleSearchCriteria();

            var selectedAccessLevels = this.AccessLevels.Where(a => a.Checked).Select(a => a.Access).ToList();
            if (selectedAccessLevels.Any())
            {
                Criteria.SearchByAccessLevel = true;
                Criteria.AccessLevels = new SortedSet<AccessLevel>(selectedAccessLevels);
            }
            else
            {
                Criteria.SearchByAccessLevel = false;
                Criteria.AccessLevels.Clear();
            }

            var selectedProgramElementTypes =
                this.ProgramElements.Where(e => e.Checked).Select(e => e.ProgramElement).ToList();
            if (selectedProgramElementTypes.Any())
            {
                Criteria.SearchByProgramElementType = true;
                Criteria.ProgramElementTypes = new SortedSet<ProgramElementType>(selectedProgramElementTypes);
            }
            else
            {
                Criteria.SearchByProgramElementType = false;
                Criteria.ProgramElementTypes.Clear();
            }

            SearchAsync(searchString, Criteria);
        }

        private void AddSearchHistory(String query)
        {
            var history = ServiceLocator.Resolve<SearchHistory>();
            history.IssuedSearchString(query);
        }


        #endregion

        public event EventHandler BeforeSearch;

        #region ISearchResultListener Implementation

        public void Update(string searchString, IQueryable<ExtensionContracts.ResultsReordererContracts.CodeSearchResult> results)
        {
            //Do nothing
        }

        public void UpdateMessage(string message)
        {
            this.SearchStatus = message;
        }

        public void UpdateRecommendedQueries(IQueryable<string> queries)
        {
            //Do nothing
        }

        #endregion
    }

    public class IndexedFile : BaseViewModel
    {

        private String _filePath;

        public IndexedFile()
        {
            this.GUID = Guid.NewGuid();
        }

        public String FilePath
        {
            get
            {
                return this._filePath;
            }
            set
            {
                this._filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        internal Guid GUID
        {
            get;
            set;
        }

        internal Status OperationStatus
        {
            get;
            set;
        }

        internal enum Status
        {
            Add,
            Remove,
            Modified,
            Normal

        }
    }

    
}