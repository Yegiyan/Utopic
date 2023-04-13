using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Numerics;

namespace Utopic.src
{
    public class Game
    {
        Music sfx_round_end;

        Music sfx_hurricane_destroy;
        Music sfx_rebel_destroy;
        Music sfx_storm_destroy;
        Music sfx_boat_destroy;
        Music sfx_rain_crops;

        Music sfx_fish1;
        Music sfx_fish2;

        public int WeatherCount { get; set; }
        public int PirateCount { get; set; }
        public int FishCount { get; set; }

        public int RoundDuration { get; set; }
        public static int RoundAmount { get; set; }
        public float RoundTime { get; set; }
        public static int CurrentRound { get; set; }
        public float RoundTimePassed { get; set; }
        public bool ShowRoundScore { get; set; }
        

        public static bool IsGameOver { get; set; }
        public static bool IsGamePaused { get; set; }

        public float TotalTime { get; set; }

        static public List<Weather> weather_list;
        static public List<Mob> pirate_list;
        static public List<Mob> fish_list;

        float elapsedTime;
        float weather_timer;
        float fishing_timer;
        Random rand;

        public AI p1;
        public AI p2;

        Font font;
        Texture2D game_gui;
        bool showInfo;

        public static bool MuteAudio { get; set; }

        public Game()
        {
            LoadResources();
            NewGame();
        }

        public void NewGame()
        {
            weather_timer = 0.0f;
            fishing_timer = 0.0f;
            rand = new();

            RoundAmount = Menu.TermOfOffice;
            RoundDuration = Menu.TurnLength;

            MuteAudio = false;
            showInfo = false;

            IsGamePaused = false;
            IsGameOver = false;
            ShowRoundScore = false;
            CurrentRound = 1;
            RoundTime = RoundDuration;

            TotalTime = 0;

            FishCount = Menu.FishCount;
            fish_list = new List<Mob>
            {
                new Mob("FISH") { AnimationInterval = 0f },
                new Mob("FISH") { AnimationInterval = 1.5f },
                new Mob("FISH") { AnimationInterval = 2.0f },
                new Mob("FISH") { AnimationInterval = 3.5f },
                new Mob("FISH") { AnimationInterval = 4.0f },
                new Mob("FISH") { AnimationInterval = 4.5f },
            };

            PirateCount = Menu.PirateCount;
            pirate_list = new List<Mob>
            {
                new Mob("PIRATE"),
                new Mob("PIRATE"),
                new Mob("PIRATE"),
                new Mob("PIRATE"),
                new Mob("PIRATE"),
                new Mob("PIRATE"),
            };

            WeatherCount = Menu.WeatherCount;
            weather_list = new List<Weather>
            {
                new Weather(),
                new Weather(),
                new Weather(),
                new Weather(),
                new Weather(),
                new Weather(),
            };

            p1 = new(1, new(120, 312));
            p2 = new(2, new(552, 264));

            p1.Opponent = p2;
            p2.Opponent = p1;
        }

