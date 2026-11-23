using System;
using System.IO;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MadanuruVoterSeva.Backend.Data;

public class PdfParserTest
{
    public static void TestPage(string pdfPath, int pageNum)
    {
        using var doc = PdfDocument.Open(pdfPath);
        var page = doc.GetPage(pageNum);
        string text = page.Text;
        Console.WriteLine($"=== Testing Page {pageNum} (Length: {text.Length}) ===");

        // Extract Ward
        int wardNumber = 0;
        var wardMatch = Regex.Match(text, @"(?:వార్డు|వారు)\s*నె[ం౦]బరు\s*:\s*(\d+)");
        if (wardMatch.Success)
        {
            wardNumber = int.Parse(wardMatch.Groups[1].Value);
            Console.WriteLine($"Found Ward: {wardNumber}");
        }

        // Match voter blocks
        // Notice each voter card starts with గా.పం.ఓ.జా.వ.సం.
        var matches = Regex.Matches(text, @"గా\.పం\.ఓ\.జా\.వ\.సం\.\s*(\d+)");
        Console.WriteLine($"Found {matches.Count} voter cards on page {pageNum}");

        for (int i = 0; i < Math.Min(3, matches.Count); i++)
        {
            int startIndex = matches[i].Index;
            int endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;
            string cardText = text.Substring(startIndex, endIndex - startIndex);
            Console.WriteLine($"\n--- Card {i + 1} ---");
            Console.WriteLine(cardText.Trim());

            // Extract individual fields
            var gpSerialMatch = Regex.Match(cardText, @"గా\.పం\.ఓ\.జా\.వ\.సం\.\s*(\d+)");
            var asmMatch = Regex.Match(cardText, @"అ\.ఓ\.జా\.వ\.సం\.\s*(\d+)\s*-\s*(\d+)\s*-\s*(\d+)");
            var nameMatch = Regex.Match(cardText, @"ఓటరు పేరు\s*:\s*(.*?)(?=(?:తం\s*డి|తండ్రి|భర\s*|భర్త)\s*పేరు|వయసు\s*:|లింగము\s*:|$)");
            var relMatch = Regex.Match(cardText, @"(?:తం\s*డి|తండ్రి|భర\s*|భర్త)\s*పేరు\s*:\s*(.*?)(?=వయసు\s*:|లింగము\s*:|ఇంటి\s*సంఖ|$)");
            var ageMatch = Regex.Match(cardText, @"వయసు\s*:\s*(\d+)");
            var genderMatch = Regex.Match(cardText, @"లింగము\s*:\s*(.*?)(?=ఇంటి\s*సంఖ|ఓటరు\s*ఐడి|$)");
            var houseMatch = Regex.Match(cardText, @"ఇంటి\s*సంఖ[్య]*\s*:\s*(.*?)(?=ఓటరు\s*ఐడి|$)");
            var epicMatch = Regex.Match(cardText, @"ఓటరు\s*ఐడి[.\s]*కార్[డు\s]*నెం[.\s]*:\s*([A-Z0-9]+)");

            Console.WriteLine($"GP Serial: {gpSerialMatch.Groups[1].Value}");
            if (asmMatch.Success)
            {
                Console.WriteLine($"Assembly: Const={asmMatch.Groups[1].Value}, Part={asmMatch.Groups[2].Value}, Serial={asmMatch.Groups[3].Value}");
            }
            Console.WriteLine($"Name: '{nameMatch.Groups[1].Value.Trim()}'");
            Console.WriteLine($"Relative: '{relMatch.Groups[1].Value.Trim()}'");
            Console.WriteLine($"Age: '{ageMatch.Groups[1].Value.Trim()}'");
            Console.WriteLine($"Gender: '{genderMatch.Groups[1].Value.Trim()}'");
            Console.WriteLine($"House: '{houseMatch.Groups[1].Value.Trim()}'");
            Console.WriteLine($"EPIC: '{epicMatch.Groups[1].Value.Trim()}'");
        }
    }
}
