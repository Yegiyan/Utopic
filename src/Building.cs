using static Raylib_cs.Raylib;
using Raylib_cs;

using System.Diagnostics;
using System.Numerics;

namespace Utopic.src
{
    public class Building
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public Vector2 Position { get; set; }
        public Rectangle Collider { get; set; }
        public Rectangle FortCollider { get; set; }
        public Player Owner { get; set; }
        public bool IsTileOccupied { get; set; }
        public int DecayTime { get; set; }

        public Building(Player owner, string type, int id, Vector2 position, Rectangle collider)
        {
            Owner = owner;
            Type = type;
            ID = id;
            Position = position;
            Collider = collider;
            DecayTime = 0;
        }

        public void Buy(Player owner, string type)
        {
            if (!IsTileOccupied)
            {
                switch (type)
                {
                    case "CROP":
                        owner.Gold -= 3;
                        break;
                    case "REBEL":
                        owner.Gold -= 30;
                        break;
                    case "SCHOOL":
                        owner.Gold -= 35;
                        break;
                    case "FACTORY":
                        owner.Gold -= 40;
                        break;
                    case "FORT":
                        owner.Gold -= 50;
                        break;
                    case "HOUSE":
                        owner.Gold -= 60;
                        break;
                    case "HOSPITAL":
                        owner.Gold -= 75;
                        break;
                    default:
                        Debug.WriteLine("Invalid input! Didn't buy building!");
                        break;
                }
            }
            IsTileOccupied = false;
        }

        public static void Draw(string building, Vector2 position)
        {
            switch (building)
            {
                case "CROP":
                    DrawTextureRec(Program.sheet, new Rectangle(49, 1, 23, 23), position, Color.WHITE);
                    break;
                case "REBEL":
                    DrawTextureRec(Program.sheet, new Rectangle(145, 1, 23, 23), position, Color.WHITE);
                    break;
                case "SCHOOL":
                    DrawTextureRec(Program.sheet, new Rectangle(121, 1, 23, 23), position, Color.WHITE);
                    break;
                case "FACTORY":
                    DrawTextureRec(Program.sheet, new Rectangle(1, 1, 23, 23), position, Color.WHITE);
                    break;
                case "FORT":
                    DrawTextureRec(Program.sheet, new Rectangle(25, 1, 23, 23), position, Color.WHITE);
                    break;
                case "HOUSE":
                    DrawTextureRec(Program.sheet, new Rectangle(97, 1, 23, 23), position, Color.WHITE);
                    break;
                case "HOSPITAL":
                    DrawTextureRec(Program.sheet, new Rectangle(73, 1, 23, 23), position, Color.WHITE);
                    break;
                default:
                    Debug.WriteLine("Error! Didn't draw building!");
                    break;
            }
        }

        public override string ToString()
        {
            return $"Type: {Type}, ID: {ID}, Position: {Position}";
        }
    }
}