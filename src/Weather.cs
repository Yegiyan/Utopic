using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class Weather
    {
        public string Type { get; private set; }
        public Vector2 Position { get; private set; }
        public Rectangle Collider { get; private set; }
        public float TimeToLive { get; private set; }

        readonly TimeSpan cloud_interval;
        readonly Stopwatch cloud_time;
        Vector2 cloud_frame;

        readonly TimeSpan storm_inverval;
        readonly Stopwatch storm_time;
        Vector2 storm_frame;

        readonly TimeSpan hurricane_interval;
        readonly Stopwatch hurricane_time;
        Vector2 hurricane_frame;

        Music sfx_hurricane_spawn;

        float current_speed;

        float vel_update_time;
        float vel_update_interval;
        float min_vel;
        float max_vel;

        Vector2 target_pos;
        Vector2 despawn_pos;

        bool isFirstRoll;
        bool isDespawning;
        Random rand;

        float elapsedTime;
        int spawnTime;

        public Weather()
        {
            cloud_time = new();
            cloud_interval = TimeSpan.FromSeconds(0.04f);
            cloud_frame = new(241, 1);

            storm_time = new();
            storm_inverval = TimeSpan.FromSeconds(0.04f);
            storm_frame = new(241, 64);

            hurricane_time = new();
            hurricane_interval = TimeSpan.FromSeconds(0.15f);
            hurricane_frame = new(448, 128);

            sfx_hurricane_spawn = LoadMusicStream("res/audio/HURRICANE_SPAWN.wav");
            SetMusicVolume(sfx_hurricane_spawn, 0.2f);
            sfx_hurricane_spawn.looping = false;

            isFirstRoll = true;
            isDespawning = false;
            rand = new();

            elapsedTime = 0.0f;
            spawnTime = rand.Next(0, 8);

            vel_update_time = 0.0f;
            vel_update_interval = 10.0f;
            min_vel = 10.0f;
            max_vel = 40.0f;

            Spawn();
        }

        public void Spawn()
        {
            RollWeather();

            Position = new Vector2(rand.Next(0, GetScreenWidth()), 0);
            TimeToLive = rand.Next(5, 25);
            current_speed = (float)(rand.NextDouble() * (max_vel - min_vel) + min_vel);

            SetRandomTarget();
            SetDespawnPoint();

            if (Type == "HURRICANE") 
                PlayMusicStream(sfx_hurricane_spawn);
        }

        private void RollWeather()
        {
            double chance = rand.NextDouble();

            if (isFirstRoll)
            {
                Type = "CLOUD";
                isFirstRoll = false;
                return;
            }

            if (chance <= 0.74f)
                Type = "CLOUD";

            else if (chance >= 0.75f && chance <= 0.97f)
                Type = "STORM";

            else if (chance >= 0.98f && chance <= 0.99f)
                Type = "HURRICANE";
        }

        public void Update(float deltaTime)
        {
            elapsedTime += deltaTime;
            if (elapsedTime <= spawnTime)
                return;

            TimeToLive -= deltaTime;
            vel_update_time += deltaTime;

            if (vel_update_time >= vel_update_interval)
            {
                current_speed = (float)(rand.NextDouble() * (max_vel - min_vel) + min_vel);
                vel_update_time = 0;
            }

            if (isDespawning)
            {
                MoveToDespawnPoint(deltaTime);
                return;
            }

            if (TimeToLive <= 0)
            {
                isDespawning = true;
                return;
            }

            Vector2 direction = Vector2.Subtract(target_pos, Position);
            float distance = direction.Length();

            if (distance < current_speed * deltaTime)
                SetRandomTarget();

            else
            {
                direction = Vector2.Normalize(direction);
                Position = Vector2.Add(Position, direction * (current_speed * deltaTime));
            }
        }

        private void SetRandomTarget()
        {
            target_pos = new Vector2(rand.Next(0, GetScreenWidth()), rand.Next(0, GetScreenHeight()));
        }

        private void MoveToDespawnPoint(float deltaTime)
        {
            Vector2 direction = Vector2.Subtract(despawn_pos, Position);
            float distance = direction.Length();

            if (distance < current_speed * deltaTime)
            {
                isDespawning = false;
                Despawn();
            }

            else
            {
                direction = Vector2.Normalize(direction);
                Position = Vector2.Add(Position, direction * (current_speed * deltaTime));
            }
        }

        private void SetDespawnPoint()
        {
            int despawn_dir = rand.Next(0, 4);

            switch (despawn_dir)
            {
                case 0:
                    despawn_pos = new(rand.Next(85, 775), 5);   // resetpoint NORTH '0'
                    break;
                case 1:
                    despawn_pos = new(rand.Next(85, 775), 500); // resetpoint SOUTH '1'
                    break;
                case 2:
                    despawn_pos = new(875, rand.Next(5, 500));  // resetpoint EAST  '2'
                    break;
                case 3:
                    despawn_pos = new(-10, rand.Next(5, 500));  // resetpoint WEST  '3'
                    break;
                default:
                    Debug.WriteLine("Error! No despawn position chosen!");
                    break;
            }
        }

        public void Despawn()
        {
            Position = new Vector2(rand.Next(0, GetScreenWidth()), -GetScreenHeight());
            Spawn();
        }

        public void Draw()
        {
            //DrawCollisionBoxes();
            UpdateMusicStream(sfx_hurricane_spawn);

            switch (Type)
            {
                case "CLOUD":
                    DrawTextureRec(Program.sheet, new Rectangle(cloud_frame.X, cloud_frame.Y, 48, 48), Position, Color.WHITE);
                    Collider = new Rectangle(Position.X, Position.Y + 18, 48, 34);
                    if (AnimateCloud() && !Game.IsGameOver && !Game.IsGamePaused)
                        cloud_frame.X += 69;
                    if (cloud_frame.X == 931)
                        cloud_frame.X = 241;
                    break;
                case "STORM":
                    DrawTextureRec(Program.sheet, new Rectangle(storm_frame.X, storm_frame.Y, 48, 48), Position, Color.WHITE);
                    Collider = new Rectangle(Position.X, Position.Y + 18, 48, 34);
                    if (AnimateStorm() && !Game.IsGameOver && !Game.IsGamePaused)
                        storm_frame.X += 69;
                    if (storm_frame.X == 931)
                        storm_frame.X = 241;
                    break;
                case "HURRICANE":
                    DrawTextureRec(Program.sheet, new Rectangle(hurricane_frame.X, hurricane_frame.Y, 64, 64), Position, Color.WHITE);
                    Collider = new Rectangle(Position.X, Position.Y, 65, 65);
                    if (AnimateHurricane() && !Game.IsGameOver && !Game.IsGamePaused)
                        hurricane_frame.X += 80;
                    if (hurricane_frame.X == 928)
                        hurricane_frame.X = 448;
                    break;
                default:
                    Debug.WriteLine("Error! Didn't spawn an environment!");
                    break;
            }
        }

        private void DrawCollisionBoxes()
        {
            DrawRectangleLines((int)Collider.x, (int)Collider.y, (int)Collider.width, (int)Collider.height, Color.DARKGRAY);
        }

        private bool AnimateCloud()
        {
            if (cloud_time.IsRunning && cloud_time.Elapsed < cloud_interval) return false;
            try { return true; }
            finally { cloud_time.Restart(); }
        }

        private bool AnimateStorm()
        {
            if (storm_time.IsRunning && storm_time.Elapsed < storm_inverval) return false;
            try { return true; }
            finally { storm_time.Restart(); }
        }

        private bool AnimateHurricane()
        {
            if (hurricane_time.IsRunning && hurricane_time.Elapsed < hurricane_interval) return false;
            try { return true; }
            finally { hurricane_time.Restart(); }
        }
    }
}