using Tools.PxUtilsIntegrationTestTool;
using Newtonsoft.Json;
using PxGraf.Utility;

namespace Tools
{
    public class Urls
    {
        public string PxUtils { get; set; }
        public string PxWebApi { get; set; }
        public string PxWebApiOld { get; set; }

        public Urls() { }

        public Urls(string pxUtils, string pxWebApi, string pxWebApiOld)
        {
            PxUtils = pxUtils;
            PxWebApi = pxWebApi;
            PxWebApiOld = pxWebApiOld;
        }
    }

    public class Paths
    {
        public string ResponseDirectory { get; set; }
        public string QueriesFile { get; set; }
        public string ResultsDirectory { get; set; }

        public Paths() { }

        public Paths(string responseTempDirectory, string queriesFile, string resultsDirectory)
        {
            ResponseDirectory = responseTempDirectory;
            QueriesFile = queriesFile;
            ResultsDirectory = resultsDirectory;
        }
    }

    public class Limits
    {
        public int DelayBetweenRequestsMilliseconds { get; set; }
        public int DelayBetweenPendingAttemptsMilliseconds { get; set; }
        public int MaximumAmountOfPendingAttempts { get; set; }

        public Limits() { }

        public Limits(int delayBetweenRequestsMilliseconds, int delayBetweenPendingAttemptsMilliseconds, int maximumAmountOfPendingAttempts)
        {
            DelayBetweenRequestsMilliseconds = delayBetweenRequestsMilliseconds;
            DelayBetweenPendingAttemptsMilliseconds = delayBetweenPendingAttemptsMilliseconds;
            MaximumAmountOfPendingAttempts = maximumAmountOfPendingAttempts;
        }
    }

    public class Configuration
    {
        public Urls Urls { get; set; }
        public Paths Paths { get; set; }
        public Limits Limits { get; set; }

        public Configuration() { }

        public Configuration(Urls urls, Paths paths, Limits limits)
        {
            Urls = urls;
            Paths = paths;
            Limits = limits;
        }
    }

    public static class JsonOptions
    {
        public static readonly JsonSerializerSettings Default = 
            new()
            {
                Converters = { 
                    new WhitelistConverter(),
                    new MultilanguageStringConverter(),
                    new ResponseKeyConverter() 
                },
                Formatting = Formatting.Indented
            };
    }
}
