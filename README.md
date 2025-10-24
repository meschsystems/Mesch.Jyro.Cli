# Jyro CLI

Command-line interpreter for the [Jyro language](https://github.com/meschsystems/Mesch.Jyro) - an imperative data manipulation language designed for secure, sandboxed processing of JSON-like data structures.

## Installation

Jyro CLI is distributed as an MSI installer. After installation, the `Jyro` command will be available in your system PATH.

## Usage

```
Jyro --input-script-file <path> [options]
```

### Required Arguments

- `-i, --input-script-file <path>` - Path to the Jyro script file to execute

### Optional Arguments

**Data Input/Output:**
- `-d, --data-json-file <path>` - JSON file providing the script's `Data` object (defaults to empty object `{}`)
- `-o, --output-json-file <path>` - Output file for script results (defaults to stdout)

**Logging:**
- `-l, --log-file <path>` - File for logging output (defaults to console only)
- `-v, --verbosity <level>` - Minimum log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, or `None` (default: `Information`)
- `-q, --quiet` - Disable all logging output
- `--console-logging <true/false>` - Enable/disable console logging (default: `true`)

**Plugin Loading:**
- `-p, --plugin-assembly <path>` - Path to a plugin assembly DLL file to load custom functions from
- `-pd, --plugin-directory <path>` - Path to a directory containing plugin assembly DLL files
- `-pp, --plugin-pattern <pattern>` - Search pattern for plugin DLL files (default: `*.dll`)
- `-pr, --plugin-recursive` - Search subdirectories for plugin DLLs

**Configuration:**
- `-c, --config <path>` - Path to JSON configuration file (searches default locations if not specified)

## Examples

### Basic Script Execution

Execute a Jyro script and output results to console:

```bash
Jyro -i script.jyro
```

### Script with Input Data

Execute a script with JSON input data:

```bash
Jyro -i transform.jyro -d input.json
```

### Save Output to File

Execute a script and save results to a file:

```bash
Jyro -i process.jyro -d data.json -o output.json
```

### Quiet Mode

Run without logging output (only script results):

```bash
Jyro -i script.jyro -d data.json --quiet
```

### Debug Mode

Run with detailed debug logging:

```bash
Jyro -i script.jyro -v Debug
```

### File Logging

Execute with logging to a file instead of console:

```bash
Jyro -i script.jyro -l execution.log --console-logging false
```

### Combined Example

Execute a script with input data, save output, and log to file:

```bash
Jyro -i transform.jyro -d input.json -o result.json -l transform.log -v Information
```

### Plugin Loading Examples

Load custom functions from a single plugin DLL:

```bash
Jyro -i script.jyro -p MyPlugins.dll
```

Load custom functions from all DLLs in a directory:

```bash
Jyro -i script.jyro -pd C:\Plugins
```

Load custom functions with a specific search pattern:

```bash
Jyro -i script.jyro -pd C:\Plugins -pp "*.JyroPlugin.dll"
```

Load custom functions recursively from subdirectories:

```bash
Jyro -i script.jyro -pd C:\Plugins -pr
```

Combine plugin loading with data input and output:

```bash
Jyro -i transform.jyro -d data.json -o output.json -pd C:\Plugins -pp "*.dll"
```

## Configuration File

Jyro CLI supports configuration files in JSON format, allowing you to set default options without having to specify them on the command line every time.

### Configuration File Locations

The CLI automatically searches for `jyro.config.json` in the following locations (in priority order):

1. **Current directory** - The directory where you run the Jyro command
2. **AppData/Roaming/Jyro** - `%APPDATA%\Jyro\jyro.config.json` (e.g., `C:\Users\YourName\AppData\Roaming\Jyro\jyro.config.json`)
3. **User home directory** - `jyro.config.json` in your user profile folder

The search stops at the first location where the file is found.

Alternatively, you can specify a custom configuration file path using the `-c` or `--config` option:

```bash
Jyro --config my-custom-config.json -i script.jyro
```

### Configuration File Format

All command-line options can be specified in the configuration file using JSON format:

```json
{
  "InputScriptFile": "default-script.jyro",
  "DataJsonFile": "data.json",
  "OutputJsonFile": "output.json",
  "LogFile": "jyro.log",
  "LogLevel": "Information",
  "NoLogging": false,
  "ConsoleLogging": true,
  "PluginAssembly": null,
  "PluginDirectory": "C:\\Plugins",
  "PluginSearchPattern": "*.dll",
  "PluginRecursive": false
}
```

**Notes:**
- Property names are case-insensitive
- Comments (both `//` and `/* */`) and trailing commas are supported
- `LogLevel` can be: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, or `None`

### Configuration Precedence

Configuration values are applied in the following order (highest priority first):

1. **Command-line arguments** - Explicitly specified CLI options
2. **Environment variables** - Variables prefixed with `JYRO_` (e.g., `JYRO_LogLevel=Debug`)
3. **Configuration file** - Values from JSON config file
4. **Defaults** - Built-in default values

This means you can set defaults in a config file and override them with command-line arguments or environment variables as needed.

### Example: Using Configuration File

Create a `jyro.config.json` in your current directory:

```json
{
  "LogLevel": "Warning",
  "PluginDirectory": "C:\\MyPlugins",
  "ConsoleLogging": true
}
```

Then run Jyro with just the script file (other options come from config):

```bash
Jyro -i script.jyro
```

Override the log level from the config file:

```bash
Jyro -i script.jyro -v Debug
```

### Example: Environment Variables

Set environment variables to override configuration:

```bash
# Windows (CMD)
set JYRO_LogLevel=Debug
set JYRO_PluginDirectory=C:\Plugins
Jyro -i script.jyro

# Windows (PowerShell)
$env:JYRO_LogLevel="Debug"
$env:JYRO_PluginDirectory="C:\Plugins"
Jyro -i script.jyro

# Linux/Mac
export JYRO_LogLevel=Debug
export JYRO_PluginDirectory=/opt/plugins
Jyro -i script.jyro
```

## Creating Custom Plugins

To create custom functions that can be loaded by the Jyro CLI, create a .NET class library that references the `Mesch.Jyro` package and implement functions by inheriting from `JyroFunctionBase`:

```csharp
using Mesch.Jyro;

public class MyCustomFunction : JyroFunctionBase
{
    public MyCustomFunction() : base(new JyroFunctionSignature(
        "MyFunction",
        new[] { new Parameter("input", ParameterType.String) },
        ParameterType.String))
    {
    }

    public override JyroValue Execute(
        IReadOnlyList<JyroValue> arguments,
        ExecutionContext executionContext)
    {
        var input = GetStringArgument(arguments, 0);
        return new JyroString($"Processed: {input}");
    }
}
```

Compile your class library to a DLL and use the plugin loading options to make your custom functions available in Jyro scripts.

**Requirements:**
- Plugin functions must have public parameterless constructors
- Plugin assemblies must reference the Mesch.Jyro package
- All function types must implement `IJyroFunction` (typically by inheriting from `JyroFunctionBase`)

## Script Execution

Jyro scripts execute with the following constraints:

- **Maximum execution time:** 30 seconds
- **Maximum statements:** 1,000,000
- **Maximum loops:** 10,000,000
- **Maximum stack depth:** 1,000
- **Maximum call depth:** 256

If a script exceeds any of these limits, execution will be terminated and an error will be reported.

## Exit Codes

- `0` - Successful execution
- Non-zero - Execution failed (error details will be logged)

## Output Format

Script output is returned as JSON. When output is written to console (default), it is formatted with indentation for readability. When written to a file, the same indented format is used.

## Error Handling

When a script fails to execute:
- Error messages are displayed in red on the console
- Detailed error information is included in the log output
- The command exits with a non-zero exit code

## About Jyro

Jyro is an imperative data manipulation language designed for secure, sandboxed processing of JSON-like data structures. It provides a controlled environment for executing data transformations with built-in safeguards against infinite loops, excessive resource consumption, and other potential issues.

For more information about the Jyro language, visit: https://github.com/meschsystems/Mesch.Jyro

## License

MIT License - Copyright © 2025 Mesch Systems

## Support

For issues and feature requests, please visit the project repository at https://github.com/meschsystems/Mesch.Jyro
