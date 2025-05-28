using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class CityViewModel
    {
        public IEnumerable<Region> RegionList { get; set; }
        public City Cities { get; set; }

        public Region Region { get; set; }
        public List<string> CityList { get; set; }
     

    }
}
