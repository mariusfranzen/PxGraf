using PxGraf.Models.Responses;
using PxGraf.Models.Requests;
using Newtonsoft.Json;
using PxGraf.Utility;
using System.Data.Common;


namespace Tools.PxUtilsIntegrationTestTool
{
    internal class ResponseCollector(int? randomAmount = null) : Command
    {
        private readonly string[] _dataSources = [TokenConstants.DATASOURCE_PXUTILS, TokenConstants.DATASOURCE_PXWEBAPI, TokenConstants.DATASOURCE_OLD];
        private readonly JsonSerializerSettings _jsonConverterSettings = new();
        private readonly int? _randomAmount = randomAmount;

        internal override async Task Start()
        {
            _jsonConverterSettings.Converters.Add(new MultilanguageStringConverter());

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
            if (!File.Exists(collectedPath))
                File.Create(collectedPath).Close();
            List<string> collected = [];

            for (int i = 0; i < queries.Length; i++)
            {
                if (collected.Contains(queries[i]))
                {
                    Console.WriteLine($"Query ID: {queries[i]} already collected");
                    continue;
                }
                foreach (string dataSource in _dataSources)
                {
                    string url = ToolsUtilities.GetDataSourceUrl(dataSource);
                    string saveLocation = ResponseSaveLocation.SetResponseSaveLocation(dataSource);
                    await StoreSqVisualizationResponse(saveLocation, queries[i], url);
                    await StoreSavedQueryResponse(saveLocation, queries[i], url);
                    await StoreSqMetaResponse(saveLocation, queries[i], url);
                }
                collected.Add(queries[i]);
                await File.WriteAllLinesAsync(collectedPath, collected);
            }
        }

        private async Task StoreSqMetaResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQMETA);
            Directory.CreateDirectory(saveDirectory);
            (QueryMetaResponse? queryMeta, string content) = await GetQueryMetaAsync($"{url}/api/sq/meta/" + query);
            if (queryMeta != null)
            {
                await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                Console.WriteLine($"Query ID: {query}, meta response stored to {saveDirectory}");
            }
            else
                Console.WriteLine($"Unable to parse query meta for query ID: {query}");
        }

        private async Task StoreSqVisualizationResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQVISUALIZATION);
            Directory.CreateDirectory(saveDirectory);
                (VisualizationResponse? queryVisualization, string content) = await GetQueryVisualizationAsync($"{url}/api/sq/visualization/" + query);
                if (queryVisualization != null)
                {
                    await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                    Console.WriteLine($"Query ID: {query}, visualization response stored to {saveDirectory}");
                }
                else
                    Console.WriteLine($"Unable to parse query visualization for query ID: {query}");
        }

        private async Task StoreSavedQueryResponse(string saveLocation, string query, string url)
        {
            string saveDirectory = Path.Combine(saveLocation, TokenConstants.RESPONSE_SQ);
            Directory.CreateDirectory(saveDirectory);
            (SaveQueryParams? queryMeta, string content) = await GetSavedQueryAsync($"{url}/api/sq/" + query);
            if (queryMeta != null)
            {
                await File.WriteAllTextAsync(Path.Combine(saveDirectory, query + ".json"), content);
                Console.WriteLine($"Query ID: {query}, saved query response stored to {saveDirectory}");
            }
            else
                Console.WriteLine($"Unable to parse saved query for query ID: {query}");
        }

        private async Task<(QueryMetaResponse?, string)> GetQueryMetaAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get query meta with status code {response.StatusCode}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            QueryMetaResponse? meta = JsonConvert.DeserializeObject<QueryMetaResponse>(responseBody, _jsonConverterSettings);
            return meta is not null ? (meta, responseBody) : (null, responseBody);
        }

        private async Task<(VisualizationResponse?, string)> GetQueryVisualizationAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get query visualization with status code {response.StatusCode}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            VisualizationResponse? visualization = JsonConvert.DeserializeObject<VisualizationResponse>(responseBody, _jsonConverterSettings);
            return visualization is not null ? (visualization, responseBody) : (null, responseBody);
        }

        private async Task<(SaveQueryParams?, string)> GetSavedQueryAsync(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get saved query with status code {response.StatusCode}");
                return (null, response.StatusCode.ToString());
            }
            string responseBody = await response.Content.ReadAsStringAsync();

            SaveQueryParams? savedQuery = JsonConvert.DeserializeObject<SaveQueryParams>(responseBody, _jsonConverterSettings);
            return savedQuery is not null ? (savedQuery, responseBody) : (null, responseBody);
        }
    }
}
