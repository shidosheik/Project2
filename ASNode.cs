using System.Collections.Generic;

public class ASNode
{
    public int Asn;  // AS number

    public HashSet<int> Providers = new HashSet<int>();
    public HashSet<int> Customers = new HashSet<int>();
    public HashSet<int> Peers     = new HashSet<int>();

    // Total IPv4 space advertised by this AS (Graph 3)
    public ulong TotalIpSpace { get; set; } = 0;

    // Graph 4 classification label
    public string Classification { get; set; } = "Unclassified";

    public int CustomerDegree => Customers.Count;
    public int PeerDegree     => Peers.Count;
    public int ProviderDegree => Providers.Count;

    // Global node degree = distinct neighbors
    public int GlobalDegree
    {
        get
        {
            var neighbors = new HashSet<int>();
            neighbors.UnionWith(Providers);
            neighbors.UnionWith(Customers);
            neighbors.UnionWith(Peers);
            return neighbors.Count;
        }
    }
}
