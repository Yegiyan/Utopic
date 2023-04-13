using static Raylib_cs.Raylib;
using Raylib_cs;

namespace Utopic.src
{
    class Environment
    {
        public static Rectangle playArea;

        public static List<Rectangle> p1_island_cols = new();
        public static List<Rectangle> p2_island_cols = new();

        public static List<Rectangle> boundary_cols = new();
        public static List<Rectangle> env_island_cols = new();

        public static List<Rectangle> env_boundary_cols = new();

        public static List<Rectangle> env_dock_cols = new();

        public Environment()
        {
            playArea = new(42, 70, 430, 792);

            boundary_cols.Add(new Rectangle(53, 68, 760, 2));  // top
            boundary_cols.Add(new Rectangle(50, 455, 760, 2)); // bot
            boundary_cols.Add(new Rectangle(40, 65, 2, 458));  // left
            boundary_cols.Add(new Rectangle(818, 65, 2, 458)); // right

            p1_island_cols.Add(new Rectangle(140, 139, 18, 40));  // top
            p1_island_cols.Add(new Rectangle(98, 170, 104, 110)); // top mid
            p1_island_cols.Add(new Rectangle(180, 250, 100, 73)); // mid left
            p1_island_cols.Add(new Rectangle(225, 285, 90, 80));  // mid right
            p1_island_cols.Add(new Rectangle(180, 370, 105, 40)); // bot left
            p1_island_cols.Add(new Rectangle(320, 333, 90, 35));  // bot right top
            p1_island_cols.Add(new Rectangle(340, 372, 183, 35)); // bot right

            p2_island_cols.Add(new Rectangle(410, 130, 60, 105)); // top left
            p2_island_cols.Add(new Rectangle(470, 165, 125, 40)); // top mid
            p2_island_cols.Add(new Rectangle(527, 128, 34, 38));  // top mid top
            p2_island_cols.Add(new Rectangle(650, 128, 34, 43));  // top right
            p2_island_cols.Add(new Rectangle(570, 170, 115, 75)); // mid top
            p2_island_cols.Add(new Rectangle(605, 210, 110, 70)); // mid
            p2_island_cols.Add(new Rectangle(715, 250, 40, 30));  // mid right
            p2_island_cols.Add(new Rectangle(660, 275, 105, 90)); // bot
            p2_island_cols.Add(new Rectangle(625, 333, 34, 36));  // bot left
            p2_island_cols.Add(new Rectangle(605, 347, 25, 25));  // bot left left
            p2_island_cols.Add(new Rectangle(698, 365, 25, 30));  // bot bot

            // Island 1 (Left)
            env_island_cols.Add(new Rectangle(130, 125, 43, 39));  // top
            env_island_cols.Add(new Rectangle(90, 163, 130, 125)); // top mid
            env_island_cols.Add(new Rectangle(170, 245, 120, 85)); // mid left
            env_island_cols.Add(new Rectangle(215, 285, 120, 84)); // mid right
            env_island_cols.Add(new Rectangle(172, 370, 115, 40)); // bot left
            env_island_cols.Add(new Rectangle(325, 328, 92, 35));  // bot right top
            env_island_cols.Add(new Rectangle(337, 369, 185, 41)); // bot right

            // Island 2 (Right)
            env_island_cols.Add(new Rectangle(400, 124, 80, 125)); // top left
            env_island_cols.Add(new Rectangle(470, 165, 175, 45)); // top mid
            env_island_cols.Add(new Rectangle(524, 128, 42, 38));  // top mid top
            env_island_cols.Add(new Rectangle(647, 128, 42, 43));  // top right
            env_island_cols.Add(new Rectangle(563, 170, 126, 79)); // mid top
            env_island_cols.Add(new Rectangle(605, 209, 122, 81)); // mid
            env_island_cols.Add(new Rectangle(715, 249, 56, 90));  // mid right
            env_island_cols.Add(new Rectangle(650, 275, 105, 97)); // bot
            env_island_cols.Add(new Rectangle(625, 333, 34, 39));  // bot left
            env_island_cols.Add(new Rectangle(605, 347, 25, 25));  // bot left left
            env_island_cols.Add(new Rectangle(696, 365, 27, 30));  // bot bot

            env_boundary_cols.Add(new Rectangle(0, 20, 860, 2));  // top
            env_boundary_cols.Add(new Rectangle(0, 495, 860, 2)); // bot
            env_boundary_cols.Add(new Rectangle(0, 0, 2, 495));   // left
            env_boundary_cols.Add(new Rectangle(860, 0, 2, 495)); // right

            env_dock_cols.Add(new Rectangle(120, 290, 48, 48));
            env_dock_cols.Add(new Rectangle(695, 155, 48, 48));
        }

        public static void DrawCollisionBoxes()
        {
            for (int i = 0; i < p1_island_cols.Count; i++)
                DrawRectangleLines((int)p1_island_cols.ElementAt(i).x, (int)p1_island_cols.ElementAt(i).y, (int)p1_island_cols.ElementAt(i).width, (int)p1_island_cols.ElementAt(i).height, Color.BLACK);

            for (int i = 0; i < p2_island_cols.Count; i++)
                DrawRectangleLines((int)p2_island_cols.ElementAt(i).x, (int)p2_island_cols.ElementAt(i).y, (int)p2_island_cols.ElementAt(i).width, (int)p2_island_cols.ElementAt(i).height, Color.BLACK);

            for (int i = 0; i < env_dock_cols.Count; i++)
                DrawRectangleLines((int)env_dock_cols.ElementAt(i).x, (int)env_dock_cols.ElementAt(i).y, (int)env_dock_cols.ElementAt(i).width, (int)env_dock_cols.ElementAt(i).height, Color.BLACK);
        }
    }
}
