﻿using Sando.DependencyInjection;
using Sando.Indexer.IndexFiltering;
using Sando.UI.Monitoring;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sando.UI.Actions
{
    public static class FileRemover
    {

        public static void Remove(string path)
        {
            var bw = new BackgroundWorker { WorkerReportsProgress = false, WorkerSupportsCancellation = false };
            bw.DoWork += RemoveFileFromIndex;
            bw.RunWorkerAsync(path); 
        }

        private static void RemoveFileFromIndex(object sender, DoWorkEventArgs e)
        {
            var path = e.Argument as string;
            var srcMLArchiveEventsHandlers = ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>();
            var deleteFile = new ABB.SrcML.FileEventRaisedArgs(path, path, ABB.SrcML.FileEventType.FileDeleted);
            srcMLArchiveEventsHandlers.SourceFileChanged(null, deleteFile);
            ServiceLocator.Resolve<IndexFilterManager>().AddFileExclusion(path);
        }

    }
}
