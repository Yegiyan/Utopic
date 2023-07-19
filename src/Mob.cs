using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class Mob
    {
        private readonly TimeSpan fish_interval;
        private readonly Stopwatch fish_time;
        private Vector2 fish_frame;

        public float AnimationInterval { get; set; }
        public Rectangle Collider { get; set; }
        public Vector2 Velocity { get; set; }
        public float Speed { get; set; }
        public string Type { get; set; }

        public bool IsFortBlocking { get; set; }
        public bool IsPTBlocking { get; set; }
        public bool IsDestroyed { get; set; }

        public Vector2 Position;

        public static Rectangle center_weight_col;

        float elapsedTime;
        int spawnTime;

        private Random rand;
        Environment env;

        public Mob(string type)
        {
            rand = new();

            fish_time = new();
            fish_interval = TimeSpan.FromSeconds(0.5f);
            RollFirstFishFrame();

            Type = type;
            Velocity = MoveRandomDirection();
            Speed = 3f;

            IsFortBlocking = false;
            IsPTBlocking = false;

            env = new();
            center_weight_col = new Rectangle(250, 155, 325, 200);

            elapsedTime = 0.0f;
            spawnTime = rand.Next(1, 8);
            RollSpawn();
        }

        private void RollSpawn()
        {
            Vector2[] spawn_points = new Vector2[]
            {
                new Vector2(5, 85),    // left top
                new Vector2(5, 405),   // left bot
                new Vector2(835, 80),  // right top
                new Vector2(835, 415), // right bot

                new Vector2(5, 225),   // left topmid
                new Vector2(5, 375),   // left botmid
                new Vector2(835, 225), // right topmid
                new Vector2(835, 375), // right botmid
            };

            int point = rand.Next(spawn_points.Length);
            Position = spawn_points[point];
        }

        private Vector2 MoveRandomDirection()
        {
            Vector2 direction = new Vector2((float)(rand.NextDouble() * 2 - 1), (float)(rand.NextDouble() * 2 - 1));
            Vector2.Normalize(direction);
            return direction;
        }

        public void Update(float deltaTime)
        {
            //DrawCollisionBoxes();

            if (IsPTBlocking)
                return;

            if (IsDestroyed)
            {
                RollSpawn();
                IsDestroyed = false;
            }

            float min_speed = 0.1f;
            float max_speed = 5.0f;

            float dir_change_time = 0.0f;
            float speed_change_time = 0.0f;
            float move_center_time = 0.0f;

            float dir_change_threshold = 5.0f;
            float speed_change_threshold = 3.50f;
            float move_center_threshold = 10.0f;

            dir_change_time += deltaTime;
            speed_change_time += deltaTime;
            move_center_time += deltaTime;

            elapsedTime += deltaTime;
            if (elapsedTime <= spawnTime)
                return;

            if (dir_change_time >= dir_change_threshold)
            {
                // 0.2% chance to change direction per second
                if (rand.NextDouble() < 0.002f * deltaTime)
                {
                    Velocity = new Vector2((float)(rand.NextDouble() * 2 - 1), (float)(rand.NextDouble() * 2 - 1));
                    Vector2.Normalize(Velocity);
                }
                dir_change_time = 0.0f;
            }

            if (speed_change_time >= speed_change_threshold)
            {
                if (rand.NextDouble() < 0.003f * deltaTime)
                    Speed = min_speed + (float)(rand.NextDouble() * (max_speed - min_speed));
                speed_change_time = 0.0f;
            }

            if (move_center_time >= move_center_threshold)
            {
                // 0.1% chance to move towards the center of the map per second
                if (rand.NextDouble() < 0.001f * deltaTime && !CheckCollisionRecs(center_weight_col, Collider))
                {
                    Vector2 center = new Vector2(550, 265);
                    Vector2 dir_to_center = Vector2.Subtract(center, Position);
                    Vector2.Normalize(dir_to_center);
                    Velocity = dir_to_center;
                }
                move_center_time = 0.0f;
            }

            Vector2 mob_pos = Position;
            mob_pos.X += Velocity.X * Speed * deltaTime;
            mob_pos.Y += Velocity.Y * Speed * deltaTime;
            Position = mob_pos;

            Teleport();

            if (CheckIslandCollision(Collider))
            {
                Vector2 new_mob_vel = new Vector2((float)(rand.NextDouble() * 2 - 1), (float)(rand.NextDouble() * 2 - 1));
                Vector2.Normalize(new_mob_vel);

                while (CheckIslandCollision(new Rectangle(Position.X + new_mob_vel.X * Speed, Position.Y + new_mob_vel.Y * Speed, Collider.width, Collider.height)))
                {
                    new_mob_vel = new Vector2((float)(rand.NextDouble() * 2 - 1), (float)(rand.NextDouble() * 2 - 1));
                    Vector2.Normalize(new_mob_vel);
                }

                Velocity = new_mob_vel;
            }

            float length = (float)Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
            Vector2 mob_vel = Velocity;
            mob_vel.X = (Velocity.X / length) * Speed;
            mob_vel.Y = (Velocity.Y / length) * Speed;
            Velocity = mob_vel;
        }

        private void Teleport()
        {
            float mob_width = Collider.width;
            float mob_height = Collider.height;

            Rectangle top_col = Environment.env_boundary_cols[0];
            Rectangle bot_col = Environment.env_boundary_cols[1];
            Rectangle left_col = Environment.env_boundary_cols[2];
            Rectangle right_col = Environment.env_boundary_cols[3];

            Vector2 mob_pos = Position;

            if (CheckCollisionRecs(left_col, Collider))
                mob_pos.X = right_col.x - mob_width;

            else if (CheckCollisionRecs(right_col, Collider))
                mob_pos.X = left_col.x + left_col.width;

            if (CheckCollisionRecs(top_col, Collider))
                mob_pos.Y = bot_col.y - mob_height;

            else if (CheckCollisionRecs(bot_col, Collider))
                mob_pos.Y = top_col.y + top_col.height;

            Position = mob_pos;
        }

        bool CheckIslandCollision(Rectangle collider)
        {
            for (int i = 0; i < Environment.env_island_cols.Count; i++)
                if (CheckCollisionRecs(Environment.env_island_cols.ElementAt(i), collider))
                    return true;

            for (int i = 0; i < Environment.env_dock_cols.Count; i++)
                if (CheckCollisionRecs(Environment.env_dock_cols.ElementAt(i), collider))
                    return true;

            return false;
        }

        public bool CheckFortCollision(List<Building> player_buildings)
        {
            foreach (Building fort in player_buildings)
                if (CheckCollisionRecs(fort.FortCollider, Collider) && Type == "PIRATE")
                    return true;

            return false;
        }

        public bool CheckPTBoatCollision(List<Boat> player_boats)
        {
            foreach (Boat boat in player_boats)
                if (CheckCollisionRecs(boat.Collider, Collider) && boat.Type == "PT_BOAT" && Type == "PIRATE")
                    return true;
                    
            return false;
        }

        public void Draw()
        {
            if (Type == "FISH")
            {
                DrawTextureRec(Program.sheet, new Rectangle(fish_frame.X, fish_frame.Y, 40, 32), Position, Color.WHITE);
                Collider = new Rectangle(Position.X, Position.Y, 32, 32);
                if (Animate() && !Game.IsGameOver && !Game.IsGamePaused)
                    fish_frame.X += 40;
                if (fish_frame.X == 160)
                    fish_frame.X = 0;
            }

            if (Type == "PIRATE")
            {
                if (Velocity.X > 0)
                    DrawTextureRec(Program.sheet, new Rectangle(97, 28, 23, 20), Position, Color.WHITE);
                if (Velocity.X < 0)
                    DrawTextureRec(Program.sheet, new Rectangle(97, 28, -23, 20), Position, Color.WHITE);
                Collider = new Rectangle(Position.X, Position.Y, 23, 20);
            }
        }

        private void DrawCollisionBoxes()
        {
            DrawRectangleLines((int)Collider.x, (int)Collider.y, (int)Collider.width, (int)Collider.height, Color.BLACK);
            //DrawRectangleLines((int)center_weight_col.x, (int)center_weight_col.y, (int)center_weight_col.width, (int)center_weight_col.height, Color.BLACK);
        }

        private void RollFirstFishFrame()
        {
            double chance = rand.Next(0, 4);
            switch (chance)
            {
                case 0:
                    fish_frame = new(0, 64);
                    break;
                case 1:
                    fish_frame = new(40, 64);
                    fish_frame.X = 40;
                    break;
                case 2:
                    fish_frame = new(80, 64);
                    fish_frame.X = 80;
                    break;
                case 3:
                    fish_frame = new(120, 64);
                    fish_frame.X = 120;
                    break;
                default:
                    Debug.WriteLine("Error! No fish animation interval chosen!");
                    break;
            }
        }

        private bool Animate()
        {
            if (fish_time.IsRunning && fish_time.Elapsed < fish_interval) return false;
            try { return true; }
            finally { fish_time.Restart(); }
        }
    }
}