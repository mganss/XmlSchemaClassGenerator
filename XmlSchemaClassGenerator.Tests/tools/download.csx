#! "net6.0"

using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

var opts = Args.Where(a => a.StartsWith("-"));

if (Args.Except(opts).Count() < 1)
{
    Console.WriteLine("Usage: dotnet script download.csx [-ddestination] URL...");
    return;
}

XNamespace xs = "http://www.w3.org/2001/XMLSchema";
var xsds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var client = new HttpClient();
var destination = opts.FirstOrDefault(a => a.StartsWith("-d"))?[2..] ?? ".";

void Download(string url, string destination)
{
    destination = Path.GetFullPath(destination);

    var key = $"{url}:{destination}";

    if (xsds.Contains(key)) return;

    Console.WriteLine($"Downloading {url} to {destination}");

    xsds.Add(key);

    var xsd = client.GetStringAsync(url).Result;
    var document = XDocument.Parse(xsd);
    var uri = new Uri(url);
    var fileName = Path.GetFileName(uri.LocalPath);
    var dir = new Uri(uri, ".");
    var locations = document.Descendants(xs + "include")
        .Concat(document.Descendants(xs + "import"))
        .Concat(document.Descendants(xs + "redefine"))
        .Select(e => e.Attribute("schemaLocation")?.Value)
        .Where(a => a != null && !new Uri(a, UriKind.RelativeOrAbsolute).IsAbsoluteUri);

    Directory.CreateDirectory(destination);
    File.WriteAllText(Path.Join(destination, fileName), xsd);

    foreach (var location in locations)
    {
        var locationUri = new Uri(dir, location).OriginalString;
        var locationDest = Path.GetDirectoryName(Path.Join(destination, location));
        Download(locationUri, locationDest);
    }
}

foreach (var url in Args.Except(opts))
{
    Download(url, destination);
}

Console.WriteLine("Done.");