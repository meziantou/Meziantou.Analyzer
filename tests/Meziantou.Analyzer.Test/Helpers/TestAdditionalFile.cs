using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TestHelper
{
    internal sealed class TestAdditionalFile : AdditionalText
    {
        private readonly string _content;

        public TestAdditionalFile(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_content);
        }
    }
}
