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
				ProcessEvent(iep);
			}
		}

		public void ProcessEvent(InputEventPtr iep)
		{
			Playback.PlayEvent(iep);
		}

		//make this LateUpdate...?
		void Update()
		{
			ProcessQueue(UniCAVEInputSystem.HeadNodeInput);
		}
	}
}