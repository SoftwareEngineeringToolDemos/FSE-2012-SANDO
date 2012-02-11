﻿
namespace Sando.SearchEngine.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;
    using Sando.SearchEngine;
    using Lucene.Net.Analysis;
    using Sando.Core;
    using Sando.Indexer;
    using Sando.Indexer.Documents;
    using Lucene.Net.Search;
	using Sando.Indexer.Searching;
	using Sando.UnitTestHelpers;

    [TestFixture]
    public class CodeSearcherFixture
    {
		private DocumentIndexer Indexer;
    	private string IndexerPath;


    	[Test]
        public void TestCreateCodeSearcher()
        {
            SimpleAnalyzer analyzer = new SimpleAnalyzer();
    		var indexer = DocumentIndexerFactory.CreateIndexer(System.IO.Path.GetTempPath() + "luceneindexer", AnalyzerType.Standard);
			//TODO - How do we get an instance of IIndexerSearcher?
			//FYI - use this IndexerSearcherFactory.CreateSearcher
            Assert.DoesNotThrow(() => new CodeSearcher( null ));
			indexer.Dispose();
        }

        [Test]     
        public void PerformBasicSearch()
        {        	
			//TODO - get an IIndexerSearcher from the Indexer project
			//FYI - use this IndexerSearcherFactory.CreateSearcher
        	CodeSearcher cs = new CodeSearcher(null);            
            List<CodeSearchResult> result = cs.Search("SimpleName");
            Assert.AreEqual(2, result.Count);                                 
        }

		[Test]
		public void TestSearchWithCache()
		{			
			var cs = new CodeSearcher(IndexerSearcherFactory.CreateSearcher(IndexerPath));
			List<CodeSearchResult> result = cs.Search("SimpleName");
			var sameResult = cs.Search("SimpleName");
			Assert.IsTrue(result==sameResult);
			var different = cs.Search("Simple Name 3");
			Assert.IsTrue(result != different);
			var alsoDifferent = cs.Search("Simple Name 4");
			Assert.IsTrue(alsoDifferent != different);
			var original = cs.Search("Simple Name");
			Assert.IsTrue(result != original);

		}

		[TestFixtureSetUp]
    	public void CreateIndexer()
		{
			IndexerPath = System.IO.Path.GetTempPath() + "luceneindexer";
			Indexer = DocumentIndexerFactory.CreateIndexer(IndexerPath, AnalyzerType.Standard);
    		ClassElement classElement = SampleProgramElementFactory.GetSampleClassElement(
				accessLevel: Core.AccessLevel.Public,
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
				accessLevel: Core.AccessLevel.Protected,
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
			Indexer.Dispose();   
    	}
    }
}
