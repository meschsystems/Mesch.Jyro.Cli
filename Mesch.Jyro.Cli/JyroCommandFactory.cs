using System.CommandLine;
using Mesch.Jyro.Cli.Options;
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
            Description = "Path to the Jyro script to execute",
            Aliases = { "-i" },
            Required = true
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

        // Add all options to root command
        rootCommand.Options.Add(inputScriptFileOption);
        rootCommand.Options.Add(dataJsonFileOption);
        rootCommand.Options.Add(outputJsonFileOption);
        rootCommand.Options.Add(logFileOption);
        rootCommand.Options.Add(logLevelOption);
        rootCommand.Options.Add(noLoggingOption);
        rootCommand.Options.Add(consoleLoggingOption);

        rootCommand.SetAction(async parseResult =>
        {
            var opts = new JyroCommandOptions
            {
                InputScriptFile = parseResult.GetValue(inputScriptFileOption),
                DataJsonFile = parseResult.GetValue(dataJsonFileOption),
                OutputJsonFile = parseResult.GetValue(outputJsonFileOption),
                LogFile = parseResult.GetValue(logFileOption),
                LogLevel = parseResult.GetValue(logLevelOption) ?? LogLevel.Information,
                NoLogging = parseResult.GetValue(noLoggingOption),
                ConsoleLogging = parseResult.GetValue(consoleLoggingOption) ?? true,
            };

            // Build host with opts injected
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(opts);
                    services.AddTransient<JyroBuilder>();
                    services.AddSingleton<IJyroScriptExecutor, JyroScriptExecutor>();
                })
                .ConfigureLogging(logging =>
                {
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
}