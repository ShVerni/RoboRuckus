using System.ComponentModel.DataAnnotations;

namespace RoboRuckus.Models
{
    public class playerSetupViewModel
    {
        [Required]
        [Display()]
        public int botX { get; set; }

        [Required]
        [Display()]
        public int botY { get; set; }

        [Required]
        [Display()]
        public int botDir { get; set; }

        [Required]
        [Display()]
        public int player { get; set; }

        [Required(ErrorMessage = "Please choose a robot")]
        [Display(Name = "Robots")]
        public string botName { get; set; }


    }
}
