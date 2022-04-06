using System;
using System.IO;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace SyntaxSearchUnitTests.Build
{
    [TestFixture]
    public class BuildTests
    {
        private SyntaxSearch.Builder.XmlTreeBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _builder = new SyntaxSearch.Builder.XmlTreeBuilder();
        }

        public string GetString()
        {
            var memStream = new MemoryStream();

            var settings = new XmlWriterSettings()
            {
                Indent = false,
                OmitXmlDeclaration = true,
                CloseOutput = false,
                NewLineHandling = NewLineHandling.None,
                NewLineChars = String.Empty,
            };

            var writer = XmlTextWriter.Create(memStream, settings);

            _builder.Document.Save(writer);

            memStream.Position = 0;

            StreamReader reader = new StreamReader(memStream);

            string data = reader.ReadToEnd();

            reader.Dispose();

            return data;
        }

        public static string BuildXml(string inner)
        {
            return $"<SyntaxSearchDefinition>{inner}</SyntaxSearchDefinition>";
        }

        [Test]
        public void TestDeclarationNothing()
        {
            var node = SyntaxFactory.ParseStatement("int a;");
            _builder.Build(node, new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = false,
                Identifiers = false,
                Keywords = false,
                Tokens = false
            });

            string data = GetString();

            Assert.That(
                data,
                Is.EqualTo(BuildXml(@"<LocalDeclarationStatement><VariableDeclaration><PredefinedType /><VariableDeclarator /></VariableDeclaration></LocalDeclarationStatement>")));
        }

        [Test]
        public void TestDeclarationWithIdentifiers()
        {
            var node = SyntaxFactory.ParseStatement("int a;");
            _builder.Build(node, new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = false,
                Identifiers = true,
                Keywords = false,
                Tokens = false
            });

            string data = GetString();

            Assert.That(
                data,
                Is.EqualTo(BuildXml(@"<LocalDeclarationStatement><VariableDeclaration><PredefinedType /><VariableDeclarator Identifier=""a"" /></VariableDeclaration></LocalDeclarationStatement>")));

        }

        [Test]
        public void TestDeclarationWithAll()
        {
            var node = SyntaxFactory.ParseStatement("int a;");
            _builder.Build(node, new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = true,
                Identifiers = true,
                Keywords = true,
                Tokens = true
            });

            string data = GetString();

            Assert.That(
                data,
                Is.EqualTo(BuildXml(@"<LocalDeclarationStatement><VariableDeclaration><PredefinedType Keyword=""int""><IntKeyword /></PredefinedType><VariableDeclarator Identifier=""a""><IdentifierToken /></VariableDeclarator></VariableDeclaration><SemicolonToken /></LocalDeclarationStatement>")));

        }
    }
}