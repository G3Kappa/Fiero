﻿using Fiero.Core;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiero.Business
{
    public class DungeonBranchGenerator : BranchGenerator
    {
        public override Floor GenerateFloor(FloorId floorId, Coord size, FloorBuilder builder)
        {
            var subdivisions = new Coord(2, 2);
            var sectors = new IntRect(new(), size).Subdivide(subdivisions).ToList();

            var info = (
                ShrineRoomChance: 0.05f, 
                MonstersChance: 0.66f,
                MonstersPerRoll: (Min: 1, Max: 2),
                ConsumablesChance: 0.20f,
                ConsumablesPerRoll: (Min: 1, Max: 2),
                ItemsChance: 0.15f,
                ItemsPerRoll: (Min: 1, Max: 1)
            );

            var roomSectors = sectors.Select(s => RoomSector.Create(s, CreateRoom)).ToList();
            var interCorridors = RoomSector.GenerateInterSectorCorridors(roomSectors).ToList();

            return builder
                .WithStep(ctx => {
                    foreach (var sector in roomSectors) {
                        ctx.Draw(sector);
                    }
                    foreach (var corridor in interCorridors) {
                        ctx.Draw(corridor);
                    }
                })
                .Build(floorId, size);

            Room CreateRoom()
            {
                Room room = new EmptyRoom();
                if(Rng.Random.NextDouble() < info.ShrineRoomChance) {
                    room = new ShrineRoom();
                }

                room.Drawn += (r, ctx) => {
                    // Chances are not actually per-room but per room square, as to make them normalized
                    var area = r.GetRects().Count();
                    if (Rng.Random.NextDouble() < info.ConsumablesChance * area) {
                        var roll = Rng.Random.Between(info.ConsumablesPerRoll.Min, info.ConsumablesPerRoll.Max) * area;
                        AddObjects(r, ctx, DungeonObjectName.Consumable, roll);
                    }
                    if (Rng.Random.NextDouble() < info.MonstersChance * area) {
                        var roll = Rng.Random.Between(info.MonstersPerRoll.Min, info.MonstersPerRoll.Max) * area;
                        AddObjects(r, ctx, DungeonObjectName.Enemy, roll);
                        AddObjects(r, ctx, DungeonObjectName.Trap, roll);
                    }
                    if (Rng.Random.NextDouble() < info.ItemsChance * area) {
                        var roll = Rng.Random.Between(info.ItemsPerRoll.Min, info.ItemsPerRoll.Max) * area;
                        AddObjects(r, ctx, DungeonObjectName.Item, roll);
                    }
                };
                return room;

                void AddObjects(Room r, FloorGenerationContext ctx, DungeonObjectName type, int roll)
                {
                    var pointCloud = r.GetPointCloud()
                        .Shuffle(Rng.Random);
                    for (int i = 0; i < roll; i++) {
                        var pos = pointCloud
                            .Where(p => ctx.GetTile(p).Name == TileName.Room)
                            .First();
                        ctx.AddObject(type, pos);
                    }
                };
            }
        }
    }
}