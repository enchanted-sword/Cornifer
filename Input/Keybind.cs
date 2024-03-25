using System.Collections.Generic;
using System.Linq;

namespace Cornifer.Input
{
    public class Keybind
    {
        public string Name { get; }

        // KeyA & KeyB, KeyA & KeyC
        public List<ComboInput> Inputs = new();

        public KeybindState State
        {
            get
            {
                KeybindState state = KeybindState.Released;

                foreach (ComboInput combo in Inputs)
                {
                    KeybindState comboState = combo.State;
                    if (comboState == KeybindState.Pressed)
                        return KeybindState.Pressed;

                    if (comboState == KeybindState.JustReleased && state < KeybindState.JustReleased)
                        state = KeybindState.JustReleased;

                    if (comboState == KeybindState.JustPressed && state < KeybindState.JustPressed)
                        state = KeybindState.JustPressed;
                }

                return state;
            }
        }

        public bool Released => State == KeybindState.Released;
        public bool JustReleased => State == KeybindState.JustReleased;
        public bool JustPressed => State == KeybindState.JustPressed;
        public bool Pressed => State == KeybindState.Pressed;

        public bool AnyKeyPressed
        {
            get
            {
                foreach (ComboInput combo in Inputs)
                    foreach (KeybindInput input in combo.Inputs)
                        if (input.CurrentState)
                            return true;
                return false;
            }
        }
        public bool AnyOldKeyPressed
        {
            get
            {
                foreach (ComboInput combo in Inputs)
                    foreach (KeybindInput input in combo.Inputs)
                        if (input.OldState)
                            return true;
                return false;
            }
        }

        public Keybind(string name, IEnumerable<KeybindInput> defaults)
        {
            Name = name;
            Inputs.Add(new(defaults.ToList()));
        }

        public Keybind(string name, IEnumerable<IEnumerable<KeybindInput>> defaults)
        {
            Name = name;

            foreach (var keyCombo in defaults)
                Inputs.Add(new(keyCombo.ToList()));
        }

        public Keybind(string name, params KeybindInput[][] @default) : this(name, (IEnumerable<KeybindInput[]>)@default) { }

        public Keybind(string name, params KeybindInput[] @default) : this(name, (IEnumerable<KeybindInput>)@default) { }
    }
}
