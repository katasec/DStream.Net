namespace DStream.Net;

public class AppConfig
{
    public string? DbType { get; set; }
    public string? DbConnectionString { get; set; }
    public string? AzureEventHubConnectionString { get; set; }
    public string? AzureEventHubName { get; set; }
    public string? OutputType { get; set; }  // Make sure this matches "output_type" in YAML
    public List<TableConfig>? Tables { get; set; }
}