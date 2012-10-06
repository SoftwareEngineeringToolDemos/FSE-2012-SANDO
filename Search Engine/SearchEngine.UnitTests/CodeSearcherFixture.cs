﻿using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using NUnit.Framework;
using Sando.Core;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer;
using Sando.Indexer.Documents;
using Sando.Indexer.Searching;
using Sando.UnitTestHelpers;
using UnitTestHelpers;

namespace Sando.SearchEngine.UnitTests
{

    [TestFixture]
    public class CodeSearcherFixture
    {
		private DocumentIndexer Indexer;
    	private string IndexerPath;
		private SolutionKey solutionKey;


    	[Test]
        public void TestCreateCodeSearcher()
        {
            SimpleAnalyzer analyzer = new SimpleAnalyzer();
    		var indexer = DocumentIndexerFactory.CreateIndexer(solutionKey, AnalyzerType.Standard);
			//TODO - How do we get an instance of IIndexerSearcher?
			//FYI - use this IndexerSearcherFactory.CreateSearcher
            Assert.DoesNotThrow(() => new CodeSearcher( null ));            
        }

        [Test]     
        public void PerformBasicSearch()
        {
			var indexerSearcher = IndexerSearcherFactory.CreateSearcher(solutionKey);
        	CodeSearcher cs = new CodeSearcher(indexerSearcher);            
            List<CodeSearchResult> result = cs.Search("SimpleName");
            Assert.True(result.Count > 0);                                 
        }

		[TestFixtureSetUp]
    	public void CreateIndexer()
		{
			TestUtils.InitializeDefaultExtensionPoints();

			IndexerPath = System.IO.Path.GetTempPath() + "luceneindexer";
		    Directory.CreateDirectory(IndexerPath);
			solutionKey = new SolutionKey(Guid.NewGuid(), "C:/SolutionPath", IndexerPath);
			Indexer = DocumentIndexerFactory.CreateIndexer(solutionKey, AnalyzerType.Standard);
    		ClassElement classElement = SampleProgramElementFactory.GetSampleClassElement(
				accessLevel: AccessLevel.Public,
				definitionLineNumber: 11,
				extendedClasses: "SimpleClassBase",
				fullFilePath: "C:/Projects/SimpleClass.cs",
				implementedInterfaces: "IDisposable",
				name: "SimpleName",
				namespaceName: "Sanod.Indexer.UnitTests"
    		);
    		SandoDocument sandoDocument = DocumentFactory.Create(classElement);
    		Indexer.AddDocument(sandoDocument);
			MethodElement methodElement = SampleProgramElementFactory.GetSampleMethodElement(
				accessLevel: AccessLevel.Protected,
    		    name: "SimpleName",
				returnType: "Void",
				fullFilePath: "C:/stuff"
			);
    		sandoDocument = DocumentFactory.Create(methodElement);
    		Indexer.AddDocument(sandoDocument);
    		Indexer.CommitChanges();
    	}

		[TestFixtureTearDown]
    	public void ShutdownIndexer()
    	{
			Indexer.ClearIndex();
			Indexer.CommitChanges();
			Indexer.Dispose(true);   
    	}
    }
}
