namespace Mesch.Jyro.Cli;

/// <summary>
/// Defines the contract for executing Jyro scripts.
/// </summary>
internal interface IJyroScriptExecutor
{
    /// <summary>
    /// Executes a Jyro script according to the configured options.
    /// </summary>
    /// <returns>A task containing the exit code: 0 for success, non-zero for failure.</returns>
    Task<int> ExecuteAsync();
}