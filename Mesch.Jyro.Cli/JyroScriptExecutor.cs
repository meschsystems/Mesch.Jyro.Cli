using System.Text.Json;
using Mesch.Jyro.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Mesch.Jyro.Cli;

/// <summary>
/// Executes Jyro scripts with optional code analysis based on command-line options.
/// </summary>
internal sealed class JyroScriptExecutor : IJyroScriptExecutor
{
    private readonly JyroBuilder _builder;
    private readonly ILogger<JyroScriptExecutor> _logger;
    private readonly JyroCommandOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JyroScriptExecutor"/> class.
    /// </summary>
    /// <param name="builder">The Jyro builder instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The command options.</param>
    public JyroScriptExecutor(
        JyroBuilder builder,
        ILogger<JyroScriptExecutor> logger,
        JyroCommandOptions options)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes the Jyro script according to the configured options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when script execution fails.</exception>
    public async Task ExecuteAsync()
    {
        _options.Validate();

        _logger.LogInformation("Starting Jyro execution. Script={Script}, Data={Data}, Output={Output}",
            _options.InputScriptFile,
            _options.DataJsonFile ?? "(empty object)",
            _options.OutputJsonFile ?? "(stdout)");

        // Validate the input script file
        if (!File.Exists(_options.InputScriptFile))
        {
            throw new FileNotFoundException("Input script file not found", _options.InputScriptFile);
        }

        var script = await File.ReadAllTextAsync(_options.InputScriptFile);
        var data = await LoadDataAsync();

        // Configure the builder with script and data
        _builder
            .WithScript(script)
            .WithData(data)
            .WithOptions(CreateExecutionOptions())
            .WithStandardLibrary()
            .WithRestApi();

        await ExecuteScriptAsync();
    }

    /// <summary>
    /// Loads the JSON data file or returns an empty object if no file is specified.
    /// </summary>
    /// <returns>A JyroValue representing the loaded data.</returns>
    private async Task<JyroValue> LoadDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.DataJsonFile))
        {
            return JyroValue.FromJson("{}");
        }

        var dataJson = await File.ReadAllTextAsync(_options.DataJsonFile);
        return JyroValue.FromJson(dataJson);
    }

    /// <summary>
    /// Executes the Jyro script
    /// </summary>
    private async Task ExecuteScriptAsync()
    {
        _logger.LogInformation("Executing script");
        var result = _builder.Run();
        await OutputExecutionResultsAsync(result);
    }

    /// <summary>
    /// Creates execution options with appropriate limits.
    /// </summary>
    /// <returns>Configured execution options.</returns>
    private static JyroExecutionOptions CreateExecutionOptions()
    {
        return new JyroExecutionOptions
        {
            MaxExecutionTime = TimeSpan.FromSeconds(30),
            MaxStatements = 1_000_000,
            MaxLoops = 10_000_000,
            MaxStackDepth = 1000,
            MaxCallDepth = 256
        };
    }

    /// <summary>
    /// Outputs the script execution results to console and/or file.
    /// </summary>
    /// <param name="result">The execution result.</param>
    /// <exception cref="InvalidOperationException">Thrown when script execution fails.</exception>
    private async Task OutputExecutionResultsAsync(JyroExecutionResult result)
    {
        if (!result.IsSuccessful)
        {
            foreach (var msg in result.Messages)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg.ToString());
                Console.ResetColor();
            }
            throw new InvalidOperationException("Jyro script execution failed.");
        }

        var outputJson = JsonSerializer.Serialize(
            result.Data.ToObjectValue(),
            new JsonSerializerOptions { WriteIndented = true });

        if (!string.IsNullOrWhiteSpace(_options.OutputJsonFile))
        {
            await File.WriteAllTextAsync(_options.OutputJsonFile, outputJson);
            _logger.LogInformation("Execution complete. Output written to {OutputFile}", _options.OutputJsonFile);
        }
        else
        {
            Console.WriteLine(outputJson);
            _logger.LogInformation("Execution complete. Output written to stdout");
        }
    }
}