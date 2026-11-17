using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Sun_Temple{

	public class CursorLock : MonoBehaviour {

		private bool isLocked;

		void Start(){
			isLocked = true;
		}


	

		void Update(){
			
			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
				if (isLocked) {
					isLocked = false;
				} else if (!isLocked) {
					isLocked = true;
				}
			}



			if (isLocked) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}

			if (!isLocked) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

			


			
	
	}

}
