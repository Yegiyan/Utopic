using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Numerics;

namespace Utopic.src
{
    public class Player
    {
        public const int CURSOR_SPEED = 120;

        protected Vector2 DockPosition { get; set; }
        protected Rectangle DockCollider { get; set; }
        public bool DestroyShip { get; set; }
        public int DestroyShipID { get; set; }

        protected Vector2 CursorPosition;
        protected Rectangle CursorCollider { get; set; }
        Rectangle CursorBoundaryCollider { get; set; }

        public Player Opponent { get; set; }
        public bool isAI { get; set; }

        public int Gold { get; set; }

        public float PrevRoundScore { get; set; }
        public float RoundScore { get; set; }
        public float Score { get; set; }

        public float RoundGDP { get; set; }
        public float GDP { get; set; }

        public float TotalRebels { get; set; }

        public float Fertility { get; set; }
        public float Mortality { get; set; }
        public float Population { get; set; }

        public int Crop { get; set; }
        public int School { get; set; }
        public int Factory { get; set; }
        public int Fort { get; set; }
        public int House { get; set; }
        public int Hospital { get; set; }
        public int Rebel { get; set; }
        public int FishingBoat { get; set; }
        public int PTBoat { get; set; }
        public bool FundRebels { get; set; }

        public List<Building> buildings = new List<Building>();
        public List<Boat> boats = new List<Boat>();
        List<int> used_IDs = new List<int>();

        Rectangle cursor_texture;
        public int player;

        bool prev_key_state;
        Music sfx_button;
        Music sfx_error;
        Environment env;

        public int active_boat_id;
        public bool isBoatActive;
        public bool isCursorActive;

        public bool toggleBoat;

        public Player(int player, Vector2 cursor_pos)
        {
            sfx_button = LoadMusicStream("res/audio/BUTTON.wav");
            SetMusicVolume(sfx_button, 0.6f);
            sfx_button.looping = false;

            sfx_error = LoadMusicStream("res/audio/ERROR.wav");
            
            if ((player == 1 && Menu.PlayerOne == 1) || (player == 2 && Menu.PlayerTwo == 1))
            {
                SetMusicVolume(sfx_error, 0.0f);
                isAI = true;
            }
                
            else
                SetMusicVolume(sfx_error, 0.3f);

            sfx_error.looping = false;

            env = new Environment();
            prev_key_state = false;
            DestroyShip = false;

            this.player = player;
            CursorPosition = cursor_pos;

            active_boat_id = 0;
            isCursorActive = true;
            isBoatActive = false;

            FundRebels = false;

            Gold = Menu.GoldCount;
            Population = 1000;

            PrevRoundScore = 0;
            RoundScore = 0;
            Score = 0;

            Fertility = 4.0f;
            Mortality = 1.1f;
            RoundGDP = 0;
            GDP = 0;

            if (player == 1)
                DockPosition = new Vector2(140, 300);
            else
                DockPosition = new Vector2(715, 165);

            DockCollider = new Rectangle(DockPosition.X, DockPosition.Y, 23, 23);
        }

        public void Update(float deltaTime)
        {
            UpdateMusicStream(sfx_button);
            UpdateMusicStream(sfx_error);

            //Debug.WriteLine("Cursor: {0}", CursorPosition);

            CheckFortProtection();

            CursorInput(deltaTime);
            BoatInput(deltaTime);
            SpawnUnit();

            //DrawCollisionBoxes();

            CursorCollider = new Rectangle(CursorPosition.X + 12, CursorPosition.Y + 14, 1, 1);
            CursorBoundaryCollider = new Rectangle(CursorPosition.X, CursorPosition.Y, 25, 25);

            if(isCursorActive)
                DrawTextureRec(Program.sheet, cursor_texture, CursorPosition, Color.WHITE);

            CountUnits();
        }

        void CheckFortProtection()
        {
            foreach (Building building in buildings)
                foreach (Boat boat in boats)
                {
                    if (CheckCollisionRecs(building.FortCollider, boat.Collider) && boat.Type == "FISHING_BOAT")
                        boat.IsProtected = true;
                    else
                        boat.IsProtected = false;
                }
        }

