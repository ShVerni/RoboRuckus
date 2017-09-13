namespace RoboRuckus.RuckusCode
{
    /// <summary>
    /// Represents a game board and all the elemtns it contains
    /// </summary>
    public class Board
    {
        public string name;
        public int[] size;
        public int[][] wrenches;
        public int[][] pits;
        public turntable[] turntables;
        public int[][] flags;

        /// <summary>
        /// Walls consist of a pair of [x,y] coordinates representing the two squares the 
        /// wall is between
        /// </summary>
        public int[][][] walls;

        public laser[] lasers;
        public conveyor[] conveyors;
        public conveyor[] expressConveyors;
    }

    /// <summary>
    /// Represents a turntable element
    /// </summary>
    public class turntable
    {
        /// <summary>
        /// The [x,y] coordinates of the turntable
        /// </summary>
        public int[] location;

        /// <summary>
        /// The direction of rotation
        /// </summary>
        public string dir;
    }

    /// <summary>
    /// Represents a spae on a conveyor belt
    /// </summary>
    public class conveyor
    {
        /// <summary>
        /// The [x,y] coordinates of the conveyor space
        /// </summary>
        public int[] location;

        /// <summary>
        /// The orientation of the starting end of the belt within the space
        /// </summary>
        public Robot.orientation entrance;

        /// <summary>
        /// The orientation of the ending end of the belt within the space
        /// </summary>
        public Robot.orientation exit;
    }

    /// <summary>
    /// Represents a board laser
    /// </summary>
    public class laser
    {
        /// <summary>
        /// The coordinate from which the laser is firing
        /// </summary>
        public int[] start;

        /// <summary>
        /// The coordinate to which the laser fires
        /// </summary>
        public int[] end;

        /// <summary>
        /// The power of the laser
        /// </summary>
        public sbyte strength;

        /// <summary>
        /// The orientation in which the laser is facig
        /// </summary>
        public Robot.orientation facing;
    }

    /// <summary>
    /// A convenient structure to group a robot and a conveyor to help facilitate movement
    /// </summary>
    public class conveyorModel
    {
        /// <summary>
        /// The conveyor space on which the bot sits
        /// </summary>
        public conveyor space;

        /// <summary>
        /// The coordinate to where the conveyor will move the bot
        /// </summary>
        public int[] destination;

        /// <summary>
        /// The robot on the conveyor space
        /// </summary>
        public Robot bot;
    }
}
