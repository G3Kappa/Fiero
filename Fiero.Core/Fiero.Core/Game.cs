﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Fiero.Core
{
    public abstract class Game<TFonts, TTextures, TLocales, TSounds, TColors, TShaders>
        where TFonts : struct, Enum
        where TTextures : struct, Enum
        where TLocales : struct, Enum
        where TSounds : struct, Enum
        where TColors : struct, Enum
        where TShaders : struct, Enum
    {
        public readonly OffButton OffButton;
        public readonly GameLoop Loop;
        public readonly GameInput Input;
        public readonly GameTextures<TTextures> Textures;
        public readonly GameSprites<TTextures> Sprites;
        public readonly GameColors<TColors> Colors;
        public readonly GameFonts<TFonts> Fonts;
        public readonly GameSounds<TSounds> Sounds;
        public readonly GameShaders<TShaders> Shaders;
        public readonly GameDirector Director;
        public readonly GameUI UI;
        public readonly GameLocalizations<TLocales> Localization;

        public Game(
            OffButton off, 
            GameLoop loop, 
            GameInput input, 
            GameTextures<TTextures> resources, 
            GameSprites<TTextures> sprites,
            GameFonts<TFonts> fonts, 
            GameSounds<TSounds> sounds, 
            GameColors<TColors> colors,
            GameShaders<TShaders> shaders,
            GameLocalizations<TLocales> localization,
            GameUI ui,
            GameDirector director)
        {
            OffButton = off;
            Loop = loop;
            Input = input;
            Colors = colors;
            Shaders = shaders;
            Textures = resources;
            Sprites = sprites;
            Sounds = sounds;
            Fonts = fonts;
            UI = ui;
            Director = director;
            Localization = localization;
        }

        protected virtual async Task InitializeAsync()
        {
            await RouteEventsAsync();
        }

        protected virtual Task RouteEventsAsync()
        {
            return Task.CompletedTask;
        }

        protected bool ValidateResources<TEnum>(Func<TEnum, bool> validate, out IEnumerable<TEnum> failures)
            where TEnum : struct, Enum
        {
            failures = Enum.GetValues<TEnum>().Where(x => !validate(x));
            return !failures.Any();
        }

        protected virtual void ValidateResources()
        {
            if (!ValidateResources<TFonts>(f => Fonts.Get(f) != null, out var missingFonts)) {
                throw new AggregateException(missingFonts.Select(x => new ResourceNotFoundException<TFonts>(x)));
            }
            if (!ValidateResources<TTextures>(f => Textures.Get(f) != null, out var missingTextures)) {
                throw new AggregateException(missingTextures.Select(x => new ResourceNotFoundException<TTextures>(x)));
            }
            if (!ValidateResources<TColors>(f => Colors.TryGet(f, out _), out var missingColors)) {
                throw new AggregateException(missingColors.Select(x => new ResourceNotFoundException<TColors>(x)));
            }
            if (!ValidateResources<TSounds>(f => Sounds.Get(f) != null, out var missingSounds)) {
                throw new AggregateException(missingSounds.Select(x => new ResourceNotFoundException<TSounds>(x)));
            }
            if (!ValidateResources<TShaders>(f => Shaders.Get(f) != null, out var missingShaders)) {
                throw new AggregateException(missingShaders.Select(x => new ResourceNotFoundException<TShaders>(x)));
            }
            if (!ValidateResources<TLocales>(f => Localization.HasLocale(f), out var missingLocales)) {
                throw new AggregateException(missingLocales.Select(x => new ResourceNotFoundException<TLocales>(x)));
            }
        }

        protected virtual void InitializeWindow(RenderWindow win)
        {
            win.SetKeyRepeatEnabled(true);
            win.SetActive(true);
            win.Resized += (e, eh) => {
                win.GetView()?.Dispose();
                win.SetView(new(new FloatRect(0, 0, eh.Width, eh.Height)));
            };
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            await InitializeAsync();
            ValidateResources();
            using var win = new RenderWindow(new VideoMode(800, 800), String.Empty);
            InitializeWindow(win);
            Loop.Tick += (t, dt) => {
                win.DispatchEvents();
            };
            Loop.Update += (t, dt) => {
                Update(win, t, dt);
            };
            // Always called once per frame before the window is drawn
            Loop.Render += (t, dt) => {
                Draw(win, t, dt);
            };
            Loop.Run(token);
        }

        public virtual void Update(RenderWindow win, float t, float dt)
        {
            if(win.HasFocus()) {
                Input.Update(Mouse.GetPosition(win));
            }
            if (UI.GetOpenModals().LastOrDefault() is { } modal) {
                modal.Update(win, t, dt);
            }
            else {
                Director.Update(win, t, dt);
            }
        }

        public virtual void Draw(RenderWindow win, float t, float dt)
        {
            Director.Draw(win, t, dt);
            foreach (var modal in UI.GetOpenModals()) {
                modal.Draw(win, t, dt);
            }
            win.Display();
        }
    }
}
