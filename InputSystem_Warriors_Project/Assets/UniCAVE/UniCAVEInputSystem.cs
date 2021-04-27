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
        public enum PhysicsStates { HeadWillSimulate, ChildWillSimulate, WaitingForChildSimulate, WaitingForHeadSimulate }
        public static PhysicsStates PhysicsState { get; set; }

        public readonly struct InputEventBytes
		{
            public readonly byte[] data;
            public readonly int deviceId;
            public readonly double time;
            public readonly int type;

            unsafe public InputEventBytes(InputEventPtr iep)
			{
                //copy raw data from InputEventPtr into a byte[]
                data = CopyEventData(iep);

                //store the other metadata we might need (might not need to do this after all)
                deviceId = iep.deviceId;
                time = iep.time;
                type = FourCC.ToInt32(iep.type);
			}

            public unsafe InputEventPtr ToInputEventPtr()
			{
                //https://forum.unity.com/threads/recording-and-replaying-input.739187/#post-6787838
                InputEventPtr newPtr = null;
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
                    //take another look at this... it works, but are both the GCHandle and fixed{} statement necessary?
                    //could maybe get away with just fixed{}
                    fixed(byte* dataPtr = data)
                    {
                        //simply use the byte[] as the raw data for the InputEvent
                        newPtr = new InputEventPtr((InputEvent*)dataPtr);
                    }
                }
				finally
				{
                    handle.Free();
				}
                return newPtr;
			}
		}

        /// <summary>
        /// Stores an InputEvent in a buffer.
        /// </summary>
        /// <param name="eventPtr">InputEventPtr that references the InputEvent.</param>
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
            Debug.LogError($"InputEventPtr {iep}:");
            //Debug.LogError($"\tdeviceId: {iep.deviceId}");
            //Debug.LogError($"\tsizeInBytes: {iep.sizeInBytes}");
            //Debug.LogError($"\ttype: {iep.type}");
            //Debug.LogError($"\ttime: {iep.time}");
        }

        public static InputEvent ToEvent(this InputEventPtr iep)
		{
            return new InputEvent(iep.type, (int)iep.sizeInBytes, iep.deviceId, iep.time);
		}

        static Queue<InputEventBytes> _headNodeInput;
        /// <summary>
        /// Stores InputEventPtrs received from the head node.
        /// </summary>
        public static Queue<InputEventBytes> HeadNodeInput
        {
            get
            {
                if(_headNodeInput == null) _headNodeInput = new Queue<InputEventBytes>();
                return _headNodeInput;
            }
        }

        static UniCAVEPlayer _player;
        /// <summary>
        /// THe UniCAVEPlayer instance for this Unity instance. Serves as the player for netowrking purposes (e.g. sending Commands to the head node).
        /// </summary>
        public static UniCAVEPlayer Player
        {
			get
			{
                if(!_player) _player = GameObject.FindObjectOfType<UniCAVEPlayer>();
                return _player;
			}
        }

        public static bool ShouldSimulatePhysicsThisFrame = false;

        public static float FixedDeltaTime = 0;

        public static void SimulatePhysics(float fixedDeltaTime)
		{
            //simulate physics while avoiding any timescale issues
            float timescale = Time.timeScale;
            Time.timeScale = 1;
            Physics.Simulate(fixedDeltaTime);
            Time.timeScale = timescale;
        }
    }
}