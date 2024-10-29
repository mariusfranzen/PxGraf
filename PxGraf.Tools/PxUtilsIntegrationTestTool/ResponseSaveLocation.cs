using PxGraf.Settings;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal static class ResponseSaveLocation
    {
        internal static string SetResponseSaveLocation(string dataSource)
        {
            string path = Program.Config.Paths.ResponseDirectory;
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Response directory path is not set in the configuration file.");
            string finalPath = Path.Combine(path, dataSource);
            Directory.CreateDirectory(finalPath);

            return finalPath;
        }
    }
}
