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

            // === 2.3 logic will go here later ===
            // (Using As2TypeClassifier and the CAIDA as2type file)
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
