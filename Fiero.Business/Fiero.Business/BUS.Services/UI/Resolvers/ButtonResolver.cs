﻿using Fiero.Core;

namespace Fiero.Business
{

    [SingletonDependency(typeof(IUIControlResolver<Button>))]
    public class ButtonResolver : UIControlResolver<Button>
    {
        public ButtonResolver(GameUI ui, GameResources resources)
            : base(ui, resources)
        {
        }

        public override Button Resolve(LayoutGrid dom)
        {
            var x = new Button(UI.Input);
            x.Font.V = GetFont();
            x.Foreground.V = Foreground;
            x.Background.V = Background;
            x.ContentAwareScale.V = false;
            x.FontSize.V = x.Font.V.Size;
            return x;
        }
    }
}
