using System.Collections.Generic;

namespace pms.Models
{
    public class JobOpeningsViewModel
    {
        public PaginatedList<Job> Jobs { get; set; }
        public List<int> AppliedJobIds { get; set; }
        public decimal? PlacedPackage { get; set; }
        public decimal StudentCGPA { get; set; }
    }
}
