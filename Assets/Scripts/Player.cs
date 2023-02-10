using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	public enum MovementState
	{
		frozen,
		walking,
		jumping,
		dbJumping
	}

	[Header("Proporties")]

	public MovementState moveState;

	public float speed = 10f;

	public float turnSpeed = 0.1f;

	public float gravity = -0.1f;

	public float jumpPower = 0.005f;

	private float turnVel;
	private Vector3 gravityVel;

	[SerializeField]
	private float forceMagnetude;

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

	// executes when tyhe controller hits a rigidbody
	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody rigidbody = hit.collider.attachedRigidbody;

		if (rigidbody != null)
		{
			Vector3 forceDir = hit.gameObject.transform.position - transform.position;
			forceDir.y = 0f;
			forceDir.Normalize();

			rigidbody.AddForceAtPosition(forceDir * forceMagnetude, transform.position, ForceMode.Impulse);
		}
	}

	void StateBlock()
	{
		switch (moveState)
		{
			case MovementState.frozen: // not moving
				break;
			case MovementState.walking: // walking on the ground
				Move();
				
				if (Jump())
				{
					moveState = MovementState.jumping;
				}

				Gravity();

				break;
			case MovementState.jumping: // in the air
				Move();

				if (Jump())
				{
					moveState = MovementState.dbJumping;
				}

				Gravity();

				if (controller.isGrounded)
				{
					moveState = MovementState.walking;
				}

				break;
			case MovementState.dbJumping:
				Move();
				Gravity();

				if (controller.isGrounded)
				{
					moveState = MovementState.walking;
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

			float magnitude = Mathf.Clamp01(moveDir.magnitude);// determine how powerful the movement direction is using magnitude before normalisation

			controller.Move(magnitude * speed * Time.deltaTime * moveDir.normalized);
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

	bool Jump()
	{
		if (Input.GetButtonDown("Jump")) 
		{
			gravityVel.y = Mathf.Sqrt(jumpPower * -2.0f * gravity);

			return true;
		}

		return false;
	}
}
