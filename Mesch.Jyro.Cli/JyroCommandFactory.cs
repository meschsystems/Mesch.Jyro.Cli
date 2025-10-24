using System.CommandLine;
using Mesch.Jyro.Cli.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mesch.Jyro.Cli;

/// <summary>
/// Factory for creating the root command with all options and handlers configured.
/// </summary>
internal sealed class JyroCommandFactory
{
    /// <summary>
    /// Creates the root command with all options and handlers configured.
    /// </summary>
    /// <returns>The configured root command.</returns>
    public RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand(
            "Jyro - an imperative data manipulation language for secure, sandboxed processing of JSON-like data structures.");

        // Script execution options
        var inputScriptFileOption = new Option<string?>("--input-script-file")
        {
            Description = "Path to the Jyro script to execute (required if not specified in config file)",
            Aliases = { "-i" }
        };

        var dataJsonFileOption = new Option<string?>("--data-json-file")
        {
            Description = "JSON file providing the script's Data object (defaults to empty object)",
            Aliases = { "-d" }
        };

        var outputJsonFileOption = new Option<string?>("--output-json-file")
        {
            Description = "Output file for script results (defaults to stdout)",
            Aliases = { "-o" }
        };

        // Logging options
        var logFileOption = new Option<string?>("--log-file")
        {
            Description = "File for logging output (defaults to console only)",
            Aliases = { "-l" }
        };

        var logLevelOption = new Option<LogLevel?>("--verbosity")
        {
            Description = "Minimum log level (Trace, Debug, Information, Warning, Error, Critical, None)",
            Aliases = { "-v" }
        };

        var noLoggingOption = new Option<bool>("--quiet")
        {
            Description = "Disable all logging output",
            Aliases = { "-q" }
        };

        var consoleLoggingOption = new Option<bool?>("--console-logging")
        {
            Description = "Enable console logging output (true/false). Default true."
        };

        // Plugin options
        var pluginAssemblyOption = new Option<string?>("--plugin-assembly")
        {
            Description = "Path to a plugin assembly DLL file to load custom functions from",
            Aliases = { "-p" }
        };

        var pluginDirectoryOption = new Option<string?>("--plugin-directory")
        {
            Description = "Path to a directory containing plugin assembly DLL files",
            Aliases = { "-pd" }
        };

        var pluginSearchPatternOption = new Option<string?>("--plugin-pattern")
        {
            Description = "Search pattern for plugin DLL files (default: *.dll)",
            Aliases = { "-pp" }
        };

        var pluginRecursiveOption = new Option<bool>("--plugin-recursive")
        {
            Description = "Search subdirectories for plugin DLLs",
            Aliases = { "-pr" }
        };

        // Configuration option
        var configOption = new Option<string?>("--config")
        {
            Description = "Path to JSON configuration file (searches default locations if not specified)",
            Aliases = { "-c" }
        };

        // Add all options to root command
        rootCommand.Options.Add(inputScriptFileOption);
        rootCommand.Options.Add(dataJsonFileOption);
        rootCommand.Options.Add(outputJsonFileOption);
        rootCommand.Options.Add(logFileOption);
        rootCommand.Options.Add(logLevelOption);
        rootCommand.Options.Add(noLoggingOption);
        rootCommand.Options.Add(consoleLoggingOption);
        rootCommand.Options.Add(pluginAssemblyOption);
        rootCommand.Options.Add(pluginDirectoryOption);
        rootCommand.Options.Add(pluginSearchPatternOption);
        rootCommand.Options.Add(pluginRecursiveOption);
        rootCommand.Options.Add(configOption);

