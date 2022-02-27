using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace RoboRuckus.Models
{
    public class boardMakerViewModel
    {
        [Required]
        [StringLength(25, ErrorMessage = "Name must be between 1 and 25 characters", MinimumLength = 1)]
        [Display(Name = "Board Name")]
        public string name { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Please enter valid integer number between 1 and 100")]
        [Display(Name = "X Size")]
        public int x_size { get; set; }

        [Display(Name = "Board")]
        public string selBoard { get; set; }

        public IEnumerable<SelectListItem> boards { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Please enter valid integer number between 1 and 100")]
        [Display(Name = "Y Size")]
        public int y_size { get; set; }

        [Required]
        public string boardData { get; set; }

        [Required]
        // [ [x, y, orientation], ...]
        public string cornerData { get; set; }
    }
}
