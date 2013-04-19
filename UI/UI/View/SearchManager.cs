﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using Configuration.OptionsPages;
using Sando.Core;
using Sando.Core.Extensions;
using Sando.DependencyInjection;
using Sando.Indexer.Searching;
using Sando.SearchEngine;
using Sando.Indexer.Searching.Criteria;
using Sando.Core.Tools;
using Sando.Core.Logging;
using Sando.UI.Monitoring;
using ABB.SrcML.VisualStudio.SolutionMonitor;
using Sando.Indexer;
using Sando.Core.Logging.Events;
using Sando.Indexer.Searching.Metrics;
using Lucene.Net.Analysis;

namespace Sando.UI.View
{
    public class SearchManager
    {
        private readonly ISearchResultListener _searchResultListener;

        public SearchManager(ISearchResultListener searchResultListener)
        {
            _searchResultListener = searchResultListener;
        }

        public void Search(String searchString, SimpleSearchCriteria searchCriteria = null, bool interactive = true)
        {            
            if (!EnsureSolutionOpen())
                return;
                        
            try
            {
                var codeSearcher = new CodeSearcher(new IndexerSearcher());
                if (String.IsNullOrEmpty(searchString))
                    return;
                
                var solutionKey = ServiceLocator.ResolveOptional<SolutionKey>(); //no opened solution
                if (solutionKey == null)
                {
                    _searchResultListener.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                    return;
                }

                searchString = ExtensionPointsRepository.Instance.GetQueryRewriterImplementation().RewriteQuery(searchString);
                var searchStringContainedInvalidCharacters = WordSplitter.InvalidCharactersFound(searchString);
                if (searchStringContainedInvalidCharacters)
                {
                    _searchResultListener.UpdateMessage("Invalid Query String - only complete words or partial words followed by a '*' are accepted as input.");
                    LogEvents.InvalidCharactersInQuery(this);
                    return;
                }

				PreRetrievalMetrics preMetrics = new PreRetrievalMetrics(ServiceLocator.Resolve<DocumentIndexer>().Reader, ServiceLocator.Resolve<Analyzer>());
				LogEvents.PreSearch(this, preMetrics.AvgIdf(searchString), preMetrics.AvgSqc(searchString), preMetrics.AvgVar(searchString));
                LogEvents.PreSearchQueryAnalysis(this, QueryMetrics.ExamineQuery(searchString).ToString(), QueryMetrics.DiceCoefficient(QueryMetrics.SavedQuery, searchString));
                QueryMetrics.SavedQuery = searchString;

                var criteria = GetCriteria(searchString, searchCriteria);
                var results = codeSearcher.Search(criteria, true).AsQueryable();
                var resultsReorderer = ExtensionPointsRepository.Instance.GetResultsReordererImplementation();
                results = resultsReorderer.ReorderSearchResults(results);

                var returnString = new StringBuilder();
                if (!results.Any())
                {
                    returnString.Append("No results found. ");
                }
                else
                {
                    returnString.Append(results.Count() + " results returned. ");
                }
                if (ServiceLocator.Resolve<InitialIndexingWatcher>().IsInitialIndexingInProgress())
                {
                    returnString.Append("Sando is still performing its initial index of this project, results may be incomplete.");
                }
                _searchResultListener.Update(results);
                _searchResultListener.UpdateMessage(returnString.ToString());

				//calculate and log post retrival metrics?
				LogEvents.PostSearch(this, results.Count());
            }
            catch (Exception e)
            {
                _searchResultListener.UpdateMessage("Sando is experiencing difficulties. See log file for details.");
                LogEvents.UISandoSearchingError(this, e);
            }
        }

        private bool EnsureSolutionOpen()
        {
            DocumentIndexer indexer = null;
            var isOpen = true;
            try
            {
                indexer = ServiceLocator.Resolve<DocumentIndexer>();
                if (indexer == null || indexer.IsDisposingOrDisposed())
                {
                    _searchResultListener.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                    isOpen = false;
                }
            }
            catch (Exception e)
            {
                _searchResultListener.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                if (indexer != null)
                    LogEvents.UISolutionOpeningError(this, e);
                isOpen = false;
            }
            return isOpen;
        }

        private static SearchCriteria GetCriteria(string searchString, SimpleSearchCriteria searchCriteria = null)
        {            
            var sandoOptions = ServiceLocator.Resolve<ISandoOptionsProvider>().GetSandoOptions();                       
            return CriteriaBuilder.GetBuilder().
                AddCriteria(searchCriteria).
                AddSearchString(searchString).
                NumResults(sandoOptions.NumberOfSearchResultsReturned).
                Ext(searchString).GetCriteria();                        
        }
    }

}
