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
		

		[SerializeField]
		UCNetwork UniCAVENetwork;

		[SerializeField]
		NetworkManager UniCAVENetworkManager;

		[SerializeField]
		UniCAVEInputReceiver InputReceiver;

		[Header("Editor Options")]
		[SerializeField]
		bool TraceInEditor = true;

		[SerializeField]
		bool ReceiveInEditor = true;

		[SerializeField]
		bool AutoSimulatePhysicsInEditor = true;

		[SerializeField]
		bool DebugPrint = false;

		[Header("Network Options")]
		[SerializeField]
		bool SendInput = true;

		[SerializeField]
		bool SendPhysicsUpdates = true;

		UniCAVEPlayer Player => UniCAVEInputSystem.Player;

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

		//server should simulate physics
		bool ShouldSimulatePhysicsManually
		{
			get
			{
#if UNITY_EDITOR
				return !AutoSimulatePhysicsInEditor;
#else
				return Application.isPlaying && isServer;
#endif
			}
		}

		bool ReadyToSimulatePhysics
		{
			get
			{
#if UNITY_EDITOR
				return true;
#else
				return (isServer && UniCAVEInputSystem.PhysicsState == UniCAVEInputSystem.PhysicsStates.HeadWillSimulate) ||
					   (isClient);
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
					//turn into bytes now instead of later?
					TraceQueue.Enqueue(curEvent);

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

				if(SendInput) RpcProcessEventsBytes(ieb);
			}
		}

#if !UNITY_EDITOR
		[ClientRpc] //define flag is here to enable testing in the Editor
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
			UniCAVEInputSystem.HeadNodeInput.Enqueue(ieb);			
		}

#if !UNITY_EDITOR
		[ClientRpc]
#endif
		void RpcSimulatePhysics(float fixedDeltaTime)
		{
			Debug.LogError("Child is simulating");
			//should only be run on children
			UniCAVEInputSystem.SimulatePhysics(fixedDeltaTime);

			//wait for head to simulate before simulating again
			UniCAVEInputSystem.PhysicsState = UniCAVEInputSystem.PhysicsStates.WaitingForHeadSimulate;

			//tell head this child has simulated
			//commands can only be run from the player object
			Player.CmdChildHasSimulated(UniCAVE.Util.GetMachineName());
			Debug.LogError($"Child sent command (hasAuthority: {hasAuthority}, localPlayer: {isLocalPlayer})");
		}

		void SimulatePhysics(float fixedDeltaTime)
		{
			//Debug.LogError("Simulating on head node");

			//should only be run on head
			UniCAVEInputSystem.SimulatePhysics(fixedDeltaTime);

			//wait for children to simulate before simulating on head again
			UniCAVEInputSystem.PhysicsState = UniCAVEInputSystem.PhysicsStates.WaitingForChildSimulate;
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

		void FixedUpdate()
		{
			//if(Time.realtimeSinceStartup < 5) return;
			if(ShouldSimulatePhysicsManually)
			{
				//check if all children have simulated
				//Debug.LogError($"{UniCAVEPlayer.SimulatedChildren} out of {UniCAVEPlayer.Children} children have simulated");

				//number of players should equal number of unicave displays plus one (because the server also spawns a player)
				int totalChildren = UniCAVENetwork.GetAllDisplays().Count; //cache this value somewhere instead of checking every time...

				if(UniCAVEPlayer.Children >= totalChildren + 1)
				{
					//Debug.LogError($"Children are ready ({UniCAVEPlayer.Children} children, {totalChildren} displays)");
				}
				else
				{
					//don't simulate yet
					//Debug.LogError($"Children are not ready ({UniCAVEPlayer.Children} children, {totalChildren} displays)");
					return;
				}

				if(UniCAVEPlayer.SimulatedChildren >= UniCAVEPlayer.Children - 1) //minus one because one 'child' is the server, which doesn't simulate here
				{
					//all children have simulated, safe to simulate again on head node
					Debug.LogError("Head will simulate");
					UniCAVEInputSystem.PhysicsState = UniCAVEInputSystem.PhysicsStates.HeadWillSimulate;
					UniCAVEPlayer.SimulatedChildren = 0;
				}

				//Debug.LogError($"PhysicsState is {UniCAVEInputSystem.PhysicsState}");

				if(ReadyToSimulatePhysics)
				{
					//store current timescale
					float timescale = Time.timeScale;
					Time.timeScale = 1;

					float fixedDeltaTime = Time.fixedDeltaTime;

					//simulate physics as usual, but on head node only
					SimulatePhysics(fixedDeltaTime);

#if !UNITY_EDITOR
					//tell the child nodes to simulate physics as well
					if(SendPhysicsUpdates)
					{
						RpcSimulatePhysics(fixedDeltaTime);
						//SimulatingChildren = UniCAVENetwork.GetAllDisplays().Count;
					}
#endif

					//restore original timescale when done
					Time.timeScale = timescale;
				}
			}
		}

		void OnEnable()
		{
			//don't simulate physics automatically - only trigger the simulation manually
#if UNITY_EDITOR
			Physics.autoSimulation = AutoSimulatePhysicsInEditor;
#else
			if(SendPhysicsUpdates) Physics.autoSimulation = false;
#endif

			if(ShouldTrace)
			{
				EnableTrace();
			}
		}

		void Start()
		{
			Debug.Log($"Physics.autoSimulation is {Physics.autoSimulation}");

			if(isServer)
			{
				//head node
				UniCAVEInputSystem.PhysicsState = UniCAVEInputSystem.PhysicsStates.HeadWillSimulate;
			}
			else
			{
				//child node
				UniCAVEInputSystem.PhysicsState = UniCAVEInputSystem.PhysicsStates.WaitingForHeadSimulate;
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