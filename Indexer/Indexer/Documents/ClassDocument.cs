﻿using System;
using Lucene.Net.Documents;
using Sando.ExtensionContracts.ProgramElementContracts;
using System.Collections.Generic;

namespace Sando.Indexer.Documents
{
	public class ClassDocument : SandoDocument
	{
		public ClassDocument(ClassElement classElement)
			: base(classElement)
		{
		}

		public ClassDocument(Document document)
			: base(document)
		{
		}

        public override List<Field> GetFieldsForLucene()
		{
            List<Field> fields = new List<Field>();
			ClassElement classElement = (ClassElement) programElement;
            fields.Add(new Field(SandoField.Namespace.ToString(), classElement.Namespace, Field.Store.YES, Field.Index.ANALYZED));
            AddBodyField(fields, new Field(SandoField.Body.ToString(), classElement.Body, Field.Store.NO, Field.Index.ANALYZED));
            fields.Add(new Field(SandoField.AccessLevel.ToString(), classElement.AccessLevel.ToString().ToLower(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            fields.Add(new Field(SandoField.ExtendedClasses.ToString(), classElement.ExtendedClasses, Field.Store.YES, Field.Index.ANALYZED));
            fields.Add(new Field(SandoField.ImplementedInterfaces.ToString(), classElement.ImplementedInterfaces, Field.Store.YES, Field.Index.ANALYZED));
            fields.Add(new Field(SandoField.Modifiers.ToString(), classElement.Modifiers, Field.Store.YES, Field.Index.ANALYZED));
            return fields;
		}

        public override object[] GetParametersForConstructor(string name, ProgramElementType programElementType, string fullFilePath, int definitionLineNumber, int definitionColumnNumber, string snippet, Document document)
		{
			string namespaceName = document.GetField(SandoField.Namespace.ToString()).StringValue();
			AccessLevel accessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), document.GetField(SandoField.AccessLevel.ToString()).StringValue(), true);
			string extendedClasses = document.GetField(SandoField.ExtendedClasses.ToString()).StringValue();
			string implementedInterfaces = document.GetField(SandoField.ImplementedInterfaces.ToString()).StringValue();
			string modifiers = document.GetField(SandoField.Modifiers.ToString()).StringValue();
            return new object[] { name, definitionLineNumber, definitionColumnNumber, fullFilePath, snippet, accessLevel, namespaceName, extendedClasses, implementedInterfaces, modifiers, "" };
		}
	}
}
