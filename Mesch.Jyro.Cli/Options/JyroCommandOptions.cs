using Microsoft.Extensions.Logging;

namespace Mesch.Jyro.Cli.Options;

/// <summary>
/// Represents the command-line options for the Jyro console application.
/// </summary>
public sealed class JyroCommandOptions
{
    /// <summary>
    /// Gets or sets the path to the Jyro script file to execute.
    /// </summary>
    public string? InputScriptFile { get; set; }

    /// <summary>
    /// Gets or sets the path to the JSON data file that will be provided as the script's Data object.
    /// </summary>
    public string? DataJsonFile { get; set; }

    /// <summary>
    /// Gets or sets the path for the JSON output file. If null, output goes to stdout.
    /// </summary>
    public string? OutputJsonFile { get; set; }

    /// <summary>
    /// Gets or sets the path for logging output. If null, logs go to console only.
    /// </summary>
    public string? LogFile { get; set; }

    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets a value indicating whether logging is completely disabled.
    /// </summary>
    public bool NoLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether console logging is enabled.
    /// </summary>
    public bool ConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to a plugin assembly DLL file to load custom functions from.
    /// </summary>
    public string? PluginAssembly { get; set; }

    /// <summary>
    /// Gets or sets the path to a directory containing plugin assembly DLL files.
    /// </summary>
    public string? PluginDirectory { get; set; }

    /// <summary>
    /// Gets or sets the search pattern for plugin DLL files when loading from a directory.
    /// </summary>
    public string PluginSearchPattern { get; set; } = "*.dll";

    /// <summary>
    /// Gets or sets a value indicating whether to search subdirectories for plugin DLLs.
    /// </summary>
    public bool PluginRecursive { get; set; }

    /// <summary>
    /// Validates the options and applies smart defaults.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(InputScriptFile))
        {
            throw new InvalidOperationException("Input script file is required.");
        }

        if (!File.Exists(InputScriptFile))
        {
            throw new InvalidOperationException($"Input script file not found: {InputScriptFile}");
        }

        if (!string.IsNullOrWhiteSpace(DataJsonFile) && !File.Exists(DataJsonFile))
        {
            throw new InvalidOperationException($"Data JSON file not found: {DataJsonFile}");
        }

        if (!string.IsNullOrWhiteSpace(PluginAssembly) && !File.Exists(PluginAssembly))
        {
            throw new InvalidOperationException($"Plugin assembly file not found: {PluginAssembly}");
        }

        if (!string.IsNullOrWhiteSpace(PluginDirectory) && !Directory.Exists(PluginDirectory))
        {
            throw new InvalidOperationException($"Plugin directory not found: {PluginDirectory}");
        }
    }
}