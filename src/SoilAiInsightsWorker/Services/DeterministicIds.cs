using System.Security.Cryptography;
using System.Text;

namespace SoilAiInsightsWorker.Services;

public static class DeterministicIds
{
    public static string RecommendationId(string deviceId, DateTime insightDateStartUtc)
    {
        return StableId("reco", deviceId, insightDateStartUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
    }

    public static string ForecastId(string deviceId, DateTime forecastDateStartUtc, int horizonHours)
    {
        return StableId(
            "fcst",
            deviceId,
            forecastDateStartUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            horizonHours.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static string StableId(string prefix, params string[] parts)
    {
        var raw = string.Join('\u001f', parts);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{prefix}_{hex[..24]}";
    }
}
