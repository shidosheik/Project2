using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Project 2.3: Inference of Tier-1 ASes by computing a large clique
/// using the greedy heuristic described in the assignment.
/// 
/// The primary method for the report is FindTier1CliqueBasic, which
/// strictly follows the "stop on first failure" rule in Section 2.3.
/// </summary>
public static class Tier1Finder
{
    /// <summary>
    /// Implements the greedy heuristic from Project 2.3:
    ///   - Rank all ASes by global degree (descending) to form R = {AS1, AS2, ...}.
    ///   - Initialize S = {AS1}.
    ///   - For AS2, AS3, ... in order:
    ///         if the candidate is connected (by any relationship) to
    ///         ALL ASes already in S, add it to S;
    ///         otherwise TERMINATE immediately.
    /// 
    /// Returns the clique S in the order ASes were added. This is the
    /// clique you should use to populate Table 1 (size of Tier-1 list,
    /// and the first 10 ASNs in S).
    /// </summary>

    public static List<int> FindTier1CliqueBasic(Dictionary<int, ASNode> nodes)
    {
        var clique = new List<int>();

        if (nodes == null || nodes.Count == 0)
            return clique;

        // Rank ASes by global degree (descending)
        var ranked = nodes.Values
                          .OrderByDescending(n => n.GlobalDegree)
                          .ToList();

        // Start with AS1
        clique.Add(ranked[0].Asn);

        // Walk AS2, AS3, ...
        for (int i = 1; i < ranked.Count; i++)
        {
            int candidateAsn = ranked[i].Asn;

            if (IsConnectedToClique(candidateAsn, clique, nodes))
            {
                clique.Add(candidateAsn);
            }
            else
            {
                // Terminate on first AS that is not connected to all in S
                break;
            }
        }

        return clique;
    }

/// <summary>
/// if the basic greedy clique S is very small
/// (e.g., |S| &lt; desiredMinSize), keep scanning further down the
/// global-degree ranking (up to maxRankToInspect) and add any AS
/// that is connected to all ASes currently in S.
/// </summary>
public static List<int> GrowCliqueIfSmall(
    Dictionary<int, ASNode> nodes,
    int desiredMinSize = 10,
    int maxRankToInspect = 50)
{
    var clique = FindTier1CliqueBasic(nodes);

    if (nodes == null || nodes.Count == 0)
        return clique;

    // Only bother growing if we got a very small clique from
    // the strict greedy algorithm.
    if (clique.Count >= desiredMinSize)
        return clique;

    var ranked = nodes.Values
                      .OrderByDescending(n => n.GlobalDegree)
                      .ToList();

    int limit = Math.Min(maxRankToInspect, ranked.Count);

    // We already processed rank 0..(clique.Count-1) in the basic heuristic.
    // Now just scan further down the list and add any AS that fits.
    for (int i = clique.Count; i < limit; i++)
    {
        int candidateAsn = ranked[i].Asn;

        if (IsConnectedToClique(candidateAsn, clique, nodes))
        {
            clique.Add(candidateAsn);
        }
    }

    return clique;
}


    private static bool IsConnectedToClique(
        int candidateAsn,
        List<int> clique,
        Dictionary<int, ASNode> nodes)
    {
        if (!nodes.TryGetValue(candidateAsn, out var cand))
            return false;

        foreach (int otherAsn in clique)
        {
            if (!nodes.TryGetValue(otherAsn, out var other))
                return false;

            if (!AreConnected(cand, other))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if there is any relationship (provider, customer, or peer)
    /// between the two ASes, in either direction. This corresponds to the
    /// "connected via any type of link" condition in Section 2.3.
    /// </summary>
    private static bool AreConnected(ASNode a, ASNode b)
    {
        int asA = a.Asn;
        int asB = b.Asn;

        // From a's perspective
        if (a.Providers.Contains(asB) ||
            a.Customers.Contains(asB) ||
            a.Peers.Contains(asB))
            return true;

        // From b's perspective (in case of asymmetry)
        if (b.Providers.Contains(asA) ||
            b.Customers.Contains(asA) ||
            b.Peers.Contains(asA))
            return true;

        return false;
    }
}
