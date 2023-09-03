﻿using Fiero.Core;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiero.Business
{
    public abstract class ContainerModal<TContainer, TActions> : Modal
        where TContainer : PhysicalEntity
        where TActions : struct, Enum
    {
        public const int RowHeight = 32; // px
        public const int PaginatorHeight = 32; // px

        public readonly TContainer Container;
        public event Action<Item, TActions> ActionPerformed;


        protected readonly List<Item> Items = new();
        public UIControlProperty<int> CurrentPage { get; private set; } = new(nameof(CurrentPage), 0, invalidate: true);
        public UIControlProperty<int> PageSize { get; private set; } = new(nameof(PageSize), 20, invalidate: true);
        protected int NumPages => (Items.Count - 1) / PageSize.V + 1;

        public ContainerModal(GameUI ui, GameResources resources, TContainer cont, ModalWindowButton[] buttons, ModalWindowStyles styles = ModalWindowStyles.Default)
            : base(ui, resources, buttons, styles)
        {
            Container = cont;
            Items.AddRange(cont.Inventory?.GetItems() ?? Enumerable.Empty<Item>());
            CurrentPage.ValueChanged += (_, __) => Invalidate();
        }

        protected override void OnLayoutRebuilt(Layout oldValue)
        {
            base.OnLayoutRebuilt(oldValue);
            Layout.Size.ValueChanged += (_, __) =>
            {
                UpdatePageSize();
            };
        }

        public override void Open(string title)
        {
            base.Open(title);
            UpdatePageSize();
        }

        protected virtual void UpdatePageSize()
        {
            if (Layout is null) return;
            // Update PageSize dynamically
            var contentElem = Layout.Dom.Query(g => g.Id == "modal-content").Single();
            var availableSpace = contentElem.ComputedSize.Y - PaginatorHeight;
            var newPageSize = (int)Math.Ceiling(availableSpace / (float)RowHeight);
            if (newPageSize != PageSize.V)
            {
                PageSize.V = newPageSize;
                RebuildLayout();
            }
            Invalidate();
        }

        protected abstract bool ShouldRemoveItem(Item i, TActions a);

        protected override void RegisterHotkeys(ModalWindowButton[] buttons)
        {
            base.RegisterHotkeys(buttons);
            Hotkeys.Add(new Hotkey(UI.Store.Get(Data.Hotkeys.Inventory)), () => Close(ModalWindowButton.ImplicitNo));
        }

        protected virtual bool OnItemClicked(Button b, int index, Mouse.Button mouseButton)
        {
            if (mouseButton != Mouse.Button.Left)
            {
                return false;
            }
            var i = CurrentPage.V * PageSize.V + index;
            if (i >= Items.Count)
            {
                return false;
            }
            var modal = UI.OptionalChoice(
                GetAvailableActions(Items[i]).Distinct().ToArray(),
                Items[i].DisplayName
            );
            modal.Confirmed += (_, __) =>
            {
                ActionPerformed?.Invoke(Items[i], modal.SelectedOption);
                if (ShouldRemoveItem(Items[i], modal.SelectedOption))
                {
                    Items.RemoveAt(i);
                }
                Invalidate();
            };
            Invalidate();
            return false;
        }

        protected abstract IEnumerable<TActions> GetAvailableActions(Item i);

        protected override LayoutStyleBuilder DefineStyles(LayoutStyleBuilder builder) => base.DefineStyles(builder)
            .AddRule<Picture>(s => s
                .Match(x => x.HasClass("item-sprite"))
                .Apply(x =>
                {
                    x.VerticalAlignment.V = VerticalAlignment.Middle;
                    x.LockAspectRatio.V = true;
                }))
            .AddRule<Button>(s => s
                .Match(x => x.HasClass("item-name"))
                .Apply(x =>
                {
                    x.HorizontalAlignment.V = HorizontalAlignment.Left;
                    x.Padding.V = new(8, 0);
                }))
            ;
        protected override LayoutGrid RenderContent(LayoutGrid layout)
        {
            return layout
                .Repeat(PageSize.V, (index, grid) => grid
                .Row(h: RowHeight, px: true, @class: index % 2 == 0 ? "row-even" : "row-odd")
                    .Col(w: RowHeight, px: true, @class: "item-sprite")
                        .Cell<Picture>(p =>
                        {
                            Invalidated += () => RefreshItemSprite(p, index);
                            p.OutlineThickness.V = 1;
                            p.OutlineColor.V = UI.GetColor(ColorName.UIBorder);
                        })
                    .End()
                    .Col(@class: "item-name")
                        .Cell<Button>(b =>
                        {
                            Invalidated += () => RefreshItemButton(b, index);
                            b.Clicked += (_, __, button) => OnItemClicked(b, index, button);
                            b.MouseEntered += (x, __) => x.Foreground.V = UI.GetColor(ColorName.UIAccent);
                            b.MouseLeft += (x, __) => x.Foreground.V = UI.GetColor(ColorName.UIPrimary);
                            b.OutlineThickness.V = 1;
                            b.OutlineColor.V = UI.GetColor(ColorName.UIBorder);
                        })
                    .End()
                .End())
                .Row(h: PaginatorHeight, px: true)
                    .Col(@class: "spacer")
                        .Cell<Layout>(x => x.Background.V = UI.Store.Get(Data.UI.DefaultBackground))
                    .End()
                    .Col(w: 16, px: true, @class: "paginator paginator-prev")
                        .Cell<Button>(b =>
                        {
                            b.Text.V = "<";
                            b.Scale.V = new Vec(2, 2);
                            b.HorizontalAlignment.V = HorizontalAlignment.Right;
                            b.Clicked += (_, __, ___) =>
                            {
                                CurrentPage.V = (CurrentPage.V - 1).Mod(NumPages);
                                return false;
                            };
                        })
                    .End()
                    .Col(w: 64, px: true, @class: "paginator paginator-current")
                        .Cell<Label>(l =>
                        {
                            l.Scale.V = new Vec(2, 2);
                            l.HorizontalAlignment.V = HorizontalAlignment.Center;
                            Invalidated += () => RefreshPageLabel(l);
                        })
                    .End()
                    .Col(w: 16, px: true, @class: "paginator paginator-next")
                        .Cell<Button>(b =>
                        {
                            b.Text.V = ">";
                            b.Scale.V = new Vec(2, 2);
                            b.HorizontalAlignment.V = HorizontalAlignment.Left;
                            b.Clicked += (_, __, ___) =>
                            {
                                CurrentPage.V = (CurrentPage.V + 1).Mod(NumPages);
                                return false;
                            };
                        })
                    .End()
                    .Col(@class: "spacer")
                        .Cell<Layout>(x => x.Background.V = UI.Store.Get(Data.UI.DefaultBackground))
                    .End()
                .End();

            void RefreshItemSprite(Picture p, int index)
            {
                var i = CurrentPage.V * PageSize.V + index;
                if (i >= Items.Count)
                {
                    p.Sprite.V = Resources.Sprites.Get(TextureName.Items, "None", ColorName.White);
                }
                else
                {
                    p.Sprite.V = Resources.Sprites.Get(TextureName.Items, Items[i].Render.Sprite, Items[i].Render.Color);
                }
            }

            void RefreshItemButton(Button b, int index)
            {
                var i = CurrentPage.V * PageSize.V + index;
                if (i >= Items.Count)
                {
                    b.Text.V = String.Empty;
                    return;
                }

                if (Container.TryCast<Actor>(out var actor))
                {
                    b.Foreground.V = actor.Equipment.IsEquipped(Items[i])
                        ? UI.Store.Get(Data.UI.DefaultAccent)
                        : UI.Store.Get(Data.UI.DefaultForeground);
                }

                b.Text.V = Items[i].DisplayName;
            }

            void RefreshPageLabel(Label l)
            {
                l.Text.V = $"{CurrentPage.V + 1}/{NumPages}";
            }
        }

    }
}
