using System.Globalization;

namespace Meziantou.Analyzer
{
    internal static class RuleIdentifiers
    {
        public const string UseStringComparison = "MA0001";
        public const string UseStringComparer = "MA0002";
        public const string UseNamedParameter = "MA0003";
        public const string UseConfigureAwaitFalse = "MA0004";
        public const string UseArrayEmpty = "MA0005";
        public const string UseStringEquals = "MA0006";
        public const string MissingCommaInObjectInitializer = "MA0007";
        public const string MissingStructLayoutAttribute = "MA0008";
        public const string MissingTimeoutParameterForRegex = "MA0009";
        public const string MarkAttributesWithAttributeUsageAttribute = "MA0010";
        public const string UseIFormatProviderParameter = "MA0011";
        public const string DoNotRaiseReservedExceptionType = "MA0012";
        public const string TypesShouldNotExtendSystemApplicationException = "MA0013";
        public const string DoNotRaiseApplicationException = "MA0014";
        public const string ArgumentExceptionShouldSpecifyArgumentName = "MA0015";
        public const string PreferReturnCollectionAbstractionInsteadOfImplementation = "MA0016";
        public const string AbstractTypesShouldNotHaveConstructors = "MA0017";
        public const string DoNotDeclareStaticMembersOnGenericTypes = "MA0018";
        public const string UseEventArgsEmpty = "MA0019";
        public const string UseListOfTMethodsInsteadOfEnumerableExtensionMethods = "MA0020";
        public const string DoNotUseStringGetHashCode = "MA0021";
        public const string ReturnTaskFromResultInsteadOfReturningNull = "MA0022";
        public const string UseRegexExplicitCaptureOptions = "MA0023";
        public const string DoNotUseEqualityComparerDefaultOfString = "MA0024";
        public const string DoNotRaiseNotImplementedException = "MA0025";

        public static string GetHelpUri(string idenfifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://github.com/meziantou/Meziantou.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);
        }
    }
}
