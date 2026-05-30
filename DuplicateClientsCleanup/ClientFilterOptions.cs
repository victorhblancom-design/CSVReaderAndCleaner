public sealed class ClientFilterOptions
{
    private const string DefaultCsvPath = @"C:\rawnew.csv";

    public string CsvPath { get; private set; } = DefaultCsvPath;

    public bool ShowHelp { get; private set; }

    public HashSet<ClientStatus> Statuses { get; } = new();

    public HashSet<string> LinkedStatuses { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<int> ClientIds { get; } = new();

    public bool? HasTask { get; private set; }

    public string? AccountContains { get; private set; }

    public string? NameContains { get; private set; }

    public static ClientFilterOptions Parse(string[] args)
    {
        var options = new ClientFilterOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var argument = args[i];

            if (argument is "--help" or "-h" or "/?")
            {
                options.ShowHelp = true;
                continue;
            }

            if (argument.StartsWith("?") || (argument.Contains('=') && !argument.StartsWith("--")))
            {
                ParseQueryLikeOptions(options, argument.TrimStart('?'));
                continue;
            }

            if (!argument.StartsWith("--"))
            {
                throw new ArgumentException($"Unknown argument '{argument}'. Use --help to see the supported filters.");
            }

            string optionName;
            string optionValue;
            var separatorIndex = argument.IndexOf('=');

            if (separatorIndex > 0)
            {
                optionName = argument[2..separatorIndex];
                optionValue = argument[(separatorIndex + 1)..];
            }
            else
            {
                optionName = argument[2..];
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for --{optionName}.");
                }

                optionValue = args[++i];
            }

            ApplyOption(options, optionName, optionValue);
        }

        return options;
    }

    public bool Matches(ClientDecision decision)
    {
        var client = decision.Client;

        if (Statuses.Count > 0 && !Statuses.Contains(decision.Status))
        {
            return false;
        }

        if (LinkedStatuses.Count > 0 && !LinkedStatuses.Contains(client.IsLinked ?? string.Empty))
        {
            return false;
        }

        if (ClientIds.Count > 0 && !ClientIds.Contains(client.ClientId))
        {
            return false;
        }

        if (HasTask.HasValue && (client.TaskId != 0) != HasTask.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(AccountContains) &&
            !(client.AccountNumber ?? string.Empty).Contains(AccountContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(NameContains) &&
            !BuildName(client).Contains(NameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public string Describe()
    {
        var filters = new List<string>();

        if (Statuses.Count > 0)
        {
            filters.Add($"status={string.Join(",", Statuses.Select(status => status.ToString().ToLowerInvariant()))}");
        }

        if (LinkedStatuses.Count > 0)
        {
            filters.Add($"linked={string.Join(",", LinkedStatuses)}");
        }

        if (ClientIds.Count > 0)
        {
            filters.Add($"clientId={string.Join(",", ClientIds)}");
        }

        if (HasTask.HasValue)
        {
            filters.Add($"hasTask={HasTask.Value.ToString().ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(AccountContains))
        {
            filters.Add($"account={AccountContains}");
        }

        if (!string.IsNullOrWhiteSpace(NameContains))
        {
            filters.Add($"name={NameContains}");
        }

        return filters.Count == 0 ? "none" : string.Join("; ", filters);
    }

    public static void WriteUsage()
    {
        Console.WriteLine("DuplicateClientsCleanup");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- --csv <path> [filters]");
        Console.WriteLine();
        Console.WriteLine("Filters:");
        Console.WriteLine("  --status keep,review,delete  Show only selected cleanup decisions.");
        Console.WriteLine("  --linked Y,N                  Show only linked or unlinked clients.");
        Console.WriteLine("  --has-task true|false         Show only clients with or without a task id.");
        Console.WriteLine("  --account <text>              Match account number text.");
        Console.WriteLine("  --name <text>                 Match first, middle, or last/corporate name.");
        Console.WriteLine("  --client-id 12,34             Show only selected client ids.");
        Console.WriteLine();
        Console.WriteLine("Query-style filters are also supported:");
        Console.WriteLine("  dotnet run -- \"status=delete&linked=N&hasTask=false\"");
    }

    private static void ParseQueryLikeOptions(ClientFilterOptions options, string query)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            var name = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            ApplyOption(options, name, value);
        }
    }

    private static void ApplyOption(ClientFilterOptions options, string name, string value)
    {
        switch (NormalizeName(name))
        {
            case "csv":
                options.CsvPath = value;
                break;
            case "status":
                AddStatuses(options, value);
                break;
            case "linked":
                AddLinkedStatuses(options, value);
                break;
            case "hastask":
                options.HasTask = ParseBoolean(value, "has-task");
                break;
            case "account":
                options.AccountContains = value;
                break;
            case "name":
                options.NameContains = value;
                break;
            case "clientid":
                AddClientIds(options, value);
                break;
            default:
                throw new ArgumentException($"Unknown filter '{name}'. Use --help to see the supported filters.");
        }
    }

    private static void AddStatuses(ClientFilterOptions options, string value)
    {
        foreach (var statusValue in SplitValues(value))
        {
            if (!Enum.TryParse<ClientStatus>(statusValue, ignoreCase: true, out var status))
            {
                throw new ArgumentException($"Unknown status '{statusValue}'. Valid values: keep, review, delete.");
            }

            options.Statuses.Add(status);
        }
    }

    private static void AddLinkedStatuses(ClientFilterOptions options, string value)
    {
        foreach (var linkedValue in SplitValues(value))
        {
            var normalized = linkedValue.ToUpperInvariant();
            if (normalized is not ("Y" or "N"))
            {
                throw new ArgumentException($"Unknown linked value '{linkedValue}'. Valid values: Y, N.");
            }

            options.LinkedStatuses.Add(normalized);
        }
    }

    private static void AddClientIds(ClientFilterOptions options, string value)
    {
        foreach (var idValue in SplitValues(value))
        {
            if (!int.TryParse(idValue, out var clientId))
            {
                throw new ArgumentException($"Invalid client id '{idValue}'.");
            }

            options.ClientIds.Add(clientId);
        }
    }

    private static bool ParseBoolean(string value, string optionName)
    {
        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return value.ToLowerInvariant() switch
        {
            "1" or "yes" or "y" => true,
            "0" or "no" or "n" => false,
            _ => throw new ArgumentException($"Invalid value for {optionName}: '{value}'. Use true or false.")
        };
    }

    private static IEnumerable<string> SplitValues(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeName(string name)
    {
        return name.Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static string BuildName(ClientModel client)
    {
        return string.Join(" ", new[]
        {
            client.FirstName,
            client.MiddleName,
            client.LastCorpName
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
