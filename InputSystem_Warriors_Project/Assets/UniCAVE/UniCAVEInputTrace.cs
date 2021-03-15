using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Networking;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using System.Reflection;

namespace UniCAVE
{
    public class UniCAVEInputTrace : NetworkBehaviour
    {
		//[SerializeField]
		//NetworkManager UniCAVENetworkManager;

		[SerializeField]
		int MaxPacketSize = 1400;

		[SerializeField]
		bool TraceInEditor = true;

		[SerializeField]
		bool ReceiveInEditor = true;

		[SerializeField]
		bool DebugPrint = false;

		int TEMP_MAX_ITEMS = 16; //just for testing

		InputEventTrace Trace;

		static Queue<InputEventPtr> _traceQueue;
		public static Queue<InputEventPtr> TraceQueue
		{
			get
			{
				if(_traceQueue == null) _traceQueue = new Queue<InputEventPtr>();
				return _traceQueue;
			}
		}

		static Queue<InputEventPtr> _traceQueueBytes;
		public static Queue<InputEventPtr> TraceQueueBytes
		{
			get
			{
				if(_traceQueueBytes == null) _traceQueueBytes = new Queue<InputEventPtr>();
				return _traceQueueBytes;
			}
		}

		//server should trace
		bool ShouldTrace
		{
			get
			{
#if UNITY_EDITOR
				return TraceInEditor;
#else
				return Application.isPlaying && isServer;
#endif
			}
		}

		//client should receive
		bool ShouldReceive
		{
			get
			{
#if UNITY_EDITOR
				return ReceiveInEditor;
#else
				return Application.isPlaying && !isServer;
#endif
			}
		}

		void TraceEvents()
		{
			if(Trace == null)
			{
				if(ShouldTrace)
				{
					Debug.Log("Enabling Trace...");
					EnableTrace();
				}
				else
				{
					Debug.LogError($"Can't trace input; Trace is null! (ShouldTrace = {ShouldTrace}, isServer = {isServer})");
				}
			}
			if(Trace != null)
			{
				//go through new events since last update
				InputEventPtr curEvent = new InputEventPtr();
				while(Trace.GetNextEvent(ref curEvent))
				{
					//don't store the pointers - they get reused by Unity
					//turn them into events, then turn them back into pointers later
					//UniCAVEInputSystem.DebugEvent(curEvent);
					//UniCAVEInputSystem.DebugEvent(new UniCAVEInputSystem.InputEventBytes(curEvent).ToInputEventPtr());
					TraceQueue.Enqueue(curEvent);
					//TODO: skip this step and just send the pointers...

					if(DebugPrint) Debug.Log($"Traced event {curEvent.ToEvent()}");
				}
				Trace.Clear();

				ProcessEventsBytes(TraceQueue);
			}
		}

		unsafe public void ProcessEventsBytes(Queue<InputEventPtr> queue)
		{
			while(queue.Count > 0)
			{
				int maxItems = Mathf.Clamp(TEMP_MAX_ITEMS, 0, queue.Count);
				UniCAVEInputSystem.InputEventBytes[] ieb = new UniCAVEInputSystem.InputEventBytes[maxItems];
				for(int i = 0; i < ieb.Length; i++)
				{
					InputEventPtr iep = queue.Dequeue();
					ieb[i] = new UniCAVEInputSystem.InputEventBytes(iep);
				}

				RpcProcessEventsBytes(ieb);
			}
		}

#if !UNITY_EDITOR
		[ClientRpc]
#endif
		void RpcProcessEventsBytes(UniCAVEInputSystem.InputEventBytes[] ieb)
		{
			if(ShouldReceive)
			{
				//process the array of input events in the same order they were sent from the head node
				for(int i = 0; i < ieb.Length; i++)
				{
					SendEventBytes(ieb[i]);
				}
			}
		}

		public void SendEventBytes(UniCAVEInputSystem.InputEventBytes ieb)
		{
			//for some reason, after being dequeued, InputEventPtrs come back broken
			//maybe because they're structs?
			//just store the bytes struct instead
			//UniCAVEInputSystem.DebugEvent(ieb.ToInputEventPtr());
			UniCAVEInputSystem.HeadNodeInput.Enqueue(ieb);			
		}

		void EnableTrace()
		{
			Trace = new InputEventTrace();
			Trace.Enable();
		}

		void DisableTrace()
		{
			if(Trace != null)
			{
				Trace.Dispose();
				Trace = null;
			}
		}

		void Update()
		{
			if(ShouldTrace)
			{
				TraceEvents();
			}
		}

		void OnEnable()
		{
			if(ShouldTrace)
			{
				EnableTrace();
			}
		}

		void OnDisable()
		{
			if(ShouldTrace)
			{
				DisableTrace();
			}
		}
	}
}