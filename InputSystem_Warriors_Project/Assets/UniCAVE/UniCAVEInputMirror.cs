using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace UniCAVE
{
    public class UniCAVEInputMirror : NetworkBehaviour, System.IObserver<InputRemoting.Message>
    {
        [SerializeField]
        InputActionAsset Actions;

		List<InputAction> SubscribedActions = new List<InputAction>();

		//basic idea:
		//for every action in the chosen asset, subscribe to their events
		//whenever any events are invoked, send something to the child nodes that invokes the same event there

		//only look at specific maps?

		void ActionInvoked(InputAction.CallbackContext cc)
		{
			//Debug.Log($"Mirroring {cc.action}...");

			//RpcMirrorAction(cc.);
		}

		public void SubscribeToAction(InputAction inputAction)
		{
			inputAction.performed += ActionInvoked;
			SubscribedActions.Add(inputAction);

			//Debug.Log($"Subscribed to InputAction {inputAction.name}.");
		}

		public void UnsubscribeFromAction(InputAction inputAction)
		{
			inputAction.performed -= ActionInvoked;
			SubscribedActions.Remove(inputAction);

			//Debug.Log($"Unsubscribed from InputAction {inputAction.name}.");
		}

		[ClientRpc]
		void RpcMirrorAction(string action)
		{
			if(isServer) return;

			//mirror the action that occured on the server
			//InputSystem.remoting
		}

		public void OnCompleted()
		{
			Debug.LogError($"InputMirror OnCompleted");
		}

		public void OnError(System.Exception error)
		{
			Debug.LogError($"InputMirror error: {error.Message}");
		}

		public void OnNext(InputRemoting.Message value)
		{
			Debug.LogError($"InputMirror OnNext ({value})");
		}

		//send byte array over an rpc call
		//on the client, turn this byte array back into an event and have the inputmanager queue it

		void OnEnable()
		{
			if(isServer)
			{
				foreach(InputAction inputAction in Actions)
				{
					SubscribeToAction(inputAction);
				}

				//start input remoting
				InputSystem.remoting.StartSending();
			}
			else
			{
				//subscribe to remoting
				InputSystem.remoting.Subscribe(this);
			}
		}

		void OnDisable()
		{
			if(isServer)
			{
				foreach(InputAction inputAction in Actions)
				{
					UnsubscribeFromAction(inputAction);
				}

				//stop input remoting
				InputSystem.remoting.StopSending();
			}
			else
			{
				//unsubscribe from remoting
				
			}
		}
	}
}