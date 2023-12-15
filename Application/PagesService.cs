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
                    page.Error = null;
                    result.Add(page);
                }
                catch(Exception ex){
                    var page = new Page() { Error = ex.Message, Name = Path.GetFileName(path)};
                    result.Add(page);
                }
            }

            return result;
        }
    }
}
