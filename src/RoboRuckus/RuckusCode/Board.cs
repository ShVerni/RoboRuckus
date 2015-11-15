using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoboRuckus.RuckusCode
{
    public class Board
    {
        public string name;
        public int[] size;
        public int[][] wrenches;
        public int[][] pits;
        public turntable[] turntables;
        public int[][][] walls;
        public laser[] lasers;
    }

    public class turntable
    {
        public int[] coord;
        public string dir;
    }

    public class laser
    {
        public int[] start;
        public int[] end;
        public byte strength;
        public Robot.orientation facing;
    }
}
