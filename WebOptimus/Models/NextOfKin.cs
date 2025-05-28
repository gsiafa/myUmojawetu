using Microsoft.Extensions.DependencyModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class NextOfKin
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;
        public int DependentId { get; set; }

        public string PersonRegNumber { get; set; } = string.Empty;
        public string NextOfKinName { get; set; } = string.Empty;

        public string Relationship { get; set; } = string.Empty;

        public string? NextOfKinAddress { get; set;} = string.Empty;

        public string? NextOfKinEmail { get; set;} = string.Empty;

        public string NextOfKinTel { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? CreatedBy { get; set; }

        [NotMapped]
        public string DepName { get; set; } = string.Empty;

        [NotMapped]
        public virtual Dependant Dependent { get; set; }

    }
}
