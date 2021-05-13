﻿using System;
using System.Linq;

namespace Fiero.Core
{

    public class UIControlProperty<T> : IUIControlProperty
    {
        private readonly Func<UIControlProperty<T>, UIControlProperty<T>, T, T> _propagate;
        private readonly Func<T, T> _get;
        private readonly Func<T, T> _set;
        public string Name { get; }
        public UIControl Owner { get; private set; }
        public bool Propagate { get; set; }

        public event Action<UIControlProperty<T>, T> ValueChanged;
        public event Action<UIControlProperty<T>, UIControl> OwnerChanged;

        public UIControlProperty(
            string name, 
            T defaultValue = default, 
            Func<UIControlProperty<T>, UIControlProperty<T>, T, T> propagate = null,
            Func<T, T> get = null,
            Func<T, T> set = null)
        {
            Name = name;
            _value = defaultValue;
            _propagate = propagate ?? ((a, b, _) => a.V);
            Propagate = propagate != null;

            _get = get ?? (a => a);
            _set = set ?? (a => a);
        }

        private T _value;
        public T V {
            get => _get(_value);
            set {
                var old = _value;
                _value = _set(value);
                ValueChanged?.Invoke(this, old);
                if (Propagate) {
                    foreach (var child in Owner.Children) {
                        var prop = child.Properties.SingleOrDefault(p => p.Name.Equals(Name));
                        if(prop is null) {
                            continue;
                        }
                        prop.Value = _propagate(this, prop as UIControlProperty<T>, old);
                    }
                }
            }
        }

        object IUIControlProperty.Value {
            get => V;
            set => V = (T)value;
        }

        public void SetOwner(UIControl newOwner)
        {
            var oldOwner = Owner;
            Owner = newOwner;
            OwnerChanged?.Invoke(this, oldOwner);
        }

        public static implicit operator T(UIControlProperty<T> prop) => prop.V;
    }
}
