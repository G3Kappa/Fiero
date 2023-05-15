﻿using System;

namespace Fiero.Core
{
    public abstract class ModalWindow : UIWindow
    {
        public event Action<ModalWindow, ModalWindowButton> Confirmed;
        public event Action<ModalWindow, ModalWindowButton> Cancelled;

        protected readonly ModalWindowButton[] Buttons;
        protected readonly ModalWindowStyles Styles;

        public ModalWindow(GameUI ui, ModalWindowButton[] buttons, ModalWindowStyles styles = ModalWindowStyles.Default)
            : base(ui)
        {
            Buttons = buttons;
            Styles = styles;
        }

        public override LayoutGrid CreateLayout(LayoutGrid grid, string title)
        {
            var hasTitle = Styles.HasFlag(ModalWindowStyles.Title);
            var hasButtons = Styles.HasFlag(ModalWindowStyles.Buttons);

            var titleHeight = hasTitle ? 0.20f : 0f;
            var buttonsHeight = hasButtons ? 0.20f : 0f;
            var contentHeight = hasTitle && hasButtons ? 2.60f : hasTitle ^ hasButtons ? 1.80f : 1f;

            return ApplyStyles(grid)
                .Col(@class: "modal")
                    .If(hasTitle, g => g.Row(h: titleHeight, @class: "modal-title")
                        .Cell<Label>(l =>
                        {
                            l.Text.V = title;
                            l.CenterContentH.V = true;
                            if (Title != null)
                            {
                                Title.V = l.Text.V;
                            }
                        })
                    .End())
                    .Row(h: contentHeight, @class: "modal-content")
                        .Repeat(1, (i, g) => RenderContent(g))
                    .End()
                    .If(hasButtons, g => g.Row(h: buttonsHeight, @class: "modal-controls")
                        .Repeat(Buttons.Length, (i, grid) => grid
                            .Col()
                                .Cell<Button>(b =>
                                {
                                    b.Text.V = Buttons[i].ToString();
                                    b.CenterContentH.V = true;
                                    b.Clicked += (_, __, ___) =>
                                    {
                                        Close(Buttons[i]);
                                        return false;
                                    };
                                })
                            .End()
                        )
                    .End())
                .End();
        }

        public override void Close(ModalWindowButton buttonPressed)
        {
            base.Close(buttonPressed);
            // ResultType is nullable
            if (buttonPressed.ResultType == true)
            {
                Confirmed?.Invoke(this, buttonPressed);
            }
            else if (buttonPressed.ResultType == false)
            {
                Cancelled?.Invoke(this, buttonPressed);
            }
        }
    }
}
