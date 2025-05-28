using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WebOptimus.Migrations;

namespace WebOptimus.Models
{
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int NotificationId { get; set; }

        public DateTime ViewedOn { get; set; } = DateTime.UtcNow;

        public string UserEmail { get;set; } = string.Empty;
        public int ViewCount { get; set; } = 0;


    }
}
