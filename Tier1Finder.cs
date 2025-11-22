using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Project 2.3: Inference of Tier-1 ASes using the greedy clique heuristic
/// described in the assignment.
/// </summary>
public static class Tier1Finder
{
    /// <summary>
    /// Implements the basic greedy heuristic:
    ///  - Sort ASes by global degree (descending).
    ///  - S = { AS_1 }.
    ///  - For AS_2, AS_3, ... in order:
    ///        if connected to ALL ASes in S, add to S
    ///        else STOP (terminate on first failure).
    ///
    /// Returns the clique S as a list of ASNs in the order they were added.
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
    /// If the basic algorithm stops very early (e.g., clique size < desiredMinSize),
    /// keep scanning further down the ranked list (up to maxRankToInspect) and
    /// add any AS that is connected to all ASes currently in S.
    /// </summary>
    public static List<int> GrowCliqueIfSmall(
        Dictionary<int, ASNode> nodes,
        int desiredMinSize = 10,
        int maxRankToInspect = 50)
    {
        var clique = FindTier1CliqueBasic(nodes);

        if (nodes == null || nodes.Count == 0)
            return clique;

        if (clique.Count >= desiredMinSize)
            return clique;  // already large enough

        var ranked = nodes.Values
                          .OrderByDescending(n => n.GlobalDegree)
                          .ToList();

        int limit = Math.Min(maxRankToInspect, ranked.Count);

        // We already processed rank 0..(clique.Count-1) in the basic heuristic.
        // Now just scan further down the list and add any AS that fits.
        for (int i = clique.Count; i < limit && clique.Count < desiredMinSize; i++)
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
