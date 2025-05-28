using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Services
{
    public class ExtendedSelectListItem : SelectListItem
    {
        public Dictionary<string, string> DataAttributes { get; set; } = new Dictionary<string, string>();
    }
}
