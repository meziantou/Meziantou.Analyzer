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
        public const string FixToDo = "MA0026";
        public const string DoNotRemoveOriginalExceptionFromThrowStatement = "MA0027";
        public const string OptimizeStringBuilderUsage = "MA0028";
        public const string OptimizeLinqUsage = "MA0029";
        public const string DuplicateEnumerable_OrderBy = "MA0030";
        public const string OptimizeEnumerable_Count = "MA0031";
        public const string UseAnOverloadThatHaveCancellationToken = "MA0032";
        public const string DontTagInstanceFieldsWithThreadStaticAttribute = "MA0033";
        public const string DontUseInstanceFieldsOfTypeAsyncLocal = "MA0034";
        public const string DontUseDangerousThreadingMethods = "MA0035";
        public const string MakeClassStatic = "MA0036";
        public const string RemoveEmptyStatement = "MA0037";
        public const string MakeMethodStatic = "MA0038";
        public const string DoNotUseServerCertificateValidationCallback = "MA0039";
        public const string UseAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailable = "MA0040";
        public const string MakePropertyStatic = "MA0041";
        public const string DoNotUseBlockingCallInAsyncContext = "MA0042";
        public const string UseNameofOperator = "MA0043";
        public const string RemoveUselessToString = "MA0044";
        public const string DoNotUseBlockingCall = "MA0045";
        public const string UseEventHandlerOfT = "MA0046";
        public const string DeclareTypesInNamespaces = "MA0047";
        public const string FileNameMustMatchTypeName = "MA0048";
        public const string TypeNameMustNotMatchNamespace = "MA0049";
        public const string ValidateArgumentsCorrectly = "MA0050";
        public const string MethodShouldNotBeTooLong = "MA0051";
        public const string ReplaceEnumToStringWithNameof = "MA0052";
        public const string ClassMustBeSealed = "MA0053";
        public const string IncludeCatchExceptionAsInnerException = "MA0054";
        public const string DoNotUseDestructor = "MA0055";
        public const string DoNotCallVirtualMethodInConstructor = "MA0056";

        public static string GetHelpUri(string idenfifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://github.com/meziantou/Meziantou.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);
        }
    }
}
