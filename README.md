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
