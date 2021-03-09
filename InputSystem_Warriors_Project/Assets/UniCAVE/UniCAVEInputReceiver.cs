using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UniCAVE
{
	public class UniCAVEInputReceiver : MonoBehaviour
	{
		//convert structs sent over RPC to InputEvents
		//use InputSystem.QueueEvent to get InputEventPtrs

		public void ProcessQueue(Queue<InputEvent> queue)
		{
			//process InputEvents in the order they were sent
			while(queue.Count > 0)
			{
				InputEvent ie = queue.Dequeue();
				ProcessEvent(ie);
			}
		}

		public void ProcessQueuePtrs(Queue<InputEventPtr> queue)
		{
			while(queue.Count > 0)
			{
				InputEventPtr iep = queue.Dequeue();
				UniCAVEInputSystem.DebugEvent(iep);
				ProcessEventPtr(iep);
			}
		}

		public void ProcessEvent(InputEvent ie)
		{
			//Debug.LogError($"Received event: {ie}");
		}

		public void ProcessEventPtr(InputEventPtr iep)
		{
			//UniCAVEInputSystem.DebugEvent(iep);
			//Debug.LogError($"Received InputEventPtr {iep}");
			//send to UniCAVEInputPlayback to play back events...
		}

		//make this LateUpdate...?
		void Update()
		{
			//ProcessQueue(UniCAVEInputSystem.HeadNodeInput);
			ProcessQueuePtrs(UniCAVEInputSystem.HeadNodeInputPtrs);
		}
	}
}