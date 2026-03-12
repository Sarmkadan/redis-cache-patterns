#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.CLI;

/// <summary>
/// Parses and validates command-line arguments for cache management operations
/// Supports subcommands with hierarchical option parsing
/// </summary>
public class CommandParser
{
    private readonly Dictionary<string, CommandHandler> _commands = new();

    public CommandParser RegisterCommand(string name, CommandHandler handler)
    {
        _commands[name.ToLower()] = handler;
        return this;
    }

    public async Task<int> ParseAndExecuteAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            var command = args[0].ToLower();
            if (command == "--help" || command == "-h")
            {
                PrintHelp();
                return 0;
            }

            if (!_commands.TryGetValue(command, out var handler))
            {
                Console.Error.WriteLine($"Unknown command: {command}");
                return 1;
            }

            var options = ParseOptions(args.Skip(1).ToArray());
            return await handler(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i][2..].ToLower();
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("--")
                    ? args[++i]
                    : "true";
                options[key] = value;
            }
            else if (args[i].StartsWith("-"))
            {
                var key = args[i][1..].ToLower();
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("-")
                    ? args[++i]
                    : "true";
                options[key] = value;
            }
        }
        return options;
    }

    private void PrintHelp()
    {
        Console.WriteLine("Redis Cache Patterns - CLI Tool");
        Console.WriteLine("Usage: program [command] [options]");
        Console.WriteLine("\nAvailable Commands:");
        foreach (var cmd in _commands.Keys)
        {
            Console.WriteLine($"  {cmd,-20} Execute cache operation");
        }
        Console.WriteLine("\nGlobal Options:");
        Console.WriteLine("  --help, -h          Show this help message");
        Console.WriteLine("  --verbose, -v       Enable verbose logging");
        Console.WriteLine("  --redis-conn        Redis connection string");
    }

    public delegate Task<int> CommandHandler(Dictionary<string, string> options);
}
