using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UniCAVE
{
    public class UniCAVEInputReceiver : MonoBehaviour
    {
		public void ProcessQueue(Queue<InputEvent> queue)
		{
			//process InputEvents in the order they were sent
			while(queue.Count > 0)
			{
				InputEvent ie = queue.Dequeue();
				ProcessEvent(ie);
			}
		}

		public void ProcessEvent(InputEvent ie)
		{
			Debug.Log($"Received event: {ie}");
		}

		//make this LateUpdate...?
		void Update()
		{
			ProcessQueue(UniCAVEInputSystem.HeadNodeInput);
		}
	}
}