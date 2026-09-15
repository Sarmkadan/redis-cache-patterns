# CommandParser

`CommandParser` registers command handlers, parses command-line options, displays CLI help, and executes the selected handler. Commands are matched without regard to case, while parsed option names are stored without leading dashes and normalized to lowercase.

## API

### `public CommandParser RegisterCommand(string name, CommandHandler handler)`

Registers a handler under a command name and returns the parser so registrations can be chained. Registering the same name again, including a name that differs only by case, replaces the previous handler.

- **Parameters**:
  - `name`: The command name used as the first command-line argument.
  - `handler`: The asynchronous delegate that receives parsed options and returns an exit code.
- **Return value**:
  - The current `CommandParser` instance.
- **Exceptions**:
  - `ArgumentException` when `name` is `null` or empty.
  - `ArgumentNullException` when `handler` is `null`.

### `public async Task<int> ParseAndExecuteAsync(string[] args)`

Selects the command from the first argument, parses the remaining arguments into an option dictionary, and awaits the registered handler.

- **Parameters**:
  - `args`: The command-line arguments to parse.
- **Return value**:
  - `0` when no arguments are supplied or the first argument is `--help` or `-h`; help is printed in each case.
  - The selected handler's return value when execution completes normally.
  - `1` for an unknown command or an exception other than `ArgumentException` or `InvalidOperationException` during parsing or handler execution.
  - `2` when parsing or handler execution throws `ArgumentException` or `InvalidOperationException`.
- **Exceptions**:
  - `ArgumentNullException` when `args` is `null`. This validation occurs before the method's exception handling, so the exception is propagated to the caller.

### `public delegate Task<int> CommandHandler(Dictionary<string, string> options)`

Represents an asynchronous command handler. The handler receives parsed options and returns the process-style exit code that `ParseAndExecuteAsync` passes back to its caller.

## Option parsing

Only arguments after the command name are considered as options. Other positional arguments are ignored.

| Input form | Parsed result |
| --- | --- |
| `--key=value` | Stores `key` with `value`. The value may be empty or begin with a hyphen. |
| `--key value` | Stores `key` with the following token when that token does not begin with `-`. |
| `-k value` | Stores `k` with the following token when that token does not begin with `-`. |
| `--flag` or `-f` | Stores the option with the string value `"true"` when no eligible value follows. |

Option names are converted to lowercase. If an option appears more than once, the last parsed value replaces the earlier value. A separate value beginning with `-` is treated as another option rather than as the preceding option's value; use the `--key=value` form when a value itself must begin with a hyphen.

The options dictionary uses its default case-sensitive comparer. Handlers should therefore look up the normalized lowercase names.

## Help and diagnostics

Help output includes the usage line, every registered command, and the built-in `--help`, `--verbose`, and `--redis-conn` option descriptions. The parser handles `--help` and `-h` only when either is the first argument.

Unknown-command and exception messages are written to standard error. Help is written to standard output. Exception diagnostics include the exception message but not a stack trace.

## Usage

```csharp
var parser = new CommandParser()
    .RegisterCommand("cache", async options =>
    {
        var key = options.GetValueOrDefault("key");
        await Console.Out.WriteLineAsync($"Requested key: {key}");
        return 0;
    });

var exitCode = await parser.ParseAndExecuteAsync(new[]
{
    "cache",
    "--key",
    "product:42",
    "--verbose"
});
```

The handler in this example receives the following entries:

```text
key = product:42
verbose = true
```

## Notes

- Command names are stored with an ordinal, case-insensitive comparer.
- The parser does not validate whether an option is supported by a command; that responsibility belongs to the handler.
- The built-in global options are shown in help, but options other than first-position help are parsed and passed through like any other option.
- Handler exceptions are converted to an error message and exit code according to the rules above.
