using System.Collections.Generic;

namespace pms.Models
{
    public class HomeViewModel
    {
        public int TotalStudentsPlaced { get; set; }
        public int TotalCompaniesVisited { get; set; }
        public List<Company> TopRecruitingPartners { get; set; }
    }
}
