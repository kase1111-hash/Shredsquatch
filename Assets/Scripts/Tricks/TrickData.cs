using UnityEngine;

namespace Shredsquatch.Tricks
{
    public enum TrickType
    {
        None,
        // Spins
        Spin180,
        Spin360,
        Spin540,
        Spin720,
        Spin900,
        Spin1080,
        // Grabs
        NoseGrab,
        IndyGrab,
        MelonGrab,
        Stalefish,
        // Flips
        Frontflip,
        Backflip,
        DoubleFront,
        DoubleBack
    }

    public enum GrabType
    {
        None = 0,
        Nose = 1,
        Indy = 2,
        Melon = 3,
        Stalefish = 4
    }

    [System.Serializable]
    public class TrickDefinition
    {
        public TrickType Type;
        public string Name;
        public int BasePoints;
        public float MinAirtime;
        public bool RequiresRamp;

        public TrickDefinition(TrickType type, string name, int points, float minAir, bool ramp = false)
        {
            Type = type;
            Name = name;
            BasePoints = points;
            MinAirtime = minAir;
            RequiresRamp = ramp;
        }
    }

    public static class TrickDatabase
    {
        public static readonly TrickDefinition[] Spins = new TrickDefinition[]
        {
            new TrickDefinition(TrickType.Spin180, "180", 500, 0.5f),
            new TrickDefinition(TrickType.Spin360, "360", 1500, 1.0f),
            new TrickDefinition(TrickType.Spin540, "540", 3000, 1.5f),
            new TrickDefinition(TrickType.Spin720, "720", 5000, 2.0f),
            new TrickDefinition(TrickType.Spin900, "900", 8000, 2.5f),
            new TrickDefinition(TrickType.Spin1080, "1080", 12000, 3.0f)
        };

        public static readonly TrickDefinition[] Grabs = new TrickDefinition[]
        {
            new TrickDefinition(TrickType.NoseGrab, "Nose Grab", 300, 0.5f),
            new TrickDefinition(TrickType.IndyGrab, "Indy", 300, 0.5f),
            new TrickDefinition(TrickType.MelonGrab, "Melon", 300, 0.5f),
            new TrickDefinition(TrickType.Stalefish, "Stalefish", 300, 0.5f)
        };

        public static readonly TrickDefinition[] Flips = new TrickDefinition[]
        {
            new TrickDefinition(TrickType.Frontflip, "Frontflip", 2000, 1.5f, true),
            new TrickDefinition(TrickType.Backflip, "Backflip", 2000, 1.5f, true),
            new TrickDefinition(TrickType.DoubleFront, "Double Frontflip", 5000, 2.5f, true),
            new TrickDefinition(TrickType.DoubleBack, "Double Backflip", 5000, 2.5f, true)
        };

        public static TrickDefinition GetDefinition(TrickType type)
        {
            foreach (var trick in Spins)
                if (trick.Type == type) return trick;
            foreach (var trick in Grabs)
                if (trick.Type == type) return trick;
            foreach (var trick in Flips)
                if (trick.Type == type) return trick;
            return null;
        }

        public static string GetGrabName(GrabType grab)
        {
            return grab switch
            {
                GrabType.Nose => "Nose Grab",
                GrabType.Indy => "Indy",
                GrabType.Melon => "Melon",
                GrabType.Stalefish => "Stalefish",
                _ => ""
            };
        }
    }

    [System.Serializable]
    public class ActiveTrick
    {
        public TrickType Type;
        public float StartTime;
        public float Rotation;        // For spins
        public GrabType Grab;
        public float GrabStartTime;
        public float GrabHoldDuration;
        public bool IsFlipping;
        public float FlipRotation;
        public bool Completed;

        public void Reset()
        {
            Type = TrickType.None;
            StartTime = 0;
            Rotation = 0;
            Grab = GrabType.None;
            GrabStartTime = 0;
            GrabHoldDuration = 0;
            IsFlipping = false;
            FlipRotation = 0;
            Completed = false;
        }
    }
}
