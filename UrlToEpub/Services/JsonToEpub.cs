using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using EpubCore.Fluent;
using UrlToEpub.Services;

public class JsonToEpub
{
  private readonly string _epubOutputPath = Path.Join(Directory.GetCurrentDirectory(), "/Outputs/epubs");
  private readonly string _jsonOutputPath = Path.Join(Directory.GetCurrentDirectory(), "/Outputs");
  private readonly Dictionary<int, EpubBookBuilder> _epubBookBuilders = new Dictionary<int, EpubBookBuilder>();

  EpubBookBuilder GetEpubBuilder(int episode)
  {
    if (_epubBookBuilders.ContainsKey(episode))
    {
      return _epubBookBuilders[episode];
    }

    var builder = EpubBookBuilder.Create();

    builder.WithTitle($"Against the Gods - Volume {episode}")
      .WithUniqueIdentifier(Guid.NewGuid().ToString())
      .AddAuthor("Mars Gravity");

    _epubBookBuilders[episode] = builder;

    return _epubBookBuilders[episode];
  }

  string GetEpubContent(string? title, string? content)
  {
    return @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"">
  <head>
    <title>Chapter 1</title>
    <link rel=""stylesheet"" type=""text/css"" href=""styles.css""/>
  </head>
  <body>
    <h1>{title ?? "Unknown"}</h1>
    {content ?? "Unknown"}
  </body>
</html>
";
  }

  int ExtractChapter(string path)
  {
    var fileName = path.Split("/")[^1];
    var matches = Regex.Match(fileName, "^Chapter (\\w+)");

    if (matches.Groups.Count > 1)
    {
      return int.Parse(matches.Groups[1].ToString());
    }
    
    
    return -1;
  }

  private List<string> GetFilePaths(string jsonDir)
  {
    var filePaths = Directory.GetFiles(jsonDir, "*.json").ToList();

    filePaths.Sort((a, b) =>
    {
      var chapA = ExtractChapter(a);
      var chapB = ExtractChapter(b);

      return chapA - chapB;
    });

    return filePaths;
  }

  public void Run()
  {
    if (!Directory.Exists(_epubOutputPath))
    {
      Directory.CreateDirectory(_epubOutputPath);
    }

    foreach (string filePath in GetFilePaths(_jsonOutputPath))
    {
      Console.WriteLine($"Converting file {filePath}");

      if (!File.Exists(filePath))
      {
        throw new Exception($"File not found {filePath}");
      }

      var fileContent = File.ReadAllText(filePath);

      var chapterInfo = JsonSerializer.Deserialize<ChapterInfo>(fileContent);
      if (chapterInfo == null)
      {
        Console.WriteLine($"Invaid json format {filePath}");
        break;
      }

      var chapterTitle = chapterInfo.ChapterTitle;
      var chapterContent = HttpUtility.HtmlDecode(chapterInfo.Content);
      var epubContent = GetEpubContent(chapterTitle, chapterContent);

      var chapterNumber = ExtractChapter(filePath);
      int maxEpisode = 100;
      var episode = chapterNumber / maxEpisode;

      GetEpubBuilder(episode).AddChapter(chapterTitle, epubContent);
    }
    
    foreach (KeyValuePair<int, EpubBookBuilder> keyValuePair in _epubBookBuilders.ToList())
    {
      var fileName = Path.Join(_epubOutputPath, $"/Against the Gods - Volume {keyValuePair.Key}.epub");
      keyValuePair.Value.Build(fileName);
    }
  }
}