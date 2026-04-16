namespace SoilAiInsightsWorker.Services;

/// <summary>
/// Loads SQL templates from output directory and replaces {{PLACEHOLDER}} tokens.
/// </summary>
public sealed class SqlTemplateProvider
{
    private readonly IHostEnvironment _env;

    public SqlTemplateProvider(IHostEnvironment env)
    {
        _env = env;
    }

    public string LoadTemplate(string relativePath)
    {
        var basePath = AppContext.BaseDirectory;
        var path = Path.Combine(basePath, "Sql", relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"SQL template not found: {path}");
        return File.ReadAllText(path);
    }

    public static string Apply(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var s = template;
        foreach (var kv in tokens)
            s = s.Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.Ordinal);
        return s;
    }
}
