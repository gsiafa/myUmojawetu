using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel.Admin
{
    public class DependentDetailsViewModel
    {
        public Dependant Dependant { get; set; }

        public User GetUserInfo { get; set; }

        public string PersonRegNumber { get; set; }
        public string Region { get;set; }
        public string TitleName { get; set; }
        public string City { get; set; }

        public string Title { get; set; }

        public string Gender { get; set; }
        public Dependant Deps { get; set; }

        public bool IsExamptFromPayment { get; set; }

        public IEnumerable<Dependant> DepsList { get; set; }
       
        public DependentWithKinViewModel NextOfKins { get; set; } 

        public List<DependantViewModel> Dependents { get; set; } = new List<DependantViewModel>();

        public List<ConfirmedDeath> DeceasedFamilyMembers { get; set; } = new List<ConfirmedDeath>();

        public List<ReportedDeath> ReportedDeaths { get; set; } = new List<ReportedDeath>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<City> Cities { get; set; } = new List<City>();
        public List<CustomPayment> CustomPayments { get; set; } = new List<CustomPayment>();

        //  List of All Reported Deaths for CauseRef Selection
        public List<ReportedDeath> AllReportedDeaths { get; set; } = new List<ReportedDeath>();
        public ReportedDeath Reported { get; set; } 
        [NotMapped]
        public string? DOB { get; set; } = string.Empty;
        
        public List<NoteHistoryViewModel> NoteHistory { get; set; } = new List<NoteHistoryViewModel>();

        public List<NoteType> NoteTypes { get; set; } = new List<NoteType>();        
    }
}
