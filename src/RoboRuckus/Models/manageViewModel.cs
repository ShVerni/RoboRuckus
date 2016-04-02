using System.ComponentModel.DataAnnotations;
using RoboRuckus.RuckusCode;

namespace RoboRuckus.Models
{
    public class manageViewModel
    {
        [Required]
        [Display(Name = "Bot X position")]
        public int botX { get; set; }

        [Required]
        [Display(Name = "Bot Y position")]
        public int botY { get; set; }

        [Required]
        [Display(Name = "Bot Facing")]
        public int botDir { get; set; }

        [Required]
        [Display()]
        public int player { get; set; }

        [Required]
        [Display(Name = "Player Lives")]
        public int lives { get; set; }

        [Required]
        [Display(Name = "Player Flags")]
        public int flags { get; set; }

        [Required]
        [Display(Name = "Bot Damage")]
        public sbyte damage { get; set; }

        [Display(Name = "Available Robots")]
        public string botName { get; set; }

        public manageViewModel(Player caller)
        {
            Robot bot = caller.playerRobot;
            botX = bot.x_pos;
            botY = bot.y_pos;
            botDir = (int)bot.currentDirection;
            player = caller.playerNumber + 1;
            damage = caller.playerRobot.damage;
            lives = caller.lives;
            flags = bot.flags;
        }
    }
}
