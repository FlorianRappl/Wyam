﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Wyam.Common.Configuration;
using Wyam.Common.Execution;
using Wyam.Common.IO;
using Wyam.Core.Execution;
using Wyam.Testing;

namespace Wyam.Configuration.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class ConfiguratorTests : BaseFixture
    {
        public class ConfigureMethodTests : ConfiguratorTests
        {
            [Test]
            public void ErrorInDeclarationsContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
class Y { };
foo bar;
---
int z = 0;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(2, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 3", exception.InnerExceptions[0].Message);
                StringAssert.StartsWith("Line 3", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void ErrorInDeclarationsWithEmptyLinesContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
class Y { };

foo bar;

---

int z = 0;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(2, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 4", exception.InnerExceptions[0].Message);
                StringAssert.StartsWith("Line 4", exception.InnerExceptions[1].Message);
            }

            [Test]
            public void ErrorInDeclarationsWithoutSetupContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
class Y { };

foo bar;

---

int z = 0;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(2, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 4", exception.InnerExceptions[0].Message);
                StringAssert.StartsWith("Line 4", exception.InnerExceptions[1].Message);
            }

            [Test]
            public void ErrorInConfigContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"

class Y { };
class X { };

---

int z = 0;

foo bar;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(1, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 10", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void ErrorInConfigWithoutDeclarationsContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
int z = 0;

foo bar;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(1, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 4", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void ErrorInConfigAfterLambdaExpansionContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
Pipelines.Add(
    Content(true
        && @doc.Get<bool>(""Key"") == false
    )
);

foo bar;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(1, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 8", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void ErrorInConfigAfterLambdaExpansionOnNewLineContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
Pipelines.Add(
    Content(
        @doc.Get<bool>(""Key"") == false
    )
);

foo bar;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(1, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 8", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void ErrorInConfigAfterLambdaExpansionWithArgumentSeparatorNewLinesContainsCorrectLineNumbers()
            {
                // Given
                RemoveListener();
                Configurator configurator = GetConfigurator();
                string configScript = @"
Pipelines.Add(
    If(
        @doc.Get<bool>(""Key""),
        Content(""Baz"")
    )
);

foo bar;
";

                // When
                AggregateException exception = null;
                try
                {
                    configurator.Configure(configScript);
                }
                catch (AggregateException ex)
                {
                    exception = ex;
                }

                // Then
                Assert.AreEqual(1, exception.InnerExceptions.Count);
                StringAssert.StartsWith("Line 9", exception.InnerExceptions[0].Message);
            }

            [Test]
            public void CanSetCustomDocumentFactory()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                string configScript = @"
public class MyDocument : CustomDocument
{
    public int Count { get; set; }

    protected override CustomDocument Clone()
    {
        return new MyDocument();
    }
}
---
DocumentFactory = new CustomDocumentFactory<MyDocument>(DocumentFactory);
";

                // When
                configurator.Configure(configScript);

                // Then
                Assert.AreEqual("CustomDocumentFactory`1", engine.DocumentFactory.GetType().Name);
            }

            [Test]
            public void SetCustomDocumentTypeSetsDocumentFactory()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                string configScript = @"
public class MyDocument : CustomDocument
{
    public int Count { get; set; }

    protected override CustomDocument Clone()
    {
        return new MyDocument();
    }
}
---
SetCustomDocumentType<MyDocument>();
";

                // When
                configurator.Configure(configScript);

                // Then
                Assert.AreEqual("CustomDocumentFactory`1", engine.DocumentFactory.GetType().Name);
            }

            [Test]
            public void SetsPrimitiveMetadata()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                string configScript = @"
                    InitialMetadata[""TestString""] = ""teststring"";
                    InitialMetadata[""TestInt""] = 1234;
                    InitialMetadata[""TestFloat""] = 1234.567;
                    InitialMetadata[""TestBool""] = true;
                ";

                // When
                configurator.Configure(configScript);

                // Then
                Assert.AreEqual("teststring", engine.InitialMetadata["TestString"]);
                Assert.AreEqual(1234, engine.InitialMetadata["TestInt"]);
                Assert.AreEqual(1234.567, engine.InitialMetadata["TestFloat"]);
                Assert.AreEqual(true, engine.InitialMetadata["TestBool"]);
            }

            [Test]
            public void AddsPipelineAndModules()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                string configScript = @"
                    Pipelines.Add(
                        new ReadFiles(""*.cshtml""),
	                    new WriteFiles("".html""));
                ";

                // When
                configurator.Configure(configScript);

                // Then
                Assert.AreEqual(1, engine.Pipelines.Count);
                Assert.AreEqual(2, engine.Pipelines.Values.First().Count);
            }

            [Test]
            public void SupportsGlobalConstructorMethods()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                string configScript = @"
                    Pipelines.Add(
                        ReadFiles(""*.cshtml""),
	                    WriteFiles("".html""));
                ";

                // When
                configurator.Configure(configScript);

                // Then
                Assert.AreEqual(1, engine.Pipelines.Count);
                Assert.AreEqual(2, engine.Pipelines.Values.First().Count);
            }

            [Test]
            public void AppliesRecipeBeforeMetadata()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                configurator.AssemblyLoader.Add(Assembly.GetExecutingAssembly().FullName);
                configurator.RecipeName = nameof(MetadataTestRecipe);
                configurator.GlobalMetadata = new Dictionary<string, object>
                {
                    { "Foo", "Baz" }
                };

                // When 
                configurator.Configure(string.Empty);

                // Then
                Assert.AreEqual("Baz", engine.GlobalMetadata["Foo"]);
            }
        }

        public class AddRecipePackageAndSetThemeMethodTests : ConfiguratorTests
        {
            [Test]
            public void AddsRecipePackage()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = nameof(KnownRecipe.Blog);
                configurator.PackageInstaller.AddPackage("Foobar");

                // When
                configurator.AddRecipePackageAndSetTheme();

                // Then
                Assert.AreEqual(2, configurator.PackageInstaller.PackageIds.Count);
            }

            [Test]
            public void DoesNotAddRecipePackageIfAlreadyAdded()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = nameof(KnownRecipe.Blog);
                configurator.PackageInstaller.AddPackage(KnownRecipe.Blog.PackageId);

                // When
                configurator.AddRecipePackageAndSetTheme();

                // Then
                Assert.AreEqual(1, configurator.PackageInstaller.PackageIds.Count);
            }

            [Test]
            public void SetsDefaultTheme()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = nameof(KnownRecipe.Blog);

                // When
                configurator.AddRecipePackageAndSetTheme();

                // Then
                Assert.AreEqual(KnownRecipe.Blog.DefaultTheme, configurator.Theme);
            }

            [Test]
            public void DoesNotSetThemeIfThemeAlreadySet()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = nameof(KnownRecipe.Blog);
                configurator.Theme = "Foo";

                // When
                configurator.AddRecipePackageAndSetTheme();

                // Then
                Assert.AreEqual("Foo", configurator.Theme);
            }

            [Test]
            public void DoesNotFailIfRecipeNotSet()
            {
                // Given
                Configurator configurator = GetConfigurator();

                // When
                configurator.AddRecipePackageAndSetTheme();
            }

            [Test]
            public void DoesNotFailIfRecipeNotFound()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = "Foo";

                // When
                configurator.AddRecipePackageAndSetTheme();
            }
        }

        public class AddThemePackagesAndPathMethodTests : ConfiguratorTests
        {
            [Test]
            public void AddsThemePackage()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.Theme = nameof(KnownTheme.CleanBlog);
                configurator.PackageInstaller.AddPackage("Foobar");

                // When
                configurator.AddThemePackagesAndPath();

                // Then
                Assert.AreEqual(2, configurator.PackageInstaller.PackageIds.Count);
            }

            [Test]
            public void DoesNotAddThemePackageIfAlreadyAdded()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.Theme = nameof(KnownTheme.CleanBlog);
                configurator.PackageInstaller.AddPackage(KnownTheme.CleanBlog.PackageIds.First());

                // When
                configurator.AddThemePackagesAndPath();

                // Then
                Assert.AreEqual(1, configurator.PackageInstaller.PackageIds.Count);
            }

            [Test]
            public void OutputsWarningIfRecipeAndThemeDoNotMatch()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = "Foo";
                configurator.Theme = nameof(KnownTheme.CleanBlog);

                // When
                Assert.Throws<Exception>(() => configurator.AddThemePackagesAndPath());
            }

            [Test]
            public void DoesNotOutputWarningIfRecipeAndThemeDoMatch()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = KnownTheme.CleanBlog.Recipe;
                configurator.Theme = nameof(KnownTheme.CleanBlog);

                // When
                configurator.AddThemePackagesAndPath();
            }

            [Test]
            public void DoesNotOutputWarningIfRecipeAndThemeCaseDoNotMatch()
            {
                // Given
                Configurator configurator = GetConfigurator();
                configurator.RecipeName = KnownTheme.CleanBlog.Recipe.ToUpper();
                configurator.Theme = nameof(KnownTheme.CleanBlog);

                // When
                configurator.AddThemePackagesAndPath();
            }

            [Test]
            public void UnknownThemeInsertsInputPath()
            {
                // Given
                Engine engine = new Engine();
                Configurator configurator = GetConfigurator(engine);
                configurator.Theme = "../MyTheme";

                // When
                configurator.AddThemePackagesAndPath();

                // Then
                Assert.AreEqual(engine.FileSystem.InputPaths.First().FullPath, "../MyTheme");
            }
        }

        private Configurator GetConfigurator()
        {
            Engine engine = new Engine();
            return GetConfigurator(engine);
        }

        private Configurator GetConfigurator(Engine engine)
        {
            Configurator configurator = new Configurator(engine);
            return configurator;
        }
    }

    public class MetadataTestRecipe : IRecipe
    {
        public void Apply(IEngine engine)
        {
            engine.GlobalMetadata["Foo"] = "Bar";
        }

        public void Scaffold(IDirectory directory)
        {
            throw new NotImplementedException();
        }
    }
}
