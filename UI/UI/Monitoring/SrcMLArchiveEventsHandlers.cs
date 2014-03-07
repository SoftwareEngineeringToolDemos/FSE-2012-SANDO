﻿using System;
using System.IO;
using ABB.SrcML;
using ABB.SrcML.VisualStudio.SrcMLService;
using Sando.Core.Extensions;
using Sando.Core.Logging;
using Sando.DependencyInjection;
using Sando.Indexer;
using Sando.Indexer.IndexFiltering;
using Sando.Recommender;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Sando.Core.Logging.Events;
using System.Linq;
using Sando.UI.View;
using Sando.ExtensionContracts.TaskFactoryContracts;
using System.Diagnostics;
using System.ComponentModel;


namespace Sando.UI.Monitoring
{
    public class SrcMLArchiveEventsHandlers : ITaskScheduler
    {

        private ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        private ConcurrentBag<CancellationTokenSource> cancellers = new ConcurrentBag<CancellationTokenSource>();
        private TaskScheduler scheduler;
        public TaskFactory factory;
        public static SrcMLArchiveEventsHandlers Instance;
        System.Timers.Timer hideProgressBarTimer = new System.Timers.Timer(500);
        public static int MAX_PARALLELISM = 2;


        private SrcMLArchiveEventsHandlers()
        {
            throw new NotImplementedException();
        }

        public SrcMLArchiveEventsHandlers(TaskScheduler aScheduler)
        {
            scheduler = aScheduler;
            factory = new TaskFactory(scheduler);
            Instance = this;            
        }

        public void SourceFileChanged(object sender, FileEventRaisedArgs args)
        {
            SourceFileChanged(sender, args, false);
        }

        private UIPackage GetPackage()
        {
            if (package == null)
            {
                package = ServiceLocator.Resolve<UIPackage>();
            }
            return package;
        }

        public int TaskCount()
        {
            lock (tasksTrackerLock)
                return tasks.Count;
        }


        public Task StartNew(Action a, CancellationTokenSource c)
        {            
            var task = factory.StartNew(a, c.Token);
            lock (tasksTrackerLock)
            {
                tasks.Add(task);
                cancellers.Add(c);
            }
            task.ContinueWith(removeTask => RemoveTask(task, c));
            return task;
        }

        private bool CanBeIndexed(string fileName) {
            var fileExtension = Path.GetExtension(fileName);
            var hasFileExtension = (fileExtension != null && !fileExtension.Equals(String.Empty));
            if(hasFileExtension) {
                return ServiceLocator.Resolve<IndexFilterManager>().ShouldFileBeIndexed(fileName);
            }
            return false;
        }
        public void SourceFileChanged(object sender, FileEventRaisedArgs args, bool commitImmediately = false)
        {
            var cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;            
            Action action =  () =>
            {
                cancelToken.ThrowIfCancellationRequested();

                var documentIndexer = ServiceLocator.Resolve<DocumentIndexer>();

                if(CanBeIndexed(args.FilePath)) {
                    
                    string sourceFilePath = args.FilePath;
                    string oldSourceFilePath = args.OldFilePath;

                    ProcessFileEvent(sender as ISrcMLGlobalService, args, commitImmediately, documentIndexer);
                } else {
                    documentIndexer.DeleteDocuments(args.FilePath, commitImmediately);
                }
            };
            CommitEveryOneHundredFiles(cancelTokenSource, action);
            StartNew(action, cancelTokenSource);
        }

        //Note: if you don't commit every so often then Lucene will take up a lot of RAM, causing performance issues on most machines.
        private void CommitEveryOneHundredFiles(CancellationTokenSource cancelTokenSource, Action action)
        {
            if (counter % 100 == 0)
            {
                counter = 1;
                Action commitAction = () =>
                {
                    var documentIndexer = ServiceLocator.Resolve<DocumentIndexer>();
                    //only public API that forces a commit
                    documentIndexer.ForceReaderRefresh();
                };
                StartNew(action, cancelTokenSource);
            }
            else
            {
                counter++;
            }
        }

        private static void ProcessFileEvent(ISrcMLGlobalService srcMLService, FileEventRaisedArgs args, bool commitImmediately, DocumentIndexer documentIndexer) {                        
            string sourceFilePath = args.FilePath;
            var fileExtension = Path.GetExtension(sourceFilePath);
            if(ExtensionPointsRepository.Instance.GetParserImplementation(fileExtension) != null) {
                if (ConcurrentIndexingMonitor.TryToLock(sourceFilePath))
                    return;
                var xelement = srcMLService.GetXElementForSourceFile(args.FilePath);
                var indexUpdateManager = ServiceLocator.Resolve<IndexUpdateManager>();
                switch(args.EventType) {
                    case FileEventType.FileAdded:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant());    //"just to be safe!"
                        indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                        SwumManager.Instance.AddSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                        break;
                    case FileEventType.FileChanged:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant());
                        indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                        SwumManager.Instance.UpdateSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                        break;
                    case FileEventType.FileDeleted:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant(), commitImmediately);
                        SwumManager.Instance.RemoveSourceFile(sourceFilePath.ToLowerInvariant());
                        break;
                    case FileEventType.FileRenamed: // FileRenamed is repurposed. Now means you may already know about it, so check and only parse if not existing
                        if(!SwumManager.Instance.ContainsFile(sourceFilePath.ToLowerInvariant())) {
                            documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant());    //"just to be safe!"
                            indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                            SwumManager.Instance.AddSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                        }
                        break;
                    default:
                        // if we get here, a new event was probably added. for now, no-op
                        break;
                }
                ConcurrentIndexingMonitor.ReleaseLock(sourceFilePath);
            }
        }

        private void RemoveTask(Task task, CancellationTokenSource cancelToken)
        {
            lock (tasksTrackerLock)
            {
                tasks.TryTake(out task);
                cancellers.TryTake(out cancelToken);
            }
        }

        private object tasksTrackerLock = new object();
        private string lastFile = "";
        private DateTime lastTime = DateTime.Now;
        private int counter=0;
        private UIPackage package;
        

   

        public void MonitoringStopped(object sender, EventArgs args)
        {
            lock (tasksTrackerLock)
            {
                foreach (var cancelToken in cancellers)
                    cancelToken.Cancel();
            }

            LogEvents.UIMonitoringStopped(this);
            var currentIndexer = ServiceLocator.ResolveOptional<DocumentIndexer>();
            if (currentIndexer != null)
            {
                currentIndexer.Dispose(false);  // Because in SolutionMonitor: public void StopMonitoring(bool killReaders = false)
            }
            if (SwumManager.Instance != null)
            {
                SwumManager.Instance.PrintSwumCache();
            }
        }



        public void UpdateStarted(object sender, EventArgs e) {
            GetPackage().ShowProgressBar(true);
        }

        public void UpdateCompleted(object sender, EventArgs e) {
            GetPackage().ShowProgressBar(false);
        }
    }
}