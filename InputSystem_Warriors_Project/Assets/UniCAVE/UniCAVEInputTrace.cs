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

		InputEventTrace Trace;

		static Queue<InputEvent> _traceQueue;
		public static Queue<InputEvent> TraceQueue
		{
			get
			{
				if(_traceQueue == null) _traceQueue = new Queue<InputEvent>();
				return _traceQueue;
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
			if(Trace != null)
			{
				//go through new events since last update
				InputEventPtr curEvent = new InputEventPtr();
				while(Trace.GetNextEvent(ref curEvent))
				{
					TraceQueue.Enqueue(curEvent.ToEvent());

					//Debug.Log($"Traced event {curEvent.ToEvent()}");
				}
				Trace.Clear();

				ProcessEvents(TraceQueue);
			}
			else
			{
				//this shouldn't happen, but just in case:
				Debug.LogError("Can't trace input; Trace is null!");
			}
		}

		public void ProcessEvents(Queue<InputEvent> queue)
		{
			//first, figure out how much data we can send at once (due to packet/buffer size limits)

			//don't need to do this, but we might need this later:
			//FieldInfo mbp = typeof(NetworkManager).GetField("m_MaxBufferedPackets", BindingFlags.Instance | BindingFlags.NonPublic);
			//int maxPackets = (int)mbp.GetValue(UniCAVENetworkManager);
			//not sure if using reflection every frame would affect performance too much, but could just make this a serialized field
			//	user would have to make sure it has the correct value in the inspector

			int maxItems = MaxPacketSize / Marshal.SizeOf(typeof(UniCAVEInputSystem.InputEventStruct));
			//probably need to change this later, since I'm pretty sure the actual event data will have a variable size
			//	(since there is a sizeInBytes field...)
			//	make an extension method for the struct that returns its total size

			//send data in chunks until the queue is empty
			int sentChunks = 0;
			while(queue.Count > 0)
			{
				//can't send more items than we have in the queue!
				maxItems = Mathf.Clamp(maxItems, 0, queue.Count);

				//fill this chunk from the queue
				UniCAVEInputSystem.InputEventStruct[] ies = new UniCAVEInputSystem.InputEventStruct[maxItems];
				for(int i = 0; i < maxItems; i++)
				{
					ies[i] = new UniCAVEInputSystem.InputEventStruct(queue.Dequeue());
				}

				//when this chunk is full, send it
				RpcProcessEvents(ies);
				sentChunks++;
			}
		}

		//[ClientRpc]
		void RpcProcessEvents(UniCAVEInputSystem.InputEventStruct[] ies)
		{
			if(ShouldReceive)
			{
				//process the array of input events in the same order they were sent from the head node
				for(int i = 0; i < ies.Length; i++)
				{
					SendEvent(ies[i].ToInputEvent());
				}
			}
		}

		public void SendEvent(InputEvent ie)
		{
			//send the event to the input queue
			UniCAVEInputSystem.HeadNodeInput.Enqueue(ie);

			//Debug.Log($"Sent event {ie}");
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
				//start tracing events
				Trace = new InputEventTrace();
				Trace.Enable();
			}
		}

		void OnDisable()
		{
			if(ShouldTrace)
			{
				if(Trace != null)
				{
					//dispose of trace (need to do this according to docs)
					Trace.Dispose();
					Trace = null;
				}
			}
		}
	}
}