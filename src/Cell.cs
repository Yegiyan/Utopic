using static Raylib_cs.Raylib;
using Raylib_cs;

namespace Utopic.src
{
    public class Cell
    {
        public int I { get; set; }
        public int J { get; set; }

        public int F { get; set; }
        public int G { get; set; }
        public int H { get; set; }

        public List<Cell> Neighbors = new();
        public Cell Previous { get; set; }

        public bool Wall { get; set; }

        Random rand = new Random();

        public Cell(int i, int j)
        {
            I = i;
            J = j;

            F = 0;
            G = 0;
            H = 0;

            Previous = null;
            Wall = false;
        }

        public void AddNeighbors(Cell[,] grid)
        {
            int i = I;
            int j = J;

            if (i < Boat.cols - 1)
                Neighbors.Add(grid[i + 1, j]);

            if (i > 0)
                Neighbors.Add(grid[i - 1, j]);

            if (j < Boat.rows - 1)
                Neighbors.Add(grid[i, j + 1]);

            if (j > 0)
                Neighbors.Add(grid[i, j - 1]);

            // Diagonals
            /*
            if (i < Boat.cols - 1 && j < Boat.rows - 1)
                Neighbors.Add(grid[i + 1, j + 1]);

            if (i < Boat.cols - 1 && j > 0)
                Neighbors.Add(grid[i + 1, j - 1]);

            if (i > 0 && j < Boat.rows - 1)
                Neighbors.Add(grid[i - 1, j + 1]);

            if (i > 0 && j > 0)
                Neighbors.Add(grid[i - 1, j - 1]);
            */
        }

        public void DrawLine(Color color)
        {
            DrawRectangleLines(I * Boat.width, J * Boat.height, Boat.width, Boat.height, color);
        }

        public void DrawRec(Color color)
        {
            DrawRectangle(I * Boat.width, J * Boat.height, Boat.width, Boat.height, color);
        }
    }
}
