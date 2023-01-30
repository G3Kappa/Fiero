﻿using Fiero.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiero.Business
{

    public class Floor
    {
        public readonly FloorId Id;
        public readonly Coord Size;

        private readonly SpatialDictionary<MapCell, PhysicalEntity> _cells;
        public IReadOnlyDictionary<Coord, MapCell> Cells => _cells;
        public readonly SpatialAStar<MapCell, PhysicalEntity> Pathfinder;

        public event Action<Floor, Tile, Tile> TileChanged;
        public event Action<Floor, Feature> FeatureAdded;
        public event Action<Floor, Feature> FeatureRemoved;
        public event Action<Floor, Actor> ActorAdded;
        public event Action<Floor, Actor> ActorRemoved;
        public event Action<Floor, Item> ItemAdded;
        public event Action<Floor, Item> ItemRemoved;

        public Floor(FloorId id, Coord size)
        {
            Id = id;
            Size = size;
            _cells = new(size);
            Pathfinder = _cells.GetPathfinder();
        }

        public IEnumerable<PhysicalEntity> GetDrawables()
            => Cells.Values.SelectMany(c => c.GetDrawables());

        public void SetTile(Tile tile)
        {
            if (!_cells.TryGetValue(tile.Position(), out var cell))
            {
                cell = _cells[tile.Position()] = new(tile);
            }
            var oldTile = cell.Tile;
            cell.Tile = tile;
            TileChanged?.Invoke(this, oldTile, tile);
            if (Pathfinder != null)
            {
                Pathfinder.Update(tile.Position(), cell, out var old);
                old?.Tile?.TryRefresh(tile.Id); // Update old references that are stored in pathfinding lists
            }
        }

        public void AddActor(Actor actor)
        {
            actor.Physics.FloorId = Id;
            if (_cells.TryGetValue(actor.Position(), out var cell))
            {
                cell.Actors.Add(actor);
                ActorAdded?.Invoke(this, actor);
            }
        }

        public void RemoveActor(Actor actor)
        {
            if (_cells.TryGetValue(actor.Position(), out var cell))
            {
                cell.Actors.Remove(actor);
                ActorRemoved?.Invoke(this, actor);
            }
        }

        public void AddItem(Item item)
        {
            item.Physics.FloorId = Id;
            if (_cells.TryGetValue(item.Position(), out var cell))
            {
                cell.Items.Add(item);
                ItemAdded?.Invoke(this, item);
            }
        }

        public void RemoveItem(Item item)
        {
            if (_cells.TryGetValue(item.Position(), out var cell))
            {
                cell.Items.Remove(item);
                ItemRemoved?.Invoke(this, item);
            }
        }

        public void AddFeature(Feature feature)
        {
            feature.Physics.FloorId = Id;
            if (_cells.TryGetValue(feature.Position(), out var cell))
            {
                cell.Features.Add(feature);
                Pathfinder.Update(feature.Position(), cell, out _);
                FeatureAdded?.Invoke(this, feature);
            }
        }

        public void RemoveFeature(Feature feature)
        {
            if (_cells.TryGetValue(feature.Position(), out var cell))
            {
                cell.Features.Remove(feature);
                Pathfinder.Update(feature.Position(), cell, out _);
                FeatureRemoved?.Invoke(this, feature);
            }
        }

        public IEnumerable<Coord> CalculateFov(Coord center, int radius)
        {
            var result = new HashSet<Coord>();
            new JordixVisibility(
                (x, y) => !_cells.TryGetValue(new(x, y), out var cell) || cell.Tile.Physics.BlocksLight || cell.Features.Any(f => f.Physics.BlocksLight),
                (x, y) => result.Add(new(x, y)),
                (x, y) => (int)new Coord().DistSq(new(x, y))
            ).Compute(center, radius * radius);
            return result;
        }
    }
}
