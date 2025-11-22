using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Adjust these paths as needed
        string relPath      = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\Project2\data\ASRelationships.txt";
        string pfx2asPath   = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\Project2\data\routeviews-rv2-20251110-1200.pfx2as";
        string outputPath   = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\Project2\data\ASDegreesAndIpSpace.csv";
        // Paths for CAIDA classification files (2.3)
        string as2015Path = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\Project2\data\AS2015.txt";
        string as2021Path = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\Project2\data\AS2021.txt";

        string dataDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
        string caidaSummaryCsvPath = Path.Combine(dataDir, "CAIDA_Summary.csv");
        string comparisonCsvPath   = Path.Combine(dataDir, "Section2_3_Comparison.csv");
        string tier1BasicCsvPath   = Path.Combine(dataDir, "Tier1Clique_Basic.csv");
        string tier1GrownCsvPath   = Path.Combine(dataDir, "Tier1Clique_Grown.csv");


        var nodes = new Dictionary<int, ASNode>();

        if (!File.Exists(relPath))
        {
            Console.WriteLine($"Relationship file not found: {relPath}");
            return;
        }

        try
        {
            // 1) Build graph
            using (var reader = new StreamReader(relPath))
            {
                string? line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    ASGraph.ProcessRelationshipLine(line, lineNumber, nodes);
                }
            }

            // 2) Classify nodes (Graph 4)
            ASGraph.ClassifyNodes(nodes);

            // 3) Load IP space (Graph 3)
            if (File.Exists(pfx2asPath))
            {
                ASGraph.LoadIpSpaceFromPfx2As(pfx2asPath, nodes);
            }
            else
            {
                Console.WriteLine($"Warning: pfx2as file not found: {pfx2asPath}");
            }

            // 4) CSV output
            ASGraph.WriteCsv(outputPath, nodes);
            Console.WriteLine($"\nCSV written to: {outputPath}");

            // ---------- CAIDA classification statistics ----------

            // 2015 & 2021 raw CAIDA counts
            var counts2015 = CaidaClassificationStats.LoadCounts(as2015Path);
            var counts2021 = CaidaClassificationStats.LoadCounts(as2021Path);

            CaidaClassificationStats.PrintSummary("2015", counts2015);
            CaidaClassificationStats.PrintSummary("2021", counts2021);

            using (var caidaWriter = new StreamWriter(caidaSummaryCsvPath, append: false))
            {
                caidaWriter.WriteLine("Label,Total,Enterprise,Content,TransitAccess,EnterprisePct,ContentPct,TransitAccessPct");
                WriteCaidaSummaryRow(caidaWriter, "2015", counts2015);
                WriteCaidaSummaryRow(caidaWriter, "2021", counts2021);
            }

            var caida2021Map = CaidaClassificationStats.LoadMap(as2021Path);

            CaidaClassificationStats.CompareWithInferred(
                nodes,
                caida2021Map,
                out int commonAses,
                out int agree,
                out int disagree);

            Console.WriteLine("\n--- 2.3 Comparison: inferred vs CAIDA (2021) ---");
            Console.WriteLine($"Common ASes (in graph & CAIDA 2021): {commonAses}");
            if (commonAses > 0)
            {
                Console.WriteLine($"Agree:      {agree} ({agree * 100.0 / commonAses:F2}%)");
                Console.WriteLine($"Disagree:   {disagree} ({disagree * 100.0 / commonAses:F2}%)");
            }

            using (var comparisonWriter = new StreamWriter(comparisonCsvPath, append: false))
            {
                comparisonWriter.WriteLine("CommonAses,Agree,Disagree,AgreePct,DisagreePct");

                double agreePct    = commonAses > 0 ? agree    * 100.0 / commonAses : 0.0;
                double disagreePct = commonAses > 0 ? disagree * 100.0 / commonAses : 0.0;

                comparisonWriter.WriteLine(
                    $"{commonAses},{agree},{disagree},{agreePct:F2},{disagreePct:F2}");
            }

            // ---------- Tier-1 inference via clique heuristic ----------

           // Step 1: basic greedy clique (exactly as described in the bullets)
            var tier1Basic = Tier1Finder.FindTier1CliqueBasic(nodes);

            Console.WriteLine("\n--- 2.3 Tier-1 clique ( greedy heuristic) ---");
            Console.WriteLine($"Clique size |S| : {tier1Basic.Count}");
            for (int i = 0; i < Math.Min(10, tier1Basic.Count); i++)
            {
                int asn = tier1Basic[i];
                var node = nodes[asn];
                Console.WriteLine($"{i + 1}. AS{asn}  (degree = {node.GlobalDegree})");
            }

            // Optional Step 2: apply the Note to try to get at least 10 nodes
            var tier1Grown = Tier1Finder.GrowCliqueIfSmall(nodes, desiredMinSize: 10, maxRankToInspect: 50);

            Console.WriteLine("\n--- 2.3 Tier-1 clique after applying Note (top 50) ---");
            Console.WriteLine($"Clique size |S| (grown): {tier1Grown.Count}");
            for (int i = 0; i < Math.Min(10, tier1Grown.Count); i++)
            {
                int asn = tier1Grown[i];
                var node = nodes[asn];
                Console.WriteLine($"{i + 1}. AS{asn}  (degree = {node.GlobalDegree})");
            }

            using (var basicWriter = new StreamWriter(tier1BasicCsvPath, append: false))
            {
                basicWriter.WriteLine("Rank,Asn,GlobalDegree");
                for (int i = 0; i < tier1Basic.Count; i++)
                {
                    int asn = tier1Basic[i];
                    var node = nodes[asn];
                    basicWriter.WriteLine($"{i + 1},{asn},{node.GlobalDegree}");
                }
            }

            // NEW: dump full grown clique to CSV
            using (var grownWriter = new StreamWriter(tier1GrownCsvPath, append: false))
            {
                grownWriter.WriteLine("Rank,Asn,GlobalDegree");
                for (int i = 0; i < tier1Grown.Count; i++)
                {
                    int asn = tier1Grown[i];
                    var node = nodes[asn];
                    grownWriter.WriteLine($"{i + 1},{asn},{node.GlobalDegree}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void WriteCaidaSummaryRow(
        StreamWriter writer,
        string label,
        CaidaClassificationStats.Counts c)
    {
        double enterprisePct = c.Total > 0 ? c.Enterprise    * 100.0 / c.Total : 0.0;
        double contentPct    = c.Total > 0 ? c.Content       * 100.0 / c.Total : 0.0;
        double transitPct    = c.Total > 0 ? c.TransitAccess * 100.0 / c.Total : 0.0;

        writer.WriteLine(
            $"{label},{c.Total},{c.Enterprise},{c.Content},{c.TransitAccess}," +
            $"{enterprisePct:F6},{contentPct:F6},{transitPct:F6}");
    }

}
