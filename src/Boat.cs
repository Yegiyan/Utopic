using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class Boat
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public Rectangle Collider { get; set; }
        public Player Owner { get; set; }
        public bool IsDockOccupied { get; set; }
        public bool FlipTexture { get; set; }
        public bool IsProtected { get; set; }
        public int SunkID { get; set; }

        const int BOAT_SPEED = 60;

        public Vector2 position;

        Random rand;
        Environment env;

        // AI Boat Variables
        const int CELL_SIZE = 23;

        Rectangle play_area;

        public static int cols = 37;
        public static int rows = 27;

        public static int width = GetScreenWidth() / cols;
        public static int height = GetScreenHeight() / rows;

        Cell[,]? grid;

        List<Cell> open_set = new();
        List<Cell> closed_set = new();
        List<Cell> path = new();

        List<Vector2> world_path = new();

        Cell? start;
        Cell? end;

        int path_index;
        bool pathFound;
        bool pathCalculated;
        bool noPath;

        bool isFishing;

        float fleeTimer;
        float fishTimer;
        float pathTimer;

        public Boat(Player owner, string type, int id, Vector2 position, Rectangle collider)
        {
            Owner = owner;
            Type = type;
            ID = id;
            this.position = position;
            Collider = collider;

            FlipTexture = false;

            rand = new Random();
            env = new Environment();

            play_area = new Rectangle(30, 70, 760, 370);

            path_index = 0;
            pathFound = false;
            pathCalculated = false;
            noPath = false;
            pathTimer = 0;

            isFishing = false;

            fleeTimer = 0;
            fishTimer = 0;

            grid = new Cell[cols, rows];

            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    grid[i, j] = new Cell(i, j);

            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    grid[i, j].AddNeighbors(grid);

            Boundaries();

            start = CoordsToCell(this.position);
            end = GetRandomCell();

            open_set.Add(start);
        }

        public void Buy(Player owner, string type)
        {
            switch (type)
            {
                case "FISHING_BOAT":
                    owner.Gold -= 25;
                    break;
                case "PT_BOAT":
                    owner.Gold -= 40;
                    break;
                default:
                    Debug.WriteLine("Invalid input! Didn't buy boat!");
                    break;
            }
        }

        public void Update(Player owner)
        {
            //DrawCells();
            //DrawGrid();

            foreach (Boat boat in owner.Opponent.boats)
                if (CheckCollisionRecs(boat.Collider, Collider) && boat.Type == "PT_BOAT")
                    SunkID = ID;

            if (Type == "FISHING_BOAT")
            {
                if (CheckPTBoatCollision(Collider, owner.boats, owner.Opponent.boats) && !IsProtected)
                {
                    owner.DestroyShip = true;
                    owner.DestroyShipID = SunkID;
                }
            }
        }

        public void Control(Player owner, float deltaTime)
        {
            Dictionary<KeyboardKey, Vector2> boat_keys;

            float adjusted_boat_speed = BOAT_SPEED * deltaTime;

            if (owner.isAI && owner.toggleBoat)
            {
                AIControl(owner, deltaTime);
                return;
            }

            if (owner.player == 1)
            {
                boat_keys = new()
                {
                    { KeyboardKey.KEY_W, new Vector2(0, -adjusted_boat_speed) },
                    { KeyboardKey.KEY_S, new Vector2(0, adjusted_boat_speed) },
                    { KeyboardKey.KEY_A, new Vector2(-adjusted_boat_speed, 0) },
                    { KeyboardKey.KEY_D, new Vector2(adjusted_boat_speed, 0) }
                };
            }

            else
            {
                boat_keys = new()
                {
                    { KeyboardKey.KEY_UP, new Vector2(0, -adjusted_boat_speed) },
                    { KeyboardKey.KEY_DOWN, new Vector2(0, adjusted_boat_speed) },
                    { KeyboardKey.KEY_LEFT, new Vector2(-adjusted_boat_speed, 0) },
                    { KeyboardKey.KEY_RIGHT, new Vector2(adjusted_boat_speed, 0) }
                };
            }

            foreach (KeyValuePair<KeyboardKey, Vector2> entry in boat_keys)
            {
                if (IsKeyDown(entry.Key))
                {
                    if (entry.Key == KeyboardKey.KEY_A || entry.Key == KeyboardKey.KEY_LEFT)
                        FlipTexture = true;
                    else if ((entry.Key == KeyboardKey.KEY_W || entry.Key == KeyboardKey.KEY_UP || entry.Key == KeyboardKey.KEY_S || entry.Key == KeyboardKey.KEY_DOWN) && FlipTexture)
                        FlipTexture = true;
                    else
                        FlipTexture = false;

                    Vector2 new_pos = position + entry.Value;
                    Rectangle new_boat_col = new(new_pos.X, new_pos.Y, Collider.width, Collider.height);

                    if (!CheckBoatCollision(new_boat_col, owner.Opponent.boats, owner.Opponent.buildings))
                        position = new_pos;
                }
            }

            Collider = new Rectangle(position.X, position.Y, 23, 23);
        }

        void AIControl(Player owner, float deltaTime)
        {
            if (owner.isAI)
            {
                CalculateThreats(owner);

                Mob closestFish = GetClosestFish(position);
                Boat closestFishingBoat = GetClosestFishingBoat(owner, position);
                Mob closestPirate = GetClosestPirate(position);
                Boat closestPTBoat = GetClosestPTBoat(owner, position);

                float pirateDetectionRange = 50f;
                bool isPirateNearby = closestPirate != null && Vector2.Distance(position, closestPirate.Position) < pirateDetectionRange;

                float ptboatDetectionRange = 50f;
                bool isPTBoatNearby = closestPTBoat != null && Vector2.Distance(position, closestPTBoat.position) < ptboatDetectionRange;

                float fishDetectionRange = 150f;
                bool isFishNearby = closestFish != null && Vector2.Distance(position, closestFish.Position) < fishDetectionRange;

                if (Type == "FISHING_BOAT")
                {
                    if (!pathFound)
                        Pathfind();

                    if (pathCalculated)
                        GoFishing(world_path, closestFish, deltaTime);

                    if (noPath)
                        ResetPathing(deltaTime);

                    fishTimer += deltaTime;
                    if (isFishNearby && fishTimer > 3 && !isFishing)
                    {
                        Escape();
                        fishTimer = 0;
                        isFishing = true;
                    }

                    if (!isFishNearby)
                        isFishing = false;

                    fleeTimer += deltaTime;
                    if ((isPirateNearby || isPTBoatNearby) && fleeTimer > 0.5f)
                    {
                        Escape();
                        fleeTimer = 0;
                    }
                }

                else if (Type == "PT_BOAT")
                {
                    if (!pathFound)
                        Pathfind();

                    if (pathCalculated)
                        GoHunting(world_path, closestFishingBoat, deltaTime);

                    pathTimer += deltaTime;
                    if ((pathTimer) > 12)
                    {
                        FindFishingBoat(closestFishingBoat);
                        pathTimer = 0;
                    }

                    if (noPath)
                        ResetPathing(deltaTime);
                }

                if (pathFound && !pathCalculated)
                {
                    foreach (Cell cell in path)
                    {
                        int worldX = cell.I * CELL_SIZE;
                        int worldY = cell.J * CELL_SIZE;
                        Vector2 worldCoord = new Vector2(worldX, worldY);
                        world_path.Add(worldCoord);
                    }
                    world_path.Reverse();
                    pathCalculated = true;
                }
            }
            Collider = new Rectangle(position.X, position.Y, 23, 23);
        }

        void Pathfind()
        {
            if (open_set.Count > 0)
            {
                int lowestIndex = 0;
                for (int i = 0; i < open_set.Count; i++)
                    if (open_set[i].F < open_set[lowestIndex].F)
                        lowestIndex = i;

                Cell current = open_set[lowestIndex];

                if (current == end)
                {
                    Cell temp = current;
                    path.Add(temp);
                    while (temp.Previous != null)
                    {
                        path.Add(temp.Previous);
                        temp = temp.Previous;
                    }
                    pathFound = true;
                }

                open_set.Remove(current);
                closed_set.Add(current);

                List<Cell> neighbors = current.Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Cell neighbor = neighbors[i];

                    if (!closed_set.Contains(neighbor) && !neighbor.Wall)
                    {
                        int tempG = current.G + 1;

                        bool newPath = false;
                        if (open_set.Contains(neighbor))
                        {
                            if (tempG < neighbor.G)
                            {
                                neighbor.G = tempG;
                                newPath = true;
                            }
                        }
                        else
                        {
                            neighbor.G = tempG;
                            newPath = true;
                            open_set.Add((neighbor));
                        }

                        if (newPath)
                        {
                            neighbor.H = Math.Abs(neighbor.I - end.I) + Math.Abs(neighbor.J - end.J);
                            neighbor.F = neighbor.G + neighbor.H;
                            neighbor.Previous = current;
                        }
                    }
                }
            }
            else
            {
                noPath = true;
                return;
            }
        }

        void GoFishing(List<Vector2> worldPath, Mob fish, float deltaTime)
        {
            if (path_index < worldPath.Count)
            {
                Vector2 target = worldPath[path_index];
                Vector2 direction = target - position;

                if (direction != Vector2.Zero)
                {
                    direction = Vector2.Normalize(direction);
                    position += direction * BOAT_SPEED * deltaTime;
                }

                if (Vector2.Distance(position, target) < BOAT_SPEED * deltaTime)
                    path_index++;

                if (Math.Abs(target.X - position.X) > 5.0f)
                {
                    if (target.X < position.X)
                        FlipTexture = true;
                    else
                        FlipTexture = false;
                }
            }
            else
                FindFish(fish);
        }

        void GoHunting(List<Vector2> worldPath, Boat boat, float deltaTime)
        {
            if (path_index < worldPath.Count)
            {
                Vector2 target = worldPath[path_index];
                Vector2 direction = target - position;

                if (direction != Vector2.Zero)
                {
                    direction = Vector2.Normalize(direction);
                    position += direction * BOAT_SPEED * deltaTime;
                }

                if (Vector2.Distance(position, target) < BOAT_SPEED * deltaTime)
                    path_index++;

                if (target.X < position.X)
                    FlipTexture = true;
                else
                    FlipTexture = false;
            }
            else
                FindFishingBoat(boat);
        }

        bool AreFishInPlayArea()
        {
            foreach (Mob fish in Game.fish_list)
                if (CheckCollisionRecs(play_area, fish.Collider))
                    return true;

            return false;
        }

        void Escape()
        {
            open_set.Clear();
            closed_set.Clear();
            path.Clear();
            world_path.Clear();

            path_index = 0;

            pathFound = false;
            pathCalculated = false;
            noPath = false;

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    grid[i, j].F = 0;
                    grid[i, j].G = 0;
                    grid[i, j].H = 0;
                    grid[i, j].Previous = null;
                }
            }

            start = CoordsToCell(position);
            end = GetRandomAdjacentCell(start);

            open_set.Add(start);
        }

        void ResetPathing(float deltaTime)
        {
            if (path_index < world_path.Count)
            {
                Vector2 target = world_path[path_index];
                Vector2 direction = target - position;

                if (direction != Vector2.Zero)
                {
                    direction = Vector2.Normalize(direction);
                    position += direction * BOAT_SPEED * deltaTime;
                }

                if (Vector2.Distance(position, target) < BOAT_SPEED * deltaTime)
                    path_index++;

                if (Math.Abs(target.X - position.X) > 5.0f)
                {
                    if (target.X < position.X)
                        FlipTexture = true;
                    else
                        FlipTexture = false;
                }
            }

            else
            {
                open_set.Clear();
                closed_set.Clear();
                path.Clear();
                world_path.Clear();

                path_index = 0;

                pathFound = false;
                pathCalculated = false;
                noPath = false;

                for (int i = 0; i < grid.GetLength(0); i++)
                {
                    for (int j = 0; j < grid.GetLength(1); j++)
                    {
                        grid[i, j].F = 0;
                        grid[i, j].G = 0;
                        grid[i, j].H = 0;
                        grid[i, j].Previous = null;
                    }
                }

                start = CoordsToCell(position);
                end = GetRandomCell();

                open_set.Add(start);
            }
        }

        void FindFish(Mob fish)
        {
            open_set.Clear();
            closed_set.Clear();
            path.Clear();
            world_path.Clear();

            path_index = 0;

            pathFound = false;
            pathCalculated = false;
            noPath = false;

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    grid[i, j].F = 0;
                    grid[i, j].G = 0;
                    grid[i, j].H = 0;
                    grid[i, j].Previous = null;
                }
            }

            start = CoordsToCell(position);

            Vector2 center_fish_pos = Vector2.Zero;
            if (fish != null)
            {
                center_fish_pos.X = fish.Position.X + (fish.Collider.width / 2);
                center_fish_pos.Y = fish.Position.Y + (fish.Collider.height / 2);
            }

            if (AreFishInPlayArea())
                end = CoordsToCell(center_fish_pos);
            else
                end = GetRandomCell();

            open_set.Add(start);
        }

        void FindFishingBoat(Boat boat)
        {
            open_set.Clear();
            closed_set.Clear();
            path.Clear();
            world_path.Clear();

            path_index = 0;

            pathFound = false;
            pathCalculated = false;
            noPath = false;

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    grid[i, j].F = 0;
                    grid[i, j].G = 0;
                    grid[i, j].H = 0;
                    grid[i, j].Previous = null;
                }
            }

            start = CoordsToCell(position);

            Vector2 center_boat_pos = Vector2.Zero;
            if (boat != null)
            {
                center_boat_pos.X = boat.position.X + (boat.Collider.width / 2);
                center_boat_pos.Y = boat.position.Y + (boat.Collider.height / 2);
            }

            if (boat != null)
                end = CoordsToCell(center_boat_pos);
            else
                end = GetRandomCell();

            open_set.Add(start);
        }

        Mob GetClosestFish(Vector2 position)
        {
            Mob closest_fish = null;
            float min_distance = float.MaxValue;

            foreach (Mob fish in Game.fish_list)
            {
                if (!CheckCollisionRecs(play_area, fish.Collider))
                    continue;

                float distance = Vector2.Distance(position, fish.Position);
                if (distance < min_distance)
                {
                    min_distance = distance;
                    closest_fish = fish;
                }
            }

            return closest_fish;
        }

        Boat GetClosestFishingBoat(Player owner, Vector2 position)
        {
            Boat closest_boat = null;
            float min_distance = float.MaxValue;
            foreach (Boat boat in owner.Opponent.boats)
            {
                if (boat.Type == "FISHING_BOAT")
                {
                    float distance = Vector2.Distance(position, boat.position);
                    if (distance < min_distance)
                    {
                        min_distance = distance;
                        closest_boat = boat;
                    }
                }
            }

            return closest_boat;
        }

        Mob GetClosestPirate(Vector2 position)
        {
            Mob closest_pirate = null;
            float min_distance = float.MaxValue;

            foreach (Mob pirate in Game.pirate_list)
            {
                float distance = Vector2.Distance(position, pirate.Position);
                if (distance < min_distance)
                {
                    min_distance = distance;
                    closest_pirate = pirate;
                }
            }

            return closest_pirate;
        }

        Boat GetClosestPTBoat(Player owner, Vector2 position)
        {
            Boat closest_PT = null;
            float min_distance = float.MaxValue;

            foreach (Boat boat in Owner.Opponent.boats)
            {
                if (boat.Type == "PT_BOAT")
                {
                    float distance = Vector2.Distance(position, boat.position);
                    if (distance < min_distance)
                    {
                        min_distance = distance;
                        closest_PT = boat;
                    }
                }
            }

            return closest_PT;
        }

        Cell GetRandomCell()
        {
            Cell cell;

            while (true)
            {
                int i = rand.Next(2, 36);
                int j = rand.Next(3, 20);

                cell = grid[i, j];

                if (!cell.Wall)
                    break;
            }

            return cell;
        }

        Cell GetRandomAdjacentCell(Cell cell)
        {
            List<Cell> adjacent_cells = new List<Cell>();

            // check left cell 
            if (cell.I > 0 && !grid[cell.I - 1, cell.J].Wall)
                adjacent_cells.Add(grid[cell.I - 1, cell.J]);

            // check right cell
            if (cell.I < grid.GetLength(0) - 1 && !grid[cell.I + 1, cell.J].Wall)
                adjacent_cells.Add(grid[cell.I + 1, cell.J]);

            // check top cell
            if (cell.J > 0 && !grid[cell.I, cell.J - 1].Wall)
                adjacent_cells.Add(grid[cell.I, cell.J - 1]);

            // check bottom cell
            if (cell.J < grid.GetLength(1) - 1 && !grid[cell.I, cell.J + 1].Wall)
                adjacent_cells.Add(grid[cell.I, cell.J + 1]);

            // check top left cell
            if (cell.I > 0 && cell.J > 0 && !grid[cell.I - 1, cell.J - 1].Wall)
                adjacent_cells.Add(grid[cell.I - 1, cell.J - 1]);

            // check top right cell
            if (cell.I < grid.GetLength(0) - 1 && cell.J > 0 && !grid[cell.I + 1, cell.J - 1].Wall)
                adjacent_cells.Add(grid[cell.I + 1, cell.J - 1]);

            // check bottom left cell
            if (cell.I > 0 && cell.J < grid.GetLength(1) - 1 && !grid[cell.I - 1, cell.J + 1].Wall)
                adjacent_cells.Add(grid[cell.I - 1, cell.J + 1]);

            // check bottom right cell
            if (cell.I < grid.GetLength(0) - 1 && cell.J < grid.GetLength(1) - 1 && !grid[cell.I + 1, cell.J + 1].Wall)
                adjacent_cells.Add(grid[cell.I + 1, cell.J + 1]);

            if (adjacent_cells.Count == 0)
                return null;

            return adjacent_cells[rand.Next(adjacent_cells.Count)];
        }

        void CalculateThreats(Player owner)
        {
            // Reset all cells to non-wall
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    grid[i, j].Wall = false;

            Boundaries();

            foreach (Mob pirate in Game.pirate_list)
            {
                Cell cell = CoordsToCell(pirate.Position);
                for (int iOffset = 0; iOffset < 2; iOffset++)
                {
                    for (int jOffset = 0; jOffset < 2; jOffset++)
                    {
                        if (cell != null)
                        {
                            int i = cell.I + iOffset;
                            int j = cell.J + jOffset;

                            if (i < cols && j < rows)
                                grid[i, j].Wall = true;
                        }
                        else
                            return;
                    }
                }
            }

            foreach (Boat boat in owner.Opponent.boats)
            {
                if (boat.Type == "PT_BOAT")
                {
                    Cell cell = CoordsToCell(boat.position);
                    for (int iOffset = 0; iOffset < 2; iOffset++)
                    {
                        for (int jOffset = 0; jOffset < 2; jOffset++)
                        {
                            if (cell != null)
                            {
                                int i = cell.I + iOffset;
                                int j = cell.J + jOffset;

                                if (i < cols && j < rows)
                                    grid[i, j].Wall = true;
                            }
                            else
                                return;
                        }
                    }
                }
            }

            foreach (Weather weather in Game.weather_list)
            {
                if (weather.Type == "HURRICANE")
                {
                    Cell cell = CoordsToCell(weather.Position);
                    for (int iOffset = 0; iOffset < 4; iOffset++)
                    {
                        for (int jOffset = 0; jOffset < 4; jOffset++)
                        {
                            if (cell != null)
                            {
                                int i = cell.I + iOffset;
                                int j = cell.J + jOffset;

                                if (i < cols && j < rows)
                                    grid[i, j].Wall = true;
                            }
                            else
                                return;
                        }
                    }
                }
            }
        }

        Vector2 CellToCoords(int cell_x, int cell_y)
        {
            float world_x = cell_x * CELL_SIZE;
            float world_y = cell_y * CELL_SIZE;
            return new Vector2(world_x, world_y);
        }

        Cell CoordsToCell(Vector2 worldPosition)
        {
            int i = (int)Math.Floor(worldPosition.X / CELL_SIZE);
            int j = (int)Math.Floor(worldPosition.Y / CELL_SIZE);

            if (i >= 0 && i < cols && j >= 0 && j < rows)
                return grid[i, j];

            else
                return null;
        }

        void Boundaries()
        {
            // TOP BOUNDARY
            for (int i = 1; i <= 36; i++)
                grid[i, 2].Wall = true;

            // BOTTOM BOUNDARY
            for (int i = 1; i <= 36; i++)
                grid[i, 20].Wall = true;

            // LEFT BOUNDARY
            for (int j = 3; j <= 19; j++)
                grid[1, j].Wall = true;

            // RIGHT BOUNDARY
            for (int j = 3; j <= 19; j++)
                grid[36, j].Wall = true;

            int[,] left_island_region =
            {
                {6, 5, 2, 2},
                {4, 7, 5, 1},
                {4, 8, 5, 1},
                {4, 9, 5, 1},
                {4, 10, 6, 1},
                {4, 11, 8, 1},
                {7, 12, 7, 1},
                {7, 13, 7, 1},
                {9, 14, 9, 1},
                {9, 15, 9, 1},
                {8, 16, 5, 1},
                {15, 16, 8, 1},
                {8, 17, 5, 1},
                {15, 17, 8, 1},
                {5, 6, 1, 1},
                {4, 12, 1, 1},
                {5, 12, 1, 1},
                {6, 12, 1, 1},
                {9, 9, 1, 1},
                {10, 10, 1, 1},
                {11, 10, 1, 1},
                {12, 11, 1, 1},
                {7, 16, 1, 1},
                {7, 17, 1, 1},
                {14, 13, 1, 1}
            };

            int[,] right_island_region =
            {
                {18, 5, 3, 1},
                {23, 5, 1, 2},
                {18, 6, 3, 1},
                {28, 6, 2, 1},
                {17, 7, 13, 1},
                {17, 8, 13, 1},
                {17, 9, 4, 1},
                {24, 9, 7, 1},
                {30, 9, 1, 1},
                {17, 10, 4, 1},
                {24, 10, 8, 1},
                {26, 11, 8, 1},
                {26, 12, 8, 1},
                {29, 13, 5, 1},
                {27, 14, 7, 1},
                {26, 15, 7, 1},
                {30, 16, 2, 1},
                {17, 5, 1, 1},
                {17, 6, 1, 1},
                {24, 5, 1, 1},
                {28, 5, 1, 1},
                {29, 5, 1, 1},
                {31, 9, 1, 1},
                {28, 13, 1, 1},
                {29, 16, 1, 1},
                {30, 17, 1, 1},
                {31, 17, 1, 1},
                {14, 16, 1, 1},
                {14, 17, 1, 1}
            };

            SetWalls(left_island_region);
            SetWalls(right_island_region);
        }

        void SetWalls(int[,] regions)
        {
            for (int r = 0; r < regions.GetLength(0); r++)
            {
                int x = regions[r, 0];
                int y = regions[r, 1];
                int width = regions[r, 2];
                int height = regions[r, 3];

                for (int i = x; i < x + width; i++)
                    for (int j = y; j < y + height; j++)
                        grid[i, j].Wall = true;
            }
        }

        bool CheckBoatCollision(Rectangle collider, List<Boat> opponent_boats, List<Building> opponent_buildings)
        {
            foreach (Rectangle boundary_col in Environment.boundary_cols)
                if (CheckCollisionRecs(boundary_col, collider))
                    return true;

            foreach (Rectangle env_island_col in Environment.env_island_cols)
                if (CheckCollisionRecs(env_island_col, collider))
                    return true;

            foreach (Boat boat in opponent_boats)
                if (CheckCollisionRecs(boat.Collider, collider) && boat.Type == "PT_BOAT")
                    return true;

            return false;
        }

        bool CheckPTBoatCollision(Rectangle collider, List<Boat> owner_boats, List<Boat> opponent_boats)
        {
            if (this.Type != "FISHING_BOAT")
                return false;

            foreach (Boat boat in opponent_boats)
                if (CheckCollisionRecs(boat.Collider, collider) && boat.Type == "PT_BOAT")
                    return true;

            return false;
        }

        void DrawCells()
        {
            for (int i = 0; i < closed_set.Count; i++)
                closed_set[i].DrawRec(Color.RED);

            for (int i = 0; i < open_set.Count; i++)
                open_set[i].DrawRec(Color.GREEN);

            for (int i = 0; i < path.Count; i++)
                path.ElementAt(i).DrawRec(Color.DARKPURPLE);
        }

        void DrawGrid()
        {
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                {
                    grid[i, j].DrawLine(Color.BLACK);
                    //if (grid[i, j].Wall)
                    //    grid[i, j].DrawRec(BLACK);
                }

        }

        public static void Draw(Player owner, string type, Vector2 position, bool flipTexture)
        {
            switch (type)
            {
                case "FISHING_BOAT":
                    if (owner.player == 1 && !flipTexture) DrawTextureRec(Program.sheet, new Rectangle(1, 27, 23, 23), position, Color.WHITE);
                    if (owner.player == 1 && flipTexture) DrawTextureRec(Program.sheet, new Rectangle(1, 27, -23, 23), position, Color.WHITE);
                    if (owner.player == 2 && !flipTexture) DrawTextureRec(Program.sheet, new Rectangle(25, 27, 23, 23), position, Color.WHITE);
                    if (owner.player == 2 && flipTexture) DrawTextureRec(Program.sheet, new Rectangle(25, 27, -23, 23), position, Color.WHITE);
                    break;
                case "PT_BOAT":
                    if (owner.player == 1) DrawTextureRec(Program.sheet, new Rectangle(49, 28, 23, 23), position, Color.WHITE);
                    if (owner.player == 2) DrawTextureRec(Program.sheet, new Rectangle(73, 28, 23, 23), position, Color.WHITE);
                    break;
                default:
                    Debug.WriteLine("Error! Didn't draw boat!");
                    break;
            }
        }

        public override string ToString()
        {
            return $"Type: {Type}, ID: {ID}, Position: {position}, Collider: {Collider}";
        }
    }
}