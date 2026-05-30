// See https://aka.ms/new-console-template for more information


using static DuplicateClientsCleanup.Helper;

ClientFilterOptions options;

try
{
    options = ClientFilterOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    ClientFilterOptions.WriteUsage();
    return;
}

if (options.ShowHelp)
{
    ClientFilterOptions.WriteUsage();
    return;
}

Dictionary<string, List<ClientModel>> clientDict = new();
List<ClientDecision> decisions = new();

try
{
    ParseCsv(clientDict, options.CsvPath);
}
catch (Exception ex)
{
    Console.WriteLine("Error Parsing CSV: " + ex.Message);
    return;
}

foreach (var account in clientDict)
{
    Console.WriteLine($"Account Number : {account.Key}");
    Console.WriteLine("");
    var totalCount = account.Value.Count;
    Console.WriteLine($"Total Clients : {totalCount} ");
    var countOfClientsWithoutTask = account.Value.Where(x => x.TaskId == 0).ToList().Count;
    Console.WriteLine($"Clients Without Task : {countOfClientsWithoutTask}");
    var countOfClientsWithTask = account.Value.Where(x => x.TaskId != 0).ToList().Count;
    Console.WriteLine($"Clients With Task : {countOfClientsWithTask}");

    bool areNamesSimilar = checkSimilarNames(account.Value);

    if (countOfClientsWithTask == totalCount)
    {
        // every client for this account has a task -- do not delete -- mark for review
        Console.WriteLine("Do not Delete - Marked for Review");
        foreach (var client in account.Value)
        {
            decisions.Add(new ClientDecision(client, ClientStatus.Review));
        }
    }
    else if (countOfClientsWithoutTask == totalCount)
    {
        //every client for this account doesnt have a task -- delete only unlinked client
        Console.WriteLine("Delete Only unlinked Client");
        foreach (var client in account.Value)
        {
            if (client.IsLinked == "N" && areNamesSimilar)
            {
                decisions.Add(new ClientDecision(client, ClientStatus.Delete));
            }
            else
            {
                decisions.Add(new ClientDecision(client, ClientStatus.Keep));
            }
        }
    }
    else
    {
        Console.WriteLine("Delete Clients that dont have a task");
        // some of the clients have a task
        // delete only the clients without task
        foreach (var client in account.Value)
        {
            if (client.TaskId == 0 && areNamesSimilar)
            {
                decisions.Add(new ClientDecision(client, ClientStatus.Delete));
            }
            else
            {
                decisions.Add(new ClientDecision(client, ClientStatus.Keep));
            }
        }
    }
    Console.WriteLine($"-------------------------------------------------");
}

var filteredDecisions = decisions.Where(options.Matches).ToList();

Console.WriteLine($"CSV Path: {options.CsvPath}");
Console.WriteLine($"Active Filters: {options.Describe()}");
Console.WriteLine($"Matched Records: {filteredDecisions.Count} of {decisions.Count}");
Console.WriteLine($"-------------------------------------------------");

WriteStatusGroup("Records to Keep", filteredDecisions, ClientStatus.Keep);
WriteStatusGroup("Records to Review", filteredDecisions, ClientStatus.Review);
WriteStatusGroup("Number of Clients to Delete", filteredDecisions, ClientStatus.Delete);

Console.WriteLine($"-------------------------------------------------");
Console.WriteLine("Delete Query");
var list = string.Join(",", filteredDecisions
    .Where(decision => decision.Status == ClientStatus.Delete)
    .Select(decision => decision.Client.ClientId.ToString()));
Console.WriteLine($"delete from client_master where client_id in ({list})");
Console.WriteLine($"-------------------------------------------------");

static void WriteStatusGroup(string heading, List<ClientDecision> decisions, ClientStatus status)
{
    var clients = decisions
        .Where(decision => decision.Status == status)
        .Select(decision => decision.Client)
        .ToList();

    Console.WriteLine($"{heading} {clients.Count}");
    foreach (var client in clients)
    {
        Console.WriteLine($" Client ID : {client.ClientId} | Name: {client.LastCorpName} | Account number : {client.AccountNumber} | Linked : {client.IsLinked} | Task ID : {client.TaskId}");
    }
}
