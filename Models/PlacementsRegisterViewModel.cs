using System.Collections.Generic;

namespace pms.Models
{
    public class PlacementsRegisterViewModel
    {
        public PaginatedList<Placement> Placements { get; set; }
        public string SelectedYear { get; set; }
        public string SelectedBranch { get; set; }
        public string SearchQuery { get; set; }
    }
}
