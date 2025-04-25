using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer.Input
{
    public class ComboInput : KeybindInput
    {
        public ComboInput(List<KeybindInput> inputs)
        {
            Inputs = inputs;
        }

        public List<KeybindInput> Inputs = new();
        public List<ComboInput> EncapsulatingCombos = new();

        public override bool CurrentState => !Inputs.Any(x => !x.CurrentState) && !EncapsulatedInputPressed();

        public override bool OldState => !Inputs.Any(x => !x.OldState) && !EncapsulatedOldInputPressed();

        public override string KeyName => Inputs.Count == 0 ? "None" : string.Join(" + ", Inputs.Select(ki => ki.KeyName));

        private bool EncapsulatedInputPressed() => EncapsulatingCombos.Any(x => x.CurrentState);

        private bool EncapsulatedOldInputPressed() => EncapsulatingCombos.Any(x => x.OldState);

        public bool ComboEncapsulates(ComboInput other)
        {
            foreach (KeybindInput input in Inputs)
            {
                if (!other.Inputs.Any(x => x.InputEquality(input))) return false;
            }
            return other.Inputs.Count != Inputs.Count;
        }

        public override bool InputEquality(KeybindInput other)
        {
            if (base.InputEquality(other)) return true;

            if (other is not ComboInput combo) return false;
            foreach (KeybindInput input in Inputs)
            {
                if (!combo.Inputs.Any(x => x.InputEquality(input))) return false;
            }
            return combo.Inputs.Count == Inputs.Count;
        }
    }
}
