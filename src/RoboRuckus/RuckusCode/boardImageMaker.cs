using System;
using System.Linq;
using System.Drawing;

namespace RoboRuckus.RuckusCode
{
    public static class boardImageMaker
    {
        private static Bitmap _boardImage;
        private static Graphics _canvas;
        private static char _separator = System.IO.Path.DirectorySeparatorChar;
        private static string _root = serviceHelpers.rootPath + _separator;
        private static string _imageRoot = _root + "Resources" + _separator + "BoardMakerFiles" + _separator;
        private static Board _board;
        private static int[][] _corners;

        /// <summary>
        /// Creates a print-ready board image
        /// </summary>
        /// <param name="board">The board object to make the image from</param>
        /// <returns>True on success</returns>
        public static bool createImage(Board board, int[][] corners)
        {
            _board = board;
            _corners = corners;
            // Create a new image of the correct size
            _boardImage = new Bitmap(1575 * (board.size[0] + 1), 1575 * (board.size[1] + 1));
            // Set the image resolution
            _boardImage.SetResolution(300, 300);
            // Create a graphics canvas to draw on
            _canvas = Graphics.FromImage(_boardImage);

            // Add the board elements to the image
            addBackground();
            addPits();
            addWrenches();
            addTurntables();
            addConveyors(false);
            addConveyors(true);
            addGrid();
            addBeams();
            addLasers();
            addCorners();
            addWalls();

            // Add the dot to the lower left corner
            using (Image dot = Image.FromFile(_imageRoot + "Dot.png"))
            {
                _canvas.DrawImage(dot, 0, board.size[1] * 1575);
            }

            // Save the canvas
            _canvas.Save();
            _canvas.Dispose();

            // Set the file names
            string filename = _root + "wwwroot" + _separator + "images" + _separator + "printable_boards" + _separator + board.name.Replace(" ", "") + ".png";
            string smallFileName = _root + "wwwroot" + _separator + "images" + _separator + "boards" + _separator + board.name.Replace(" ", "") + ".png";

            //Save the full size image 
            _boardImage.Save(filename, System.Drawing.Imaging.ImageFormat.Png);

            // Reduce the image resolution
            _boardImage.SetResolution(150, 150);

            // Create a new, smaller image for the server to use
            Bitmap smaller = new Bitmap(_boardImage, 100 * (board.size[0] + 1), 100 * (board.size[1] + 1));
            // Save smaller image
            smaller.Save(smallFileName, System.Drawing.Imaging.ImageFormat.Png);

            _boardImage.Dispose();
            smaller.Dispose();
            _board = null;
            _corners = null;
            return true;
        }

