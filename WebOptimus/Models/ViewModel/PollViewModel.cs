using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class PollViewModel
    {
        public int PollId { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string AnswerType { get; set; } // 'Radio', 'Checkbox', 'Input', 'Textarea'

        public List<string> Options { get; set; } = new List<string>();  // For radio or checkbox answers

        public string SelectedAnswer { get; set; }  // For storing radio, input, or textarea answers

        public List<string> SelectedAnswers { get; set; } = new List<string>();
    }
}
