
using System.Text.Json.Serialization;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal static class TokenConstants
    {
        internal const string DATASOURCE_PXUTILS = "pxUtils";
        internal const string DATASOURCE_PXWEBAPI = "pxWebApi";
        internal const string DATASOURCE_OLD = "pxWebApiOld";

        internal const string RESPONSE_SQ = "sq";
        internal const string RESPONSE_SQMETA = "sq-meta";
        internal const string RESPONSE_SQVISUALIZATION = "sq-visualization";

        internal const string ACCEPTED_FILE = "accepted.txt";
        internal const string REJECTED_FILE = "rejected.json";
        internal const string WHITELIST_FILE = "whitelist.json";
        internal const string SKIPPED_FILE = "skipped.txt";
        internal const string COLLECTED_FILE = "collected.txt";
        internal const string ISSUES_FILE = "issues.txt";

        internal const string DATA_PATH = "data";
    }

    internal enum ComparisonResult
    {
        Accepted,
        Rejected,
        Skipped
    }

    internal class WhitelistRule
    {
        public bool AnythingGoes { get; set; } = false;
        public string? ExpectedValue1 { get; set; } = null;
        public string? ExpectedValue2 { get; set; } = null;

        // Empty constructor for JSON serialization
        internal WhitelistRule() { }

        internal WhitelistRule(bool anythingGoes, string? expectedValue1 = null, string? expectedValue2 = null)
        {
            AnythingGoes = anythingGoes;
            ExpectedValue1 = expectedValue1;
            ExpectedValue2 = expectedValue2;
        }

        public override bool Equals(object? obj)
        {
            if (obj is WhitelistRule other)
            {
                return AnythingGoes == other.AnythingGoes && ExpectedValue1 == other.ExpectedValue1 && ExpectedValue2 == other.ExpectedValue2;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AnythingGoes, ExpectedValue1, ExpectedValue2);
        }
    }

    internal class WhitelistKey
    {
        public string ResponseToken { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        // Empty constructor for JSON serialization
        internal WhitelistKey() { }

        internal WhitelistKey(string responseToken, string path)
        {
            ResponseToken = responseToken;
            Path = path;
        }

        public override bool Equals(object? obj)
        {
            if (obj is WhitelistKey other)
            {
                return ResponseToken == other.ResponseToken && Path == other.Path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResponseToken, Path);
        }
    }
}
