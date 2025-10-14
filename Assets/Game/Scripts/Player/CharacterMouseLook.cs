using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
	public class CharacterMouseLook : MonoBehaviour 
	{
		public float sensitivityX = 5F;
		public NetworkObject networkObject;
		[HideInInspector] public bool isLocalTesting;
		
		private void Update ()
		{
			if (isLocalTesting)
			{
				transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
			}
			else if(networkObject.IsOwner)
			{
				transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
			}
		}
	}
}