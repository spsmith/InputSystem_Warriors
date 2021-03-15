using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UniCAVE
{
	public class UniCAVEInputReceiver : MonoBehaviour
	{
		[SerializeField]
		UniCAVEInputPlayback Playback;

		public void ProcessQueue(Queue<UniCAVEInputSystem.InputEventBytes> queue)
		{
			while(queue.Count > 0)
			{
				UniCAVEInputSystem.InputEventBytes ieb = queue.Dequeue();
				InputEventPtr iep = ieb.ToInputEventPtr();
				UniCAVEInputSystem.DebugEvent(iep);
				//ProcessEvent(iep);
			}
		}

		public void ProcessEvent(InputEventPtr iep)
		{
			UniCAVEInputSystem.DebugEvent(iep);

			//this is the part that's still broken
			//need to queue state or text event explicitly?
			if(iep.type.ToString() == "STAT")
			{
				Debug.LogError("STAT event");
				//InputSystem.QueueStateEvent<>
			}
			else if(iep.type.ToString() == "TEXT")
			{
				Debug.LogError("TEXT event");
				//InputSystem.QueueTextEvent(InputSystem.GetDeviceById(ieb.deviceId), )
			}
			else
			{
				Debug.LogError($"Unkown event type: {iep.type}");
			}

			//send to UniCAVEInputPlayback to play back events...
		}

		//make this LateUpdate...?
		void Update()
		{
			ProcessQueue(UniCAVEInputSystem.HeadNodeInput);
		}
	}
}