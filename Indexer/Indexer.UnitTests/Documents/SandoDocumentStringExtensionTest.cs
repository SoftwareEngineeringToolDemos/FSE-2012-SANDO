﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Indexer.Documents;

namespace Sando.Indexer.UnitTests.Documents
{
	[TestFixture]
	public class SandoDocumentStringExtensionTest
	{
		[Test]
		public void SandoDocumentStringExtension_ToSandoSearchableReturnsValidString()
		{
			string testString = "SetFileExtension";
			Assert.AreEqual(testString.ToSandoSearchable(), "SetFileExtension#Set File Extension");

			testString = "donothing";
			Assert.AreEqual(testString.ToSandoSearchable(), "donothing");

			testString = String.Empty;
			Assert.AreEqual(testString.ToSandoSearchable(), String.Empty);
		}

		[Test]
		public void SandoDocumentStringExtension_ToSandoDisplayableReturnsValidString()
		{
			string testString = "SetFileExtension#Set File Extension";
			Assert.AreEqual(testString.ToSandoDisplayable(), "SetFileExtension");

			testString = "donothing";
			Assert.AreEqual(testString.ToSandoDisplayable(), "donothing");

			testString = String.Empty;
			Assert.AreEqual(testString.ToSandoDisplayable(), String.Empty);
		}
	}
}