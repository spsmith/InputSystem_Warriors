using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace UniCAVE
{
    public static class UniCAVEInputSystem
    {
        //old:
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

        public readonly struct InputEventPtrBytes
		{
            public readonly byte[] data;
            //public readonly int deviceId;
            //public readonly double time;
            //public readonly int type;

            unsafe public InputEventPtrBytes(InputEventPtr iep)
			{
                //copy raw data from InputEventPtr into a byte[]
                data = CopyEventData(iep);

                //store the other information we might need (might not need to do this after all)
                //deviceId = iep.deviceId;
                //time = iep.time;
                //type = FourCC.ToInt32(iep.type);
			}

            public unsafe InputEventPtr ToNewPtr()
			{
                InputEventPtr newPtr = null;
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
                    //this seems to be the correct way to do it:
                    InputEvent ie = (InputEvent)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(InputEvent));
                    newPtr = new InputEventPtr(&ie); //will this pointer always be valid?
                }
				finally
				{
                    handle.Free();
				}
                if(newPtr != null) DebugEvent(newPtr);
                else Debug.LogError("ToNewPtr returned null!");
                return newPtr;
			}

            //old version:
            public InputEventPtr ToInputEventPtr()
			{
                //convert back into an InputEventPtr based on type
                //https://forum.unity.com/threads/recording-and-replaying-input.739187/#post-6787838
                GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
                InputEventPtr iep = (InputEventPtr)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(InputEventPtr));
                pinned.Free();
                //Debug.Log($"Deserialized {iep}");
                DebugEvent(iep);
                return iep;
			}
		}

        public static unsafe byte[] CopyEventData(InputEventPtr eventPtr)
        {
            //https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_sizeInBytes
            uint sizeInBytes = eventPtr.sizeInBytes;
            byte[] buffer = new byte[sizeInBytes];
            fixed(byte* bufferPtr = buffer)
            {
                UnsafeUtility.MemCpy(bufferPtr, eventPtr.data, sizeInBytes);
            }
            return buffer;
        }

        public static void DebugEvent(InputEventPtr iep)
        {
            if(iep != null)
            {
                Debug.LogError($"InputEventPtr {iep}:");
                Debug.LogError($"\tdeviceId: {iep.deviceId}");
                Debug.LogError($"\tsizeInBytes: {iep.sizeInBytes}");
                Debug.LogError($"\ttype: {iep.type}");
                Debug.LogError($"\ttime: {iep.time}");
            }
            else
            {
                Debug.LogError("Error! InputEventPtr is null!");
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

        static Queue<InputEventPtr> _headNodeInputPtrs;
        /// <summary>
        /// Stores InputEventPtrs received from the head node.
        /// </summary>
        public static Queue<InputEventPtr> HeadNodeInputPtrs
        {
            get
            {
                if(_headNodeInputPtrs == null) _headNodeInputPtrs = new Queue<InputEventPtr>();
                return _headNodeInputPtrs;
            }
        }
    }
}