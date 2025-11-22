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
        string as2015Path = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\AS2015.txt";
        string as2021Path = @"C:\Users\david\OneDrive\Documents\ECE Masters\578\Project 2\AS2021.txt";


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

            // ---------- Section 2.3: CAIDA classification statistics ----------

            // 2015 & 2021 raw CAIDA counts
            var counts2015 = CaidaClassificationStats.LoadCounts(as2015Path);
            var counts2021 = CaidaClassificationStats.LoadCounts(as2021Path);

            CaidaClassificationStats.PrintSummary("2015", counts2015);
            CaidaClassificationStats.PrintSummary("2021", counts2021);

            // Optional: compare our inferred 2021 types (Graph 4) to CAIDA 2021
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
