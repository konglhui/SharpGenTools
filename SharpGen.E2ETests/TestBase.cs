﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SharpGen.E2ETests
{
    public class TestBase
    {
        private const string DefaultSharpGenArguments = @"--castxml castxml/bin/castxml.exe --vctools ""C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.10.25017\";
        private ITestOutputHelper outputHelper;
        private DirectoryInfo testDirectory;

        public TestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            testDirectory = GenerateTestDirectory();
        }

        public (bool success, string output) RunWithConfig(Config.ConfigFile config, string appType = "true", [CallerMemberName] string configName = "")
        {
            SaveConfigFile(config, configName);

            var codeGenApp = new CodeGenApp
            {
                CastXmlExecutablePath = "../../../CastXML/bin/castxml.exe",
                VcToolsPath = @"C:\Program Files(x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.10.25017\",
                AppType = appType,
                ConfigRootPath = Path.Combine(testDirectory.FullName, configName + "-Mapping.xml"),
                EnableCheckFiles = false,
                OutputDirectory = testDirectory.FullName
            };
            var logger = new XUnitLogger(outputHelper);
            Logger.LoggerOutput = logger;
            codeGenApp.Init();
            codeGenApp.Run();

            return (logger.Success, logger.ExitReason);
        }

        public static void AssertRanSuccessfully(int exitCode, string output)
        {
            if(exitCode != 0)
            {
                throw new Xunit.Sdk.AssertActualExpectedException(0, exitCode, output);
            }
        }

        private void SaveConfigFile(Config.ConfigFile config, string configName)
        {
            config.Id = configName;

            var serializer = new XmlSerializer(typeof(Config.ConfigFile));

            using (var configFile = File.Create(Path.Combine(testDirectory.FullName, configName + "-Mapping.xml")))
            {
                serializer.Serialize(configFile, config);
            }
        }

        public Config.IncludeRule CreateCppFile(string cppFileName, string cppFile, [CallerMemberName] string testName = "")
        {
            var includesDir = testDirectory.CreateSubdirectory("includes");
            File.WriteAllText(Path.Combine(includesDir.FullName, cppFileName + ".h"), cppFile);
            return new Config.IncludeRule
            {
                Attach = true,
                File = cppFileName + ".h",
                Namespace = testName,
            };
        }

        public static Config.IncludeDirRule GetTestFileIncludeRule([CallerMemberName] string testName = "")
        {
            return new Config.IncludeDirRule
            {
                Path = "includes"
            };
        }

        public Compilation GetCompilationForGeneratedCode([CallerMemberName] string assemblyName = "")
        {
            return CSharpCompilation.Create(assemblyName)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(GetSyntaxTrees(assemblyName));
        }

        private IEnumerable<SyntaxTree> GetSyntaxTrees(string assemblyName)
        {
            foreach (var child in testDirectory.CreateSubdirectory(assemblyName).EnumerateFiles("*.cs", SearchOption.AllDirectories))
            {
                using (var file = child.OpenRead())
                {
                    yield return CSharpSyntaxTree.ParseText(SourceText.From(file));
                }
            }
        }

        public static DirectoryInfo GenerateTestDirectory()
        {
            var tempFolder = Path.GetTempPath();
            var testFolderName = Path.GetRandomFileName();
            var testDirectoryInfo = Directory.CreateDirectory(Path.Combine(tempFolder, testFolderName));
            return testDirectoryInfo;
        }
    }
}