        public void Gameplay(float deltaTime)
        {
            UpdateMusicStream(sfx_round_end);
            UpdateMusicStream(sfx_hurricane_destroy);
            UpdateMusicStream(sfx_storm_destroy);
            UpdateMusicStream(sfx_rebel_destroy);
            UpdateMusicStream(sfx_boat_destroy);
            UpdateMusicStream(sfx_rain_crops);

            UpdateMusicStream(sfx_fish1);
            UpdateMusicStream(sfx_fish2);

            if (IsKeyPressed(KeyboardKey.KEY_M))
                MuteAudio = !MuteAudio;

            if (p1.isAI)
                p1.UpdateAI(deltaTime);

            if (p2.isAI)
                p2.UpdateAI(deltaTime);

            TotalTime += deltaTime;

            RoundTime -= deltaTime;
            RoundTimePassed = RoundDuration - RoundTime;

            if (RoundTimePassed >= 6)
                ShowRoundScore = false;

            if (p1.FundRebels)
            {
                GenerateRebelBuilding(p2);
                p1.FundRebels = false;
            }

            if (p2.FundRebels)
            {
                GenerateRebelBuilding(p1);
                p2.FundRebels = false;
            }

            // ROUND END
            if (RoundTime <= 0)
            {
                if (CurrentRound != RoundAmount && !MuteAudio)
                    PlayMusicStream(sfx_round_end);

                ShowRoundScore = true;

                IncomeCalculation(p1);
                IncomeCalculation(p2);

                PopulationCalculation(p1);
                PopulationCalculation(p2);

                ScoreCalculation(p1);
                ScoreCalculation(p2);

                CropDecayCalculation(p1);
                CropDecayCalculation(p2);

                RebellionCalculation(p1);
                RebellionCalculation(p2);

                p1.GDP += p1.RoundGDP;
                p1.Score += p1.RoundScore;
                p1.PrevRoundScore = p1.RoundScore;

                p2.GDP += p2.RoundGDP;
                p2.Score += p2.RoundScore;
                p2.PrevRoundScore = p2.RoundScore;

                p1.RoundScore = 0;
                p1.RoundGDP = 0;

                p2.RoundScore = 0;
                p2.RoundGDP = 0;

                RoundTime = RoundDuration;
                CurrentRound++;

                // GAME OVER
                if (CurrentRound > RoundAmount)
                {
                    CurrentRound = RoundAmount;
                    RoundTime = 0;
                    IsGameOver = true;
                }

                ClampValues(p1);
                ClampValues(p2);
            }

            for (int i = 0; i < FishCount; i++)
                fish_list.ElementAt(i).Update(deltaTime);

            for (int i = 0; i < PirateCount; i++)
                pirate_list.ElementAt(i).Update(deltaTime);

            p1.Update(deltaTime);
            p2.Update(deltaTime);

            for (int i = 0; i < WeatherCount; i++)
                weather_list.ElementAt(i).Update(deltaTime);

            weather_timer += deltaTime;
            if (weather_timer >= 1.0f)
            {  
                double destruction_chance = rand.NextDouble();
                for (int i = 0; i < WeatherCount; i++)
                {
                    CheckWeatherCollisions(p1, weather_list.ElementAt(i), ref destruction_chance);
                    CheckWeatherCollisions(p2, weather_list.ElementAt(i), ref destruction_chance);
                }
                weather_timer = 0.0f;
            }

            fishing_timer += deltaTime;
            if (fishing_timer >= 3.0f)
            {
                for (int i = 0; i < FishCount; i++)
                {
                    CheckMobCollision(p1, fish_list.ElementAt(i));
                    CheckMobCollision(p2, fish_list.ElementAt(i));
                }
                fishing_timer = 0.0f;
            }

            for (int i = 0; i < PirateCount; i++)
            {
                CheckMobCollision(p1, pirate_list.ElementAt(i));
                CheckMobCollision(p2, pirate_list.ElementAt(i));
            }

            CheckPTBoatCollision(p1);
            CheckPTBoatCollision(p2);
        }

        void IncomeCalculation(Player player)
        {
            int productivity = ((player.School + player.Hospital) * player.Factory) + player.Hospital;
            if (productivity > 30) productivity = 30;

            player.Gold += productivity;
            player.RoundGDP += productivity;

            player.Gold += player.Factory * 4;
            player.RoundGDP += player.Factory * 4;

            player.Gold += player.FishingBoat;
            player.RoundGDP += player.FishingBoat;

            player.Gold += 10;
        }

        void PopulationCalculation(Player player)
        {
            float new_fertility = (player.Crop * 0.3f) + (player.Hospital * 0.3f) + (player.House * 0.1f) - (player.School * 0.3f);
            float new_mortality = (player.Factory * 0.1f) - (player.Hospital * 0.3f);

            player.Fertility = Math.Clamp(player.Fertility + new_fertility, float.MinValue, 6.4f);
            player.Mortality = Math.Clamp(player.Mortality + new_mortality, 0.2f, 6.4f);

            float new_population = player.Population + (player.Population * (player.Fertility / 100)) - (player.Population * (player.Mortality / 100));
            player.Population = Math.Clamp(new_population, 0, 9999);
        }

