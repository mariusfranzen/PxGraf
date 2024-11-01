using Newtonsoft.Json.Linq;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal static class ResponseComparisonUtilities
    {
        internal static void CompareData(JArray arr1, JArray arr2, List<string> differences)
        {
            for (int i = 0; i < arr1.Count; i++)
            {
                JToken item1 = arr1[i];
                JToken item2 = arr2[i];

                bool item1IsNumber = item1.Type == JTokenType.Float || item1.Type == JTokenType.Integer;
                bool item2IsNumber = item2.Type == JTokenType.Float || item2.Type == JTokenType.Integer;

                if (item1IsNumber != item2IsNumber)
                {
                    differences.Add($"Data type mismatch at {TokenConstants.DATA_PATH}[{i}]: {item1.Type} vs {item2.Type} when comparing {TokenConstants.RESPONSE_SQVISUALIZATION} and {TokenConstants.DATASOURCE_PXWEBAPI}");
                    continue;
                }

                if (!item1IsNumber && !item2IsNumber)
                {
                    continue;
                }

                decimal value1 = item1.Value<decimal>();
                decimal value2 = item2.Value<decimal>();

                int decimalPlaces1 = BitConverter.GetBytes(decimal.GetBits(value1)[3])[2];
                int decimalPlaces2 = BitConverter.GetBytes(decimal.GetBits(value2)[3])[2];
                int decimalPlaces = Math.Min(decimalPlaces1, decimalPlaces2);

                decimal roundedValue1 = Math.Round(value1, decimalPlaces, MidpointRounding.AwayFromZero);
                decimal roundedValue2 = Math.Round(value2, decimalPlaces, MidpointRounding.AwayFromZero);

                if (roundedValue1 != roundedValue2)
                {
                    differences.Add($"Data mismatch at {TokenConstants.DATA_PATH}[{i}]: {value1} vs {value2} when comparing {TokenConstants.RESPONSE_SQVISUALIZATION}");
                }
            }
        }
    }
}
