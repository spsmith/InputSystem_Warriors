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

        [SerializeField]
        bool ReceivePhysics = true;

        public void PlayEvent(InputEventPtr iep)
		{
            if(Delay > 0) StartCoroutine(PlayEventAfterTime(Delay, iep));
            else QueueEvent(iep);
        }

        void QueueEvent(InputEventPtr iep)
		{
            InputSystem.QueueEvent(iep);
		}

        public void SimulatePhysics(float fixedDeltaTime)
        {
            float timescale = Time.timeScale;
            Time.timeScale = 1;

            Physics.Simulate(fixedDeltaTime);

            Time.timeScale = timescale;
        }

        IEnumerator PlayEventAfterTime(float time, InputEventPtr iep)
		{
            time = Mathf.Clamp(time, 0, float.MaxValue);
            yield return new WaitForSeconds(time);
            QueueEvent(iep);
		}

        void FixedUpdate()
        {
            if(ReceivePhysics)
            {
                //check if physics needs to be simulated
                if(UniCAVEInputSystem.ShouldSimulatePhysicsThisFrame)
                {
                    SimulatePhysics(UniCAVEInputSystem.FixedDeltaTime);
                    UniCAVEInputSystem.ShouldSimulatePhysicsThisFrame = false;
                }
            }
        }
    }
}