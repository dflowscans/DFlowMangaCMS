using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class SiteSetting
{
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
