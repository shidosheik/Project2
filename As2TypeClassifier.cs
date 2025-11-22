using System;
using System.Collections.Generic;
using System.IO;

public static class As2TypeClassifier
{
    // Load ASN -> type string from as2type file
    public static Dictionary<int, string> LoadAsTypes(string path)
    {
        var types = new Dictionary<int, string>();

        using var reader = new StreamReader(path);
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            // Split on whitespace, tab, or '|'
            var parts = line.Split(new[] { '\t', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);

            // Expect at least "ASN | source | type" or similar
            if (parts.Length < 3)
                continue;

            if (!int.TryParse(parts[0], out int asn))
                continue;

            string asType = parts[parts.Length - 1].Trim();
            types[asn] = asType;
        }

        return types;
    }

    // Count CAIDA classes (Transit/Access, Content, Enterprise),
    // similar to your old AsCounts class.
    public static (int TransitAccess, int Content, int Enterprise) CountTypes(
        Dictionary<int, string> types)
    {
        int transit = 0, content = 0, enterprise = 0;

        foreach (var kvp in types)
        {
            string type = kvp.Value.ToLowerInvariant();

            if (type.Contains("transit"))
                transit++;
            else if (type.Contains("content"))
                content++;
            else if (type.Contains("enterprise"))
                enterprise++;
        }

        return (transit, content, enterprise);
    }
}
