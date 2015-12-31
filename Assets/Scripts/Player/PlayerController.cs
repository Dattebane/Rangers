﻿using UnityEngine;
using TeamUtility.IO;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Class that handles player specific components of the controller
    /// Uses input
    /// </summary>
    public class PlayerController : Controller
    {
        private bool fire;

        void Update()
        {
			

			if (InputManager.GetButtonDown("Jump_P" + (int)id, id)) parkour.Jump();
			if (InputManager.GetButtonDown("Crouch_P" + (int)id, id)) parkour.SlideOn();
			if (InputManager.GetButtonUp("Crouch_P" + (int)id, id)) parkour.SlideOff();
            // Updating the indicator of which direction the player is going to fire
            if (InputManager.GetAxis("LookHorizontal_P" + (int)id, id) == 0 && InputManager.GetAxis("LookVertical_P" + (int)id, id) == 0)
            {
                archery.UpdateFirePoint(Vector3.Normalize(Vector3.right + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f)) * distanceToPlayer);
            }
            else
            {
                archery.UpdateFirePoint(Vector3.Normalize(Vector3.Normalize(new Vector3(
                                          InputManager.GetAxis("LookHorizontal_P" + (int)id, id) + (Random.Range(-shake, shake)),
                                          InputManager.GetAxis("LookVertical_P" + (int)id, id) + (Random.Range(-shake, shake)),
                                          0))) * distanceToPlayer);
            }
            if (InputManager.GetAxis("Fire_P" + (int)id, id) > 0)
            {
                fire = true;
                shake = 0.1f;
            }
			else if (fire && (InputManager.GetAxis("Fire_P" + (int)id, id) == 0 || InputManager.GetAxis("Fire_P" + (int)id, id) == -1))
            {
				//I added a check for -1 because the Mac gamepad axis goes from -1 to 1, not 0 to 1
				//Feel free to find another solution if this messes with Win gamepad
				//--Kartik
                archery.Fire();
                fire = false;
                shake = 0f;
            }
        }

		void FixedUpdate() {
			//This has to happen every fixed update as of now, can't think of a better way to handle it --kartik
			parkour.Locomote(InputManager.GetAxis("Horizontal_P" + (int)id, id));
		}
    }
}
