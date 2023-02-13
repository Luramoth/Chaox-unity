using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Player : MonoBehaviour
{
	public enum MovementState
	{
		frozen,
		walking,
		jumping,
		dbJumping,
		rolling
	}

	[Header("Proporties")]

	public MovementState moveState;

	public float speed = 10f;

	public float turnSpeed = 0.1f;

	public float gravity = -0.1f;

	public float jumpPower = 0.005f;

	private float turnVel;

	private Vector3 walkvel;
	private Vector3 gravityVel;

	private MovementState LastState;

	[SerializeField]
	private float forceMagnetude;

	[Header("References")]

	public CharacterController controller;

	public Transform cam;

	public Rigidbody rb;

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
		Rigidbody rigidbody = hit.collider.attachedRigidbody; // get the rigidbody of the object that was hit

		if (rigidbody != null) //if the object has a rigidbody
		{
			Vector3 forceDir = hit.gameObject.transform.position - transform.position; // grab the direction
			forceDir.y = 0f;
			forceDir.Normalize();

			rigidbody.AddForceAtPosition(forceDir * forceMagnetude, transform.position, ForceMode.Impulse);// apply the force
		}
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////

	/*
	 this is the big part that makes the movement code function, it uses a switch statement relying on a Enum to say what the state the player currently is in
	 the reason why this worska s well as it does is because each state self govorns what state it can and cant get into.

	 this isent the most 'dynamic' system out there but its robust, only big issue with it is it relies on a lot of repetittion, so it's helped largely on
	 the use of functions so repetittion is kept to a minimum, the other issue is that no matter what, switching from one state to another, there will always be a
	 single frame of delay between a polled input and a state switch, but at large this isent going to be noticeable.
	 */
	void StateBlock()
	{
		switch (moveState)
		{
			case MovementState.frozen: // not moving
				break;
			case MovementState.walking: // walking on the ground
				walkvel = Move(); // get movement vector

				IntoRoll();// check if player is rolling

				if (Jump()) // if the player jumped, put them into the jumping state
				{
					moveState = MovementState.jumping;
				}

				Gravity(); // apply gravity

				break;
			case MovementState.jumping: // in the air after one jump
				walkvel = Move();

				IntoRoll();

				if (Jump())
				{
					moveState = MovementState.dbJumping;
				}

				Gravity();

				// if the ground is reached then go back to the walking state
				if (controller.isGrounded)
				{
					moveState = MovementState.walking;
				}

				break;
			case MovementState.dbJumping: // in the air after a second jump
				walkvel = Move();
				Gravity();

				IntoRoll();

				if (controller.isGrounded)
				{
					moveState = MovementState.walking;
				}

				break;
			case MovementState.rolling: // rolling
				Roll(); // use roll movement code

				if (Input.GetButtonUp("Crouch"))// if stopped crouching, then get upright and get back to a relevant state
				{
					Vector3 TargetAngle = new(0f, cam.rotation.eulerAngles.y, 0f); // use the direction the camera is facing in as the forward direction
					
					StartCoroutine(LerpUp(Quaternion.Euler(TargetAngle), 0.1f)); // smothly turn back upwards using a lerp
					// NOTE: ^^^^ THIS IS NOT MULTITHREDING! This just tell unity to run this code without halting the whole program!

					moveState = controller.isGrounded ? MovementState.walking : LastState; // if not on the ground then go back to the state the player was in before
				}

				break;
			default:
				break;
		}

		if (controller.enabled) // if the controller is enabled, then apply movements
		{
			Vector3 finalVel = walkvel + gravityVel;// combine the two velocities to make final movement

			controller.Move(finalVel);
		}
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////

	Vector3 Move() // code to make the movement velocity
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 direction = new(horizontal, 0f, vertical); // get the direction the player is trying to input

		if (direction.magnitude >= 0.1f)
		{
			// turning the player model (Mesh)
			float targetAngle = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg) + cam.eulerAngles.y;// determine what direction the player should turn to
			float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSpeed);// smooth it out to the mesh doesent just snap in every direction
			transform.rotation = Quaternion.Euler(0f, angle, 0);// turn the mesh

			// moving the player

			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;// make it so the player moves in the direction they look

			float magnitude = Mathf.Clamp01(moveDir.magnitude);// determine how powerful the movement direction is using magnitude before normalisation

			return magnitude * speed * Time.deltaTime * moveDir.normalized; // return the final vector to be used
		}

		return Vector3.zero; // the player aint moving so just make the result zeros
	}

	void Gravity() // code to apply gravity
	{
		if (controller.isGrounded && gravityVel.y < 0)
		{
			gravityVel.y = -0.01f; // if on the floor apply a bit of gravity so player isent just floating off the ground
		}
		
		gravityVel.y += gravity * Time.deltaTime; // actually apply gravity

		gravityVel.y = Mathf.Clamp(gravityVel.y, -0.5f, 0.5f);// clamp it down so it has a "terminal velocity"
	}

	bool Jump() // code to get jump working
	{
		if (Input.GetButtonDown("Jump")) // if the Jump button is pressed then return True and apply jump force
		{
			gravityVel.y = Mathf.Sqrt(jumpPower * -2.0f * gravity);

			return true;
		}

		return false;
	}

	void Roll() // cde to get roll working, very simular to movement code, might eventually get simplified with a general "GetInputDirection()" function or whatever
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 direction= new(horizontal, 0f, vertical);

		if(direction.magnitude > 0.1f)
		{
			float targetAngle = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg) + cam.eulerAngles.y;

			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.right;

			float magnitude = Mathf.Clamp01(moveDir.magnitude);

			rb.AddTorque(magnitude * 100000 * moveDir.normalized, ForceMode.Force); // apply a torque force for that physics-based movement (hai speedrunners)
		}
	}

	IEnumerator LerpUp(Quaternion endValue, float duration) // function to lerp the player back upright
	{
		float time = 0; // start the timer at 0
		Quaternion startValue = transform.rotation; // get current rotation for reference

		while (time < duration) // while the timer is still rolling..
		{
			transform.rotation = Quaternion.Lerp(startValue, endValue, time / duration); // lerp the rotation using the timer
			time += Time.deltaTime; // increment the timer
			yield return null; // tell unity to wait till next frame before continuing
		}

		transform.rotation = endValue; // once the timer is done, snap to the final rotation

		// now dissable the Rigidbody and enable the CharacterController
		controller.enabled = true;
		rb.isKinematic = true;
	}// this already gets done when you move around using the character controller but this is more polished, aditionally this function could be used for any rotation
	//	but i mainly wanted to use it so i dont have to install a bloated Tweening library just in case this really is my only need for somthign like this

	void IntoRoll() // bit of code to see if the player wants to roll
	{
		if (Input.GetButtonDown("Crouch") && controller.enabled) // if they are, do some initialisation and use the Roll state
		{
			LastState = moveState; // set the last state that was used before roll

			moveState = MovementState.rolling; // set the state to rolling

			bool wasGrounded = controller.isGrounded; // check if the player was origonally grounded

			// dissable CharacterController and enable the Rigidbody
			controller.enabled = false;
			rb.isKinematic = false;

			// give the Rigidbody the velocity that the player had (and clamp it so it doesent come with a huge jump boost)
			Vector3 vel = new(controller.velocity.x, Mathf.Clamp(controller.velocity.y, -5, 5), controller.velocity.z);
			rb.velocity = vel;

			rb.angularVelocity = wasGrounded ? Vector3.zero : cam.right * 10; // spin them a bit forward dpepending if they were grounded or not
		}
	}
}
