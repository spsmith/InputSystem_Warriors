using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UniCAVE
{
    public static class UniCAVEInputSystem
    {
        /// <summary>
        /// Struct used to send InputEvents via RPC calls.
        /// </summary>
        public readonly struct InputEventStruct
		{
            public readonly int type;
            public readonly uint sizeInBytes;
            public readonly int deviceId;
            public readonly double time;

            public InputEventStruct(InputEventPtr iep) : this(iep.ToEvent()) { }

            public InputEventStruct(InputEvent ie) : this(ie.type, ie.sizeInBytes, ie.deviceId, ie.time) { }

            public InputEventStruct(FourCC type, uint sizeInBytes, int deviceId, double time)
			{
                this.type = FourCC.ToInt32(type);
                this.sizeInBytes = sizeInBytes;
                this.deviceId = deviceId;
                this.time = time;
            }

            public InputEvent ToInputEvent()
			{
                return new InputEvent(FourCC.FromInt32(type), (int)sizeInBytes, deviceId, time);
			}
        }

        public static InputEvent ToEvent(this InputEventPtr iep)
		{
            return new InputEvent(iep.type, (int)iep.sizeInBytes, iep.deviceId, iep.time);
		}

        static Queue<InputEvent> _headNodeInput;
        /// <summary>
        /// Stores InputEvents received from the head node.
        /// </summary>
        public static Queue<InputEvent> HeadNodeInput
		{
			get
			{
                if(_headNodeInput == null) _headNodeInput = new Queue<InputEvent>();
                return _headNodeInput;
			}
		}
    }
}