        void ScoreCalculation(Player player)
        {
            float housing_score = ((player.House * 500) / (player.Population / 100)) / 3;
            if (housing_score > 30) housing_score = 30;

            float per_capita_gdp_score = ((player.RoundGDP * 100) / (player.Population / 100)) / 12;
            if (per_capita_gdp_score > 30) per_capita_gdp_score = 30;

            float food_supply_score = ((player.FishingBoat + player.Crop) * 500 / (player.Population / 100)) / 3;
            if (food_supply_score > 30) food_supply_score = 30;

            float general_welfare_score = player.School + player.Hospital;

            player.RoundScore = housing_score + per_capita_gdp_score + food_supply_score + general_welfare_score;
            if (player.RoundScore > 100) player.RoundScore = 100;
        }

        void CropDecayCalculation(Player player)
        {
            List<Building> destroy_queue = new();
            for (int i = 0; i < player.buildings.Count; i++)
            {
                Random rand = new();
                double chance = rand.NextDouble();

                if (player.buildings.ElementAt(i).Type == "CROP")
                {
                    player.buildings.ElementAt(i).DecayTime += 1;

                    if (chance < .25)
                        destroy_queue.Add(player.buildings.ElementAt(i));

                    if (player.buildings.ElementAt(i).DecayTime == 3)
                        destroy_queue.Add(player.buildings.ElementAt(i));
                }

            }

            foreach (var building in destroy_queue)
                player.DestroyUnit(player, building.ID);
        }

        void RebellionCalculation(Player player)
        {
            float score_difference = player.RoundScore - player.PrevRoundScore;

            if (score_difference < -10 || player.RoundScore < 30) 
                GenerateRebelBuilding(player);

            if (score_difference > 10 || player.RoundScore > 70)
            {
                for (int i = 0; i < player.buildings.Count; i++)
                {
                    if (player.buildings.ElementAt(i).Type == "REBEL")
                    {
                        player.DestroyUnit(player, player.buildings.ElementAt(i).ID);
                        return;
                    }
                }
            }

            foreach (Building building in player.buildings)
            {
                if (building.Type == "REBEL")
                {
                    if (rand.NextDouble() < .49)
                        player.Gold -= rand.Next(0, 10);
                    else
                        player.Score -= rand.Next(0, 15);
                }
            }

            if (player.Gold < 0) player.Gold = 0;
            if (player.Score < 0) player.Score = 0;
        }

        void GenerateRebelBuilding(Player player)
        {
            Rectangle island_cols;
            int x = 0;
            int y = 0;
            bool isValidPosition = false;

            int max_attempt = 500;
            int attempt = 0;

            while (!isValidPosition && attempt < max_attempt)
            {
                if (player.player == 1)
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

                isValidPosition = CanGenerateRebel(x, y, player);
                attempt++;
            }

            if (isValidPosition)
            {
                Building rebel = new(player, "REBEL", player.GenerateID(), new(Round(x), Round(y)), new(Round(x), Round(y), 23, 23));
                for (int i = 0; i < player.buildings.Count; i++)
                    if (player.buildings.ElementAt(i).Position == new Vector2(Round(rebel.Position.X), Round(rebel.Position.Y)) && player.buildings.ElementAt(i).Type != "REBEL")
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_rebel_destroy);
                        player.DestroyUnit(player, player.buildings.ElementAt(i).ID);
                    }

