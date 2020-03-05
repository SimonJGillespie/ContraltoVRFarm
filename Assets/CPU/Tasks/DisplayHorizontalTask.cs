﻿/*  
    This file is part of ContrAlto.

    ContrAlto is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    ContrAlto is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with ContrAlto.  If not, see <http://www.gnu.org/licenses/>.
*/

using Contralto.Display;
using System;

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// DisplayHorizontalTask provides implementations of the DHT task functions.
        /// </summary>
        private sealed class DisplayHorizontalTask : Task
        {
            public DisplayHorizontalTask(AltoCPU cpu) : base(cpu)
            {
                _taskType = TaskType.DisplayHorizontal;                
                _wakeup = false;

                _displayController = _cpu._system.DisplayController;
            }

            public override void OnTaskSwitch()
            {
                // We put ourselves back to sleep immediately once we've started running.
                _wakeup = false;
            }

            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                DisplayHorizontalF2 dh2 = (DisplayHorizontalF2)instruction.F2;
                switch (dh2)
                {
                    case DisplayHorizontalF2.EVENFIELD:
                        _nextModifier |= (ushort)(_displayController.EVENFIELD ? 1 : 0);
                        break;

                    case DisplayHorizontalF2.SETMODE:
                        // "If bit 0 = 1, the bit clock rate is set to 100ns period (at the start of the next scan line),
                        // and a 1 is merged into NEXT[9]."
                        _displayController.SETMODE(_busData);

                        if ((_busData & 0x8000) != 0)
                        {
                            _nextModifier |= 1;
                        }
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled display word F2 {0}.", dh2));                        
                }
            }

            protected override void ExecuteBlock()
            {
                _displayController.DHTBLOCK = true;                
            }

            private DisplayController _displayController;
        }
    }
}
