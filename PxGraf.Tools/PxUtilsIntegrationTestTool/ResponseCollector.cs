using PxGraf.Models.Responses;
using PxGraf.Models.Requests;
using Newtonsoft.Json;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal class ResponseCollector(int? randomAmount = null) : Command
    {
        private readonly string[] _dataSources = [TokenConstants.DATASOURCE_PXUTILS, TokenConstants.DATASOURCE_PXWEBAPI, TokenConstants.DATASOURCE_OLD];
        private readonly int? _randomAmount = randomAmount;

        internal override async Task Start()
        {
            string[] queries = await File.ReadAllLinesAsync(Program.Config.Paths.QueriesFile);
            for (int i = 0; i < queries.Length; i++)
            {
                queries[i] = Path.GetFileNameWithoutExtension(queries[i]);
            }
            if (_randomAmount.HasValue)
            {
                Random r = new();
                queries = queries.OrderBy(x => r.Next()).Take(_randomAmount.Value).ToArray();
                Console.WriteLine($"Random queries selected: {string.Join(", ", queries)}");
            }

            string collectedPath = Path.Combine(Program.Config.Paths.ResponseDirectory, TokenConstants.COLLECTED_FILE);
            Directory.CreateDirectory(Program.Config.Paths.ResponseDirectory);
            List<string> collected = [];
            if (!File.Exists(collectedPath))
                File.Create(collectedPath).Close();
            else
            {
                string[] strings = await File.ReadAllLinesAsync(collectedPath);
                collected = [..strings];
            }

            string issuesPath = Path.Combine(Program.Config.Paths.ResponseDirectory, TokenConstants.ISSUES_FILE);
            List<string> issues = [];
            if (File.Exists(issuesPath))
            {
                string[] strings = await File.ReadAllLinesAsync(issuesPath);
                issues = [..strings];
            }

            for (int i = 0; i < queries.Length; i++)
            {
                List<string> queryIssues = [];
                if (collected.Contains(queries[i]))
                {
                    Console.WriteLine($"Query ID: {queries[i]} already collected");
                    continue;
                }
                foreach (string dataSource in _dataSources)
                {
                    string url = ToolsUtilities.GetDataSourceUrl(dataSource);
                    string saveLocation = ResponseSaveLocation.SetResponseSaveLocation(dataSource);
                    string? visualizationIssue = await StoreSqVisualizationResponse(saveLocation, queries[i], url);
                    if (visualizationIssue != null)
                        queryIssues.Add(visualizationIssue);
                    string? sqIssue = await StoreSavedQueryResponse(saveLocation, queries[i], url);
                    if (sqIssue != null)
                        queryIssues.Add(sqIssue);
                    string? sqMetaIssue = await StoreSqMetaResponse(saveLocation, queries[i], url);
                    if (sqMetaIssue != null)
                        queryIssues.Add(sqMetaIssue);
                }
                if (queryIssues.Count != 0)
                {
                    issues.Add($"Query ID: {queries[i]}: {string.Join(", ", queryIssues)}");
                    await File.WriteAllLinesAsync(issuesPath, issues);
                }
                collected.Add(queries[i]);
                await File.WriteAllLinesAsync(collectedPath, collected);
            }
        }

        private static async Task<string?> StoreSqMetaResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQMETA);
            Directory.CreateDirectory(saveDirectory);
            (QueryMetaResponse? queryMeta, string content) = await GetQueryMetaAsync($"{url}/api/sq/meta/" + query);
            if (queryMeta != null)
            {
                await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                Console.WriteLine($"Query ID: {query}, meta response stored to {saveDirectory}");
                return null;
            }
            else
            {
                return $"{content} for sq-meta from {url}";
            }
        }

        private static async Task<string?> StoreSqVisualizationResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQVISUALIZATION);
            Directory.CreateDirectory(saveDirectory);
                (VisualizationResponse? queryVisualization, string content) = await GetQueryVisualizationAsync($"{url}/api/sq/visualization/" + query);
            if (queryVisualization != null)
            {
                await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                Console.WriteLine($"Query ID: {query}, visualization response stored to {saveDirectory}");
                return null;
            }
            else
            {
                return $"{content} response for visualization from {url}";
            }
        }

        private static async Task<string?> StoreSavedQueryResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQ);
            Directory.CreateDirectory(saveDirectory);
            (SaveQueryParams? queryMeta, string content) = await GetSavedQueryAsync($"{url}/api/sq/" + query);
            if (queryMeta != null)
            {
                await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                Console.WriteLine($"Query ID: {query}, saved query response stored to {saveDirectory}");
                return null;
            }
            else
            {
                return $"{content} response for sq from {url}";
            }
        }

        private static async Task<(QueryMetaResponse?, string)> GetQueryMetaAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get query meta with status code {response.StatusCode} from {url}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            QueryMetaResponse? meta = JsonConvert.DeserializeObject<QueryMetaResponse>(responseBody, JsonOptions.Default);
            if (meta == null)
            {
                Console.WriteLine($"Failed to deserialize query meta from {url}");
                return (null, "Failed to deserialize");
            }
            return (meta, responseBody);
        }

        private static async Task<(VisualizationResponse?, string)> GetQueryVisualizationAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get query visualization with status code {response.StatusCode} from {url}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            VisualizationResponse? visualization = JsonConvert.DeserializeObject<VisualizationResponse>(responseBody, JsonOptions.Default);
            if (visualization == null)
            {
                Console.WriteLine($"Failed to deserialize query visualization from {url}");
                return (null, "Failed to deserialize");
            }
            return (visualization, responseBody);
        }

        private static async Task<(SaveQueryParams?, string)> GetSavedQueryAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get saved query with status code {response.StatusCode} from {url}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            SaveQueryParams? savedQuery = JsonConvert.DeserializeObject<SaveQueryParams>(responseBody, JsonOptions.Default);
            if (savedQuery == null)
            {
                Console.WriteLine($"Failed to deserialize saved query from {url}");
                return (null, "Failed to deserialize");
            }
            return (savedQuery, responseBody);
        }
    }
}
