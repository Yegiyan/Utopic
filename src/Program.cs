using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;

namespace Utopic.src
{
    class Program
    {
        public static readonly Texture2D sheet = LoadTexture("res/sprites.png");
        static Image icon = LoadImage("res/icon.png");

        const float FIXED_TIME_STEP = 1.0f / 60.0f; // 60 updates per second
        public static float accumulator = 0.0f;

        static bool prev_key_state;
        static Game game;
        static Font font;

        public static void Main()
        {
            InitWindow(860, 635, "Utopic");
            SetWindowIcon(icon);
            InitAudioDevice();
            SetTargetFPS(60);
            SetExitKey(0);

            Music sfx_game_over = LoadMusicStream("res/audio/GAME_OVER.wav");
            Menu menu = new();
            game = new();
            prev_key_state = false;

            SetMusicVolume(sfx_game_over, 0.3f);
            sfx_game_over.looping = false;
            bool playEndMusic = false;
            bool isNewGame = true;

            font = LoadFont("res/font/IntellivisionBold.TTF");
            float fade_opacity = 0;
            float elapsedTime = 0;

            Color WATER = new(61, 63, 233, 255);

            while (!WindowShouldClose())
            {
                BeginDrawing();

                if (IsKeyPressedWithBuffer(KeyboardKey.KEY_ESCAPE))
                {
                    if (!Game.IsGamePaused && !Game.IsGameOver)
                        Game.IsGamePaused = true;

                    else
                        Game.IsGamePaused = false;
                }

                if (menu.IsGameStarted)
                {
                    ClearBackground(WATER);
                    if (isNewGame)
                    {
                        game.NewGame();
                        isNewGame = false;
                    }

                    game.DrawIslands();
                    game.DrawMobs();
                    game.DrawPlayers();

                    if (!Game.IsGameOver && !Game.IsGamePaused)
                        UpdateLogic();

                    game.DrawWeather();
                    game.DrawGUI();
                    game.DrawRoundClock();

                    if (Game.IsGamePaused)
                    {
                        DrawRectangle(0, 0, GetScreenWidth(), GetScreenHeight(), Fade(Color.BLACK, 0.5f));
                        DrawTextPro(font, "PAUSED", new(255, 225), new(0, 0), 0, 60, 0, Color.WHITE);
                        DrawTextPro(font, "Press 'Q' to quit", new(259, 285), new(0, 0), 0, 24, 0, Color.WHITE);

                        if (IsKeyPressed(KeyboardKey.KEY_Q))
                        {
                            Game.IsGamePaused = false;
                            Game.IsGameOver = true;
                        }
                    }

                    if (Game.IsGameOver && menu.STATE != Menu.MENU.SCORES)
                    {
                        elapsedTime += GetFrameTime();
                        if (elapsedTime > 9f)
                        {
                            menu.STATE = Menu.MENU.SCORES;
                            menu.IsGameStarted = false;
                        }

                        UpdateMusicStream(sfx_game_over);
                        if (!playEndMusic && !Game.MuteAudio)
                        {
                            PlayMusicStream(sfx_game_over);
                            playEndMusic = true;
                        }

                        fade_opacity += 0.002f;
                        fade_opacity = Math.Clamp(fade_opacity, 0.0f, 1.0f);
                        DrawRectangle(0, 0, GetScreenWidth(), GetScreenHeight(), new Color(0, 0, 0, (int)(fade_opacity * 255)));
                    }
                }

                else
                {
                    menu.Update(game);

                    Game.IsGameOver = false;
                    playEndMusic = false;
                    isNewGame = true;

                    elapsedTime = 0;
                    fade_opacity = 0;
                }    

                EndDrawing();
                game.UnloadTextures();
            }

            game.UnloadAudio();
            menu.UnloadResources();
            UnloadFont(font);

            CloseAudioDevice();
            CloseWindow();
        }

        static void UpdateLogic()
        {
            float deltaTime = GetFrameTime();
            accumulator += deltaTime;

            while (accumulator >= FIXED_TIME_STEP)
            {
                game.Gameplay(FIXED_TIME_STEP);
                accumulator -= FIXED_TIME_STEP;
            }
        }

        static bool IsKeyPressedWithBuffer(KeyboardKey key)
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
    }
}