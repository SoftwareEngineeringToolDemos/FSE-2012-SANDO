﻿using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Snowball;
using NUnit.Framework;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer;
using Sando.Indexer.Searching;
using Sando.Indexer.Searching.Criteria;
using Sando.SearchEngine;
using Sando.UI.Monitoring;
using UnitTestHelpers;
using Sando.Recommender;
using Sando.Core.Tools;
using Sando.Indexer.IndexFiltering;
using Sando.UI.Options;
using Configuration.OptionsPages;
using ABB.SrcML;
using System.Threading;

namespace Sando.IntegrationTests.Search
{
	[TestFixture]
	public class MethodElementSearchTest: AutomaticallyIndexingTestClass
	{
		[Test]
		public void MethodElementReturnedFromSearchContainsAllFields()
		{
            var codeSearcher = new CodeSearcher();
			string keywords = "fetch output stream";
			List<CodeSearchResult> codeSearchResults = codeSearcher.Search(keywords);
			Assert.AreEqual(codeSearchResults.Count, 7, "Invalid results number");
			var methodSearchResult = codeSearchResults.Find(el => el.ProgramElement.ProgramElementType == ProgramElementType.Method && el.ProgramElement.Name == "FetchOutputStream");
			if(methodSearchResult == null)
			{ 
				Assert.Fail("Failed to find relevant search result for search: " + keywords);
			}
			var method = methodSearchResult.ProgramElement as MethodElement;
			Assert.AreEqual(method.AccessLevel, AccessLevel.Public, "Method access level differs!");
			Assert.AreEqual(method.Arguments, "A B string fileName Image image", "Method arguments differs!");
			Assert.NotNull(method.Body, "Method body is null!");
			Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
            Assert.AreEqual(method.ClassName, "ImageCapture", "Method class name differs!");
			Assert.AreEqual(method.DefinitionLineNumber, 83, "Method definition line number differs!");
            Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\ImageCapture.cs".ToLowerInvariant()), "Method full file path is invalid!");
			Assert.AreEqual(method.Name, "FetchOutputStream", "Method name differs!");
			Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
			Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
			Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");
		}

		[Test]
		public void MethodSearchRespectsAccessLevelCriteria()
		{
            var codeSearcher = new CodeSearcher();
			string keywords = "to string";
            SimpleSearchCriteria searchCriteria = new SimpleSearchCriteria()
			{ 
				AccessLevels = new SortedSet<AccessLevel>() { AccessLevel.Public },
				SearchByAccessLevel = true,
				SearchTerms = new SortedSet<string>(keywords.Split(' '))
			};
			List<CodeSearchResult> codeSearchResults = codeSearcher.Search(searchCriteria);
			Assert.AreEqual(7, codeSearchResults.Count, "Invalid results number");
			var methodSearchResult = codeSearchResults.Find(el => el.ProgramElement.ProgramElementType == ProgramElementType.Method && el.ProgramElement.Name == "ToQueryString");
			if(methodSearchResult == null)
			{
				Assert.Fail("Failed to find relevant search result for search: " + keywords);
			}
			var method = methodSearchResult.ProgramElement as MethodElement;
			Assert.AreEqual(method.AccessLevel, AccessLevel.Public, "Method access level differs!");
			Assert.AreEqual(method.Arguments, String.Empty, "Method arguments differs!");
			Assert.NotNull(method.Body, "Method body is null!");
			Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
            Assert.AreEqual(method.ClassName, "SimpleSearchCriteria", "Method class name differs!");
			Assert.AreEqual(method.DefinitionLineNumber, 31, "Method definition line number differs!");
            Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\Searcher.cs".ToLowerInvariant()), "Method full file path is invalid!");
			Assert.AreEqual(method.Name, "ToQueryString", "Method name differs!");
			Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
			Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
			Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");
		}

        [Test]
        public void MethodSearchRespectsFileExtensionsCriteria()
        {
            var codeSearcher = new CodeSearcher();
            var keywords = "main";
            var searchCriteria = new SimpleSearchCriteria()
                {
                    SearchTerms = new SortedSet<string>(keywords.Split(' ')),
                    SearchByFileExtension = true,
                    FileExtensions = new SortedSet<string> {"cpp"}
                };
            var codeSearchResults = codeSearcher.Search(searchCriteria);
            Assert.AreEqual(2, codeSearchResults.Count, "Invalid results number");
            var methodSearchResult = codeSearchResults.Find(el => el.ProgramElement.ProgramElementType == ProgramElementType.Method && el.ProgramElement.Name == "main");
            if (methodSearchResult == null)
            {
                Assert.Fail("Failed to find relevant search result for search: " + keywords);
            }
            //var method = methodSearchResult.Element as MethodElement;
            //Assert.AreEqual(method.AccessLevel, AccessLevel.Public, "Method access level differs!");
            //Assert.AreEqual(method.Arguments, String.Empty, "Method arguments differs!");
            //Assert.NotNull(method.Body, "Method body is null!");
            //Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
            //Assert.AreEqual(method.ClassName, "SimpleSearchCriteria", "Method class name differs!");
            //Assert.AreEqual(method.DefinitionLineNumber, 31, "Method definition line number differs!");
            //Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\Searcher.cs"), "Method full file path is invalid!");
            //Assert.AreEqual(method.Name, "ToQueryString", "Method name differs!");
            //Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
            //Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
            //Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");
        }

        [Test]
        public void MethodIncludesDocCommentsInSearch()
        {
            var codeSearcher = new CodeSearcher();
            var keywords = "Retrieves a stream for saving an image";
            var codeSearchResults = codeSearcher.Search(keywords);
            Assert.Greater(codeSearchResults.Count, 1, "Invalid results number");
            var methodSearchResult = codeSearchResults.Find(el => el.ProgramElement.ProgramElementType == ProgramElementType.Method && el.ProgramElement.Name == "FetchOutputStream");
            if (methodSearchResult == null)
            {
                Assert.Fail("Failed to find relevant search result for search: " + keywords);
            }
        }

        public override string GetIndexDirName()
        {            
            return "MethodElementSearchTest";
        }

        public override string GetFilesDirectory()
        {
            return "..\\..\\IntegrationTests\\TestFiles\\MethodElementTestFiles";
        }

        public override TimeSpan? GetTimeToCommit()
        {
            return TimeSpan.FromSeconds(2);
        }
	}
}
