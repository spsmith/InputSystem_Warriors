using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugMachineName : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Text;

	void Start()
	{
		if(Text)
		{
			Text.text = UniCAVE.Util.GetMachineName();
		}
	}
}
