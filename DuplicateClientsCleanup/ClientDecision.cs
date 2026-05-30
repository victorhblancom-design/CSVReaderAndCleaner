public enum ClientStatus
{
    Keep,
    Review,
    Delete
}

public sealed class ClientDecision
{
    public ClientDecision(ClientModel client, ClientStatus status)
    {
        Client = client;
        Status = status;
    }

    public ClientModel Client { get; }

    public ClientStatus Status { get; }
}
