﻿using Fiero.Core;
using SFML.Graphics;

namespace Fiero.Business
{
    public static partial class Data
    {
        public static class UI
        {
            public static readonly GameDatum<int> TileSize = new(nameof(UI) + nameof(TileSize));
            public static readonly GameDatum<Coord> WindowSize = new(nameof(UI) + nameof(WindowSize));
            public static readonly GameDatum<Color> DefaultForeground = new(nameof(UI) + nameof(DefaultForeground));
            public static readonly GameDatum<Color> DefaultBackground = new(nameof(UI) + nameof(DefaultBackground));
            public static readonly GameDatum<Color> DefaultAccent = new(nameof(UI) + nameof(DefaultAccent));
        }

    }
}