                player.TotalRebels++;
                player.buildings.Add(rebel);
            }
        }

        bool CanGenerateRebel(int x, int y, Player player)
        {
            foreach (Building building in player.buildings)
            {
                if (building.Type == "FORT" && CheckCollisionRecs(building.FortCollider, new Rectangle(x, y, 23, 23)))
                    return false;

                if (building.Type == "FORT" && CheckCollisionRecs(building.Collider, new Rectangle(x, y, 23, 23)))
                    return false;

                if (building.Type == "REBEL" && CheckCollisionRecs(building.Collider, new Rectangle(x, y, 23, 23)))
                    return false;
            }

            return true;
        }

        void ClampValues(Player player)
        {
            if (player.Gold < 0) player.Gold = 0;
            if (player.Score < 0) player.Score = 0;
            if (player.Population < 0) player.Population = 0;
        }

        void CheckWeatherCollisions(Player player, Weather weather, ref double destruction_chance)
        {
            int id = 0;

            for (int i = 0; i < player.buildings.Count; i++) // player buildings vs weather check
            {
                if (CheckCollisionRecs(player.buildings.ElementAt(i).Collider, weather.Collider) && player.buildings.ElementAt(i).Type == "CROP" && weather.Type == "CLOUD")
                {
                    if (!MuteAudio) PlayMusicStream(sfx_rain_crops);
                    player.Gold++;
                    player.RoundGDP++;
                }

                else if (CheckCollisionRecs(player.buildings.ElementAt(i).Collider, weather.Collider) && player.buildings.ElementAt(i).Type == "CROP" && weather.Type == "STORM")
                {
                    if (!MuteAudio) PlayMusicStream(sfx_rain_crops);
                    player.Gold++;
                    player.RoundGDP++;
                    id = player.buildings.ElementAt(i).ID;

                    if (destruction_chance <= 0.10)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_storm_destroy);
                        player.Population -= rand.Next(0, 102);
                        player.DestroyUnit(player, id);
                    }
                }

                else if (CheckCollisionRecs(player.buildings.ElementAt(i).Collider, weather.Collider) && weather.Type == "HURRICANE")
                {
                    id = player.buildings.ElementAt(i).ID;

                    if (player.buildings.ElementAt(i).Type == "CROP")
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_rain_crops);
                        player.Gold++;
                        player.RoundGDP++;
                    }

                    if (destruction_chance <= 0.30)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_hurricane_destroy);
                        player.Population -= rand.Next(0, 102);
                        player.DestroyUnit(player, id);
                    }
                }
            }

            for (int i = 0; i < player.boats.Count; i++) // player boats vs weather check
            {
                if (CheckCollisionRecs(player.boats.ElementAt(i).Collider, weather.Collider) && weather.Type == "STORM")
                {
                    id = player.boats.ElementAt(i).ID;
                    if (destruction_chance <= 0.01)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_boat_destroy);
                        player.DestroyUnit(player, id);
                    }
                }

                else if (CheckCollisionRecs(player.boats.ElementAt(i).Collider, weather.Collider) && weather.Type == "HURRICANE")
                {
                    id = player.boats.ElementAt(i).ID;
                    if (player.active_boat_id == id)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_hurricane_destroy);
                        player.DestroyUnit(player, id);
                    }

                    if (destruction_chance <= 0.10)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_hurricane_destroy);
                        player.DestroyUnit(player, id);
                    }
                }
            }

            for (int i = 0; i < PirateCount; i++)  // pirates vs weather check
            {
                if (CheckCollisionRecs(pirate_list.ElementAt(i).Collider, weather.Collider) && weather.Type == "STORM")
                {
                    if (destruction_chance <= 0.05)
                    {
                        if (!MuteAudio) PlayMusicStream(sfx_storm_destroy);
                        pirate_list.ElementAt(i).IsDestroyed = true;
                    }
                }

                else if (CheckCollisionRecs(pirate_list.ElementAt(i).Collider, weather.Collider) && weather.Type == "HURRICANE")
                {
                    if (!MuteAudio) PlayMusicStream(sfx_hurricane_destroy);
                    pirate_list.ElementAt(i).IsDestroyed = true;
                }
            }
        }

        void CheckMobCollision(Player player, Mob mob)
        {
            double chance = rand.NextDouble();
            int id = 0;

            for (int i = 0; i < player.boats.Count; i++)
            {
                if (CheckCollisionRecs(player.boats.ElementAt(i).Collider, mob.Collider) && player.boats.ElementAt(i).Type == "FISHING_BOAT" && mob.Type == "PIRATE" && !player.boats.ElementAt(i).IsProtected)
                {
                    if (!MuteAudio) PlayMusicStream(sfx_boat_destroy);
                    id = player.boats.ElementAt(i).ID;
                    player.DestroyUnit(player, id);
                }

                else if (CheckCollisionRecs(player.boats.ElementAt(i).Collider, mob.Collider) && player.boats.ElementAt(i).Type == "FISHING_BOAT" && mob.Type == "FISH")
                {
                    player.Gold++;
                    player.RoundGDP++;

                    if (!MuteAudio)
                    {
                        if (chance <= .50)
                            PlayMusicStream(sfx_fish1);
                        else
                            PlayMusicStream(sfx_fish2);
                    }
                }
            }
        }

        void CheckPTBoatCollision(Player player)
        {
            foreach (Boat boat in player.boats)
                boat.Update(player);

            foreach (Boat boat in player.Opponent.boats)
                boat.Update(player.Opponent);

            if (player.DestroyShip)
            {
                if (!MuteAudio) PlayMusicStream(sfx_boat_destroy);
                player.DestroyUnit(player, player.DestroyShipID);
                player.DestroyShip = false;

                if (player.active_boat_id != player.DestroyShipID && player.isBoatActive)
                    player.isCursorActive = false;

                player.DestroyShipID = 0;
            }

            foreach (Mob pirate in pirate_list)
            {
                if (pirate.CheckPTBoatCollision(player.boats) || pirate.CheckPTBoatCollision(player.Opponent.boats))
                    pirate.IsPTBlocking = true;
                else
                    pirate.IsPTBlocking = false;
            }
        }

        static float Round(float coord)
        {
            float remainder = coord % 23;
            return (remainder >= 11.5) ? (coord - remainder + 23) : (coord - remainder);
        }

        public void DrawPlayers()
        {
            p2.DrawCursor();
            p1.DrawCursor();
            p2.DrawUnits();
            p1.DrawUnits();
        }

        public void DrawMobs()
        {
            elapsedTime += GetFrameTime();

            foreach (Mob fish in fish_list)
                if (elapsedTime >= fish.AnimationInterval)
                    fish.Draw();

            foreach (Mob pirate in pirate_list)
                pirate.Draw();
        }

        public void DrawWeather()
        {
            foreach(Weather weather in weather_list)
                weather.Draw();
        }

        public void DrawIslands()
        {
            DrawTextureRec(Program.sheet, new Rectangle(0, 323, 448, 285), new(90, 125), Color.WHITE);
            DrawTextureRec(Program.sheet, new Rectangle(557, 337, 371, 271), new(400, 125), Color.WHITE);
        }

        public void DrawGUI()
        {
            DrawTexture(game_gui, 0, 0, Color.WHITE);

            if (IsKeyPressed(KeyboardKey.KEY_G)) showInfo = !showInfo;

            if (showInfo)
                game_gui = LoadTexture("res/game_gui_info.png");

            else if (!showInfo && !ShowRoundScore)
            {
                game_gui = LoadTexture("res/game_gui.png");
                DrawTextPro(font, "    GOLD:   " + p1.Gold, new(140, 505), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "  CENSUS:   " + Math.Ceiling(p1.Population), new(136, 540), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "   SCORE:   " + Math.Ceiling(p1.Score), new(140, 575), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "    GOLD:   " + p2.Gold, new(532, 505), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "  CENSUS:   " + Math.Ceiling(p2.Population), new(528, 540), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "   SCORE:   " + Math.Ceiling(p2.Score), new(532, 575), new(0, 0), 0, 32, 0, Color.RAYWHITE);

                if (!MuteAudio) DrawTextPro(font, "Press 'M' to mute audio!", new(595, 614), new(0, 0), 0, 16, 0, new(115, 111, 166, 255));
                else if (MuteAudio) DrawTextPro(font, "Press 'M' to unmute audio!", new(595, 614), new(0, 0), 0, 16, 0, new(115, 111, 166, 255));
            }
            
            else
            {
                game_gui = LoadTexture("res/game_gui.png");
                DrawTextPro(font, "    GOLD:   " + p1.Gold, new(140, 505), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "  CENSUS:   " + Math.Ceiling(p1.Population), new(136, 540), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "   SCORE:  +" + Math.Ceiling(p1.PrevRoundScore), new(140, 575), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "    GOLD:   " + p2.Gold, new(532, 505), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "  CENSUS:   " + Math.Ceiling(p2.Population), new(528, 540), new(0, 0), 0, 32, 0, Color.RAYWHITE);
                DrawTextPro(font, "   SCORE:  +" + Math.Ceiling(p2.PrevRoundScore), new(532, 575), new(0, 0), 0, 32, 0, Color.RAYWHITE);

                if (!MuteAudio) DrawTextPro(font, "Press 'M' to mute audio!", new(595, 614), new(0, 0), 0, 16, 0, new(115, 111, 166, 255));
                else if (MuteAudio) DrawTextPro(font, "Press 'M' to unmute audio!", new(595, 614), new(0, 0), 0, 16, 0, new(115, 111, 166, 255));
            }
        }

        public void DrawRoundClock()
        {
            DrawTextPro(font, "ROUND " + CurrentRound + " of " + RoundAmount, new(43, 12), new(0, 0), 0, 48, 0, new(203, 219, 252, 255));
            DrawTextPro(font, "TIME LEFT: " + Math.Ceiling(RoundTime), new(500, 12), new(0, 0), 0, 48, 0, new(203, 219, 252, 255));
        }

        void LoadResources()
        {
            sfx_round_end = LoadMusicStream("res/audio/ROUND_END.wav");
            SetMusicVolume(sfx_round_end, 0.05f);
            sfx_round_end.looping = false;

            sfx_hurricane_destroy = LoadMusicStream("res/audio/HURRICANE_DESTROY.wav");
            SetMusicVolume(sfx_hurricane_destroy, 0.2f);
            sfx_hurricane_destroy.looping = false;

            sfx_rebel_destroy = LoadMusicStream("res/audio/STORM_DESTROY.wav");
            SetMusicVolume(sfx_rebel_destroy, 0.2f);
            sfx_rebel_destroy.looping = false;

            sfx_storm_destroy = LoadMusicStream("res/audio/STORM_DESTROY.wav");
            SetMusicVolume(sfx_storm_destroy, 0.1f);
            sfx_storm_destroy.looping = false;

            sfx_boat_destroy = LoadMusicStream("res/audio/BOAT_DESTROY.wav");
            SetMusicVolume(sfx_boat_destroy, 0.1f);
            sfx_boat_destroy.looping = false;

            sfx_rain_crops = LoadMusicStream("res/audio/RAIN_CROPS.wav");
            SetMusicVolume(sfx_rain_crops, 0.05f);
            sfx_rain_crops.looping = false;

            sfx_fish1 = LoadMusicStream("res/audio/FISH_1.wav");
            SetMusicVolume(sfx_fish1, 0.05f);
            sfx_fish1.looping = false;

            sfx_fish2 = LoadMusicStream("res/audio/FISH_2.wav");
            SetMusicVolume(sfx_fish2, 0.05f);
            sfx_fish2.looping = false;

            font = LoadFont("res/font/04B_03.TTF");
            game_gui = LoadTexture("res/game_gui.png");
        }

        public void UnloadTextures()
        {
            UnloadTexture(game_gui);
        }

        public void UnloadAudio()
        {
            UnloadMusicStream(sfx_round_end);
            UnloadMusicStream(sfx_hurricane_destroy);
            UnloadMusicStream(sfx_storm_destroy);
            UnloadMusicStream(sfx_rebel_destroy);
            UnloadMusicStream(sfx_boat_destroy);
            UnloadMusicStream(sfx_rain_crops);
            UnloadMusicStream(sfx_fish1);
            UnloadMusicStream(sfx_fish2);
        }
    }
}