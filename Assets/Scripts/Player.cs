using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	public enum movementState
	{
		frozen,
		walking,
		jumping
	}

	[Header("Proporties")]

	public movementState moveState;

	public float speed = 10f;

	public float turnSpeed = 0.1f;

	public float gravity = -0.1f;

	public float jumpPower = 0.01f;

	private float turnVel;
	private Vector3 gravityVel;

	[Header("References")]

	public CharacterController controller;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
	public Transform camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

	// Start is called before the first frame update
	void Start()
	{
		print("hello world!");

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible= false;
	}

	// Update is called once per frame
	void Update()
	{
		StateBlock();
	}

	void StateBlock()
	{
		switch (moveState)
		{
			case movementState.frozen:
				break;
			case movementState.walking:
				Move();
				Jump();
				Gravity();

				break;
			case movementState.jumping:
				Move();
				Gravity();

				if (controller.isGrounded)
				{
					moveState = movementState.walking;
				}

				break;
			default:
				break;
		}
	}

	void Move()
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

		if (direction.magnitude >= 0.1f)
		{
			// turning the player model (Mesh)
			float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camera.eulerAngles.y;// determine what direction the player should turn to
			float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSpeed);// smooth it out to the mesh doesent just snap in every direction
			transform.rotation = Quaternion.Euler(0f, angle, 0);// turn the mesh

			// moving the player

			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;// make it so the player moves in the direction they look
			controller.Move(moveDir.normalized * speed * Time.deltaTime);
		}
	}

	void Gravity()
	{
		if (controller.isGrounded && gravityVel.y < 0)
		{
			gravityVel.y = -0.01f;
		}
		
		gravityVel.y += gravity * Time.deltaTime;

		gravityVel.y = Mathf.Clamp(gravityVel.y, -0.5f, 0.5f);

		controller.Move(gravityVel);
	}

	void Jump()
	{
		if (Input.GetButtonDown("Jump")) 
		{
			moveState = movementState.jumping;

			gravityVel.y = Mathf.Sqrt(jumpPower * -2.0f * gravity);
		}
	}
}
