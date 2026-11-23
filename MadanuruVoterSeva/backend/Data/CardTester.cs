using System;
using System.IO;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MadanuruVoterSeva.Backend.Data;

public class CardTester
{
    public static void TestCard(string pdfPath)
    {
        using var doc = PdfDocument.Open(pdfPath);
        var page = doc.GetPage(3);
        string text = page.Text;
        var m = Regex.Match(text, @"(\u0c17\u0c3e\.\u0c2a\u0c02\.\u0c13\.\u0c1c\u0c3e\.\u0c35\.\u0c38\u0c02\.\s*1\s.*?)(?=\u0c17\u0c3e\.\u0c2a\u0c02\.\u0c13\.\u0c1c\u0c3e\.\u0c35\.\u0c38\u0c02\.\s*2|$)");
        string card = m.Groups[1].Value;

        var parts = card.Split(':');
        Console.WriteLine($"Colon split count: {parts.Length}");

        if (parts.Length >= 6)
        {
            // Name is in parts[1], remove the relative label at the end
            string nameRaw = parts[1].Trim();
            // remove trailing relation label (e.g. "...తం డి పేరు", "...తండ్రి పేరు", "...భర  పేరు")
            var nameClean = Regex.Replace(nameRaw, @"[\u0c00-\u0c7f\s]*\u0c2a\u0c47\u0c30\u0c41$", "").Trim();
            if (string.IsNullOrEmpty(nameClean))
            {
                nameClean = Regex.Replace(nameRaw, @"(?:\u0c24\u0c02\s*\u0c21\u0c3f|\u0c2d\u0c30|\u0c2d\u0c30\u0c4d\u0c24|\u0c24\u0c02\u0c21\u0c4d\u0c30\u0c3f)\s*$", "").Trim();
            }

            // Relative name is in parts[2], remove trailing "వయసు"
            string relRaw = parts[2].Trim();
            string relClean = Regex.Replace(relRaw, @"\u0c35\u0c2f\u0c38\u0c41\s*$", "").Trim();

            // Age is in parts[3]
            var ageMatch = Regex.Match(parts[3], @"\d+");
            string age = ageMatch.Value;

            // Gender is in parts[4]
            string gender = Regex.Replace(parts[4], @"\u0c07\u0c02\u0c1f\u0c3f.*", "").Trim();

            // House is in parts[5]
            string house = Regex.Replace(parts[5], @"\u0c13\u0c1f\u0c30\u0c41.*", "").Trim();

            // EPIC from the entire card
            var epic = Regex.Match(card, @"[A-Z]{2,4}\d{6,8}").Value;

            Console.WriteLine($"Parsed Voter Name: '{nameClean}'");
            Console.WriteLine($"Parsed Relative Name: '{relClean}'");
            Console.WriteLine($"Parsed Age: '{age}'");
            Console.WriteLine($"Parsed Gender: '{gender}'");
            Console.WriteLine($"Parsed House: '{house}'");
            Console.WriteLine($"Parsed EPIC: '{epic}'");

            var images = page.GetImages();
            Console.WriteLine($"Page 3 Total Images: {images.Count()}");
            int idx = 0;
            foreach (var img in images)
            {
                Console.WriteLine($"Img {idx++}: BoundingBox={img.BoundingBox}, RawBytes={img.RawBytes.Length}");
                if (img.TryGetPng(out var pngBytes))
                {
                    Console.WriteLine($"  PNG bytes: {pngBytes.Length}");
                }
            }
        }
    }
}
