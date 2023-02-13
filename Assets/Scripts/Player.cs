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
	private Vector3 finalVel;

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
				walkvel = Walk();

				IntoRoll();

				if (Jump())
				{
					moveState = MovementState.jumping;
				}

				Gravity();

				break;
			case MovementState.jumping: // in the air
				walkvel = Walk();

				IntoRoll();

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
			case MovementState.dbJumping: // double jumping
				walkvel = Walk();
				Gravity();

				IntoRoll();

				if (controller.isGrounded)
				{
					moveState = MovementState.walking;
				}

				break;
			case MovementState.rolling: // rolling

				Roll();

				if (Input.GetButtonUp("Crouch"))
				{
					Vector3 TargetAngle = new(0f, cam.rotation.eulerAngles.y, 0f);
					
					StartCoroutine(LerpUp(Quaternion.Euler(TargetAngle), 0.1f));

					moveState = controller.isGrounded ? MovementState.walking : LastState;
				}

				break;
			default:
				break;
		}

		if (controller.enabled)
		{
			finalVel = walkvel + gravityVel;

			controller.Move(finalVel);
		}
	}

	Vector3 Walk()
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

		if (direction.magnitude >= 0.1f)
		{
			// turning the player model (Mesh)
			float targetAngle = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg) + cam.eulerAngles.y;// determine what direction the player should turn to
			float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSpeed);// smooth it out to the mesh doesent just snap in every direction
			transform.rotation = Quaternion.Euler(0f, angle, 0);// turn the mesh

			// moving the player

			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;// make it so the player moves in the direction they look

			float magnitude = Mathf.Clamp01(moveDir.magnitude);// determine how powerful the movement direction is using magnitude before normalisation

			return magnitude * speed * Time.deltaTime * moveDir.normalized;
		}

		return Vector3.zero;
	}

	void Gravity()
	{
		if (controller.isGrounded && gravityVel.y < 0)
		{
			gravityVel.y = -0.01f;
		}
		
		gravityVel.y += gravity * Time.deltaTime;

		gravityVel.y = Mathf.Clamp(gravityVel.y, -0.5f, 0.5f);
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

	void Roll()
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 direction= new(horizontal, 0f, vertical);

		if(direction.magnitude > 0.1f)
		{
			float targetAngle = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg) + cam.eulerAngles.y;

			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.right;

			float magnitude = Mathf.Clamp01(moveDir.magnitude);

			rb.AddTorque(magnitude * 100000 * moveDir.normalized, ForceMode.Force);
		}
	}

	IEnumerator LerpUp(Quaternion endValue, float duration)
	{
		float time = 0;
		Quaternion startValue = transform.rotation;
		while (time < duration)
		{
			transform.rotation = Quaternion.Lerp(startValue, endValue, time / duration);
			time += Time.deltaTime;
			yield return null;
		}
		transform.rotation = endValue;

		controller.enabled = true;
		rb.isKinematic = true;
	}

	void IntoRoll()
	{
		if (Input.GetButtonDown("Crouch") && controller.enabled)
		{
			LastState = moveState;

			moveState = MovementState.rolling;

			bool wasGrounded = controller.isGrounded;

			controller.enabled = false;
			rb.isKinematic = false;

			Vector3 vel = new(controller.velocity.x, Mathf.Clamp(controller.velocity.y, -5, 5), controller.velocity.z);

			rb.velocity = vel;
			rb.angularVelocity = wasGrounded ? Vector3.zero : cam.right * 10;
		}
	}
}
