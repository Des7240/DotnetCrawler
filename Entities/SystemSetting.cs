using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Entities
{
    public class SystemSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