        /// <summary>
        /// Adds the background tiles to the image
        /// </summary>
        private static void addBackground()
        {
            using (Image background = Image.FromFile(_imageRoot + "Background.png"))
            {
                for (int y = 0; y <= _board.size[1]; y++)
                {
                    for (int x = 0; x <= _board.size[0]; x++)
                    {
                        _canvas.DrawImage(background, x * 1575, y * 1575);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the grid tiles to the image
        /// </summary>
        private static void addGrid()
        {
            using (Image grid = Image.FromFile(_imageRoot + "Border.png"))
            {
                for (int y = 0; y <= _board.size[1]; y++)
                {
                    for (int x = 0; x <= _board.size[0]; x++)
                    {
                        _canvas.DrawImage(grid, x * 1575, y * 1575);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the pit tiles to the image
        /// </summary>
        private static void addPits()
        {
            using (Image pitImg = Image.FromFile(_imageRoot + "Pit.png"))
            {
                foreach (int[] pit in _board.pits)
                {
                    _canvas.DrawImage(pitImg, pit[0] * 1575, (_board.size[1] - pit[1]) * 1575);
                }
            }
        }

        /// <summary>
        /// Adds the wrench tiles to the image
        /// </summary>
        private static void addWrenches()
        {
            using (Image wrenchImg = Image.FromFile(_imageRoot + "Wrench.png"))
            {
                foreach (int[] wrench in _board.wrenches)
                {
                    _canvas.DrawImage(wrenchImg, wrench[0] * 1575, (_board.size[1] - wrench[1]) * 1575);
                }
            }
        }

        /// <summary>
        /// Adds the turntable tiles to the image
        /// </summary>
        private static void addTurntables()
        {
            Image CCW = Image.FromFile(_imageRoot + "CCWRotator.png");
            Image CW = Image.FromFile(_imageRoot + "CWRotator.png");
            foreach (Turntable rotator in _board.turntables)
            {
                if (rotator.dir == "right")
                {
                    _canvas.DrawImage(CW, rotator.location[0] * 1575, (_board.size[1] - rotator.location[1]) * 1575);
                }
                else
                {
                    _canvas.DrawImage(CCW, rotator.location[0] * 1575, (_board.size[1] - rotator.location[1]) * 1575);
                }
            }
            CCW.Dispose();
            CW.Dispose();
        }

        /// <summary>
        /// Adds corner tiles to the image
        /// </summary>
        private static void addCorners()
        {
            foreach (int[] corner in _corners)
            {
                using (Image cornerImg = Image.FromFile(_imageRoot + "Corner.png"))
                {
                    // Rotate the corner if needed
                    switch ((Robot.orientation)corner[2])
                    {
                        case Robot.orientation.Y:
                            cornerImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        case Robot.orientation.NEG_X:
                            cornerImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case Robot.orientation.NEG_Y:
                            cornerImg.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                    }
                    _canvas.DrawImage(cornerImg, corner[0] * 1575, (_board.size[1] - corner[1]) * 1575);
                }
            }
        }


        /// <summary>
        /// Adds the wall tiles to the image
        /// </summary>
        private static void addWalls()
        {
            foreach (int[][] wall in _board.walls)
            {
                using (Image wallImg = Image.FromFile(_imageRoot + "Wall.png"))
                {
                    // Check if the walls are facing along the Y axis or not
                    if (wall[0][0] == wall[1][0])
                    {
                        // Check if wall is facing in the positive Y direction or not
                        if (wall[0][1] < wall[1][1])
                        {
                            // Check if this wall should be a laser or corner instead
                            if (_board.lasers.Any(l => l.facing == Robot.orientation.NEG_Y && l.start[0] == wall[0][0] && l.start[1] == wall[0][1]))
                            {
                                continue;
                            }
                            else if (_corners.Any(c => c[0] == wall[0][0] && c[1] == wall[0][1] && (c[2] == 1 || c[2] == 2)))
                            {
                                continue;
                            }
                            wallImg.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        }
                        else
                        {
                            // Check if this wall should be a laser or corner instead
                            if (_board.lasers.Any(l => l.facing == Robot.orientation.Y && l.start[0] == wall[0][0] && l.start[1] == wall[0][1]))
                            {
                                continue;
                            }
                            else if (_corners.Any(c => c[0] == wall[0][0] && c[1] == wall[0][1] && (c[2] == 0 || c[2] == 3)))
                            {
                                continue;
                            }
                            wallImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        }
                    }
                    else
                    {
                        // Check if wall is facing in the positive X direction or not
                        if (wall[0][0] < wall[1][0])
                        {
                            // Check if this wall should be a laser or corner instead
                            if (_board.lasers.Any(l => l.facing == Robot.orientation.NEG_X && l.start[0] == wall[0][0] && l.start[1] == wall[0][1]))
                            {
                                continue;
                            }
                            else if (_corners.Any(c => c[0] == wall[0][0] && c[1] == wall[0][1] && (c[2] == 0 || c[2] == 1)))
                            {
                                continue;
                            }
                            wallImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        }
                        else
                        {
                            // Check if this wall should be a laser or corner instead
                            if (_board.lasers.Any(l => l.facing == Robot.orientation.X && l.start[0] == wall[0][0] && l.start[1] == wall[0][1]))
                            {
                                continue;
                            }
                            else if (_corners.Any(c => c[0] == wall[0][0] && c[1] == wall[0][1] && (c[2] == 2 || c[2] == 3)))
                            {
                                continue;
                            }
                        }
                    }
                    _canvas.DrawImage(wallImg, wall[0][0] * 1575, (_board.size[1] - wall[0][1]) * 1575);
                }
            }
        }

        /// <summary>
        /// Adds the laser tiles to the image
        /// </summary>
        private static void addLasers()
        {
            foreach (Laser laser in _board.lasers)
            {
                Image laserImg = null;
                // Get appropriate laser image
                switch (laser.strength)
                {
                    case 1:
                        laserImg = Image.FromFile(_imageRoot + "Laser-1.png");
                        break;
                    case 2:
                        laserImg = Image.FromFile(_imageRoot + "Laser-2.png");
                        break;
                    case 3:
                        laserImg = Image.FromFile(_imageRoot + "Laser-3.png");
                        break;
                }
                // Rotate laser as needed
                switch (laser.facing)
                {
                    case Robot.orientation.NEG_X:
                        laserImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case Robot.orientation.Y:
                        laserImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    case Robot.orientation.NEG_Y:
                        laserImg.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                }
                _canvas.DrawImage(laserImg, laser.start[0] * 1575, (_board.size[1] - laser.start[1]) * 1575);
                laserImg.Dispose();
            }
        }

        /// <summary>
        /// Adds the laser beam tiles to the image
        /// </summary>
        private static void addBeams()
        {
            foreach (Laser laser in _board.lasers)
            {
                using (Image border = Image.FromFile(_imageRoot + "Border-End.png"))
                {
                    Image beam = null;
                    // Get appropriate beam image
                    switch (laser.strength)
                    {
                        case 1:
                            beam = Image.FromFile(_imageRoot + "Beam-1.png");
                            break;
                        case 2:
                            beam = Image.FromFile(_imageRoot + "Beam-2.png");
                            break;
                        case 3:
                            beam = Image.FromFile(_imageRoot + "Beam-3.png");
                            break;
                    }

                    // Set the starting and ending coordinates of the beams and rotate the beams if necessary
                    int start = laser.start[0];
                    int end = laser.end[0];
                    switch (laser.facing)
                    {
                        case Robot.orientation.NEG_X:
                            start = laser.end[0];
                            end = laser.start[0];
                            break;
                        case Robot.orientation.Y:
                            start = laser.start[1];
                            end = laser.end[1];
                            beam.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            border.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        case Robot.orientation.NEG_Y:
                            start = laser.end[1];
                            end = laser.start[1];
                            beam.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            border.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                    }
                    bool first = true;
                    int x, y;
                    for (; start <= end; start++)
                    {
                        // Check if the beams are moving along the X axis or not
                        if (laser.facing == Robot.orientation.X || laser.facing == Robot.orientation.NEG_X)
                        {
                            x = start * 1575;
                            y = (_board.size[1] - laser.start[1]) * 1575;
                        }
                        else
                        {
                            x = laser.start[0] * 1575;
                            y = (_board.size[1] - start) * 1575;
                        }
                        _canvas.DrawImage(beam, x, y);
                        // Add borders to cover ends of laser beams
                        if (first)
                        {
                            first = false;
                            _canvas.DrawImage(border, x, y);
                        }
                        if (start == end)
                        {
                            border.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            _canvas.DrawImage(border, x, y);
                        }
                    }
                    beam.Dispose();
                }
            }
        }

        /// <summary>
        /// Adds the conveyor tiles to the image
        /// </summary>
        /// <param name="express">Add express conveyors</param>
        private static void addConveyors(bool express)
        {
            Conveyor[] conveyors;
            if (express)
            {
                conveyors = _board.expressConveyors;
            }
            else
            {
                conveyors = _board.conveyors;
            }
            foreach (Conveyor conveyor in conveyors)
            {
                Image conveyorImg;
                int difference = conveyor.entrance - conveyor.exit;
                // Get the appropriate conveyor image (linear, curve, or curve S)
                if (express)
                {
                    if (Math.Abs(difference) == 2)
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "ExpressConveyor.png");
                    }
                    else if (difference == -1 || difference == 3)
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "ExpressConveyorCurve.png");
                    }
                    else
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "ExpressConveyorCurveS.png");
                    }
                }
                else
                {
                    if (Math.Abs(difference) == 2)
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "Conveyor.png");
                    }
                    else if (difference == -1 || difference == 3)
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "ConveyorCurve.png");
                    }
                    else
                    {
                        conveyorImg = Image.FromFile(_imageRoot + "ConveyorCurveS.png");
                    }
                }
                // Rotate the image as necessary
                switch (conveyor.entrance)
                {
                    case Robot.orientation.X:
                        conveyorImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    case Robot.orientation.Y:
                        conveyorImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case Robot.orientation.NEG_X:
                        conveyorImg.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                }
                _canvas.DrawImage(conveyorImg, conveyor.location[0] * 1575, (_board.size[1] - conveyor.location[1]) * 1575);
                conveyorImg.Dispose();
            }
        }

    }
}
