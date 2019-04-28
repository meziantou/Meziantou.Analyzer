using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class ValidateArgumentsCorrectlyAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ValidateArgumentsCorrectlyAnalyzer>()
                .WithCodeFixProvider<ValidateArgumentsCorrectlyFixer>();
        }

        [TestMethod]
        public async Task ReturnVoid()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    void A()
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ReturnString()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    string A()
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task OutParameter()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    IEnumerable<int> A(out int a)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NoValidation()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    IEnumerable<int> A(string a)
    {
        yield return 0;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task SameBlock()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    IEnumerable<int> A(string a)
    {
        if (a == null)
        {
            throw new System.ArgumentNullException(nameof(a));
            yield return 0;
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StatementInMiddleOfArgumentValidation()
        {
            const string SourceCode = @"using System.Collections;
class TypeName
{
    IEnumerable A(string a)
    {
        if (a == null)
            throw new System.ArgumentNullException(nameof(a));

        yield break;

        if (a == null)
            throw new System.ArgumentNullException(nameof(a));
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ReportDiagnostic()
        {
            const string SourceCode = @"using System.Collections.Generic;
class TypeName
{
    IEnumerable<int> [|]A(string a)
    {
        if (a == null)
            throw new System.ArgumentNullException(nameof(a));

        yield return 0;
        if (a == null)
        {
            yield return 1;
        }
    }
}";

            const string CodeFix = @"using System.Collections.Generic;
class TypeName
{
    IEnumerable<int> A(string a)
    {
        if (a == null)
            throw new System.ArgumentNullException(nameof(a));

        return A();
        IEnumerable<int> A()
        {
            yield return 0;
            if (a == null)
            {
                yield return 1;
            }
        }
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
