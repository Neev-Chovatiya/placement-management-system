using System.Collections.Generic;

namespace pms.Models
{
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; }
        public int TotalJobsAvailable { get; set; }
        public int ApplicationsSubmitted { get; set; }
        public int InterviewCalls { get; set; }
        public PaginatedList<Notification> Notifications { get; set; }
    }
}
