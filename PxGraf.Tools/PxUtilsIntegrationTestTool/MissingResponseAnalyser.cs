using Newtonsoft.Json;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal class MissingResponseAnalyser : Command
    {
        private string _responseLocation = string.Empty;
        private string _resultsLocation = string.Empty;
        private string[] _queries = [];

        internal override async Task Start()
        {
            _resultsLocation = Program.Config.Paths.ResultsDirectory;
            if (string.IsNullOrEmpty(_resultsLocation))
            {
                throw new InvalidOperationException("Results directory is not set in the configuration file.");
            }

            ToolsUtilities.CreateDirectoryOrRemoveContents(_resultsLocation, true);
            _responseLocation = Program.Config.Paths.ResponseDirectory;

            _queries = await File.ReadAllLinesAsync(Path.Combine(Program.Config.Paths.ResponseDirectory, TokenConstants.COLLECTED_FILE));

            Dictionary<ResponsesKey, List<string>> responsesInformation = [];
            for (int i = 0; i < _queries.Length; i++)
            {
                _queries[i] = Path.GetFileNameWithoutExtension(_queries[i]);
                ResponsesKey key = new()
                {
                    Sq = SearchForResponse(_queries[i], TokenConstants.RESPONSE_SQ),
                    SqMeta = SearchForResponse(_queries[i], TokenConstants.RESPONSE_SQMETA),
                    SqVisualization = SearchForResponse(_queries[i], TokenConstants.RESPONSE_SQVISUALIZATION)
                };
                if (responsesInformation.ContainsKey(key))
                {
                    responsesInformation[key].Add(_queries[i]);
                }
                else
                {
                    responsesInformation[key] = [_queries[i]];
                }
            }
            string content = JsonConvert.SerializeObject(responsesInformation, JsonOptions.Default);
            await File.WriteAllTextAsync(Path.Combine(_resultsLocation, TokenConstants.MISSING_INFO_FILE), content);
            Console.WriteLine("-------------------------------------------------------------");
            Console.WriteLine("| {0,-14} | {1,-14} | {2,-14} | {3,-14} |", "Query", "Px.Utils", "PxWebApi", "PxWebApiOld");
            Console.WriteLine("-------------------------------------------------------------");

            for (int i = 0; i < responsesInformation.Count; i++)
            {
                var key = responsesInformation.Keys.ElementAt(i);
                Console.WriteLine("| {0,-14} | {1,-14} | {2,-14} | {3,-14} |", "sq", FormatBool(key.Sq.PxUtilsResponseFound), FormatBool(key.Sq.PxWebApiResponseFound), FormatBool(key.Sq.PxWebApiOldResponseFound));
                Console.WriteLine("| {0,-14} | {1,-14} | {2,-14} | {3,-14} |", "sq-meta", FormatBool(key.SqMeta.PxUtilsResponseFound), FormatBool(key.SqMeta.PxWebApiResponseFound), FormatBool(key.SqMeta.PxWebApiOldResponseFound));
                Console.WriteLine("| {0,-14} | {1,-14} | {2,-14} | {3,-14} |", "visualization", FormatBool(key.SqVisualization.PxUtilsResponseFound), FormatBool(key.SqVisualization.PxWebApiResponseFound), FormatBool(key.SqVisualization.PxWebApiOldResponseFound));
                Console.WriteLine($"| Queries        | {string.Join(", ", responsesInformation.Values.ElementAt(i))}");
                Console.WriteLine("-------------------------------------------------------------");
            }
            Console.WriteLine($"Missing responses information saved to {Path.Combine(_resultsLocation, TokenConstants.MISSING_INFO_FILE)}");
        }

        private static string FormatBool(bool value)
        {
            const string missingText = "\u001b[31mMissing\u001b[0m";
            return value ? "Found" : missingText + new string(' ', 14 - "Missing".Length);
        }

        internal Responses SearchForResponse(string query, string responseToken)
        {
            Responses responses = new()
            {
                PxUtilsResponseFound = CheckIfResponseExists(query, responseToken, Path.Combine(_responseLocation, TokenConstants.DATASOURCE_PXUTILS)),
                PxWebApiResponseFound = CheckIfResponseExists(query, responseToken, Path.Combine(_responseLocation, TokenConstants.DATASOURCE_PXWEBAPI)),
                PxWebApiOldResponseFound = CheckIfResponseExists(query, responseToken, Path.Combine(_responseLocation, TokenConstants.DATASOURCE_OLD))
            };
            return responses;
        }

        internal static bool CheckIfResponseExists(string query, string responseToken, string path)
        {
            return File.Exists(Path.Combine(path, responseToken, $"{query}.json"));
        }
    }

    internal struct Responses
    {
        internal bool PxUtilsResponseFound { get; set; }
        internal bool PxWebApiResponseFound { get; set; }
        internal bool PxWebApiOldResponseFound { get; set; }
    }

    internal struct ResponsesKey
    {
        internal Responses Sq { get; set; }
        internal Responses SqMeta { get; set; }
        internal Responses SqVisualization { get; set; }
    }
}
