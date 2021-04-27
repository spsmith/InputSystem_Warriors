using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace UniCAVE
{
    public class UniCAVEPlayer : NetworkBehaviour
    {
		public static int Children { get; private set; } = 0;

		//children that have simulated physics
		public static int SimulatedChildren = 0;

		bool Registered = false;

#if !UNITY_EDITOR
		[Command]
#endif
		public void CmdChildHasSimulated(string machineName)
		{
			//notify that this child node has simulated
			SimulatedChildren++;
			Debug.LogError($"{machineName} notified head; SimulatedChildren = {SimulatedChildren}");
		}

#if !UNITY_EDITOR
		[Command]
#endif
		public void CmdReady(string machineName)
		{
			//tell head node this child is ready
			Children++;
			Debug.LogError($"{machineName} is ready! There are {Children} children");
		}

		void Start()
		{
			Debug.LogError($"UniCAVEPlayer Start() (isLocalPlayer = {isLocalPlayer}) [{UniCAVE.Util.GetMachineName()}]");

			if(!isServer)
			{
				Debug.LogError($"Sending ready command [{UniCAVE.Util.GetMachineName()}]");
				CmdReady(UniCAVE.Util.GetMachineName());
				Registered = true;
				Debug.LogError($"__There are {Children} children (isServer = {isServer})");
			}
		}
	}
}
