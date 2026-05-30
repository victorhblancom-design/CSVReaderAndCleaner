// See https://aka.ms/new-console-template for more information
public class ClientModel
{
    public int ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastCorpName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IsLinked { get; set; } = string.Empty;
    public string LinkedAccountNumber { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
