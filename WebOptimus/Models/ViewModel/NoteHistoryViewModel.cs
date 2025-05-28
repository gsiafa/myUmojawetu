namespace WebOptimus.Models.ViewModel.Admin
{
    public class NoteHistoryViewModel
    {
        public int Id { get; set; }
        
        public int NoteTypeId { get; set; }
        public string Description { get; set; }
        public string PersonRegNumber { get; set; }


        public string NoteTypeName { get; set; }

        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public string CreatedByName { get; set; }

    }
}
