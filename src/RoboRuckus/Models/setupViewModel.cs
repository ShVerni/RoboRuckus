using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace RoboRuckus.Models
{
    public class setupViewModel
    {
        [Required]
        [Range(1, 12, ErrorMessage = "Please enter valid integer number between 1 and 12")]
        [Display(Name = "Number of players")]
        public int numberOfPlayers { get; set; }

        [Required]
        [Display(Name = "Board")]
        public string selBoard { get; set; }

        [Required(ErrorMessage = "Please place at least one flag")]
        [Display(Name = "Flags")]
        public string flags { get; set; }

        public IEnumerable<SelectListItem> boards { get; set; }

        [Required]
        [Display(Name = "Enable Timer")]
        public bool timerEnabled { get; set; }
    }
}