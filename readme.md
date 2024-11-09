# DStream.Net

DStream.Net is a .NET application designed to monitor SQL Server Change Data Capture (CDC) tables and publish change notifications to various external systems such as Azure Event Hub, Azure Service Bus, or the console. This project supports a configurable approach, making it easy to add new database providers and downstream publishers.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Folder Structure](#folder-structure)
- [Configuration](#configuration)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Extending the Project](#extending-the-project)
- [Contributing](#contributing)
- [License](#license)

## Features

- **CDC Monitoring**: Monitors specified tables in SQL Server for CDC changes.
- **Pluggable Downstream Publishers**: Supports sending CDC change notifications to multiple external systems.
- **Configurable**: Set up your connection strings, polling intervals, and downstream providers through a YAML configuration file.
- **Extensible**: Easily add new downstream systems and database providers.

## Architecture

DStream.Net is built with the following components:

- **Database Monitor**: Manages CDC polling and change detection for SQL Server tables.
- **Change Publishers**: Responsible for sending CDC change notifications to various downstream systems. Current supported options are:
  - Console Output
  - Azure Event Hub
  - Azure Service Bus
- **Configuration Loader**: Reads configuration from a YAML file and loads it into the application.
- **Startup Validator**: Validates required configurations before the application starts.

## Folder Structure

```plaintext
DStream.Net
├── Config                    # Configuration loading and validation
│   ├── AppConfig.cs
│   ├── ConfigLoader.cs
│   └── TableConfig.cs
├── Database                  # Database-related code
│   ├── SQLServer
│   │   ├── CDCChangeResult.cs
│   │   ├── CheckPointManager.cs
│   │   ├── DatabaseMetadataHelper.cs
│   │   ├── LSNManager.cs
│   │   └── SQLServerMonitor.cs
│   ├── BackOffManager.cs
│   └── IDatabaseMonitor.cs
├── ChangePublishers          # Downstream publishers for sending notifications
│   ├── IChangePublisher.cs
│   ├── ChangePublisherFactory.cs
│   ├── ConsolePublisher.cs
│   └── EventHubPublisher.cs
├── dstream.yaml              # Configuration file
├── MonitorMessage.cs
├── Program.cs
├── Server.cs
├── ServiceProvider.cs
├── StartupValidator.cs
└── TableMonitoringService.cs
```

## Configuration

The application is configured via a `dstream.yaml` file located in the root of the project. This YAML file specifies database connection details, the downstream provider to use, and the tables to monitor.

### Example Configuration (`dstream.yaml`)

```yaml
DbType: "sqlserver"
DbConnectionString: "{{ env 'DSTREAM_DB_CONNECTION_STRING' }}"

# Downstream provider configuration
DownstreamProviderType: "EventHub"  # Options: "EventHub", "ServiceBus", "Console"
DownstreamConnectionString: "{{ env 'DSTREAM_DOWNSTREAM_CONNECTION_STRING' }}"  # Connection string for Event Hub or Service Bus

Tables:
  - Name: "Persons"
    PollInterval: "5s"
    MaxPollInterval: "1m"
  - Name: "Cars"
    PollInterval: "5s"
    MaxPollInterval: "2m"
  - Name: "Dano"
    PollInterval: "5s"
    MaxPollInterval: "2m"

```
### Configuration Fields

- **DbType**: Database type (e.g., `"sqlserver"`).
- **DbConnectionString**: The connection string for the database.
- **DownstreamProviderType**: Specifies the downstream system to publish changes to (`"EventHub"`, `"ServiceBus"`, or `"Console"`).
- **DownstreamConnectionString**: Connection string for the selected downstream provider.
- **Tables**: List of tables to monitor, each with its name and polling intervals.

## Getting Started

### Prerequisites

- [.NET 6 or higher](https://dotnet.microsoft.com/download)
- Azure account (if using Event Hub or Service Bus)

### Setup

1. **Clone the repository**:

```bash
   git clone https://github.com/yourusername/DStream.Net.git
    cd DStream.Net
```

2. **Install dependencies**:
   Ensure that your project has the necessary NuGet packages installed:
```bash
   dotnet restore
```

3. **Set up environment variables**:
   Configure environment variables used in `dstream.yaml`:
   - `DSTREAM_DB_CONNECTION_STRING`: Database connection string for SQL Server
   - `DSTREAM_DOWNSTREAM_CONNECTION_STRING`: Connection string for Event Hub or Service Bus (if using these providers)

4. **Run the application**:
```bash
   dotnet run
```

## Usage

The application will start monitoring the specified tables for CDC changes and will publish these changes to the configured downstream provider (e.g., Azure Event Hub, Service Bus, or Console).

### Logging

The application uses [Serilog](https://serilog.net/) for logging. Logs are output to the console by default. You can configure additional logging options in `Program.cs`.

## Extending the Project

You can extend DStream.Net by adding new downstream providers or supporting additional databases:

### Adding a New Downstream Provider

1. Create a new class that implements `IChangePublisher` in the `ChangePublishers` folder.
2. Implement the `SendAsync` method to define how the changes should be sent.
3. Register your new provider in `ChangePublisherFactory`.

### Adding a New Database Provider

1. Create a new class that implements `IDatabaseMonitor`.
2. Implement methods for CDC polling and change tracking.
3. Register your new provider in the application configuration.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub if you have suggestions, bug fixes, or enhancements.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.



  
