using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ComposeMailViewModel
    {

        // String to hold the emails separated by a semicolon
        public string SelectedEmailsString { get; set; }

        // Converted list of emails
        public List<string> SelectedEmails { get; set; }

        public bool SendToAll { get; set; }
        public string Subject { get; set; }
     

        [Required (ErrorMessage ="Please compose your message")]
        public string Message { get; set; }

  

    }
}
