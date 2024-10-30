using Tools.PxUtilsIntegrationTestTool;
using Newtonsoft.Json;

namespace Tools
{
    internal static class Program
    {
        internal static Configuration Config;

        internal static async Task Main()
        {
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            string jsonString = await File.ReadAllTextAsync(configFilePath);
            Configuration? config = JsonConvert.DeserializeObject<Configuration>(jsonString, JsonOptions.Default);
            Config = config ?? throw new JsonException("Configuration file could not be parsed properly");

            int option;
            do
            {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1. Collect all responses from PxGraf instances");
                Console.WriteLine("2. Collect X random responses from PxGraf instances");
                Console.WriteLine("3. Compare responses");
                Console.WriteLine("4. Exit");
                option = ToolsUtilities.GetNumericAnswer(4);
                switch (option)
                {
                    case 1:
                        ResponseCollector responseCollector = new();
                        await responseCollector.Start();
                        break;
                    case 2:
                        Console.WriteLine("Enter the number of random responses to collect:");
                        int randomAmount = ToolsUtilities.GetNumericAnswer(int.MaxValue);
                        ResponseCollector randomResponseCollector = new(randomAmount);
                        await randomResponseCollector.Start();
                        break;
                    case 3:
                        ResponseComparer responseComparer = new();
                        await responseComparer.Start();
                        break;
                    default:
                        break;
                }
            } while (option != 4);

            Console.WriteLine("Exiting...");
            return;
        }
    }
}
