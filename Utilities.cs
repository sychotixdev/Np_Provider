using System.Collections.Generic;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Np_Provider
{
    public static class Utilities
    {
        public static Dictionary<string, int> buttonNames = new Dictionary<string, int>()
        {
            {"A", 0 },
            {"B", 1 },
            {"X", 2 },
            {"Y", 3 },
            {"LB", 4 },
            {"RB", 5 },
            {"LS", 6 },
            {"RS", 7 },
            {"Back", 8 },
            {"Start", 9 },
        };

        public static Dictionary<string, int> povNames = new Dictionary<string, int>()
        {
            {"Up", 10 },
            {"Right", 11 },
            {"Down", 12 },
            {"Left", 13 }
        };

        public static Dictionary<string, int> axisNames = new Dictionary<string, int>()
        {
            {"LX", 0 },
            {"LY", 1 },
            {"RX", 2 },
            {"RY", 3 },
            {"LT", 4 },
            {"RT", 5 },
        };
    }
}
