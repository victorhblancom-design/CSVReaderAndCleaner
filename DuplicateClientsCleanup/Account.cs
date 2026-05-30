// See https://aka.ms/new-console-template for more information........
public class Account
{
    public ClientModel ClientObj { get; set; } = new();
    public string AccountNumber { get; set; } = string.Empty;
    public bool ShouldDelete { get; set; }
}
