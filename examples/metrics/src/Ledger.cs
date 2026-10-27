namespace MetricsExample;

// The peer cycle is intentional demonstration input, not a design recommendation.
public sealed partial class Ledger
{
    private int balance;
    public LedgerPeer? Peer { get; set; }

    public void AddIfPositive(int amount)
    {
        if (amount > 0)
            balance += amount;
    }
}
