using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class Menu
    {
        public bool IsGameStarted { get; set; }

        public int Button { get; set; }
        public int PlayButton { get; set; }
        public int PlayButtonOption { get; set; }
        public int HowToButton { get; set; }

        public static int PlayerOne { get; set; }
        public static int PlayerTwo { get; set; }

        public static int FishCount { get; set; }
        public static int PirateCount { get; set; }
        public static int WeatherCount { get; set; }
        public static int GoldCount { get; set; }

        public static int TermOfOffice { get; set; }
        public static int TurnLength { get; set; }

        string[][] play_button_option = 
        {
            new string[] { "HUMAN", "CPU" },
            new string[] { "HUMAN", "CPU" },
            new string[] { "0", "1", "2", "3", "4", "5", "6" }, // FISH COUNT
            new string[] { "0", "1", "2", "3", "4", "5", "6" }, // PIRATE COUNT
            new string[] { "0", "1", "2", "3", "4", "5", "6" },  // WEATHER COUNT
        };

        Vector2[] play_button_pos = new Vector2[]
        {
            new Vector2(195, 65), // OPTION 1
            new Vector2(195, 165), // OPTION 2
            new Vector2(430, 265), // FISH COUNT
            new Vector2(430, 315), // PIRATE COUNT
            new Vector2(430, 365), // WEATHER COUNT
            new Vector2(430, 415), // GOLD COUNT
            new Vector2(440, 515), // TERM OF OFFICE
            new Vector2(440, 565), // TURN LENGTH
        };

        int[] play_button_selected;

        public enum MENU { MAIN, PLAY, HOWTOPLAY, ABOUT, SCORES }
        public MENU STATE;

        Shader shader_menu;

        bool playScoreAudio;
        Music sfx_scores;

        Image img_play;
        Texture2D texture_play;

        Image img_about;
        Texture2D texture_about;

        Image img_howtoplay1;
        Texture2D texture_howtoplay1;

        Image img_howtoplay2;
        Texture2D texture_howtoplay2;

        Image img_howtoplay3;
        Texture2D texture_howtoplay3;

        RenderTexture2D text_render;
        Font font;

        float keyPressTime;
        float elapsedTime;
        float enterDelay;

        public Menu()
        {
            sfx_scores = LoadMusicStream("res/audio/ROUND_END.wav");
            SetMusicVolume(sfx_scores, 0.08f);
            sfx_scores.looping = false;

            STATE = MENU.MAIN;

            IsGameStarted = false;

            PlayButtonOption = 0;
            HowToButton = 0;
            Button = 0;

            playScoreAudio = true;

            keyPressTime = 0;
            elapsedTime = 0;
            enterDelay = 0;

            img_play = LoadImage("res/menu/Play.png");
            texture_play = LoadTextureFromImage(img_play);

            img_about = LoadImage("res/menu/About.png");
            texture_about = LoadTextureFromImage(img_about);

            img_howtoplay1 = LoadImage("res/menu/HowToPlay1.png");
            texture_howtoplay1 = LoadTextureFromImage(img_howtoplay1);

            img_howtoplay2 = LoadImage("res/menu/HowToPlay2.png");
            texture_howtoplay2 = LoadTextureFromImage(img_howtoplay2);

            img_howtoplay3 = LoadImage("res/menu/HowToPlay3.png");
            texture_howtoplay3 = LoadTextureFromImage(img_howtoplay3);

            shader_menu = LoadShader(null, "res/shader/MenuFilter.frag");
            text_render = LoadRenderTexture(GetScreenWidth(), GetScreenHeight());
            font = LoadFont("res/font/IntellivisionBold.TTF");
        }

        public void Update(Game game)
        {
            BeginTextureMode(text_render);
            ClearBackground(Color.BLACK);
            
            Color ISLAND_BROWN = new(230, 206, 128, 255);

            Input();

            if (IsKeyPressed(KeyboardKey.KEY_ESCAPE) && STATE != MENU.MAIN && STATE != MENU.SCORES)
                STATE = MENU.MAIN;

            if (IsKeyPressed(KeyboardKey.KEY_ESCAPE) && STATE == MENU.SCORES && elapsedTime > 6)
                STATE = MENU.MAIN;

            switch (STATE)
            {
                case MENU.MAIN:
                    play_button_selected = new int[] { 0, 1, 2, 2, 2, 100, 10, 60 };
                    FishCount = play_button_selected[2];
                    PirateCount = play_button_selected[3];
                    WeatherCount = play_button_selected[4];
                    GoldCount = play_button_selected[5];
                    TermOfOffice = play_button_selected[6];
                    TurnLength = play_button_selected[7];

                    PlayButtonOption = 0;
                    enterDelay = 0;

                    DrawTextPro(font, "UTOPIC", new(195, 75), new(0, 0), 0, 84, 0, ISLAND_BROWN);
                    string[] main_button_type = { "PLAY", "HOW TO PLAY", "ABOUT", "EXIT" };
                    Vector2[] main_button_pos = new Vector2[]
                    {
                        new Vector2(345, 250), // PLAY
                        new Vector2(247, 300), // HOW TO PLAY
                        new Vector2(330, 350), // ABOUT
                        new Vector2(350, 400)  // EXIT
                    };

                    for (int i = 0; i < main_button_pos.Length; i++)
                    {
                        Color textColor = i == Button ? Color.RAYWHITE : Color.GRAY;
                        DrawTextPro(font, main_button_type[i], main_button_pos[i], Vector2.Zero, 0, 36, 0, textColor);
                    }
                    DrawTextPro(font, "2023", new(367, 565), new(0, 0), 0, 24, 0, ISLAND_BROWN);
                    DrawTextPro(font, "Recreated by Hike Yegiyan", new(165, 600), new(0, 0), 0, 24, 0, ISLAND_BROWN);
                    break;

                case MENU.PLAY:
                    elapsedTime = 0;
                    enterDelay += GetFrameTime();
                    playScoreAudio = true;

                    DrawTexture(texture_play, 0, 0, Color.WHITE);

                    DrawTextPro(font, "PLAYER TYPE", new(50, 17), new(0, 0), 0, 36, 0, ISLAND_BROWN);

                    DrawTextPro(font, "LEFT", new(50, 67), new(0, 0), 0, 24, 0, Color.GREEN);
                    DrawTextPro(font, "RIGHT", new(50, 167), new(0, 0), 0, 24, 0, Color.RED);

                    DrawTextPro(font, "VS", new(195, 115), new(0, 0), 0, 36, 0, Color.DARKGRAY);
                    for (int i = 0; i < 5; i++)
                    {
                        Color textColor = i == PlayButtonOption ? Color.RAYWHITE : Color.GRAY;
                        DrawTextPro(font, play_button_option[i][play_button_selected[i]], play_button_pos[i], Vector2.Zero, 0, 36, 0, textColor);
                    }

                    for (int i = 5; i < 8; i++)
                    {
                        Color textColor = i == PlayButtonOption ? Color.RAYWHITE : Color.GRAY;
                        DrawTextPro(font, play_button_selected[i].ToString(), play_button_pos[i], Vector2.Zero, 0, 36, 0, textColor);
                    }

                    DrawTextPro(font, "GAME RULES", new(50, 220), new(0, 0), 0, 36, 0, ISLAND_BROWN);
                    DrawTextPro(font, "FISH COUNT: ", new(136, 270), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);
                    DrawTextPro(font, "PIRATE COUNT: ", new(83, 320), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);
                    DrawTextPro(font, "WEATHER COUNT: ", new(50, 370), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);
                    DrawTextPro(font, "STARTING GOLD:", new(54, 420), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);

                    DrawTextPro(font, "GAME TIME", new(50, 470), new(0, 0), 0, 36, 0, ISLAND_BROWN);
                    DrawTextPro(font, "TERM OF OFFICE:", new(50, 520), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);
                    DrawTextPro(font, "TURN LENGTH:", new(119, 570), new(0, 0), 0, 30, 0, Color.LIGHTGRAY);

                    if (enterDelay > 1)
                        if (IsKeyPressed(KeyboardKey.KEY_ENTER) || IsKeyPressed(KeyboardKey.KEY_SPACE))
                            IsGameStarted = true;

                    DrawTextPro(font, "Press 'ENTER' to start", new(520, 610), new(0, 0), 0, 18, 0, ISLAND_BROWN);
                    break;

                case MENU.HOWTOPLAY:
                    if (HowToButton == 0) DrawTexture(texture_howtoplay1, 0, 0, Color.WHITE);
                    if (HowToButton == 1) DrawTexture(texture_howtoplay2, 0, 0, Color.WHITE);
                    if (HowToButton == 2) DrawTexture(texture_howtoplay3, 0, 0, Color.WHITE);
                    DrawTextPro(font, "HOW TO PLAY", new(155, 25), new(0, 0), 0, 60, 0, ISLAND_BROWN);
                    DrawTextPro(font, "Press 'escape' to go back", new(180, 600), new(0, 0), 0, 24, 0, ISLAND_BROWN);          
                    break;

                case MENU.ABOUT:
                    DrawTexture(texture_about, 0, 0, Color.WHITE);
                    DrawTextPro(font, "ABOUT", new(260, 50), new(0, 0), 0, 72, 0, ISLAND_BROWN);
                    DrawTextPro(font, "Press 'escape' to go back", new(180, 600), new(0, 0), 0, 24, 0, ISLAND_BROWN);
                    if (IsKeyPressed(KeyboardKey.KEY_ESCAPE)) STATE = MENU.MAIN;
                    break;

                case MENU.SCORES:
                    elapsedTime += GetFrameTime();
                    UpdateMusicStream(sfx_scores);

                    if (playScoreAudio && !Game.MuteAudio)
                    {
                        PlayMusicStream(sfx_scores);
                        playScoreAudio = false;
                    }

                    DrawTextPro(font, "AFTERMATH", new(165, 15), new(0, 0), 0, 64, 0, ISLAND_BROWN);

                    int score_min = (int)game.TotalTime / 60;
                    int score_sec = (int)game.TotalTime % 60;
                    string score_time = string.Format("{0:00}:{1:00}", score_min, score_sec);
                    DrawTextPro(font, "" + score_time, new(373, 105), new(0, 0), 0, 24, 0, Color.BEIGE);

                    DrawTextPro(font, "PLAYER 1", new(50, 165), new(0, 0), 0, 36, 0, Color.GREEN);
                    DrawTextPro(font, "PLAYER 2", new(555, 165), new(0, 0), 0, 36, 0, Color.RED);

                    if (elapsedTime > 2)
                    {
                        DrawTextPro(font, "G.D.P", new(375, 265), new(0, 0), 0, 24, 0, Color.GRAY);
                        DrawTextPro(font, "" + Math.Ceiling(game.p1.GDP), new(75, 265), new(0, 0), 0, 24, 0, Color.WHITE);
                        DrawTextPro(font, "" + Math.Ceiling(game.p2.GDP), new(675, 265), new(0, 0), 0, 24, 0, Color.WHITE);

                        DrawTextPro(font, "POPULATION", new(315, 335), new(0, 0), 0, 24, 0, Color.GRAY);
                        DrawTextPro(font, "" + Math.Ceiling(game.p1.Population), new(75, 334), new(0, 0), 0, 24, 0, Color.WHITE);
                        DrawTextPro(font, "" + Math.Ceiling(game.p2.Population), new(675, 334), new(0, 0), 0, 24, 0, Color.WHITE);

                        DrawTextPro(font, "REBELLIONS", new(315, 400), new(0, 0), 0, 24, 0, Color.GRAY);
                        DrawTextPro(font, "" + Math.Ceiling(game.p1.TotalRebels), new(75, 400), new(0, 0), 0, 24, 0, Color.WHITE);
                        DrawTextPro(font, "" + Math.Ceiling(game.p2.TotalRebels), new(675, 400), new(0, 0), 0, 24, 0, Color.WHITE);
                    }

                    if (elapsedTime > 3.75f)
                    {
                        if ((int)game.p1.Score > (int)game.p2.Score)
                            DrawTextPro(font, "VICTOR", new(50, 200), new(0, 0), 0, 24, 0, Color.GOLD);
                        else if ((int)game.p1.Score < (int)game.p2.Score)
                            DrawTextPro(font, "VICTOR", new(555, 200), new(0, 0), 0, 24, 0, Color.GOLD);
                        else
                            DrawTextPro(font, "DRAW", new(376, 190), new(0, 0), 0, 24, 0, Color.PURPLE);

                        DrawTextPro(font, "FINAL SCORE", new(293, 490), new(0, 0), 0, 28, 0, Color.RAYWHITE);
                        DrawTextPro(font, "" + Math.Ceiling(game.p1.Score), new(65, 480), new(0, 0), 0, 40, 0, Color.WHITE);
                        DrawTextPro(font, "" + Math.Ceiling(game.p2.Score), new(665, 480), new(0, 0), 0, 40, 0, Color.WHITE);
                    }

                    if (elapsedTime > 6)
                        DrawTextPro(font, "Press 'escape' to go to main menu", new(110, 600), new(0, 0), 0, 24, 0, ISLAND_BROWN);

                    break;

                default:
                    Debug.WriteLine("Error! No menu loaded!");
                    break;
            }

            EndTextureMode();

            BeginShaderMode(shader_menu);
            SetShaderValue(shader_menu, GetShaderLocation(shader_menu, "resolution"), new Vector2(GetScreenWidth(), GetScreenHeight()), ShaderUniformDataType.SHADER_UNIFORM_VEC2);
            SetShaderValue(shader_menu, GetShaderLocation(shader_menu, "time"), new float[] { (float)GetTime() }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
            DrawTexture(text_render.texture, 0, 0, Color.WHITE);
            EndShaderMode();
        }

        void Input()
        {
            if (STATE == MENU.MAIN) 
            {
                if (IsKeyPressed(KeyboardKey.KEY_DOWN) || IsKeyPressed(KeyboardKey.KEY_S)) Button++;
                if (IsKeyPressed(KeyboardKey.KEY_UP) || IsKeyPressed(KeyboardKey.KEY_W)) Button--;

                if ((IsKeyPressed(KeyboardKey.KEY_ENTER) || IsKeyPressed(KeyboardKey.KEY_SPACE)) && Button == 0)
                    STATE = MENU.PLAY;

                if ((IsKeyPressed(KeyboardKey.KEY_ENTER) || IsKeyPressed(KeyboardKey.KEY_SPACE)) && Button == 1)
                    STATE = MENU.HOWTOPLAY; 

                if ((IsKeyPressed(KeyboardKey.KEY_ENTER) || IsKeyPressed(KeyboardKey.KEY_SPACE)) && Button == 2)
                    STATE = MENU.ABOUT;

                if ((IsKeyPressed(KeyboardKey.KEY_ENTER) || IsKeyPressed(KeyboardKey.KEY_SPACE)) && Button == 3)
                    System.Environment.Exit(1);

                if (Button > 3) Button = 0;
                if (Button < 0) Button = 3;
            }

            if (STATE == MENU.PLAY)
            {
                keyPressTime += GetFrameTime();

                if (IsKeyPressed(KeyboardKey.KEY_DOWN) || IsKeyPressed(KeyboardKey.KEY_S)) PlayButtonOption++;
                if (IsKeyPressed(KeyboardKey.KEY_UP) || IsKeyPressed(KeyboardKey.KEY_W)) PlayButtonOption--;

                if (IsKeyPressed(KeyboardKey.KEY_LEFT) || IsKeyPressed(KeyboardKey.KEY_A)) play_button_selected[PlayButtonOption]--;
                if (IsKeyPressed(KeyboardKey.KEY_RIGHT) || IsKeyPressed(KeyboardKey.KEY_D)) play_button_selected[PlayButtonOption]++;

                if (PlayButtonOption > 7) PlayButtonOption = 0;
                if (PlayButtonOption < 0) PlayButtonOption = 7;

                for (int i = 0; i < 5; i++)
                {
                    if (play_button_selected[i] >= play_button_option[i].Length) play_button_selected[i] = 0;
                    if (play_button_selected[i] < 0) play_button_selected[i] = play_button_option[i].Length - 1;
                }

                if (PlayButtonOption >= 5 && PlayButtonOption <= 7)
                {
                    if (IsKeyPressed(KeyboardKey.KEY_LEFT) || IsKeyPressed(KeyboardKey.KEY_A))
                        play_button_selected[PlayButtonOption] -= 4;

                    if (IsKeyPressed(KeyboardKey.KEY_RIGHT) || IsKeyPressed(KeyboardKey.KEY_D))
                        play_button_selected[PlayButtonOption] += 4;


                    if (PlayButtonOption == 5)
                    {
                        play_button_selected[5] = Math.Clamp(play_button_selected[5], 5, 1000);
                        GoldCount = play_button_selected[5];
                    }
                    else if (PlayButtonOption == 6)
                    {
                        play_button_selected[6] = Math.Clamp(play_button_selected[6], 5, 1000);
                        TermOfOffice = play_button_selected[6];
                    }
                    else if (PlayButtonOption == 7)
                    {
                        play_button_selected[7] = Math.Clamp(play_button_selected[7], 10, 1000);
                        TurnLength = play_button_selected[7];
                    }
                }

                PlayerOne = play_button_selected[0];
                PlayerTwo = play_button_selected[1];

                FishCount = int.Parse(play_button_option[2][play_button_selected[2]]);
                PirateCount = int.Parse(play_button_option[3][play_button_selected[3]]);
                WeatherCount = int.Parse(play_button_option[4][play_button_selected[4]]);
            }

            else if (STATE == MENU.HOWTOPLAY)
            {
                if (IsKeyPressed(KeyboardKey.KEY_LEFT) || IsKeyPressed(KeyboardKey.KEY_A))
                    HowToButton -= 1;
                if (IsKeyPressed(KeyboardKey.KEY_RIGHT) || IsKeyPressed(KeyboardKey.KEY_D))
                    HowToButton += 1;

                if (HowToButton > 2) HowToButton = 0;
                if (HowToButton < 0) HowToButton = 2;
            }
        }

        public void UnloadResources()
        {
            UnloadImage(img_about);
            UnloadTexture(texture_about);
            UnloadShader(shader_menu);
            UnloadRenderTexture(text_render);
            UnloadFont(font);
            UnloadMusicStream(sfx_scores);
        }
    }
}