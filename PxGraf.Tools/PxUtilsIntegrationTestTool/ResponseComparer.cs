using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal class ResponseComparer : Command
    {
        private string _resultsLocation = string.Empty;
        private string[] _queries = [];
        private Dictionary<string, string> _rejected = [];
        private Dictionary<WhitelistKey, List<WhitelistRule>> _whitelist = [];
        private string _responseLocation = string.Empty;

        internal override async Task Start()
        {
            InitiateResults();
            _responseLocation = Program.Config.Paths.ResponseDirectory;
            _queries = await File.ReadAllLinesAsync(Path.Combine(Program.Config.Paths.ResponseDirectory, TokenConstants.COLLECTED_FILE));
            for (int i = 0; i < _queries.Length; i++)
            {
                _queries[i] = Path.GetFileNameWithoutExtension(_queries[i]);
            }

            await IterateResponses();
            ToolsUtilities.DeleteDirectory(Path.Combine(_resultsLocation, "temp"));
        }

        private void InitiateResults()
        {
            _resultsLocation = Program.Config.Paths.ResultsDirectory;
            if (string.IsNullOrEmpty(_resultsLocation))
            {
                throw new InvalidOperationException("Results directory is not set in the configuration file.");
            }

            ToolsUtilities.CreateDirectoryOrRemoveContents(_resultsLocation, true);

            string acceptedPath = Path.Combine(_resultsLocation, TokenConstants.ACCEPTED_FILE);
            string rejectedPath = Path.Combine(_resultsLocation, TokenConstants.REJECTED_FILE);
            string whitelistPath = Path.Combine(_resultsLocation, TokenConstants.WHITELIST_FILE);
            string skippedPath = Path.Combine(_resultsLocation, TokenConstants.SKIPPED_FILE);
            if (!File.Exists(acceptedPath))
                File.Create(acceptedPath).Close();
            if (!File.Exists(rejectedPath))
                File.Create(rejectedPath).Close();
            if (!File.Exists(whitelistPath))
                File.Create(whitelistPath).Close();
            if (!File.Exists(skippedPath))
                File.Create(skippedPath).Close();
        }

        private async Task IterateResponses()
        {
            string acceptedPath = Path.Combine(_resultsLocation, TokenConstants.ACCEPTED_FILE);
            string rejectedPath = Path.Combine(_resultsLocation, TokenConstants.REJECTED_FILE);
            string whitelistPath = Path.Combine(_resultsLocation, TokenConstants.WHITELIST_FILE);
            string skippedPath = Path.Combine(_resultsLocation, TokenConstants.SKIPPED_FILE);

            string[] acceptedArray = await File.ReadAllLinesAsync(acceptedPath);
            string[] skippedArray = await File.ReadAllLinesAsync(skippedPath);
            List<string> accepted = [.. acceptedArray];
            List<string> skipped = [.. skippedArray];

            _rejected = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(rejectedPath), JsonOptions.Default) ?? [];
            _whitelist = JsonConvert.DeserializeObject<Dictionary<WhitelistKey, List<WhitelistRule>>>(await File.ReadAllTextAsync(whitelistPath), JsonOptions.Default) ?? [];

            bool skipReevaluations = false;

            foreach (string query in _queries)
            {
                if (accepted.Contains(query))
                {
                    Console.WriteLine($"Query: {query} has already been accepted.");
                    continue;
                }
                else if (skipped.Contains(query))
                {
                    Console.WriteLine($"Query: {query} has already been skipped.");
                    continue;
                }
                else if (_rejected.TryGetValue(query, out string? value) && !skipReevaluations)
                {
                    Console.WriteLine($"Query: {query} has been rejected. Reason: {value}. Do you want to re-evaluate this query? (y/n):");
                    if (!ToolsUtilities.GetBooleanAnswer())
                    {
                        Console.WriteLine("Do you want to skip re-evaluations for the rest of the queries? (y/n):");
                        skipReevaluations = ToolsUtilities.GetBooleanAnswer();
                        continue;
                    }
                }

                ComparisonResult sqResult = CompareResults(query, TokenConstants.RESPONSE_SQ);
                ComparisonResult sqMetaResult = CompareResults(query, TokenConstants.RESPONSE_SQMETA);
                ComparisonResult sqVisualizationResult = CompareResults(query, TokenConstants.RESPONSE_SQVISUALIZATION);

                if (sqResult != ComparisonResult.Rejected && sqMetaResult != ComparisonResult.Rejected && sqVisualizationResult != ComparisonResult.Rejected)
                {
                    if (sqResult == ComparisonResult.Skipped && sqMetaResult == ComparisonResult.Skipped && sqVisualizationResult == ComparisonResult.Skipped)
                    {
                        Console.WriteLine($"Query: {query} has been skipped because it yielded no responses.");
                        skipped.Add(query);
                        await File.WriteAllLinesAsync(Path.Combine(_resultsLocation, TokenConstants.SKIPPED_FILE), skipped);
                    }
                    else
                    {
                        Console.WriteLine($"Query: {query} has been accepted.");
                        accepted.Add(query);
                        await File.WriteAllLinesAsync(Path.Combine(_resultsLocation, TokenConstants.ACCEPTED_FILE), accepted);

                        _rejected.Remove(query);
                        await File.WriteAllTextAsync(Path.Combine(_resultsLocation, TokenConstants.REJECTED_FILE), JsonConvert.SerializeObject(_rejected));
                    }
                }
            }
        }

        private ComparisonResult CompareResults(string query, string responseToken)
        {
            string pxUtilsPath = Path.Combine(_responseLocation, TokenConstants.DATASOURCE_PXUTILS, responseToken, query + ".json");
            string pxWebApiPath = Path.Combine(_responseLocation, TokenConstants.DATASOURCE_PXWEBAPI, responseToken, query + ".json");
            string pxWebOldPath = Path.Combine(_responseLocation, TokenConstants.DATASOURCE_OLD, responseToken, query + ".json");

            bool pxUtilsExists = File.Exists(pxUtilsPath);
            bool pxWebApiExists = File.Exists(pxWebApiPath);
            bool pxWebOldExists = File.Exists(pxWebOldPath);

            if (!pxUtilsExists || !pxWebApiExists || !pxWebOldExists)
            {
                if (!pxUtilsExists && !pxWebApiExists && !pxWebOldExists)
                {
                    Console.WriteLine($"No responses for {query} {responseToken}. Skipping.");
                    return ComparisonResult.Skipped;
                }
                string reason = $"Reason: {responseToken}:";
                reason += !pxUtilsExists ? " PxUtils response file does not exist." : "";
                reason += !pxWebApiExists ? " PxWebApi response file does not exist." : "";
                reason += !pxWebOldExists ? " PxWebOld response file does not exist." : "";
                reason = reason.Trim();
                Console.WriteLine($"Query: {query} has been rejected. Reason: {reason}");
                TryAddRejection(query, reason);
                return ComparisonResult.Rejected;
            }

            string pxUtilsResponse = File.ReadAllText(pxUtilsPath);
            string pxWebApiResponse = File.ReadAllText(pxWebApiPath);
            string pxWebOldResponse = File.ReadAllText(pxWebOldPath);

            KeyValuePair<string, string> pxUtils = new (TokenConstants.DATASOURCE_PXUTILS, pxUtilsResponse);
            KeyValuePair<string, string> pxWebApi = new (TokenConstants.DATASOURCE_PXWEBAPI, pxWebApiResponse);
            KeyValuePair<string, string> pxWebOld = new (TokenConstants.DATASOURCE_OLD, pxWebOldResponse);

            if (pxUtilsResponse != pxWebApiResponse &&
                CompareDeserializedObjects(pxUtils, pxWebApi, responseToken) > 0 &&
                DifferenceControls(query, pxUtilsPath, pxWebApiPath, responseToken, TokenConstants.DATASOURCE_PXUTILS, TokenConstants.DATASOURCE_PXWEBAPI))
            {
                return ComparisonResult.Rejected;
            }

            if (pxUtilsResponse != pxWebOldResponse &&
                CompareDeserializedObjects(pxUtils, pxWebOld, responseToken) > 0 &&
                DifferenceControls(query, pxUtilsPath, pxWebOldPath, responseToken, TokenConstants.DATASOURCE_PXUTILS, TokenConstants.DATASOURCE_OLD))
            {
                return ComparisonResult.Rejected;
            }

            return ComparisonResult.Accepted;
        }

        private bool DifferenceControls(string query, string resp1, string resp2, string responseToken, string name1, string name2)
        {
            Console.WriteLine($"Select an option to handle {responseToken} response for {query}:");
            Console.WriteLine("1. Open diff in VSCode editor");
            Console.WriteLine("2. Accept reviewed differences");
            Console.WriteLine("3. Reject the query");
            int option = ToolsUtilities.GetNumericAnswer(3);
            switch (option)
            {
                case 1:
                    OpenDiffInEditor(resp1, resp2, query, name1, name2);
                    return DifferenceControls(query, resp1, resp2, responseToken, name1, name2);
                case 2:
                    return false;
                case 3:
                    ReportRejection(query);
                    return true;
            }
            return true;
        }

        private void ReportRejection(string query)
        {
            Console.WriteLine("Enter a reason for rejecting the response: ");
            string? reason = Console.ReadLine();
            if (reason is null || reason == string.Empty)
            {
                Console.WriteLine("Invalid input. Try again.");
                ReportRejection(query);
            }
            TryAddRejection(query, reason);
        }
        
        private void TryAddRejection(string query, string reason)
        {
            if (!_rejected.TryAdd(query, reason))
            {
                _rejected[query] = reason;
            }
            File.WriteAllText(Path.Combine(_resultsLocation, TokenConstants.REJECTED_FILE), JsonConvert.SerializeObject(_rejected));
        }

        private void OpenDiffInEditor(string resp1, string resp2, string query, string name1, string name2)
        {
            string json1 = File.ReadAllText(resp1);
            string json2 = File.ReadAllText(resp2);

            string formattedJson1 = JToken.Parse(json1).ToString(Formatting.Indented);
            string formattedJson2 = JToken.Parse(json2).ToString(Formatting.Indented);

            if (!Directory.Exists($"{_resultsLocation}\\temp")) Directory.CreateDirectory($"{_resultsLocation}\\temp");

            string temp1 = $"{_resultsLocation}\\temp\\diff-{name1}-for-{query}-temp.json";
            string temp2 = $"{_resultsLocation}\\temp\\diff-{name2}-for-{query}-temp.json";

            File.WriteAllText(temp1, formattedJson1);
            File.WriteAllText(temp2, formattedJson2);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"--diff \"{temp1}\" \"{temp2}\"",
                    RedirectStandardOutput = false,
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };
            process.Start();
        }

        private int CompareDeserializedObjects(KeyValuePair<string, string> resp1, KeyValuePair<string, string> resp2, string responseToken)
        {
            JObject obj1 = JObject.Parse(resp1.Value);
            JObject obj2 = JObject.Parse(resp2.Value);

            KeyValuePair<string, JToken> kvp1 = new (resp1.Key, obj1);
            KeyValuePair<string, JToken> kvp2 = new (resp2.Key, obj2);

            var differences = GetDifferences(kvp1, kvp2, responseToken);
            if (differences.Count > 0)
            {
                Console.WriteLine("Differences found:");
                foreach (var difference in differences)
                {
                    Console.WriteLine(difference);
                }
            }
            else
            {
                Console.WriteLine("No differences found.");
            }
            return differences.Count;
        }

        private List<string> GetDifferences(KeyValuePair<string, JToken> kvp1, KeyValuePair<string, JToken> kvp2, string responseToken)
        {
            List<string> differences = [];
            CompareJTokens(kvp1, kvp2, differences, "", responseToken);

            return differences;
        }

        private void CompareJTokens(KeyValuePair<string, JToken> token1, KeyValuePair<string, JToken> token2, List<string> differences, string path, string responseToken)
        {
            WhitelistKey wlKey = new(responseToken, path);
            if (_whitelist.TryGetValue(wlKey, out List<WhitelistRule>? whitelist) &&
                whitelist.Exists(wl => wl.AnythingGoes || (wl.ExpectedValue1 == token1.Value.ToString() && wl.ExpectedValue2 == token2.Value.ToString())))
            {
                Console.WriteLine($"{path} is a whitelisted exception");
                return;
            }

            if (token1.Value.Type != token2.Value.Type)
            {
                Console.WriteLine($"Type mismatch at {path}: {token1.Value.Type} vs {token2.Value.Type} when comparing {token1.Key} and {token2.Key}");
                if (!PromptWhitelisting(responseToken, path, token1.Value.ToString(), token2.Value.ToString()))
                {
                    differences.Add($"Type mismatch at {path}: {token1.Value.Type} vs {token2.Value.Type} when comparing {token1.Key} and {token2.Key}");
                }
                return;
            }

            switch (token1.Value.Type)
            {
                case JTokenType.Object:
                    var obj1 = (JObject)token1.Value;
                    var obj2 = (JObject)token2.Value;
                    var allKeys = new HashSet<string>(obj1.Properties().Select(p => p.Name).Union(obj2.Properties().Select(p => p.Name)));
                    foreach (var key in allKeys)
                    {
                        var childPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";
                        KeyValuePair<string, JToken> kvp1 = new(token1.Key, obj1[key]);
                        KeyValuePair<string, JToken> kvp2 = new(token2.Key, obj2[key]);
                        CompareJTokens(kvp1, kvp2, differences, childPath, responseToken);
                    }
                    break;

                case JTokenType.Array:
                    CompareArrays((JArray)token1.Value, (JArray)token2.Value, differences, path, responseToken);
                    break;

                default:
                    if (!JToken.DeepEquals(token1.Value, token2.Value))
                    {
                        Console.WriteLine($"Value mismatch at {path}: {token1.Value} vs {token2.Value} when comparing {token1.Key} and {token2.Key}");
                        if (!PromptWhitelisting(responseToken, path, token1.Value.ToString(), token2.Value.ToString()))
                        {
                            differences.Add($"Value mismatch at {path}: {token1.Value} vs {token2.Value} when comparing {token1.Key} and {token2.Key}");
                        }
                    }
                    break;
            }
        }

        private void CompareArrays(JArray arr1, JArray arr2, List<string> differences, string path, string responseToken)
        {
            if (responseToken == TokenConstants.RESPONSE_SQVISUALIZATION && path == TokenConstants.DATA_PATH)
            {
                ResponseComparisonUtilities.CompareData(arr1, arr2, differences);
                return;
            }

            if (arr1.Count != arr2.Count)
            {
                Console.WriteLine($"Array length mismatch at {path}: {arr1.Count} vs {arr2.Count}");
                differences.Add($"Array length mismatch at {path}: {arr1.Count} vs {arr2.Count}");
                return;
            }

            for (int i = 0; i < arr1.Count; i++)
            {
                KeyValuePair<string, JToken> kvp1 = new(responseToken, arr1[i]);
                KeyValuePair<string, JToken> kvp2 = new(responseToken, arr2[i]);
                CompareJTokens(kvp1, kvp2, differences, path, responseToken);
            }
        }

        private bool PromptWhitelisting(string responseToken, string path, string value1, string value2)
        {
            Console.WriteLine($"Do you want to add a whitelist rule for this property: {path} for {responseToken}? (y/n)");
            if (ToolsUtilities.GetBooleanAnswer())
            {
                WhitelistKey wlKey = new(responseToken, path);
                Console.WriteLine($"Only accept these values: {value1} and {value2} (y) or whitelist the entire property (n)? (y/n)");
                bool anythingGoes = !ToolsUtilities.GetBooleanAnswer();
                WhitelistRule rule = anythingGoes ? new(true) : new(false, value1, value2);
                if (_whitelist.TryGetValue(wlKey, out List<WhitelistRule>? whitelist))
                {
                    whitelist.Add(rule);
                }
                else
                {
                    whitelist = [rule];
                    _whitelist.Add(wlKey, whitelist);
                }
                File.WriteAllText(Path.Combine(_resultsLocation, TokenConstants.WHITELIST_FILE), JsonConvert.SerializeObject(_whitelist, JsonOptions.Default));
                return true;
            }
            return false;
        }
    }
}
