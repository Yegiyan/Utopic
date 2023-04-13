using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class AI : Player
    {
        bool isBuilding;
        bool isCursorMoving;
        Vector2 target_pos;

        float huntingTimer;
        float huntingInterval;
        float fishingTimer;
        float fishingInterval;

        float decisionTimer;
        float decisionInterval;

        float buildTimer;
        float elapsedTime;

        bool isHunting;
        bool isFishing;

        Random rand;

        enum Stage { EARLY, MID, LATE }

        Stage GetStage()
        {
            if (elapsedTime < 120)
                return Stage.EARLY;
            else if (elapsedTime < 240)
                return Stage.MID;
            else
                return Stage.LATE;
        }

        public AI(int player, Vector2 cursor_pos) : base(player, cursor_pos)
        {
            isBuilding = false;
            isCursorMoving = false;
            rand = new Random();

            isHunting = false;
            isFishing = false;
            toggleBoat = false;

            decisionTimer = 0;
            decisionInterval = rand.Next(0, 6);

            fishingInterval = rand.Next(30, 120);
            huntingInterval = rand.Next(15, 45);

            buildTimer = 0;
            elapsedTime = 0;
        }

        public void UpdateAI(float deltaTime)
        {
            NewTask(deltaTime);
            
            if (CursorPosition == target_pos && !isBuilding && !isFishing && !isHunting && decisionTimer >= decisionInterval)
                Build();

            if (isFishing || isHunting)
                ControlBoat(deltaTime);
        }

        void Build()
        {
            int decision = Strategize();
            if (elapsedTime < 6) decision = 0;

            switch (decision)
            {
                case 0:
                    Debug.WriteLine("{0} - Focus Start!", player);
                    FocusStart();
                    break;
                case 1:
                    Debug.WriteLine("{0} - Focus Gold!", player);
                    if (FishingBoat >= 1 && Gold <= 35)
                    {
                        isFishing = true;
                        fishingTimer = 0;
                    }
                    else
                        FocusGold();
                    break;
                case 2:
                    Debug.WriteLine("{0} - Focus Welfare!", player);
                    FocusWelfare();
                    break;
                case 3:
                    Debug.WriteLine("{0} - Focus Opponent!", player);
                    if (PTBoat >= 1 && Opponent.FishingBoat >= 1)
                    {
                        isHunting = true;
                        huntingTimer = 0;
                    }
                    else
                        FocusOpponent();
                    break;
                case 4:
                    Debug.WriteLine("{0} - Focus Balance!", player);
                    FocusBalance();
                    break;
                default:
                    break;
            }
            isBuilding = true;
            decisionTimer = 0;
            decisionInterval = rand.Next(0, 6);
        }

        int Strategize()
        {
            float threshold = 0.10f;
            bool score_similarity = Math.Abs(Score - Opponent.Score) / (float)Math.Max(Score, Opponent.Score) <= threshold;
            bool gold_similarity = Math.Abs(Gold - Opponent.Gold) / (float)Math.Max(Gold, Opponent.Gold) <= threshold;
            bool pop_similarity = Math.Abs(Population - Opponent.Population) / (float)Math.Max(Population, Opponent.Population) <= threshold;

            Stage stage = GetStage();

            int[] strategy_weights = new int[5]
            {
                0,
                10,
                stage >= Stage.MID ? 10 : 0,
                stage >= Stage.LATE ? 10 : 0,
                10
            };

            // adjust weights based on game conditions
            if (Gold < Opponent.Gold * 0.50 || RoundGDP < 15)
                strategy_weights[1] += rand.Next(10, 20);

            if (Population < Opponent.Population * 0.75)
                strategy_weights[2] += rand.Next(10, 20);

            if (Score < Opponent.Score * 0.75)
                strategy_weights[3] += rand.Next(10, 20);

            if (Score > Opponent.Score * 1.25)
            {
                if (rand.NextDouble() < .50)
                    strategy_weights[3] += rand.Next(10, 20);
                else
                    strategy_weights[4] += rand.Next(10, 20);
            }

            if (score_similarity && gold_similarity && pop_similarity)
                strategy_weights[4] += rand.Next(10, 20);

            // choose strategy based on weights
            int total_weight = strategy_weights.Sum();
            int random_weight = rand.Next(1, total_weight + 1);
            int cumulative_weight = 0;

            for (int i = 0; i < strategy_weights.Length; i++)
            {
                cumulative_weight += strategy_weights[i];
                if (random_weight <= cumulative_weight)
                    return i;
            }

            return 0;
        }

        void FocusStart()
        {
            Dictionary<string, int> cost = new()
            {
                { "CROP", 3 },
                { "FISHING_BOAT", 25 },
                { "SCHOOL", 35 },
                { "HOUSE", 60 },
            };

            List<string> affordable_building = cost.Where(b => Gold >= cost[b.Key]).Select(b => b.Key).ToList();

            if (affordable_building.Count > 0)
            {
                string chosen_building = affordable_building[rand.Next(affordable_building.Count)];
                SpawnUnit(chosen_building);
            }
        }

        void FocusGold()
        {
            Dictionary<string, int> cost = new()
            {
                { "CROP", 3 },
                { "FISHING_BOAT", 25 },
                { "FACTORY", 40 }
            };

            List<string> affordable_building = cost.Where(b => Gold >= cost[b.Key]).Select(b => b.Key).ToList();

            double crop_priority = (double)(Opponent.Crop - Crop) / (Crop + 1);
            double factory_priority = (double)(Opponent.Factory - Factory) / (Factory + 1);

            if (RoundScore < 30 && affordable_building.Contains("FACTORY"))
            {
                affordable_building.Clear();
                affordable_building.Add("FACTORY");
            }
            else if (crop_priority > factory_priority)
            {
                affordable_building.Remove("FACTORY");
                affordable_building.Remove("FISHING_BOAT");
            }
            else if (factory_priority > crop_priority)
            {
                affordable_building.Remove("CROP");
                affordable_building.Remove("FISHING_BOAT");
            }

            if (Crop > 5)
                affordable_building.Remove("CROP");

            int fishing_boat_per_pop_goal = 500;
            float fishing_boat_per_pop_actual = FishingBoat != 0 ? (float)Population / FishingBoat : 0;

            if ((fishing_boat_per_pop_actual >= fishing_boat_per_pop_goal) && affordable_building.Contains("FISHING_BOAT"))
                affordable_building.Remove("FISHING_BOAT");

            if (affordable_building.Count > 0)
            {
                string chosen_building = affordable_building[rand.Next(affordable_building.Count)];

                if (Game.CurrentRound / Game.RoundAmount >= .20 && FishingBoat < 1 && (Gold >= 25 && Gold <= 40))
                    chosen_building = "FISHING_BOAT";

                SpawnUnit(chosen_building);
            }
        }

        void FocusWelfare()
        {
            Dictionary<string, int> cost = new()
            {
                { "SCHOOL", 35 },
                { "HOUSE", 60 },
                { "HOSPITAL", 75 },
            };

            List<string> affordable_building = cost.Where(b => Gold >= cost[b.Key]).Select(b => b.Key).ToList();

            int house_per_pop_goal = 500;
            int hospital_per_pop_goal = 1000;
            int school_per_pop_goal = 1500;

            float house_per_pop_actual = House != 0 ? (float)Population / House : 0;
            float hospital_per_pop_actual = Hospital != 0 ? (float)Population / Hospital : 0;
            float school_per_pop_actual = School != 0 ? (float)Population / School : 0;

            if (RoundScore < 30)
            {
                affordable_building.Clear();
                if (Gold >= cost["SCHOOL"]) affordable_building.Add("SCHOOL");
                if (Gold >= cost["HOUSE"]) affordable_building.Add("HOUSE");
                if (Gold >= cost["HOSPITAL"]) affordable_building.Add("HOSPITAL");
            }

            else
            {
                if (house_per_pop_actual >= house_per_pop_goal && affordable_building.Contains("HOUSE"))
                    affordable_building.Remove("HOUSE");
                if (hospital_per_pop_actual >= hospital_per_pop_goal && affordable_building.Contains("HOSPITAL"))
                    affordable_building.Remove("HOSPITAL");
                if (school_per_pop_actual >= school_per_pop_goal && affordable_building.Contains("SCHOOL"))
                    affordable_building.Remove("SCHOOL");
            }

            if (affordable_building.Count > 0)
            {
                string chosen_building = affordable_building.OrderBy(b => Population / cost[b]).FirstOrDefault();
                SpawnUnit(chosen_building);
            }
        }

        void FocusOpponent()
        {
            Dictionary<string, int> cost = new()
            {
                { "REBEL", 30 },
                { "PT_BOAT", 40 },
                { "FORT", 50 },
            };

            List<string> affordable_building = cost.Where(b => Gold >= cost[b.Key]).Select(b => b.Key).ToList();

            Dictionary<string, float> unit_impact = new()
            {
                { "REBEL", Opponent.Factory + Opponent.School },
                { "PT_BOAT", Opponent.FishingBoat * 2 },
                { "FORT", Fort > 5 ? 0 : 1 },
            };

            if (affordable_building.Count > 0)
            {
                string chosen_building = affordable_building.OrderByDescending(b => unit_impact[b] / cost[b]).FirstOrDefault();

                if (chosen_building == "FORT" && !ImportantBuildingNearby(CursorPosition, 30f))
                {
                    affordable_building.Remove("FORT");
                    chosen_building = affordable_building.OrderByDescending(b => unit_impact[b] / cost[b]).FirstOrDefault();
                }

                if (chosen_building == "PT_BOAT" && Opponent.FishingBoat < 1)
                {
                    affordable_building.Remove("PT_BOAT");
                    chosen_building = affordable_building.OrderByDescending(b => unit_impact[b] / cost[b]).FirstOrDefault();
                }

                if (chosen_building != null)
                    SpawnUnit(chosen_building);
            }
        }

        void FocusBalance()
        {
            Dictionary<string, int> cost = new()
            {
                { "SCHOOL", 35 },
                { "FACTORY", 40 },
                { "FORT", 50 },
                { "HOUSE", 60 },
                { "HOSPITAL", 75 },
            };

            List<string> affordable_building = cost.Where(b => Gold >= cost[b.Key]).Select(b => b.Key).ToList();

            Dictionary<string, float> unit_impact = new()
            {
                { "SCHOOL", 2 * (float)Population / Math.Max(1, School) },
                { "FACTORY", 3 * (float)Population / Math.Max(1, Factory) },
                { "FORT", Fort > 5 ? 0 : 1 },
                { "HOUSE", 4 * (float)Population / Math.Max(1, House) },
                { "HOSPITAL", 5 * (float)Population / Math.Max(1, Hospital) },
            };

            if (affordable_building.Count > 0)
            {
                string chosen_building = affordable_building.OrderByDescending(b => unit_impact[b] / cost[b]).FirstOrDefault();

                if (chosen_building == "FORT" && !ImportantBuildingNearby(CursorPosition, 30f))
                {
                    affordable_building.Remove("FORT");
                    chosen_building = affordable_building.OrderByDescending(b => unit_impact[b] / cost[b]).FirstOrDefault();
                }

                if (chosen_building != null)
                    SpawnUnit(chosen_building);
            }
        }

        void NewTask(float deltaTime)
        {
            huntingTimer += deltaTime;
            fishingTimer += deltaTime;
            decisionTimer += deltaTime;

            buildTimer += deltaTime;
            elapsedTime += deltaTime;

            if (!isCursorMoving)
            {
                FindValidPosition();
                isCursorMoving = true;
            }
            else
                MoveCursor(deltaTime);

            if (isBuilding && buildTimer >= 1)
            {
                isBuilding = false;
                isCursorMoving = false;
                buildTimer = 0;
            }

            if (isFishing && fishingTimer >= fishingInterval)
            {
                isFishing = false;
                isCursorMoving = false;
                toggleBoat = false;
                fishingTimer = 0;
                fishingInterval = rand.Next(30, 120);
            }

            if (isHunting && huntingTimer >= huntingInterval)
            {
                isHunting = false;
                isCursorMoving = false;
                toggleBoat = false;
                huntingTimer = 0;
                huntingInterval = rand.Next(15, 45);
            }
        }

        void ControlBoat(float deltaTime)
        {
            Boat closest_boat = null;
            
            if (isHunting)
                closest_boat = GetClosestBoat(CursorPosition, "PT_BOAT");

            if (isFishing)
                closest_boat = GetClosestBoat(CursorPosition, "FISHING_BOAT");

            if (closest_boat == null || !boats.Any(boat => boat.ID == closest_boat.ID))
            {
                toggleBoat = false;
                isFishing = false;
                isHunting = false;
                return;
            }

            if (closest_boat != null)
                target_pos = closest_boat.position;

            if (CursorPosition == closest_boat.position && !toggleBoat)
            {
                toggleBoat = true;
                BoatInput(deltaTime, closest_boat.position);
            }
        }

        Boat GetClosestBoat(Vector2 position, string type)
        {
            Boat closest_fishingboat = null;
            float min_distance = float.MaxValue;

            foreach (Boat boat in boats)
            {
                if (boat.Type == type)
                {
                    float distance = Vector2.Distance(position, boat.position);
                    if (distance < min_distance)
                    {
                        min_distance = distance;
                        closest_fishingboat = boat;
                    }
                }
            }

            return closest_fishingboat;
        }

        bool ImportantBuildingNearby(Vector2 position, float distance_threshold)
        {
            foreach (Building building in buildings)
            {
                if (building.Type != "CROP")
                {
                    float distance = Vector2.Distance(position, building.Position);
                    if (distance <= distance_threshold)
                        return true;
                }
            }

            return false;
        }

        void FindValidPosition()
        {
            Rectangle island_cols;
            int x = 0;
            int y = 0;
            bool isValidPosition = false;

            int max_attempt = 250;
            int attempt = 0;

            while (!isValidPosition && attempt < max_attempt)
            {
                if (player == 1 && isAI)
                    island_cols = Environment.p1_island_cols.ElementAt(rand.Next(Environment.p1_island_cols.Count));
                else
                    island_cols = Environment.p2_island_cols.ElementAt(rand.Next(Environment.p2_island_cols.Count));

                int island_width = (int)island_cols.width - 24;
                int island_height = (int)island_cols.height - 24;

                if (island_width <= 0 || island_height <= 0)
                {
                    attempt++;
                    continue;
                }

                x = rand.Next((int)island_cols.x + 1, (int)island_cols.x + island_width);
                y = rand.Next((int)island_cols.y + 1, (int)island_cols.y + island_height);

                isValidPosition = CanGenerateBuilding(x, y, this);
                attempt++;
            }

            if (isValidPosition)
                target_pos = new Vector2(x, y);
        }

        static bool CanGenerateBuilding(int x, int y, AI ai)
        {
            foreach (Building building in ai.buildings)
                if (CheckCollisionRecs(building.Collider, new Rectangle(x, y, 23, 23)))
                    return false;

            return true;
        }

        void MoveCursor(float deltaTime)
        {
            float move_amount_x = CURSOR_SPEED * deltaTime;
            float move_amount_y = CURSOR_SPEED * deltaTime;

            if (CursorPosition.X < target_pos.X)
                CursorPosition.X = Math.Min(CursorPosition.X + move_amount_x, target_pos.X);

            if (CursorPosition.X > target_pos.X)
                CursorPosition.X = Math.Max(CursorPosition.X - move_amount_x, target_pos.X);

            if (CursorPosition.Y < target_pos.Y)
                CursorPosition.Y = Math.Min(CursorPosition.Y + move_amount_y, target_pos.Y);

            if (CursorPosition.Y > target_pos.Y)
                CursorPosition.Y = Math.Max(CursorPosition.Y - move_amount_y, target_pos.Y);
        }
    }
}