        rootCommand.SetAction(async parseResult =>
        {
            // Build configuration with proper precedence: Command-line > Environment > Config file > Defaults
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory());

            // 1. Start with JSON config file (lowest priority)
            var configPath = parseResult.GetValue(configOption);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                // Search for config file in default locations
                configPath = FindConfigurationFile();
            }

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                // If the path is absolute, use it directly; otherwise, make it relative to base path
                var isAbsolutePath = Path.IsPathRooted(configPath);
                if (isAbsolutePath)
                {
                    configBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: false);
                }
                else
                {
                    // For relative paths, SetBasePath already handles it
                    configBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: false);
                }
            }

            // 2. Add environment variables (override config file)
            configBuilder.AddEnvironmentVariables(prefix: "JYRO_");

            // 3. Add command-line arguments (highest priority)
            var cmdLineConfig = new Dictionary<string, string?>();

            if (parseResult.GetValue(inputScriptFileOption) is string inputScript)
            {
                cmdLineConfig["InputScriptFile"] = inputScript;
            }

            if (parseResult.GetValue(dataJsonFileOption) is string dataFile)
            {
                cmdLineConfig["DataJsonFile"] = dataFile;
            }

            if (parseResult.GetValue(outputJsonFileOption) is string outputFile)
            {
                cmdLineConfig["OutputJsonFile"] = outputFile;
            }

            if (parseResult.GetValue(logFileOption) is string logFile)
            {
                cmdLineConfig["LogFile"] = logFile;
            }

            if (parseResult.GetValue(logLevelOption) is LogLevel logLevel)
            {
                cmdLineConfig["LogLevel"] = logLevel.ToString();
            }

            if (parseResult.GetValue(noLoggingOption))
            {
                cmdLineConfig["NoLogging"] = "true";
            }

            if (parseResult.GetValue(consoleLoggingOption) is bool consoleLogging)
            {
                cmdLineConfig["ConsoleLogging"] = consoleLogging.ToString();
            }

            if (parseResult.GetValue(pluginAssemblyOption) is string pluginAssembly)
            {
                cmdLineConfig["PluginAssembly"] = pluginAssembly;
            }

            if (parseResult.GetValue(pluginDirectoryOption) is string pluginDirectory)
            {
                cmdLineConfig["PluginDirectory"] = pluginDirectory;
            }

            if (parseResult.GetValue(pluginSearchPatternOption) is string pluginPattern)
            {
                cmdLineConfig["PluginSearchPattern"] = pluginPattern;
            }

            if (parseResult.GetValue(pluginRecursiveOption))
            {
                cmdLineConfig["PluginRecursive"] = "true";
            }

            configBuilder.AddInMemoryCollection(cmdLineConfig!);

            var configuration = configBuilder.Build();

            // Check if InputScriptFile is provided (either from command-line, environment, or config file)
            var inputScriptFile = configuration["InputScriptFile"];
            if (string.IsNullOrWhiteSpace(inputScriptFile))
            {
                // Show help when required argument is missing
                var helpResult = rootCommand.Parse("--help");
                await helpResult.InvokeAsync();
                return 1;
            }

            // Build host with IOptions pattern
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<JyroCommandOptions>(context.Configuration);
                    services.AddTransient<JyroBuilder>();
                    services.AddSingleton<IJyroScriptExecutor, JyroScriptExecutor>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    var opts = new JyroCommandOptions();
                    context.Configuration.Bind(opts);

                    logging.ClearProviders();
                    if (opts.NoLogging || opts.LogLevel == LogLevel.None)
                    {
                        logging.SetMinimumLevel(LogLevel.None);
                        return;
                    }
                    if (opts.ConsoleLogging)
                    {
                        logging.AddSimpleConsole(o =>
                        {
                            o.IncludeScopes = false;
                            o.SingleLine = true;
                            o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(opts.LogFile))
                    {
                        logging.AddProvider(new SimpleFileLoggerProvider(opts.LogFile));
                    }

                    logging.SetMinimumLevel(opts.LogLevel);
                })
                .Build();

            // Resolve from DI
            var executor = host.Services.GetRequiredService<IJyroScriptExecutor>();
            await executor.ExecuteAsync();
            return 0;
        });

        return rootCommand;
    }

    /// <summary>
    /// Searches for a configuration file in default locations.
    /// Priority order:
    /// 1. Current directory
    /// 2. User's AppData/Roaming/Jyro directory
    /// 3. User's home directory
    /// </summary>
    /// <returns>The path to the first configuration file found, or null if none found.</returns>
    private static string? FindConfigurationFile()
    {
        const string defaultConfigFileName = "jyro.config.json";

        // 1. Current directory
        var currentDirConfig = Path.Combine(Directory.GetCurrentDirectory(), defaultConfigFileName);
        if (File.Exists(currentDirConfig))
        {
            return currentDirConfig;
        }

        // 2. AppData/Roaming/Jyro
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDataConfig = Path.Combine(appDataPath, "Jyro", defaultConfigFileName);
        if (File.Exists(appDataConfig))
        {
            return appDataConfig;
        }

        // 3. User home directory
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var homeConfig = Path.Combine(homePath, defaultConfigFileName);
        if (File.Exists(homeConfig))
        {
            return homeConfig;
        }

        return null;
    }
}