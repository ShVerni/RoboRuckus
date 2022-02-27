using System;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace RoboRuckus.RuckusCode
{
    public class boardImageMaker : IDisposable
    {
        private  SKBitmap _boardImage;
        private  SKCanvas _canvas;
        private  char _separator = Path.DirectorySeparatorChar;
        private  string _root;
        private  string _imageRoot;
        private  Board _board;
        private  int[][] _corners;
      
        /// <summary>
        /// Constructs a board image maker.
        /// </summary>
        /// <param name="board">The board file to base the image off of</param>
        /// <param name="corners">Additional info for corner walls</param>
        public boardImageMaker(Board board, int[][] corners)
        {
            // Initialize values
            _root = serviceHelpers.rootPath + _separator;
            _imageRoot = _root + "Resources" + _separator + "BoardMakerFiles" + _separator;
            _board = board;
            _corners = corners;
            // Create a new image of the correct size
            _boardImage = new SKBitmap(1575 * (board.size[0] + 1), 1575 * (board.size[1] + 1));
            // Create a graphics canvas to draw on
            _canvas = new SKCanvas(_boardImage);

        }

        /// <summary>
        /// Creates a print-ready board image
        /// </summary>
        /// <param name="board">The board object to make the image from</param>
        /// <param name="corners">The locations of the corner walls</param>
        /// <returns>True on success</returns>
        public bool createImage()
        {            
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
            using (SKImage dot = SKImage.FromEncodedData(_imageRoot + "Dot.png"))
            {
                _canvas.DrawImage(dot, 0, _board.size[1] * 1575);
            }

            // Save the canvas
            _canvas.Flush();
            // Dispose of the canvas since the image is saved
            _canvas.Dispose();

            // Set the file names
            string filename = _board.name.Replace(" ", "") + ".png";
            string printableDirectory = _root + "wwwroot" + _separator + "images" + _separator + "printable_boards";
            string smallFileDirectory = _root + "wwwroot" + _separator + "images" + _separator + "boards";

            //Save the full size image 
            saveFile(_boardImage, printableDirectory, filename);

            // Create a new, smaller image for the web server to use
            SKImageInfo newsize = new SKImageInfo(100 * (_board.size[0] + 1), 100 * (_board.size[1] + 1));
            _boardImage = _boardImage.Resize(newsize, SKFilterQuality.Medium);
            // Save smaller image
            saveFile(_boardImage, smallFileDirectory, filename);
            return true;
        }

        /// <summary>
        /// Saves a bitmap to a file
        /// </summary>
        /// <param name="bitmap">The bitmap to save</param>
        /// <param name="folder">The folder to save in</param>
        /// <param name="filename">The filename to save</param>
        private  void saveFile(SKBitmap bitmap, string folder, string filename)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                using (FileStream fs = new FileStream(folder + _separator + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    // Discard current file content if any
                    fs.SetLength(0);
                    SKData data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 25);
                    data.SaveTo(fs);
                    data.Dispose();
                }
            }
        }

        /// <summary>
        /// Adds the background tiles to the image
        /// </summary>
        private  void addBackground()
        {
            using (SKImage image = SKImage.FromEncodedData(_imageRoot + "Background.png"))
            {         
                for (int y = 0; y <= _board.size[1]; y++)
                {
                    for (int x = 0; x <= _board.size[0]; x++)
                    {
                        _canvas.DrawImage(image,x * 1575, y * 1575);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the grid tiles to the image
        /// </summary>
        private  void addGrid()
        {
            using (SKImage image = SKImage.FromEncodedData(_imageRoot + "Border.png"))
            {
                for (int y = 0; y <= _board.size[1]; y++)
                {
                    for (int x = 0; x <= _board.size[0]; x++)
                    {
                        _canvas.DrawImage(image, x * 1575, y * 1575);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the pit tiles to the image
        /// </summary>
        private  void addPits()
        {
            using (SKImage image = SKImage.FromEncodedData(_imageRoot + "Pit.png"))
            {
                foreach (int[] pit in _board.pits)
                {
                    _canvas.DrawImage(image, pit[0] * 1575, (_board.size[1] - pit[1]) * 1575);
                }
            }
        }

        /// <summary>
        /// Adds the wrench tiles to the image
        /// </summary>
        private  void addWrenches()
        {
            using (SKImage image = SKImage.FromEncodedData(_imageRoot + "Wrench.png"))
            {
                foreach (int[] wrench in _board.wrenches)
                {
                    _canvas.DrawImage(image, wrench[0] * 1575, (_board.size[1] - wrench[1]) * 1575);
                }
            }
        }

        /// <summary>
        /// Adds the turntable tiles to the image
        /// </summary>
        private  void addTurntables()
        {
            SKImage CCW = SKImage.FromEncodedData(_imageRoot + "CCWRotator.png");
            SKImage CW = SKImage.FromEncodedData(_imageRoot + "CWRotator.png");
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
        private  void addCorners()
        {
            foreach (int[] corner in _corners)
            {
                SKBitmap image = SKBitmap.Decode(_imageRoot + "Corner.png");
                // Rotate the corner if needed
                switch ((Robot.orientation)corner[2])
                {
                    case Robot.orientation.Y:
                        image = rotate(270, image);
                        break;
                    case Robot.orientation.NEG_X:
                        image = rotate(180, image);
                        break;
                    case Robot.orientation.NEG_Y:
                        image = rotate(90, image);
                        break;
                }
                _canvas.DrawBitmap(image, corner[0] * 1575, (_board.size[1] - corner[1]) * 1575);
                image.Dispose();
            }
        }

        /// <summary>
        /// Adds the wall tiles to the image
        /// </summary>
        private  void addWalls()
        {
            foreach (int[][] wall in _board.walls)
            {
                SKBitmap image = SKBitmap.Decode(_imageRoot + "Wall.png");

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
                        image = rotate(90, image);
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
                        image = rotate(270, image);
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
                        image = rotate(180, image); ;
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
                _canvas.DrawBitmap(image, wall[0][0] * 1575, (_board.size[1] - wall[0][1]) * 1575);
                image.Dispose();
            }
        }

        /// <summary>
        /// Adds the laser tiles to the image
        /// </summary>
        private  void addLasers()
        {
            foreach (Laser laser in _board.lasers)
            {
                SKBitmap laserImg = null;
                // Get appropriate laser image
                switch (laser.strength)
                {
                    case 1:
                        laserImg = SKBitmap.Decode(_imageRoot + "Laser-1.png");
                        break;
                    case 2:
                        laserImg = SKBitmap.Decode(_imageRoot + "Laser-2.png");
                        break;
                    case 3:
                        laserImg = SKBitmap.Decode(_imageRoot + "Laser-3.png");
                        break;
                }
                // Rotate laser as needed
                switch (laser.facing)
                {
                    case Robot.orientation.Y:
                        laserImg = rotate(270, laserImg);
                        break;
                    case Robot.orientation.NEG_X:
                        laserImg = rotate(180, laserImg);
                        break;
                    case Robot.orientation.NEG_Y:
                        laserImg = rotate(90, laserImg);
                        break;
                }
                _canvas.DrawBitmap(laserImg, laser.start[0] * 1575, (_board.size[1] - laser.start[1]) * 1575);
                laserImg.Dispose();
            }
        }

        /// <summary>
        /// Adds the laser beam tiles to the image
        /// </summary>
        private  void addBeams()
        {
            foreach (Laser laser in _board.lasers)
            {
                SKBitmap border = SKBitmap.Decode(_imageRoot + "Border-End.png");
                SKBitmap beam = null;
                // Get appropriate beam image
                switch (laser.strength)
                {
                    case 1:
                        beam = SKBitmap.Decode(_imageRoot + "Beam-1.png");
                        break;
                    case 2:
                        beam = SKBitmap.Decode(_imageRoot + "Beam-2.png");
                        break;
                    case 3:
                        beam = SKBitmap.Decode(_imageRoot + "Beam-3.png");
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
                        beam = rotate(90, beam);
                        border = rotate(270, border);
                        break;
                    case Robot.orientation.NEG_Y:
                        start = laser.end[1];
                        end = laser.start[1];
                        beam = rotate(90, beam);
                        border = rotate(270, border);
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
                    _canvas.DrawBitmap(beam, x, y);
                    // Add borders to cover ends of laser beams
                    if (first)
                    {
                        first = false;
                        _canvas.DrawBitmap(border, x, y);
                    }
                    if (start == end)
                    {
                        border = rotate(180, border);
                        _canvas.DrawBitmap(border, x, y);
                    }
                }
                border.Dispose();
                beam.Dispose();
            }
        }

        /// <summary>
        /// Adds the conveyor tiles to the image
        /// </summary>
        /// <param name="express">Add express conveyors</param>
        private  void addConveyors(bool express)
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
                SKBitmap conveyorImg;
                int difference = conveyor.entrance - conveyor.exit;
                // Get the appropriate conveyor image (linear, curve, or curve S)
                if (express)
                {
                    if (Math.Abs(difference) == 2)
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "ExpressConveyor.png");
                    }
                    else if (difference == -1 || difference == 3)
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "ExpressConveyorCurve.png");
                    }
                    else
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "ExpressConveyorCurveS.png");
                    }
                }
                else
                {
                    if (Math.Abs(difference) == 2)
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "Conveyor.png");
                    }
                    else if (difference == -1 || difference == 3)
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "ConveyorCurve.png");
                    }
                    else
                    {
                        conveyorImg = SKBitmap.Decode(_imageRoot + "ConveyorCurveS.png");
                    }
                }

                // Rotate the image as necessary
                switch (conveyor.entrance)
                {
                    case Robot.orientation.X:
                        conveyorImg = rotate(270, conveyorImg);
                        break;
                    case Robot.orientation.Y:
                        conveyorImg = rotate(180, conveyorImg);
                        break;
                    case Robot.orientation.NEG_X:
                        conveyorImg = rotate(90, conveyorImg);
                        break;
                }
                _canvas.DrawBitmap(conveyorImg, conveyor.location[0] * 1575, (_board.size[1] - conveyor.location[1]) * 1575);
                conveyorImg.Dispose();
            }
        }

        /// <summary>
        /// Rotates a square bitmap
        /// </summary>
        /// <param name="degrees">The degree to rotate</param>
        /// <param name="orignal">The original image to rotate</param>
        /// <returns>The rotated bitmap</returns>
        private  SKBitmap rotate(float degrees, SKBitmap orignal)
        {
            SKBitmap rotated = new SKBitmap(orignal.Width, orignal.Height);
            using (var surface = new SKCanvas(rotated))
            {
                // Rotate canvas around image center
                surface.RotateDegrees(degrees, orignal.Width / 2, orignal.Height / 2);
                surface.DrawBitmap(orignal, 0, 0);
                surface.Flush();
            }
            return rotated;
        }

        /// <summary>
        /// Dispose of board image maker and all resources
        /// </summary>
        public void Dispose()
        {
            _boardImage?.Dispose();
            _canvas?.Dispose(); 
            _board = null;

            /*
             * Triggering the GC here is probably
             * unnecessary but so much memory is 
             * used that freeing it up quickly seems
             * like it could be helpful.
             */
            GC.Collect();
        }
        ~boardImageMaker()
        {
            Dispose();
        }

    }
}
