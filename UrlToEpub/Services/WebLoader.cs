using System.Text.Json;

namespace UrlToEpub.Services;
using HtmlAgilityPack;

public class ChapterInfo
{
  public string? ChapterTitle { get; set; }
  public string? Content { get; set; }
  public string? NextUrl { get; set; }
}

public class WebLoader
{
  private const string BaseUrl = "https://www.webnovelpub.pro";
  private readonly string _outputDir = Path.Join(Directory.GetCurrentDirectory(), "/Outputs");

  public WebLoader()
  {
    if (!Directory.Exists(_outputDir))
    {
      Directory.CreateDirectory(_outputDir);
    }
  }

  private ChapterInfo Load(string fullUrl)
  {
    Console.WriteLine($"looking at {fullUrl}");

    HtmlWeb web = new HtmlWeb();
    var htmlDoc = web.Load(fullUrl);

    var chapterContainer = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='chapter-container']");
    var chapterTitleContainer = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='chapter-title']");
    var nextButton = htmlDoc.DocumentNode.SelectSingleNode("//a[@rel='next' and @class='button nextchap ']");

    if (nextButton == null)
    {
      throw new Exception($"Next button not found on page {fullUrl}");
    }

    var chapterTitle = chapterTitleContainer.InnerHtml;
    var innerContent = chapterContainer.InnerHtml;

    return new ChapterInfo
    {
      ChapterTitle = chapterTitle,
      Content = innerContent,
      NextUrl = nextButton.Attributes["href"].Value
    };
  }

  public void Run()
  {
    var currentPath = "/novel/against-the-gods-7/chapter-1301";
    
    while (true)
    {
      var fullUrl = $"{BaseUrl}{currentPath}";
      
      var chapterInfo = Load(fullUrl);
  
      var json = JsonSerializer.Serialize(chapterInfo);
      var jsonOutputFile = Path.Join(_outputDir, $"/{chapterInfo.ChapterTitle}.json");
      File.WriteAllText(jsonOutputFile, json);

      if (chapterInfo.NextUrl == null)
      {
        Console.WriteLine("There's no next URL");
        break;
      }

      currentPath = chapterInfo.NextUrl;
    }
  }
}