﻿namespace Fiero.Business
{
    public readonly struct TileDef
    {
        public readonly TileName Name;
        public readonly ColorName? Color;
        public readonly Coord Position;
        public TileDef(TileName name, Coord pos, ColorName? color = null) => (Name, Position, Color) = (name, pos, color);

        public TileDef WithCustomColor(ColorName c) => new(Name, Position, c);
        public TileDef WithDefaultColor() => new(Name, Position, null);
        public TileDef WithTileName(TileName newTile) => new(newTile, Position, Color);
        public TileDef WithPosition(Coord newPos) => new(Name, newPos, Color);

        private IEntityBuilder<Tile> Decorate(IEntityBuilder<Tile> builder, FloorId id)
        {
            if (Color.HasValue)
            {
                builder = builder.WithColor(Color.Value);
            }
            builder = builder.WithPosition(Position, id);
            return builder;
        }

        public IEntityBuilder<Tile> Resolve(GameEntityBuilders entities, FloorId id) => Name switch
        {
            TileName.Room => Decorate(entities.Tile_Room(), id),
            TileName.Corridor => Decorate(entities.Tile_Corridor(), id),
            TileName.Wall => Decorate(entities.Tile_Wall(), id),
            TileName.Shop => Decorate(entities.Tile_Shop(), id),
            TileName.Water => Decorate(entities.Tile_Water(), id),
            _ => Decorate(entities.Tile_Unimplemented(), id),
        };
    }
}
