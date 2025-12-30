using MangaReader.Models;

namespace MangaReader.Services;

public interface IChapterService
{
    Task CreateChapterAsync(Chapter chapter, List<IFormFile> pages, string pageUrls);
}