        void CursorInput(float deltaTime)
        {
            Dictionary<KeyboardKey, Vector2> cursor_keys;

            float adjusted_cursor_speed = CURSOR_SPEED * deltaTime;

            if (player == 1)
            {
                cursor_keys = new()
                {
                    { KeyboardKey.KEY_W, new Vector2(0, -adjusted_cursor_speed) },
                    { KeyboardKey.KEY_S, new Vector2(0, adjusted_cursor_speed) },
                    { KeyboardKey.KEY_A, new Vector2(-adjusted_cursor_speed, 0) },
                    { KeyboardKey.KEY_D, new Vector2(adjusted_cursor_speed, 0) }
                };
            }

            else
            {
                cursor_keys = new()
                {
                    { KeyboardKey.KEY_UP, new Vector2(0, -adjusted_cursor_speed) },
                    { KeyboardKey.KEY_DOWN, new Vector2(0, adjusted_cursor_speed) },
                    { KeyboardKey.KEY_LEFT, new Vector2(-adjusted_cursor_speed, 0) },
                    { KeyboardKey.KEY_RIGHT, new Vector2(adjusted_cursor_speed, 0) }
                };
            }

            foreach (KeyValuePair<KeyboardKey, Vector2> entry in cursor_keys)
            {
                if (IsKeyDown(entry.Key))
                {
                    Vector2 newPos = CursorPosition + entry.Value;
                    Rectangle newCursorCol = new(newPos.X, newPos.Y, CursorBoundaryCollider.width, CursorBoundaryCollider.height);

                    if (!Environment.boundary_cols.Any(col => CheckCollisionRecs(col, newCursorCol)))
                        CursorPosition = newPos;
                }
            }
        }

        void CountUnits()
        {
            School = 0;
            Factory = 0;
            Fort = 0;
            Hospital = 0;
            House = 0;
            Crop = 0;
            FishingBoat = 0;
            PTBoat = 0;
            Rebel = 0;

            foreach (Building building in buildings)
            {
                if (building.Type == "CROP") Crop++;
                if (building.Type == "FACTORY") Factory++;
                if (building.Type == "SCHOOL") School++;
                if (building.Type == "HOUSE") House++;
                if (building.Type == "HOSPITAL") Hospital++;
                if (building.Type == "REBEL") Rebel++;
            }

            foreach (Boat boat in boats)
            {
                if (boat.Type == "FISHING_BOAT") FishingBoat++;
                if (boat.Type == "PT_BOAT") PTBoat++;
            }
        }

