﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Assets.Scripts.Tokens;

namespace Assets.Scripts.Player.AI
{
	/// <summary>
	/// Rushes the opponent to fire at close range.
	/// </summary>
	public class RushEnemy : IPolicy
	{
		/// <summary> The desired horizontal distance between the AI and the target. </summary>
		internal float targetDistance;

		/// <summary> The object that the AI is targeting. </summary>
		internal GameObject target;

		/// <summary> The AI's speed on the previous tick.</summary>
		private float lastSpeed;

		/// <summary> Timer for allowing the AI to turn. </summary>
		private float turnTimer;
		/// <summary> Tick cooldown for the AI turning. </summary>
		private const float TURNCOOLDOWN = 0.5f;

		/// <summary> The distance away from a ledge that the AI will tolerate. </summary>
		private const float LEDGEGRABDISTANCE = 0.6f;

		/// <summary> The ledges in the scene. </summary>
		private static GameObject[] ledges;
		/// <summary> The name of the level that ledges are currently cached for. </summary>
		private static string levelName;

		/// <summary>
		/// Initializes a new AI.
		/// </summary>
		/// <param name="targetDistance">The desired horizontal distance between the AI and the opponent.</param>
		internal RushEnemy(float targetDistance) {
			this.targetDistance = targetDistance;
			string sceneName = SceneManager.GetActiveScene().name;
			if (ledges == null || levelName != sceneName)
			{
				// Load the level's edges if not already cached.
				ledges = GameObject.FindGameObjectsWithTag("Ledge");
				levelName = sceneName;
			}
		}

