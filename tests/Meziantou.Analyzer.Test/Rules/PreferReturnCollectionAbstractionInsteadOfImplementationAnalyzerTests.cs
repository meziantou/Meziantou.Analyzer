﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzer>();
        }

        private static IEnumerable<object[]> ReturnTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "Task<List<int>>", false };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        private static IEnumerable<object[]> ParametersTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async Task Fields(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[|]") + type + @" _a;
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async Task Delegates(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate
    " + (isValid ? "" : "[|]") + type + @" A();
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async Task Delegates_Parameters(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate void A(
    " + (isValid ? "" : "[|]") + type + @" p);
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async Task Indexers(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[|]") + type + @" this[int value] => throw null;
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async Task Indexers_Parameters(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" int this[
    " + (isValid ? "" : "[|]") + type + @" value] => throw null;
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async Task Properties(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[|]") + type + @" A => throw null;
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async Task Methods(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[|]") + type + @" A() => throw null;
}");

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async Task Methods_Parameters(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" void A(
    " + (isValid ? "" : "[|]") + type + @" p) => throw null;
}");

            await project.ValidateAsync();
        }

        [TestMethod]
        public async Task PrivateContainer()
        {
            const string SourceCode = @"using System.Collections.Generic;
internal class Test
{
    public delegate List<int> B();
    public List<int> _a;
    protected List<int> _b;
    public List<int> A() => throw null;
}";
            await CreateProjectBuilder()
                 .WithSourceCode(SourceCode)
                 .ValidateAsync();
        }

        [TestMethod]
        public async Task InterfaceImplementation()
        {
            const string SourceCode = @"using System.Collections.Generic;
public interface ITest
{
    [|]List<int> A();
}

public class Test : ITest
{
    public List<int> A() => throw null;
}";
            await CreateProjectBuilder()
                 .WithSourceCode(SourceCode)
                 .ValidateAsync();
        }
    }
}