        protected void SpawnUnit(string ai_type = null)
        {
            Dictionary<KeyboardKey, (string, int)> building_keys;

            if (player == 1)
            {
                building_keys = new()
                {
                    { KeyboardKey.KEY_ONE,   ("CROP",         3)  },
                    { KeyboardKey.KEY_TWO,   ("REBEL",        30) },
                    { KeyboardKey.KEY_THREE, ("SCHOOL",       35) },
                    { KeyboardKey.KEY_FOUR,  ("FACTORY",      40) },
                    { KeyboardKey.KEY_FIVE,  ("FORT",         50) },
                    { KeyboardKey.KEY_SIX,   ("HOUSE",        60) },
                    { KeyboardKey.KEY_SEVEN, ("HOSPITAL",     75) },
                    { KeyboardKey.KEY_EIGHT, ("FISHING_BOAT", 25) },
                    { KeyboardKey.KEY_NINE,  ("PT_BOAT",      40) }
                };
            }

            else
            {
                building_keys = new()
                {
                    { KeyboardKey.KEY_KP_1, ("CROP",         3)  },
                    { KeyboardKey.KEY_KP_2, ("REBEL",        30) },
                    { KeyboardKey.KEY_KP_3, ("SCHOOL",       35) },
                    { KeyboardKey.KEY_KP_4, ("FACTORY",      40) },
                    { KeyboardKey.KEY_KP_5, ("FORT",         50) },
                    { KeyboardKey.KEY_KP_6, ("HOUSE",        60) },
                    { KeyboardKey.KEY_KP_7, ("HOSPITAL",     75) },
                    { KeyboardKey.KEY_KP_8, ("FISHING_BOAT", 25) },
                    { KeyboardKey.KEY_KP_9, ("PT_BOAT",      40) }
                };
            }

            foreach (KeyValuePair<KeyboardKey, (string, int)> entry in building_keys)
            {
                if (IsKeyPressed(entry.Key) || ai_type != null)
                {
                    (string type, int cost) = entry.Value;

                    if (ai_type != null)
                        type = ai_type;

                    if (Gold < cost)
                    {
                        if (!Game.MuteAudio) PlayMusicStream(sfx_error);
                        continue;
                    }

                    if (!IsCursorOnIsland(CursorCollider, type) && Gold >= cost && type == "REBEL")
                    {
                        if (!Game.MuteAudio) PlayMusicStream(sfx_button);
                        Gold -= 30;
                        FundRebels = true;
                    }

                    if (IsCursorOnIsland(CursorCollider, type) && type != "FISHING_BOAT" && type != "PT_BOAT")
                    {
                        int id = GenerateID();

                        Building new_building = new Building(this, type, id, new(Round(CursorPosition.X), Round(CursorPosition.Y)), new(Round(CursorPosition.X), Round(CursorPosition.Y), 23, 23));

                        for (int i = 0; i < buildings.Count; i++)
                            if (buildings.ElementAt(i).Position == new Vector2(Round(CursorPosition.X), Round(CursorPosition.Y)))
                                new_building.IsTileOccupied = true;

                        for (int i = 0; i < Opponent.buildings.Count; i++)
                            if (Opponent.buildings.ElementAt(i).Position == new Vector2(Round(CursorPosition.X), Round(CursorPosition.Y)))
                                new_building.IsTileOccupied = true;

                        if (!new_building.IsTileOccupied && Gold >= cost && type == "REBEL")
                        {
                            Opponent.TotalRebels++;
                            Opponent.buildings.Add(new_building);
                            new_building.Buy(this, type);
                            if (!Game.MuteAudio) PlayMusicStream(sfx_button);
                        }

                        if (!new_building.IsTileOccupied && Gold >= cost)
                        {
                            if (type != "REBEL")
                            {
                                if (type == "FORT")
                                {
                                    int col_offset_x = (69 - 23) / 2;
                                    int col_offset_y = (69 - 23) / 2;
                                    new_building.FortCollider = new Rectangle(Round(CursorPosition.X) - col_offset_x, Round(CursorPosition.Y) - col_offset_y, 69, 69);
                                }

                                buildings.Add(new_building);
                                new_building.Buy(this, type);
                                if (!Game.MuteAudio) PlayMusicStream(sfx_button);
                                //Debug.WriteLine("Player {0} - {1}", player, string.Join(", ", buildings).ToString());
                            }
                        }

                        else if (new_building.IsTileOccupied && type != "REBEL")
                            if (!Game.MuteAudio) PlayMusicStream(sfx_error);
                    }

                    else if (!IsCursorOnIsland(CursorCollider, type) && type != "FISHING_BOAT" && type != "PT_BOAT" && type != "REBEL")
                        if (!Game.MuteAudio) PlayMusicStream(sfx_error);

                    if (!IsDockBlocked(DockCollider) && Gold >= cost && (type == "FISHING_BOAT" || type == "PT_BOAT"))
                    {
                        int id = GenerateID();
                        Boat new_boat = new Boat(this, type, id, DockPosition, DockCollider);
                        boats.Add(new_boat);
                        new_boat.Buy(this, type);
                        if (!Game.MuteAudio) PlayMusicStream(sfx_button);
                    }

                    else if (IsDockBlocked(DockCollider) && (type == "FISHING_BOAT" || type == "PT_BOAT"))
                        if (!Game.MuteAudio) PlayMusicStream(sfx_error);
                }
            }
        }

        protected void BoatInput(float deltaTime, Vector2? ai_boat_pos = null)
        {
            KeyboardKey key = (player == 1) ? KeyboardKey.KEY_SPACE : KeyboardKey.KEY_KP_ENTER;

            if (isAI && toggleBoat)
            {
                (bool isCursorOnBoat, int boat_id) = GetBoatInfo();

                isBoatActive = true;
                isCursorActive = false;
                active_boat_id = boat_id;
            }

            else if (isAI && !toggleBoat)
            {
                isBoatActive = false;
                isCursorActive = true;
                active_boat_id = 0;
            }

            if (IsKeyPressedWithBuffer(key) || (isAI && CursorPosition == ai_boat_pos))
            {
                if (!isBoatActive && isCursorActive)
                {
                    (bool isCursorOnBoat, int boat_id) = GetBoatInfo();

                    if (isCursorOnBoat)
                    {
                        isBoatActive = true;
                        isCursorActive = false;
                        active_boat_id = boat_id;
                    }
                }
                else if (isBoatActive)
                {
                    isBoatActive = false;
                    isCursorActive = true;
                    active_boat_id = 0;
                }
            }

            if (isBoatActive && active_boat_id != 0)
                ControlBoat(active_boat_id, deltaTime);
        }

        protected void ControlBoat(int id, float deltaTime)
        {
            Boat controlled_boat = boats.FirstOrDefault(boat => boat.ID == id); // Finds the boat with the given ID
            
            if (controlled_boat != null)
            {
                controlled_boat.Control(this, deltaTime);
                CursorPosition = controlled_boat.position;
            }
        }

