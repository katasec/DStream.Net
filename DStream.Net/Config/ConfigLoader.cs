using HandlebarsDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DStream.Net.Config;

public class ConfigLoader
{
    private readonly string _filePath;
    private readonly IDeserializer _deserializer;

    public ConfigLoader(string filePath = "dstream.yaml")
    {
        _filePath = filePath;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        // Register custom Handlebars helper for environment variables
        Handlebars.RegisterHelper("env", (output, context, arguments) =>
        {
            if (arguments.Length == 1 && arguments[0] is string envVarName)
            {
                string envValue = Environment.GetEnvironmentVariable(envVarName)
                                  ?? $"{{Missing env var: {envVarName}}}";
                output.Write(envValue);
            }
        });
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Configuration file not found at: {_filePath}");
        }

        // Read the YAML content
        string yamlContent = File.ReadAllText(_filePath);

        // Compile and render the template with environment variable values
        var template = Handlebars.Compile(yamlContent);
        string renderedYaml = template(null);

        // Deserialize the rendered YAML into AppConfig
        return _deserializer.Deserialize<AppConfig>(renderedYaml);
    }
}
