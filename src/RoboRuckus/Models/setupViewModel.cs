using System.ComponentModel.DataAnnotations;

namespace RoboRuckus.Models
{
    public class setupViewModel
    {
        [Required]
        [Range(1, 12, ErrorMessage = "Please enter valid integer number between 1 and 12")]
        [Display(Name = "Number of players")]
        public int numberOfPlayers { get; set; }
    }
}