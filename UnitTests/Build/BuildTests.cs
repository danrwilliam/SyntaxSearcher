using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace SyntaxSearchUnitTests.Build
{
    [TestFixture]
    public class BuildTests
    {
        private SyntaxSearch.Builder.XmlTreeBuilder _builder;
        private SyntaxSearch.Parser.SearchFileParser _parser;

        [SetUp]
        public void Setup()
        {
            _builder = new SyntaxSearch.Builder.XmlTreeBuilder();
            _parser = new SyntaxSearch.Parser.SearchFileParser();
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
            memStream.Dispose();

            return data;
        }

        public static string BuildXml(string inner)
        {
            return $"<SyntaxSearchDefinition>{inner}</SyntaxSearchDefinition>";
        }

        [TestCase("int a;", "int b;", "List<int> a = new();")]
        [TestCase("List<int> a = new();", "List<int> n = new();", "List<int> a = new List<int>();")]
        [TestCase("a(obj);", "b(test);", "a(extra, num);")]
        public void StatementTest(string statement, string alsoMatch, string wontMatch)
        {
            var statementExpr = SyntaxFactory.ParseStatement(statement);

            var options = new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = false,
                Identifiers = false,
                Keywords = false,
                Tokens = false
            };

            _builder.Build(statementExpr, options);
            string data = GetString();

            var searcher = _parser.ParseFromString(data);

            Assert.That(searcher.Search(statementExpr).Count(), Is.EqualTo(1), $"\"{statementExpr}\" should be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(alsoMatch)).Count(), Is.EqualTo(1), $"\"{alsoMatch}\" should also be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(wontMatch)).Count(), Is.EqualTo(0), $"\"{wontMatch}\" should not be found");

            options.NamedChildren = true;
            _builder.Build(statementExpr, options);

            Assert.That(searcher.Search(statementExpr).Count(), Is.EqualTo(1), $"\"{statementExpr}\" should be found in explicit mode");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(alsoMatch)).Count(), Is.EqualTo(1), $"\"{alsoMatch}\" should also be found in explicit mode");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(wontMatch)).Count(), Is.EqualTo(0), $"\"{wontMatch}\" should not be found in explicit mode");
        }

        [TestCase("obj[a].Value", "obj[a].Value", "obj[a].X")]
        public void ExpressionTest(string statement, string alsoMatch, string wontMatch)
        {
            var expression = SyntaxFactory.ParseStatement(statement);

            var options = new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = false,
                Identifiers = true,
                Keywords = false,
                Tokens = false
            };

            _builder.Build(expression, options);

            var searcher = _parser.FromBuilder(_builder);

            Assert.That(searcher.Search(expression).Count(), Is.EqualTo(1), $"\"{expression}\" should be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(alsoMatch)).Count(), Is.EqualTo(1), $"\"{alsoMatch}\" should also be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(wontMatch)).Count(), Is.EqualTo(0), $"\"{wontMatch}\" should not be found");
        }

        [TestCase("obj[a].Value", "d[c].X", "obj[a+1].X")]
        public void ExpressionTest_NoIdentifiers(string statement, string alsoMatch, string wontMatch)
        {
            var expression = SyntaxFactory.ParseStatement(statement);

            var options = new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                Modifiers = false,
                Identifiers = false,
                Keywords = false,
                Tokens = false
            };

            _builder.Build(expression, options);

            var searcher = _parser.FromBuilder(_builder);

            Assert.That(searcher.Search(expression).Count(), Is.EqualTo(1), $"\"{expression}\" should be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(alsoMatch)).Count(), Is.EqualTo(1), $"\"{alsoMatch}\" should also be found");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(wontMatch)).Count(), Is.EqualTo(0), $"\"{wontMatch}\" should not be found");
        }

        [TestCase("if (a != null) a();", "if (a != null) b();")]
        public void TestAutoCapture(string autoCapture, string wontCapture)
        {
            var node = SyntaxFactory.ParseStatement(autoCapture);

            _builder.Build(node, new SyntaxSearch.Builder.TreeBuilderOptions()
            {
                AutomaticCapture = true,
                UseAnythingForAutomaticCapture = true
            });

            var searcher = _parser.FromBuilder(_builder);

            Assert.That(
                searcher.Search(node).Count(),
                Is.EqualTo(1),
                $"builder built from {nameof(autoCapture)} should match itself");
            Assert.That(searcher.Search(SyntaxFactory.ParseStatement(wontCapture)).Count(),
                        Is.EqualTo(0),
                        $"builder built from {nameof(autoCapture)} should not match {nameof(wontCapture)}");
        }
    }
}