		/// <summary>
		/// Picks an action for the character to do every tick.
		/// </summary>
		/// <param name="controller">The controller for the character.</param>
		public void ChooseAction(AIController controller)
		{
			if (target == null) {
				controller.SetRunInDirection(0);
				return;
			}
			Controller targetController = target.GetComponent<Controller>();
			if (targetController != null && targetController.LifeComponent.Health <= 0)
			{
				controller.SetRunInDirection(0);
				return;
			}

			lastSpeed = controller.runSpeed;

			controller.jump = false;
			float currentTargetDistance = targetDistance;
			Vector3 opponentOffset = target.transform.position - controller.transform.position;
			Vector3 targetOffset = opponentOffset;
			float distanceTolerance = targetDistance - 1;

			// Check if there is a platform in the way of shooting.
			RaycastHit hit;
			controller.HasClearShot(opponentOffset, out hit);

			Transform blockingLedge = null;
			if (hit.collider != null)
			{
				// If an obstacle is in the way, move around it.
				float closestDistance = Mathf.Infinity;
				// Find ledges on the obstructing platform.
				BoxCollider[] children = hit.collider.GetComponentsInChildren<BoxCollider>();
				if (children.Length == 1 && hit.collider.transform.parent != null)
				{
					children = hit.collider.transform.parent.GetComponentsInChildren<BoxCollider>();
				}
				bool foundBetween = false;
				foreach (BoxCollider child in children)
				{
					if (child.tag == "Ledge")
					{
						Vector3 ledgeOffset = child.transform.position - controller.transform.position;
						if (Mathf.Sign(ledgeOffset.y) == Mathf.Sign(targetOffset.y)) {
							// Look for the closest ledge to grab or fall from.
							float currentDistance = Mathf.Abs(child.transform.position.x - controller.transform.position.x);
							bool between = BetweenX(child.transform, controller.transform, target.transform);
							if (!(foundBetween && !between) && currentDistance < closestDistance || !foundBetween && between)
							{
								foundBetween = foundBetween || between;
								// Make sure the edge isn't off the side of the map.
								float edgeMultiplier = 3;
								if (Physics.Raycast(child.transform.position + Vector3.down * 0.5f + Vector3.left * edgeMultiplier, Vector3.down, 30, AIController.LAYERMASK) ||
									Physics.Raycast(child.transform.position + Vector3.down * 0.5f + Vector3.right * edgeMultiplier, Vector3.down, 30, AIController.LAYERMASK))
								{
									// Don't target ledges that have already been jumped over.
									if ((currentDistance > LEDGEGRABDISTANCE * 2 / 3 || child.transform.position.y >= controller.transform.position.y + 1.5f || BetweenX(blockingLedge, target.transform, child.transform)) && child.transform.position.y < controller.transform.position.y + 5)
									{
										blockingLedge = child.transform;
										closestDistance = currentDistance;
									}
								}
							}
						}
					}
				}
			}

			Transform gapLedge = null;
			RaycastHit under;
			Physics.Raycast(controller.transform.position + Vector3.up, Vector3.down, out under, 30, AIController.LAYERMASK);
			if (blockingLedge == null)
			{
				if (hit.collider != null || !Physics.Raycast(controller.transform.position + Vector3.right * Mathf.Sign(opponentOffset.x), Vector3.down, out under, 30, AIController.LAYERMASK))
				{
					// If the ranger and its target are not on the same platform, and there's a gap between, go to a nearby ledge.
					RaycastHit underTarget;
					Physics.Raycast(target.transform.position + Vector3.up, Vector3.down, out underTarget, 30, AIController.LAYERMASK);
					if (under.collider == null)
					{
						Physics.Raycast(controller.transform.position + Vector3.up + Vector3.right, Vector3.down, out under, 30, AIController.LAYERMASK);
					}
					if (under.collider == null)
					{
						Physics.Raycast(controller.transform.position + Vector3.up + Vector3.left, Vector3.down, out under, 30, AIController.LAYERMASK);
					}
					if (under.collider != null && underTarget.collider != null && under.collider.gameObject != underTarget.collider.gameObject)
					{
						float closestLedgeDistance = Mathf.Infinity;
						foreach (GameObject ledge in ledges)
						{
							float currentDistance = Mathf.Abs(ledge.transform.position.x - controller.transform.position.x);
							bool between = hit.collider != null || BetweenX(ledge.transform, controller.transform, target.transform, 1);
							if (currentDistance < closestLedgeDistance && 
								ledge.transform.position.y > controller.transform.position.y - 1 && ledge.transform.position.y < controller.transform.position.y + 5 &&
								between)
							{
								gapLedge = ledge.transform;
								closestLedgeDistance = currentDistance;
							}
						}
					}
				}
			}

			Transform closestLedge;
			if (blockingLedge == null)
			{
				closestLedge = gapLedge;
			}
			else if (gapLedge == null)
			{
				closestLedge = blockingLedge;
			}
			else
			{
				if (Vector3.Distance(blockingLedge.position, controller.transform.position) <= Vector3.Distance(gapLedge.position, controller.transform.position))
				{
					closestLedge = blockingLedge;
				}
				else
				{
					closestLedge = gapLedge;
				}
			}

			Debug.Log(blockingLedge + ":" + gapLedge + ":" + closestLedge);

			if (closestLedge != null)
			{
				// Move towards the nearest ledge, jumping if needed.
				Vector3 closestVector = closestLedge.position - controller.transform.position;
				if (closestLedge.position.x - closestLedge.parent.position.x > 0)
				{
					closestVector.x += LEDGEGRABDISTANCE;
				}
				else
				{
					closestVector.x -= LEDGEGRABDISTANCE;
				}
				if (Math.Abs(closestVector.x) < 1f)
				{
					controller.jump = opponentOffset.y > 0 || gapLedge != null;
				}
				else
				{
					controller.jump = false;
				}
				currentTargetDistance = 0;
				distanceTolerance = 0.1f;
				targetOffset = closestVector;
			}
			else if (Physics.Raycast(controller.transform.position + Vector3.up * 0.1f, opponentOffset, Vector3.Magnitude(opponentOffset), AIController.LAYERMASK))
			{
				// Jump when just below a ledge with the target in sight.
				controller.jump = true;
			}
			Debug.DrawRay(controller.transform.position, targetOffset, Color.red);

			// Check if the AI is falling to its death.
			if (under.collider == null)
			{
				// Find the closest ledge to go to.
				closestLedge = null;
				float closestLedgeDistance = Mathf.Infinity;
				foreach (GameObject ledge in ledges)
				{
					float currentDistance = Mathf.Abs(ledge.transform.position.x - controller.transform.position.x);
					if (currentDistance < closestLedgeDistance && ledge.transform.position.y < controller.transform.position.y + 1)
					{
						closestLedge = ledge.transform;
						closestLedgeDistance = currentDistance;
					}
				}
				bool awayFromLedge = false;
				if (closestLedge == null)
				{
					controller.SetRunInDirection(-controller.transform.position.x);
				}
				else {
					float ledgeOffsetX = closestLedge.position.x - controller.transform.position.x;
					if (Mathf.Abs(ledgeOffsetX) > LEDGEGRABDISTANCE)
					{
						controller.SetRunInDirection(ledgeOffsetX);
						awayFromLedge = true;
					}
				}
				controller.jump = true;
				if (awayFromLedge)
				{
					return;
				}
			}

			if (currentTargetDistance > 0 && targetOffset.y < -1 && (closestLedge != null || Mathf.Abs(opponentOffset.x) > 1))
			{
				// Move onto a platform if a ledge was just negotiated.
				currentTargetDistance = 0;
			}

			// Move towards the opponent.
			float horizontalDistance = Mathf.Abs(targetOffset.x);
			if (horizontalDistance > currentTargetDistance)
			{
				controller.SetRunInDirection(targetOffset.x);
			}
			else if (horizontalDistance < currentTargetDistance - distanceTolerance)
			{
				controller.SetRunInDirection(-targetOffset.x);
			}
			else if (opponentOffset == targetOffset && under.collider != null && (controller.ParkourComponent.FacingRight ^ opponentOffset.x > 0))
			{
				controller.ParkourComponent.FacingRight = opponentOffset.x > 0;
			}
			else
			{
				controller.runSpeed = 0;
			}
			if (controller.runSpeed != 0)
			{
				// Don't chase an opponent off the map.
				Vector3 offsetPosition = controller.transform.position;
				offsetPosition.x += controller.runSpeed;
				offsetPosition.y += 0.5f;
				Vector3 offsetPosition3 = offsetPosition;
				offsetPosition3.x += controller.runSpeed * 2;
				if (!Physics.Raycast(offsetPosition, Vector3.down, out hit, 30, AIController.LAYERMASK) &&
					!Physics.Raycast(offsetPosition3, Vector3.down, out hit, 30, AIController.LAYERMASK))
				{
					if (controller.ParkourComponent.Sliding)
					{
						controller.SetRunInDirection(-opponentOffset.x);
					}
					else if (closestLedge == null)
					{
						controller.runSpeed = 0;
					}
					controller.slide = false;
				}
				else
				{
					// Slide if the opponent is far enough away for sliding to be useful.
					controller.slide = horizontalDistance > targetDistance * 2;
				}
			}

			if (controller.runSpeed == 0 && Mathf.Abs(opponentOffset.x) < 1 && opponentOffset.y < 0 && target.GetComponent<Controller>() && controller.GetComponent<Rigidbody>().velocity.y <= Mathf.Epsilon)
			{
				// Don't sit on top of the opponent.
				controller.SetRunInDirection(-controller.transform.position.x);
			}

			if (controller.runSpeed > 0 && lastSpeed < 0 || controller.runSpeed < 0 && lastSpeed > 0)
			{
				// Check if the AI turned very recently to avoid thrashing.
				turnTimer -= Time.deltaTime;
				if (turnTimer <= 0) {
					turnTimer = TURNCOOLDOWN;
				} else {
					controller.runSpeed = 0;
				}
			}

			// Jump to reach some tokens.
			if (targetDistance == 0 && controller.runSpeed == 0 && target.GetComponent<ArrowToken>()) {
				controller.jump = true;
			}
		}

		/// <summary>
		/// Checks whether an object is between two other objects in the x direction.
		/// </summary>
		/// <returns>Whether the object is between two other objects in the x direction.</returns>
		/// <param name="middle">The object to check for being between two others.</param>
		/// <param name="limit1">One object to be between.</param>
		/// <param name="limit2">The other object to be between.</param>
		/// <param name="tolerance>Tolerance for how far the object can be outside the bounds.</param>
		private bool BetweenX(Transform middle, Transform limit1, Transform limit2, float tolerance = 0)
		{
			if (middle == null || limit1 == null || limit2 == null)
			{
				return false;
			}
			if (limit2.position.x < limit1.position.x)
			{
				Transform temp = limit1;
				limit1 = limit2;
				limit2 = temp;
			}
			return Mathf.Sign(middle.position.x - (limit1.position.x - tolerance)) != Mathf.Sign(middle.position.x - (limit2.position.x + tolerance));
		}
	}
}