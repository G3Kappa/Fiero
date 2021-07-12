﻿using Fiero.Core;
using SFML.Window;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fiero.Business
{
    public class InventoryModal : Modal
    {
        public readonly Actor Actor;
        public event Action<Item, InventoryActionName> ActionPerformed;

        public UIControlProperty<int> CurrentPage { get; private set; } = new(nameof(CurrentPage), 0);
        public UIControlProperty<int> PageSize { get; private set; } = new(nameof(PageSize), 20);

        protected readonly List<Item> Items = new();
        protected int NumPages => (Items.Count - 1) / PageSize.V + 1;


        public InventoryModal(
            GameUI ui,
            Actor following
        )
            : base(ui)
        {
            Actor = following;
            Hotkeys.Add(new Hotkey(UI.Store.Get(Data.Hotkeys.Inventory)), () => Close(ModalWindowButtons.ImplicitNo));

            Items.AddRange(Actor.Inventory?.GetItems() ?? Enumerable.Empty<Item>());
            CurrentPage.ValueChanged += (_, __) => Invalidate();
        }

        public override void Open(string title, ModalWindowButtons buttons)
        {
            base.Open(title, buttons);
        }

        protected virtual bool OnItemClicked(Button b, int index, Mouse.Button mouseButton)
        {
            if (mouseButton != Mouse.Button.Left) {
                return false;
            }
            var i = CurrentPage.V * PageSize.V + index;
            if (i >= Items.Count) {
                return false;
            }
            var modal = UI.OptionalChoice(
                GetAvailableActions(Items[i]).Distinct().ToArray(), 
                Items[i].DisplayName
            );
            modal.Confirmed += (_, __) => {
                ActionPerformed?.Invoke(Items[i], modal.SelectedOption);
                bool shouldRemoveMenuItem = modal.SelectedOption == InventoryActionName.Drop;
                shouldRemoveMenuItem |= modal.SelectedOption == InventoryActionName.Use
                    && Items[i].TryCast<Consumable>(out var c) 
                    && c.ConsumableProperties.ConsumedWhenEmpty && c.ConsumableProperties.RemainingUses == 1;
                if(shouldRemoveMenuItem) {
                    Items.RemoveAt(i);
                }
                Invalidate();
            };
            Invalidate();
            return false;
        }

        protected virtual IEnumerable<InventoryActionName> GetAvailableActions(Item i) 
        {
            if (Actor.Equipment.IsEquipped(i)) {
                yield return InventoryActionName.Unequip;
            }
            else {
                yield return InventoryActionName.Drop;
                if (i.TryCast<Weapon>(out _) || i.TryCast<Armor>(out _)) {
                    yield return InventoryActionName.Equip;
                }
            }
            if (i.TryCast<Consumable>(out _)) {
                yield return InventoryActionName.Use;
            }
        }
        
        protected override LayoutStyleBuilder DefineStyles(LayoutStyleBuilder builder) => base.DefineStyles(builder)
            .AddRule<Picture<TextureName>>(s => s
                .Match(x => x.HasClass("item-sprite"))
                .Apply(x => {
                    x.HorizontalAlignment.V = HorizontalAlignment.Right;
                    x.TextureName.V = TextureName.Atlas;
                    x.LockAspectRatio.V = true;
                }))
            .AddRule<Button>(s => s
                .Match(x => x.HasClass("item-name"))
                .Apply(x => {
                    x.CenterContentH.V = false;
                    x.FontSize.V = 18;
                    x.Padding.V = new(16, 0);
                }))
            ;


        protected override LayoutGrid RenderContent(LayoutGrid layout)
        {
            return layout
                .Repeat(PageSize.V, (index, grid) => grid
                .Row(@class: index % 2 == 0 ? "row-even" : "row-odd")
                    .Col(w: 0.06f, @class: "item-sprite")
                        .Cell<Picture<TextureName>>(p => {
                            Invalidated += () => RefreshItemSprite(p, index);
                        })
                    .End()
                    .Col(w: 1.94f, @class: "item-name")
                        .Cell<Button>(b => {
                            Invalidated += () => RefreshItemButton(b, index);
                            b.Clicked += (_, __, button) => OnItemClicked(b, index, button);
                        })
                    .End()
                .End())
                .Row()
                    .Col(w: 0.25f, @class: "paginator paginator-prev")
                        .Cell<Button>(b => {
                            b.Text.V = "<";
                            b.Clicked += (_, __, ___) => {
                                CurrentPage.V = (CurrentPage.V - 1).Mod(NumPages);
                                return false;
                            };
                        })
                    .End()
                    .Col(w: 2.50f, @class: "paginator paginator-current")
                        .Cell<Label>(l => {
                            Invalidated += () => RefreshPageLabel(l);
                        })
                    .End()
                    .Col(w: 0.25f, @class: "paginator paginator-next")
                        .Cell<Button>(b => {
                            b.Text.V = ">";
                            b.Clicked += (_, __, ___) => {
                                CurrentPage.V = (CurrentPage.V + 1).Mod(NumPages);
                                return false;
                            };
                        })
                    .End()
                .End()
                ;

            void RefreshItemSprite(Picture<TextureName> p, int index)
            {
                var i = CurrentPage.V * PageSize.V + index;
                if (i >= Items.Count) {
                    p.SpriteName.V = "None";
                }
                else {
                    p.SpriteName.V = Items[i].Render.SpriteName;
                    p.Sprite.Color = Items[i].Render.Sprite.Color;
                }
            }

            void RefreshItemButton(Button b, int index)
            {
                var i = CurrentPage.V * PageSize.V + index;
                if (i >= Items.Count) {
                    b.Text.V = String.Empty;
                    return;
                }

                b.Foreground.V = Actor.Equipment.IsEquipped(Items[i])
                    ? UI.Store.Get(Data.UI.DefaultAccent)
                    : UI.Store.Get(Data.UI.DefaultForeground);

                b.Text.V = Items[i].DisplayName;
            }

            void RefreshPageLabel(Label l)
            {
                l.Text.V = $"{CurrentPage.V + 1}/{NumPages}";
            }
        }
    }
}
