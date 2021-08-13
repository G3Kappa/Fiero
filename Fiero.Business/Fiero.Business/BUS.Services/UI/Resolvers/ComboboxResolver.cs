﻿using Fiero.Core;

namespace Fiero.Business
{
    [SingletonDependency(typeof(IUIControlResolver<Combobox>))]
    public class ComboboxResolver : UIControlResolver<Combobox>
    {
        public ComboboxResolver(GameUI ui, GameResources resources)
            : base(ui, resources)
        {
        }

        public override Combobox Resolve(LayoutGrid dom)
        {
            var x = new Combobox(UI.Input, GetText, () => new(UI.Input, GetText));
            x.Foreground.V = Foreground;
            x.Background.V = Background;
            x.Accent.V = Accent;
            return x;
        }
    }
}