using System;
using System.Collections.Generic;
using System.IO;

public static class ASGraph
{
    public static void ProcessRelationshipLine(
        string line,
        int lineNumber,
        Dictionary<int, ASNode> nodes)
    {
        // Expected format:
        // provider|customer|-1|source   (p2c)
        // peer|peer|0|source            (p2p)
        var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

        // Need at least: AS1, AS2, relType
        if (parts.Length < 3)
            return;

        if (!int.TryParse(parts[0], out int as1)) return;
        if (!int.TryParse(parts[1], out int as2)) return;
        if (!int.TryParse(parts[2], out int relType)) return;

        var node1 = GetOrCreateNode(nodes, as1);
        var node2 = GetOrCreateNode(nodes, as2);

        if (relType == -1)
        {
            // p2c: node1 = provider, node2 = customer
            node1.Customers.Add(as2);
            node2.Providers.Add(as1);
        }
        else if (relType == 0)
        {
            // p2p: peers
            node1.Peers.Add(as2);
            node2.Peers.Add(as1);
        }
        // ignore other relTypes if present
    }

    public static ASNode GetOrCreateNode(
        Dictionary<int, ASNode> nodes,
        int asn)
    {
        if (!nodes.TryGetValue(asn, out var node))
        {
            node = new ASNode { Asn = asn };
            nodes[asn] = node;
        }
        return node;
    }

    public static void ClassifyNodes(Dictionary<int, ASNode> nodes)
    {
        int enterpriseCount = 0;
        int contentCount = 0;
        int transitCount = 0;

        foreach (var kvp in nodes)
        {
            var n = kvp.Value;

            // Rules from the project:
            // • Enterprise: any AS without customers or peers.
            // • Content:   any AS with no customers and at least one peer.
            // • Transit:   any AS with at least one customer.

            if (n.CustomerDegree == 0 && n.PeerDegree == 0)
            {
                n.Classification = "Enterprise";
                enterpriseCount++;
            }
            else if (n.CustomerDegree == 0 && n.PeerDegree > 0)
            {
                n.Classification = "Content";
                contentCount++;
            }
            else if (n.CustomerDegree > 0)
            {
                n.Classification = "Transit";
                transitCount++;
            }
            else
            {
                // Shouldn't really happen with these rules, but just in case:
                n.Classification = "Unclassified";
            }
        }
    }

    public static void LoadIpSpaceFromPfx2As(
        string pfx2asPath,
        Dictionary<int, ASNode> nodes)
    {
        using var reader = new StreamReader(pfx2asPath);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Expected format (tab-separated):
            // prefix \t prefixLength \t ASN
            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                continue;

            // We don't actually need the text of the prefix for Graph 3
            if (!int.TryParse(parts[1], out int prefixLen)) continue;
            if (!int.TryParse(parts[2], out int asn)) continue;

            // IPv4: number of addresses in this prefix = 2^(32 - prefixLen)
            if (prefixLen < 0 || prefixLen > 32)
                continue; // skip weird lines

            ulong blockSize = 1UL << (32 - prefixLen);

            var node = GetOrCreateNode(nodes, asn);
            node.TotalIpSpace += blockSize;
        }
    }

    public static void WriteCsv(
        string path,
        Dictionary<int, ASNode> nodes)
    {
        using var writer = new StreamWriter(path);

        writer.WriteLine("ASN,CustomerDegree,PeerDegree,ProviderDegree,GlobalDegree,TotalIpSpace,Classification");

        foreach (var kvp in nodes)
        {
            var n = kvp.Value;
            writer.WriteLine(
                $"{n.Asn}," +
                $"{n.CustomerDegree}," +
                $"{n.PeerDegree}," +
                $"{n.ProviderDegree}," +
                $"{n.GlobalDegree}," +
                $"{n.TotalIpSpace}," +
                $"{n.Classification}");
        }
    }
}
