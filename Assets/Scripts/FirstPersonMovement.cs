using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
using TriTools;
using Rewired;

public class FirstPersonMovement : NetworkedMonoBehavior {
    [Header("Player Variables")]
    [Space]
    public Transform FirstPersonCamera;
    public GameObject CharacterModel;

    public enum C_State
    {
        Idle,
        Moving,
        Jumping,
        Falling
    }
    public C_State CurrentState = new C_State();
    public float movementSpeed;
    public float gravityForce,jumpForce;
    private float vSpeed=0; // current vertical velocity
    public float groundedHeight;
    public bool isJumping,isGrounded;
    public float rotationSpeed;
    public float cameraSensitivity;
    private float rotH, rotV,rotF;
    public float cameraMaxAngle, cameraMinAngle;
    public bool invertCamVertical, invertCamHorizontal;

    private Vector3 initialCameraRotation;
    private Vector3 MovementVector;
    private Vector3 RotationVector;
    private Vector3 ModelLookVector;
    private CharacterController CharController;
    private Player thisPlayer;
    private RaycastHit groundHit;
	void Awake ()
    {
        thisPlayer = ReInput.players.GetPlayer((int)OwnerId+1);
        CharController = GetComponent<CharacterController>();
        initialCameraRotation = FirstPersonCamera.rotation.eulerAngles;
    }
	
	protected override void UnityUpdate()
    {
        base.UnityUpdate();
        if (!IsOwner)
            return;
        if (CharController.isGrounded)
            isGrounded = true;
        if (!CharController.isGrounded)
            isGrounded=AssessGround();
        CurrentState = CalculateState();
        switch (CurrentState)
        {
            case C_State.Idle:
                CreateMovementVector();
                vSpeed = 0;
                break;
            case C_State.Moving:
                CreateMovementVector();
                vSpeed = 0;
                break;
            case C_State.Jumping:
                CreateMovementVector();
                vSpeed = jumpForce;
                AffectMovementVectorY(vSpeed);
                break;
            case C_State.Falling:
                CreateMovementVector();
                vSpeed -= gravityForce * Time.deltaTime;
                AffectMovementVectorY(vSpeed);
                break;
            default:
                break;
        }
        CharController.Move(MovementVector);
        #region Rotation
        rotV += initialCameraRotation.x + thisPlayer.GetAxis("CamHorizontal");
        if(invertCamVertical)
            rotH += initialCameraRotation.y + thisPlayer.GetAxis("CamVertical");
        else rotH += initialCameraRotation.y - thisPlayer.GetAxis("CamVertical");
        rotF = 0;
        rotH = Mathf.Clamp(rotH, cameraMinAngle, cameraMaxAngle);
        RotationVector = new Vector3(rotH* cameraSensitivity, rotV* cameraSensitivity, rotF);
        TriToolHub.SetRotation(FirstPersonCamera.gameObject, RotationVector, Space.Self);
        ModelLookVector = new Vector3(-90, FirstPersonCamera.localEulerAngles.y, CharacterModel.transform.localRotation.z);
        TriToolHub.SetRotation(CharacterModel, ModelLookVector, Space.Self);
        #endregion
    }
    private C_State CalculateState()
    {
        if (thisPlayer.GetButton("Jump"))
            return C_State.Jumping;
        if (!isGrounded)
            return C_State.Falling;
        if (thisPlayer.GetAxis2D("Horizontal", "Vertical").magnitude > 0.1f)
            return C_State.Moving;
        return C_State.Idle;
    }
    private bool AssessGround()
    {
        Physics.SphereCast(transform.position, 1f, Vector3.down,out groundHit);
        if (groundHit.distance <= groundedHeight)
        {
            return true;
        }
        else return false;
    }
    private void CreateMovementVector()
    {
        TriToolHub.CreateVector3(thisPlayer.GetAxis("Horizontal"),
           thisPlayer.GetAxis("Vertical"),
           movementSpeed,
           TriToolHub.AxisPlane.XZ,
           FirstPersonCamera.gameObject,
           out MovementVector);
    }
    private void AffectMovementVectorY(float amount)
    {
        MovementVector.y = amount;
    }
    protected override void OnApplicationQuit()
    {
        Networking.Destroy(this);
    }
}
