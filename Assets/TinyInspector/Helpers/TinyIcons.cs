using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector
{
    public enum TinyIcon
    {
        None = 0,
        Add = 1,
        Airplane = 2,
        Alert = 3,
        AlignBottom = 4,
        AlignHorizontal = 5,
        AlignLeft = 6,
        AlignRight = 7,
        AlignTop = 8,
        AlignVertical = 9,
        Annoucment = 10,
        Apple = 11,
        ArrowBottom = 12,
        ArrowBottomLeft = 13,
        ArrowBottomRight = 14,
        ArrowLeft = 15,
        ArrowRight = 16,
        ArrowTop = 17,
        ArrowTopLeft = 18,
        ArrowTopRight = 19,
        Audio = 20,
        Backpack = 21,
        Bars = 22,
        Battery = 23,
        Bell = 24,
        Bold = 25,
        Bookmark = 26,
        Bot = 27,
        Box = 28,
        Brush = 29,
        Bug = 30,
        Calendar = 31,
        Camera = 32,
        Car = 33,
        Cart = 34,
        Chat = 35,
        Check = 36,
        Checked = 37,
        ChevronDown = 38,
        ChevronUp = 39,
        Chip = 40,
        Circle = 41,
        Clock = 42,
        Cloth = 43,
        Clover = 44,
        Cloud = 45,
        Code = 46,
        Collapse = 47,
        ColorDropper = 48,
        Computer = 49,
        Console = 50,
        Conversation = 51,
        Cookie = 52,
        Copy = 53,
        Crown = 54,
        Cursor = 55,
        Data = 56,
        Database = 57,
        Delete = 58,
        Diamond = 59,
        Discount = 60,
        Dislike = 61,
        Download = 62,
        Drop = 63,
        Edit = 64,
        Eraser = 65,
        Error = 66,
        Expand = 67,
        Eye = 68,
        EyeOff = 69,
        File = 70,
        Filter = 71,
        Fire = 72,
        Flashlight = 73,
        Folder = 74,
        FolderOpen = 75,
        Forward = 76,
        Gallery = 77,
        Gamepad = 78,
        Gift = 79,
        GPU = 80,
        Grass = 81,
        Graph = 82,
        Grid = 83,
        Hand = 84,
        Heart = 85,
        Hexagon = 86,
        Hexagons = 87,
        Home = 88,
        Hourglass = 89,
        Image = 90,
        Info = 91,
        Italic = 92,
        Key = 93,
        Keyboard = 94,
        Lab = 95,
        Layers = 96,
        Leaf = 97,
        Light = 98,
        Like = 99,
        Link = 100,
        List = 101,
        Location = 102,
        Lock = 103,
        Mail = 104,
        Mana = 105,
        Map = 106,
        Material = 107,
        Microphone = 108,
        Money = 109,
        Monitor = 110,
        Moon = 111,
        More = 112,
        Mouse = 113,
        Move = 114,
        NoEntry = 115,
        Object = 116,
        Palette = 117,
        Paperclip = 118,
        Paste = 119,
        Pause = 120,
        Pet = 121,
        Phone = 122,
        Pin = 123,
        Planet = 124,
        Play = 125,
        PowerOff = 126,
        Question = 127,
        Quote = 128,
        Radioactive = 129,
        Rainy = 130,
        Reload = 131,
        Reward = 132,
        Rewind = 133,
        Road = 134,
        Rocket = 135,
        Rotate = 136,
        Ruler = 137,
        Save = 138,
        Scissors = 139,
        Script = 140,
        Search = 141,
        Send = 142,
        Settings = 143,
        Share = 144,
        Shield = 145,
        Skull = 146,
        Sliders = 147,
        Snow = 148,
        Speedometer = 149,
        Square = 150,
        Star = 151,
        Store = 152,
        Storm = 153,
        Strike = 154,
        Success = 155,
        Sun = 156,
        Tag = 157,
        Target = 158,
        Temperature = 159,
        Thunder = 160,
        Ticket = 161,
        Toolbox = 162,
        Tools = 163,
        Trash = 164,
        Tree = 165,
        Trophy = 166,
        Underline = 167,
        Unlock = 168,
        Upload = 169,
        User = 170,
        Users = 171,
        Wand = 172,
        Warning = 173,
        Wifi = 174,
        Wind = 175,
        Wrench = 176,
        WWW = 177,
        ZoomIn = 178,
        ZoomOut = 179
    }







    public static class TinyIcons
    {
        // Wczytuje teksturê z Resources
        public static Texture GetIcon(TinyIcon type)
        {
            string path = "TinyInspector/" + type;
            if (string.IsNullOrEmpty(path))
                return null;
            return Resources.Load<Texture>(path);
        }

        // Try to get icon by string: either enum name or Resources path
        public static Texture GetIcon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // Try parse as enum name
            try
            {
                if (Enum.TryParse<TinyIcon>(name, true, out var parsed))
                {
                    var tex = GetIcon(parsed);
                    if (tex != null) return tex;
                }
            }
            catch { }

            // Try load from Resources directly
            var res = Resources.Load<Texture>(name);
            return res;
        }
    }
}
