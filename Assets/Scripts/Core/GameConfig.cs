namespace NoBall
{
    public static class GameConfig
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public const int LandscapeColumns = 40;
        public const int LandscapeRows = 24;
        public const float CellSize = 0.32f;

        public const int StartingAtoms = 2;
        public const int MaxAtoms = 50;
        public const float CaptureTarget = 0.75f;
        public const int LevelBaseScore = 500;
        public const float AtomSpeed = 3.4f;
        public const float WallCellsPerSecond = 16f;
        public const float SwipeMinPixels = 36f;

        public const int PixelsPerCell = 4;

        public static int CaptureTargetPercent => UnityEngine.Mathf.RoundToInt(CaptureTarget * 100f);

        public static int LevelMultiplier(int claimedPercent)
        {
            int extra = UnityEngine.Mathf.Max(0, claimedPercent - CaptureTargetPercent);
            return 1 + extra;
        }

        public static int ScoreForLevel(int claimedPercent)
        {
            return LevelBaseScore * LevelMultiplier(claimedPercent);
        }
    }

    public static class GameColors
    {
        public static readonly UnityEngine.Color CameraBg = Hex("070B14");
        public static readonly UnityEngine.Color Playfield = Hex("1E7A82");
        public static readonly UnityEngine.Color PlayfieldGrid = Hex("18656C");
        public static readonly UnityEngine.Color Captured = Hex("0B1220");
        public static readonly UnityEngine.Color Building = Hex("F0C14A");
        public static readonly UnityEngine.Color Preview = Hex("F0C14A", 0.28f);
        public static readonly UnityEngine.Color Frame = Hex("05080F");
        public static readonly UnityEngine.Color Title = Hex("F4F7FA");
        public static readonly UnityEngine.Color Muted = Hex("9AA8B5");
        public static readonly UnityEngine.Color ButtonPrimary = Hex("E24E3B");
        public static readonly UnityEngine.Color ButtonSecondary = Hex("243044");
        public static readonly UnityEngine.Color Panel = Hex("121A28");
        public static readonly UnityEngine.Color PanelBorder = Hex("2A3A4A");
        public static readonly UnityEngine.Color Hud = Hex("0B1220", 0.82f);
        public static readonly UnityEngine.Color AtomRed = Hex("D63B2F");
        public static readonly UnityEngine.Color AtomWhite = Hex("F6F3EA");
        public static readonly UnityEngine.Color AtomOutline = Hex("1A0E0C");

        static UnityEngine.Color Hex(string hex, float alpha = 1f)
        {
            UnityEngine.ColorUtility.TryParseHtmlString("#" + hex, out var color);
            color.a = alpha;
            return color;
        }
    }

    public enum GameBackgroundId
    {
        Classic = 0,
        Waterfall = 1,
        Laboratory = 2,
        Aquarium = 3,
        Aurora = 4
    }

    public static class GameBackgrounds
    {
        public const int Count = 5;

        public static string DisplayName(GameBackgroundId id) => id switch
        {
            GameBackgroundId.Waterfall => "Waterfall",
            GameBackgroundId.Laboratory => "Laboratory",
            GameBackgroundId.Aquarium => "Aquarium",
            GameBackgroundId.Aurora => "Aurora",
            _ => "Classic"
        };
    }

    public static class GameSettings
    {
        const string SoundKey = "noball.sound";
        const string BackgroundKey = "noball.background";

        public static event System.Action BackgroundChanged;
        public static event System.Action SoundChanged;

        public static bool SoundEnabled
        {
            get => UnityEngine.PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                UnityEngine.PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                UnityEngine.PlayerPrefs.Save();
                SoundChanged?.Invoke();
            }
        }

        public static bool ShowsBackground => Background != GameBackgroundId.Classic;

        public static GameBackgroundId Background
        {
            get
            {
                int value = UnityEngine.PlayerPrefs.GetInt(BackgroundKey, 0);
                if (value < 0 || value >= GameBackgrounds.Count)
                    return GameBackgroundId.Classic;
                return (GameBackgroundId)value;
            }
            set
            {
                UnityEngine.PlayerPrefs.SetInt(BackgroundKey, (int)value);
                UnityEngine.PlayerPrefs.Save();
                BackgroundChanged?.Invoke();
            }
        }
    }
}
