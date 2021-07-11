﻿using Fiero.Core;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Transactions;
using Unconcern.Common;
using static SFML.Window.Keyboard;

namespace Fiero.Business.Scenes
{
    public class GameplayScene : GameScene<GameplayScene.SceneState>
    {
        public enum SceneState
        {
            [EntryState]
            Main,
            [ExitState]
            Exit_GameOver,
            [ExitState]
            Exit_SaveAndQuit
        }

        protected readonly GameDataStore Store;
        protected readonly GameEntities Entities;
        protected readonly GameInput Input;

        protected readonly FloorSystem FloorSystem;
        protected readonly FactionSystem FactionSystem;
        protected readonly ActionSystem ActionSystem;
        protected readonly RenderSystem RenderSystem;
        protected readonly DialogueSystem DialogueSystem;
        protected readonly GameDialogues Dialogues;
        protected readonly GameSounds<SoundName> Sounds;
        protected readonly GameEntityBuilders EntityBuilders;
        protected readonly OffButton OffButton;
        protected readonly GameUI UI;

        public Actor Player { get; private set; }

        public GameplayScene(
            GameInput input,
            GameDataStore store,
            GameEntities entities,
            GameDialogues dialogues,
            FloorSystem floorSystem,
            RenderSystem renderSystem,
            DialogueSystem dialogueSystem,
            FactionSystem factionSystem,
            ActionSystem actionSystem,
            GameEntityBuilders entityBuilders,
            GameUI ui,
            OffButton off,
            GameSounds<SoundName> sounds)
        {
            Input = input;
            Store = store;
            Entities = entities;
            FloorSystem = floorSystem;
            RenderSystem = renderSystem;
            ActionSystem = actionSystem;
            FactionSystem = factionSystem;
            DialogueSystem = dialogueSystem;
            Dialogues = dialogues;
            EntityBuilders = entityBuilders;
            UI = ui;
            OffButton = off;
            Sounds = sounds;
        }

        public override async Task InitializeAsync()
        {
            RenderSystem.Initialize();
            DialogueSystem.Initialize();
            SubscribeDialogueHandlers();
            await base.InitializeAsync();
        }

        public override async IAsyncEnumerable<Subscription> RouteEventsAsync()
        {
            yield return await ActionSystem.PlayerTurnStarted.SubscribeHandler(DialogueSystem.CheckTriggers);
        }

        public bool TrySpawn(int entityId, out Actor actor, float maxDistance = 10)
        {
            actor = FloorSystem.CurrentFloor.AddActor(entityId);
            if(!FloorSystem.TryGetClosestFreeTile(actor.Physics.Position, out var spawnTile, maxDistance)) {
                return false;
            }
            actor.Physics.Position = spawnTile.Physics.Position;
            ActionSystem.AddActor(entityId);
            return true;
        }

        public bool TryUseItem(Item item, Actor actor, out bool consumed)
        {
            var used = false;
            consumed = false;
            if (item.TryCast<Consumable>(out var consumable)) {
                if(consumable.TryCast<Potion>(out var potion) 
                && TryApply(potion.PotionProperties.Effect)) {
                    used = TryConsume(out consumed);
                }
                if (consumable.TryCast<Scroll>(out var scroll)
                && TryApply(scroll.ScrollProperties.Effect)) {
                    used = TryConsume(out consumed);
                }
            }
            if(consumed) {
                // Assumes item was used from inventory
                _ = actor.Inventory.TryTake(item);
            }
            return used;

            bool TryConsume(out bool consumed)
            {
                consumed = false;
                if (consumable.ConsumableProperties.RemainingUses <= 0) {
                    return false;
                }
                if (--consumable.ConsumableProperties.RemainingUses <= 0
                 && consumable.ConsumableProperties.ConsumedWhenEmpty) {
                    consumed = true;
                }
                return true;
            }

            bool TryApply(EffectName effect)
            {
                switch (effect) {
                    default: return true;
                }
            }
        }

        protected void SubscribeDialogueHandlers()
        {
            Dialogues.GetDialogue(NpcName.GreatKingRat, GKRDialogueName.JustMet)
                .Triggered += (t, eh) => {
                    Sounds.Get(SoundName.BossSpotted).Play();
                };
            Dialogues.GetDialogue(NpcName.GreatKingRat, GKRDialogueName.JustMet_Friend)
                .Triggered += (t, eh) => {
                    foreach (var player in eh.DialogueListeners.Players()) {
                        FactionSystem.TryUpdateRelationship(FactionName.Rats, FactionName.Players, 
                            x => x.With(StandingName.Loved), out _);
                    }
                };
            Dialogues.GetDialogue(NpcName.GreatKingRat, GKRDialogueName.JustMet_Enemy)
                .Triggered += (t, eh) => {
                    foreach (var player in eh.DialogueListeners.Players()) {
                        FactionSystem.TryUpdateRelationship(FactionName.Rats, FactionName.Players,
                            x => x.With(StandingName.Hated), out _);
                        FactionSystem.TryCreateConflict(
                            FactionName.Rats, (r, i) => i < 3,
                            FactionName.Players, (p, i) => i == 0,
                            out _);
                        FactionSystem.TryCreateConflict(
                            FactionName.Rats, (r, i) => i < 3,
                            FactionName.Players, (p, i) => i == 0,
                            out _);
                        FactionSystem.TryCreateConflict(
                            FactionName.Rats, (r, i) => i < 3,
                            FactionName.Players, (p, i) => i == 0,
                            out _);
                        FactionSystem.TryCreateConflict(
                            FactionName.Rats, (r, i) => i < 3,
                            FactionName.Players, (p, i) => i == 0,
                            out _);
                    }
                };
            Dialogues.GetDialogue(FeatureName.Shrine, ShrineDialogueName.Smintheus_Follow)
                .Triggered += (t, eh) => {
                    foreach (var player in eh.DialogueListeners.Players()) {
                        var friends = Enumerable.Range(0, 5)
                            .Select(i => EntityBuilders
                                .Rat(MonsterTierName.Two)
                                .WithFaction(FactionName.Players)
                                .WithPosition(player.Physics.Position)
                                .Build());
                        foreach (var f in friends) {
                            TrySpawn(f.Id, out _);
                        }
                    }
                    // Remove trigger from the shrine
                    if(Entities.TryGetFirstComponent<DialogueComponent>(eh.DialogueStarter.Id, out var dialogue)) {
                        dialogue.Triggers.Remove(t);
                    }
                };
        }

