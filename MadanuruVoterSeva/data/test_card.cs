using System;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

using var doc = PdfDocument.Open(@"C:\Voters ID\MadanuruVoterSeva\data\source\MADANURU WITH SORTING VOTER.pdf");
var page = doc.GetPage(3);
string text = page.Text;
var m = Regex.Match(text, @"(గా\.పం\.ఓ\.జా\.వ\.సం\.\s*1\s.*?)(?=గా\.పం\.ఓ\.జా\.వ\.సం\.\s*2|$)");
string card = m.Groups[1].Value;
Console.WriteLine("Card 1 text: " + card);
foreach (char c in card) {
    if (c > 127) Console.Write($"\\u{(int)c:x4}");
    else Console.Write(c);
}
Console.WriteLine();
var epic = Regex.Match(card, @"[A-Z]{2,4}\d{6,8}");
Console.WriteLine("Regex EPIC: " + epic.Value);

var nameM = Regex.Match(card, @"ఓటరు\s*పేరు\s*:\s*(.*?)(?=(?:తం\s*డి|తండ్రి|భర\s*|భర్త)\s*పేరు|వయసు)");
Console.WriteLine("Regex Name: " + nameM.Groups[1].Value);

var relM = Regex.Match(card, @"(?:తం\s*డి|తండ్రి|భర\s*|భర్త)\s*పేరు\s*:\s*(.*?)(?=వయసు)");
Console.WriteLine("Regex Rel: " + relM.Groups[1].Value);
