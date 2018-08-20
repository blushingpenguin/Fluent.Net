using System.IO;
using NUnit.Framework;
using FluentAssertions;

namespace Fluent.Net.SyntaxTest.Test
{
    public class SyntaxTestTest
    {
        [Test]
        public void TestNoArgs()
        {
            using (var capture = new ConsoleCapture())
            {
                Program.Main(new string[0]);
                capture.GetOutput().Should().Contain(
                    "no input files specified");
            }
        }

        [Test]
        public void TestHelp()
        {
            using (var capture = new ConsoleCapture())
            {
                Program.Main(new string[] { "--help" });
                capture.GetOutput().Should().Contain(
                    "Parse fluent translation files, printing any errors encountered");
            }
        }

        string Resolve(string file)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\", file);
        }

        [Test]
        public void TestParseFile()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] { Resolve("TestData\\test.ftl") });
                result.Should().Be(0);
            }
        }

        [Test]
        public void TestParseMissingFile()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] { Resolve("TestData\\testMissingFile.ftl") });
                capture.GetOutput().Should().Contain(
                    "testMissingFile.ftl does not exist");
                result.Should().Be(1);
            }
        }

        [Test]
        public void TestParseMissingFolder()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] {
                        "--folders",
                        Resolve("TestData\\TestMissingFolder") });
                capture.GetOutput().Should().Contain(
                    "Could not find a part of the path");
                result.Should().Be(1);
            }
        }

        [Test]
        public void TestParseFileWithError()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] { Resolve("TestData\\testError.ftl") });
                capture.GetOutput().Should().Contain("error E0003");
                result.Should().Be(1);
            }
        }

        [Test]
        public void TestParseFileWithRuntimeParser()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] { "--runtime", Resolve("TestData\\test.ftl") });
                result.Should().Be(0);
            }
        }

        [Test]
        public void TestParseFileWithErrorWithRuntimeParser()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] { "--runtime", Resolve("TestData\\testError.ftl") });
                capture.GetOutput().Should().Contain("Expected \"=\" after the identifier");
                result.Should().Be(1);
            }
        }

        [Test]
        public void TestParseFoldersOk()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] {
                        "--folders",
                        Resolve("TestData\\set1.1"),
                        Resolve("TestData\\set1.2") });
                result.Should().Be(0);
            }
        }

        [Test]
        public void TestParseFoldersWithDuplicate()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] {
                        "--folders",
                        Resolve("TestData\\set2.1"),
                        Resolve("TestData\\set2.2") });
                capture.GetOutput().Should().Contain(
                    "duplicate message identifier 'foo'");
                result.Should().Be(1);
            }
        }

        [Test]
        public void TestParseFoldersOkWithRuntimeParser()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] {
                        "--folders",
                        "--runtime",
                        Resolve("TestData\\set1.1"),
                        Resolve("TestData\\set1.2") });
                result.Should().Be(0);
            }
        }

        [Test]
        public void TestParseFoldersWithDuplicateWithRuntimeParser()
        {
            using (var capture = new ConsoleCapture())
            {
                int result = Program.Main(
                    new string[] {
                        "--folders",
                        "--runtime",
                        Resolve("TestData\\set2.1"),
                        Resolve("TestData\\set2.2") });
                capture.GetOutput().Should().Contain(
                    "duplicate message identifier 'foo'");
                result.Should().Be(1);
            }
        }
    }
}