        protected void OnInventoryClosed(InventoryModal modal, Item item, InventoryActionName action)
        {
            switch (action) {
                case InventoryActionName.Equip:
                    if (modal.Actor.Equipment.TryEquip(item)) {
                        modal.Actor.Log?.Write($"$Instantaneous.YouEquip$ {item.DisplayName}.");
                    }
                    else {
                        modal.Actor.Log?.Write($"$Instantaneous.YouFailEquipping$ {item.DisplayName}.");
                    }
                    break;
                case InventoryActionName.Unequip:
                    if (modal.Actor.Equipment.TryUnequip(item)) {
                        modal.Actor.Log?.Write($"$Instantaneous.YouUnequip$ {item.DisplayName}.");
                    }
                    else {
                        modal.Actor.Log?.Write($"$Instantaneous.YouFailUnequipping$ {item.DisplayName}.");
                    }
                    break;
                case InventoryActionName.Drop:
                    _ = modal.Actor.Inventory.TryTake(item);
                    if (FloorSystem.TryGetClosestFreeTile(modal.Actor.Physics.Position, out var tile)) {
                        item.Physics.Position = tile.Physics.Position;
                        FloorSystem.CurrentFloor.AddItem(item.Id);
                        modal.Actor.Log?.Write($"$Instantaneous.YouDrop$ {item.DisplayName}.");
                    }
                    else {
                        modal.Actor.Log?.Write($"$Instantaneous.NoSpaceToDrop$ {item.DisplayName}.");
                    }
                    break;
                case InventoryActionName.Use:
                    if (TryUseItem(item, modal.Actor, out var consumed)) {
                        modal.Actor.Log?.Write($"$Instantaneous.YouUse$ {item.DisplayName}.");
                    }
                    else {
                        modal.Actor.Log?.Write($"$Instantaneous.YouFailUsing$ {item.DisplayName}.");
                    }
                    if(consumed) {
                        modal.Actor.Log?.Write($"$Miscellaneous.AnItemIsConsumed$ {item.DisplayName}.");
                    }
                    break;
                default: break;
            }
        }

        public override void Update(RenderWindow win, float t, float dt)
        {
            RenderSystem.Update(win, t, dt);
            if(DialogueSystem.CurrentDialogue != null) {
                DialogueSystem.Update(t, dt);
            }
            else {
                ActionSystem.Update();
                Entities.RemoveFlaggedItems();
            }
            if (Input.IsKeyPressed(Key.R)) {
                Task.Run(async () => await TrySetStateAsync(SceneState.Main));
            }
            if (Input.IsKeyPressed(Store.Get(Data.Hotkeys.ToggleInventory))) {
                var modal = UI.Inventory(Player);
                modal.ActionPerformed += (item, action) => OnInventoryClosed(modal, item, action);
            }
        }

        public override void Draw(RenderWindow win, float t, float dt)
        {
            win.Clear();
            RenderSystem.Draw(win, t, dt);
            DialogueSystem.Draw(win, t, dt);
        }

        protected override Task<bool> CanChangeStateAsync(SceneState newState) => Task.FromResult(true);
        protected override async Task OnStateChangedAsync(SceneState oldState)
        {
            if(State == SceneState.Main) {
                var newRngSeed = (int)DateTime.Now.ToBinary();
                Store.SetValue(Data.Global.RngSeed, newRngSeed);
                Entities.Clear();
                FloorSystem.Clear();
                ActionSystem.Clear();
                // Generate map
                FloorSystem.AddFloor(new (100, 100), floor =>
                    floor.WithStep(ctx => {
                        var dungeon = new DungeonGenerator(DungeonGenerationSettings.Default)
                            .Generate();
                        ctx.DrawBox((0, 0), (ctx.Size.X, ctx.Size.Y), TileName.Wall);
                        ctx.DrawDungeon(dungeon);
                    }));
                // Track agents
                foreach (var comp in Entities.GetComponents<ActionComponent>()) {
                    ActionSystem.AddActor(comp.EntityId);
                }
                // Create player on top of the starting stairs
                var playerName = Store.GetOrDefault(Data.Player.Name, "Player");
                var upstairs = FloorSystem.CurrentFloor.Tiles.Values
                    .Single(t => t.TileProperties.Name == TileName.Upstairs)
                    .Physics.Position;
                var pid = EntityBuilders.Player
                    .WithPosition(upstairs)
                    .WithName(playerName)
                    .Build().Id;
                if(!TrySpawn(pid, out var player)) {
                    throw new InvalidOperationException("Can't spawn the player??");
                }
                RenderSystem.SelectedActor.Following.V = Player = player;
            }
            await base.OnStateChangedAsync(oldState);
        }
    }
}
