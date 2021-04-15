using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Networking;

namespace UniCAVE
{
    public class UniCAVEInputPlayback : MonoBehaviour
    {
        //should accept events from the InputReceiver and play them back with the correct timing

        [SerializeField]
        [Range(0, 10)]
        float Delay = 0f;

        Dictionary<int, Queue<InputEventPtr>> Events = new Dictionary<int, Queue<InputEventPtr>>();

        public void QueueEvent(InputEventPtr iep, int frameNumber)
		{
            Queue<InputEventPtr> queue;
            if(!Events.ContainsKey(frameNumber))
            {
                queue = new Queue<InputEventPtr>();
                Events[frameNumber] = queue;
			}
			else
			{
                queue = Events[frameNumber];
			}
            queue.Enqueue(iep);
        }

        public bool ProcessQueue(int frameNumber, int numEvents)
		{
            InputEventPtr iep;
            if(!Events.ContainsKey(frameNumber)) return false;
            Queue<InputEventPtr> events = Events[frameNumber];
            if(events.Count != numEvents) return false;
            while(events.Count > 0)
            {
                iep = events.Dequeue();
                if(Delay > 0) StartCoroutine(PlayEventAfterTime(Delay, iep));
                else PlayEvent(iep);
            }
            return true;
		}

        void PlayEvent(InputEventPtr iep)
		{
            InputSystem.QueueEvent(iep);
		}

        IEnumerator PlayEventAfterTime(float time, InputEventPtr iep)
		{
            time = Mathf.Clamp(time, 0, float.MaxValue);
            yield return new WaitForSeconds(time);
            PlayEvent(iep);
		}

		void Update()
		{
            while(UniCAVEInputSystem.HeadNodeFrames.Count > 0)
            {
                //grab frame event data
                int numEvents = -1;
                int frameNumber = -1;
                UniCAVEInputSystem.FrameEvents frameEvents = UniCAVEInputSystem.HeadNodeFrames.Peek();
                while(frameEvents.numEvents == 0 && UniCAVEInputSystem.HeadNodeFrames.Count > 0)
                {
                    //skip empty frames
                    UniCAVEInputSystem.HeadNodeFrames.Dequeue();
                    if(UniCAVEInputSystem.HeadNodeFrames.Count > 0) frameEvents = UniCAVEInputSystem.HeadNodeFrames.Peek();
                }
                numEvents = frameEvents.numEvents;
                frameNumber = frameEvents.frameNumber;

                //for non empty frames, process events
                bool processed = true;
                if(numEvents > 0)
                {
                    processed = ProcessQueue(frameNumber, numEvents);
                }
				if(processed)
				{
                    if(UniCAVEInputSystem.HeadNodeFrames.Count > 0) UniCAVEInputSystem.HeadNodeFrames.Dequeue();
				}
				else
				{
                    break;
				}
            }
		}
	}
}