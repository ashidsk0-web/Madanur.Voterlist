using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MadanuruVoterSeva.Backend.Data;

public class PdfInspector
{
    public static void Inspect(string pdfPath)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"Error: PDF not found at {pdfPath}");
            return;
        }

        var fileInfo = new FileInfo(pdfPath);
        Console.WriteLine($"=== PDF Inspection: {fileInfo.Name} ===");
        Console.WriteLine($"File Size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");

        using var document = PdfDocument.Open(pdfPath);
        Console.WriteLine($"Total Pages: {document.NumberOfPages}");

        if (document.Information != null)
        {
            Console.WriteLine($"Title: {document.Information.Title}");
            Console.WriteLine($"Author: {document.Information.Author}");
            Console.WriteLine($"Creator: {document.Information.Creator}");
            Console.WriteLine($"Producer: {document.Information.Producer}");
        }

        // Check pages 1, 2, 3, 4, 5
        int sampleCount = Math.Min(5, document.NumberOfPages);
        for (int i = 1; i <= sampleCount; i++)
        {
            var page = document.GetPage(i);
            string text = page.Text;
            Console.WriteLine($"\n--- Page {i} (Width: {page.Width}, Height: {page.Height}, Letters count: {page.Letters.Count}) ---");
            Console.WriteLine($"Text Length: {text.Length}");
            if (text.Length > 0)
            {
                var preview = text.Length > 500 ? text.Substring(0, 500) + "..." : text;
                Console.WriteLine($"Preview:\n{preview}");
            }
            else
            {
                Console.WriteLine("Warning: No extractable text found on this page (might be scanned/image-only).");
                Console.WriteLine($"Number of Images on page: {page.GetImages().Count()}");
            }
        }
    }
}
