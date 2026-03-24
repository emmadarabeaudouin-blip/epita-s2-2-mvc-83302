using FSIT.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace epita_s2_2_mvc_83302.Models
{
    [NotMapped]
    public class DashboardViewModel
    {
        public int InspectionsThisMonth { get; set; }
        public int FailedThisMonth { get; set; }
        public int OverdueFollowUps { get; set; }
        public string? FilterTown { get; set; }
        public RiskRating? FilterRiskRating { get; set; }
        public List<string> Towns { get; set; } = new();
    }
}
