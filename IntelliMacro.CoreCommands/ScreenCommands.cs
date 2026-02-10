using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    class GetColorCommand : AbstractCommand
    {
        internal GetColorCommand() : base("GetColor", true, "Get &color", "&Screen") { }

        public override string Description
        {
            get
            {
                return "Determine the color of a pixel or image.\n\n" +
                    "The source object can be a screen number or an image object (default is the primary screen).\n" +
                    "Colors are returned as a list [red, green, blue]\n" +
                    "If width and/or height is given, the resulting object is an image if histogram mode is absent or zero.\n" +
                    "Histogram mode 1 will result in a list of all included colors, with a fourth component giving the count of each one.\n";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "X coordinate"),
                    new ParameterDescription(false, "Y coordinate"),
                    new ParameterDescription(true, "Source screen/image"),
                    new ParameterDescription(true, "Width"),
                    new ParameterDescription(true, "Height"),
                    new ParameterDescription(true, "Histogram Mode")
                };
            }
        }
        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int x = (int)parameters[0].Number, y = (int)parameters[1].Number;
            int width = parameters[3] == null ? 1 : (int)parameters[3].Number;
            int height = parameters[4] == null ? 1 : (int)parameters[4].Number;
            Bitmap bmp = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bmp);
            if (MacroWrappedObject.Unwrap(parameters[2]) is Image)
            {
                Image img = (Image)((MacroWrappedObject)parameters[2]).Wrapped;
                g.DrawImage(img, -x, -y);
            }
            else
            {
                int screen = parameters[2] == null ? 0 : (int)parameters[2].Number;
                if (screen != 0)
                    MouseCommand.ConvertCoordinatesForScreen(screen, ref x, ref y);
                g.CopyFromScreen(x, y, 0, 0, bmp.Size);
            }
            g.Dispose();
            if (width == 1 && height == 1)
            {
                Color c = bmp.GetPixel(0, 0);
                return new MacroList(new MacroObject[] { c.R & 0xff, c.G & 0xff, c.B & 0xff });
            }
            else if (parameters[5] != null && parameters[5].Number == 1)
            {
                Dictionary<Color, int> colorCounter = new Dictionary<Color, int>();
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Color c = bmp.GetPixel(i, j);
                        if (!colorCounter.ContainsKey(c))
                            colorCounter.Add(c, 0);
                        colorCounter[c]++;
                    }
                }
                List<MacroObject> lst = new List<MacroObject>();
                while (colorCounter.Count > 0)
                {
                    int bestCount = 0;
                    Color c = default(Color);
                    foreach (KeyValuePair<Color, int> pair in colorCounter)
                    {
                        if (pair.Value > bestCount)
                        {
                            c = pair.Key;
                            bestCount = pair.Value;
                        }
                        else if (pair.Value == bestCount &&
                            (pair.Key.R < c.R ||
                            (pair.Key.R == c.R && pair.Key.G < c.G) ||
                            (pair.Key.R == c.R && pair.Key.G == c.G && pair.Key.B < c.B)))
                        {
                            c = pair.Key;
                        }
                    }
                    lst.Add(new MacroList(new MacroObject[] { c.R & 0xff, c.G & 0xff, c.B & 0xff, bestCount })); colorCounter.Remove(c);
                }
                return new MacroList(lst);
            }
            else
            {
                return new MacroWrappedObject(bmp);
            }
        }
    }

    class ScreenSizeCommand : AbstractCommand
    {
        internal ScreenSizeCommand() : base("ScreenSize", true, "Get &screen size", "&Screen") { }


        public override string Description
        {
            get
            {
                return "Determine the size of a screen or image.\n\n" +
                    "The returned value is a list [width, height, x, y, workingAreaWidth, workingAreaHeight, workingAreaX, workingAreaY] for screens, " +
                    "or a list [width, height] for images.\n" +
                    "screen can be an image or a screen number.\n" +
                    "Screen number 0 will return a list of screen sizes of all screens.\n" +
                    "Screen number -1 will return the size of the bounding box of all screens.\n";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Screen/Image")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (MacroWrappedObject.Unwrap(parameters[0]) is Image)
            {
                Image img = (Image)((MacroWrappedObject)parameters[0]).Wrapped;
                return new MacroList(new MacroObject[] { img.Width, img.Height });
            }
            int screen = (int)parameters[0].Number;
            if (screen == -1)
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                foreach (Screen s in Screen.AllScreens)
                {
                    bounds = Rectangle.Union(bounds, s.Bounds);
                    workingArea = Rectangle.Union(workingArea, s.WorkingArea);
                }
                return GetScreenSize(bounds, workingArea);
            }
            else if (screen == 0)
            {
                Screen[] all = Screen.AllScreens;
                MacroObject[] screens = new MacroObject[all.Length];
                for (int i = 0; i < screens.Length; i++)
                {
                    screens[i] = GetScreenSize(all[i]);
                }
                return new MacroList(screens);
            }
            else if (screen > 0 && screen <= Screen.AllScreens.Length)
            {
                return GetScreenSize(Screen.AllScreens[screen - 1]);
            }
            else
            {
                return MacroObject.EMPTY;
            }
        }

        private MacroList GetScreenSize(Screen screen)
        {
            return GetScreenSize(screen.Bounds, screen.WorkingArea);
        }

        private MacroList GetScreenSize(Rectangle bounds, Rectangle workingArea)
        {
            return new MacroList(new MacroObject[] { 
                bounds.Width, bounds.Height, 
                bounds.Left, bounds.Top, 
                workingArea.Width, workingArea.Height, 
                workingArea.X, workingArea.Y
            });
        }
    }
}