        public void DestroyUnit(Player owner, int id)
        {
            for (int i = 0; i < owner.buildings.Count; i++)
                if (owner.buildings.ElementAt(i).ID == id)
                    owner.buildings.RemoveAt(i);

            for (int i = 0; i < owner.boats.Count; i++)
                if (owner.boats.ElementAt(i).ID == id)
                    owner.boats.RemoveAt(i);

            if (!isCursorActive && active_boat_id == id)
                isCursorActive = true;
        }

        (bool, int) GetBoatInfo()
        {
            for (int i = 0; i < boats.Count; i++)
                if (CheckCollisionRecs(boats.ElementAt(i).Collider, CursorBoundaryCollider))
                    return (true, boats.ElementAt(i).ID);
                    
            return (false, 0);
        }

        protected bool IsCursorOnIsland(Rectangle collider, string type)
        {
            List<Rectangle> p_island_cols;

            if (player == 1)
                p_island_cols = type == "REBEL" ? Environment.p2_island_cols : Environment.p1_island_cols;
            else
                p_island_cols = type == "REBEL" ? Environment.p1_island_cols : Environment.p2_island_cols;

            foreach (Rectangle island_cols in p_island_cols)
                if (CheckCollisionRecs(island_cols, collider))
                    return true;

            return false;
        }

        protected bool IsDockBlocked(Rectangle collider)
        {
            for (int i = 0; i < boats.Count; i++)
                if (CheckCollisionRecs(boats.ElementAt(i).Collider, collider))
                    return true;

            for (int i = 0; i < Opponent.boats.Count; i++)
                if (CheckCollisionRecs(Opponent.boats.ElementAt(i).Collider, collider) && Opponent.boats.ElementAt(i).Type == "PT_BOAT")
                    return true;

            for (int i = 0; i < Game.pirate_list.Count; i++)
                if (CheckCollisionRecs(Game.pirate_list.ElementAt(i).Collider, collider))
                    return true;

            return false;
        }

        public int GenerateID()
        {
            Random random = new Random();
            int unique_ID = random.Next(1, 1000);

            while (used_IDs.Contains(unique_ID))
                unique_ID = random.Next(1, 1000);

            used_IDs.Add(unique_ID);
            return unique_ID;
        }

        static float Round(float coord)
        {
            float remainder = coord % 23;
            return (remainder >= 11.5f) ? (coord - remainder + 23) : (coord - remainder);
        }

        protected bool IsKeyPressedWithBuffer(KeyboardKey key)
        {
            bool current_key_state = IsKeyDown(key);

            if (current_key_state && !prev_key_state)
            {
                prev_key_state = true;
                return true;
            }

            else if (!current_key_state && prev_key_state)
                prev_key_state = false;

            return false;
        }

        public void DrawCursor()
        {
            if (player == 1)
                cursor_texture = new Rectangle(173, 1, 25, 25);
            else
                cursor_texture = new Rectangle(200, 1, 25, 25);
        }

        public void DrawUnits()
        {
            for (int i = 0; i < buildings.Count; i++)
                Building.Draw(buildings.ElementAt(i).Type, buildings.ElementAt(i).Position);

            for (int i = 0; i < boats.Count; i++)
                Boat.Draw(this, boats.ElementAt(i).Type, boats.ElementAt(i).position, boats.ElementAt(i).FlipTexture);
        }

        void DrawCollisionBoxes()
        {
            //DrawRectangleLines((int)cursor_col.x, (int)cursor_col.y, (int)cursor_col.width, (int)cursor_col.height, Color.VIOLET);
            //DrawRectangleLines((int)cursor_boundary_col.x, (int)cursor_boundary_col.y, (int)cursor_boundary_col.width, (int)cursor_boundary_col.height, Color.RED);

            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings.ElementAt(i).Type == "FORT")
                    DrawRectangleLines((int)buildings.ElementAt(i).FortCollider.x, (int)buildings.ElementAt(i).FortCollider.y, (int)buildings.ElementAt(i).FortCollider.width, (int)buildings.ElementAt(i).FortCollider.height, Color.BLACK);
            }

            for (int i = 0; i < buildings.Count; i++)
                DrawRectangleLines((int)buildings.ElementAt(i).Collider.x, (int)buildings.ElementAt(i).Collider.y, (int)buildings.ElementAt(i).Collider.width, (int)buildings.ElementAt(i).Collider.height, Color.BLACK);

            for (int i = 0; i < boats.Count; i++)
                DrawRectangleLines((int)boats.ElementAt(i).Collider.x, (int)boats.ElementAt(i).Collider.y, (int)boats.ElementAt(i).Collider.width, (int)boats.ElementAt(i).Collider.height, Color.BLACK);
        }
    }
}