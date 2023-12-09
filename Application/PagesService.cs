using Domain;
using Newtonsoft.Json;

namespace Application
{
    public static class PagesService
    {
        public static List<Page> GetPages()
        {
            var result = new List<Page>();

            var pageFilePaths = Directory.GetFiles(Directory.GetParent(Environment.CurrentDirectory)?.ToString() ?? throw new Exception("Failed to get dir"), "*.autocrud.json", SearchOption.TopDirectoryOnly);

            foreach (var path in pageFilePaths)
            {
                try
                {
                    var page = JsonConvert.DeserializeObject<Page>(File.ReadAllText(path));
                    result.Add(page);
                }
                catch { }
            }

            return result;
        }
    }
}
