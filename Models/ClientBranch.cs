namespace Models;

public class ClientBranch
{
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
}
