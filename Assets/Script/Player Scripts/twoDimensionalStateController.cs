using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class twoDimensionalStateController : MonoBehaviour
{
    Animator animator;
    [SerializeField] public CharacterController characterController;
    
    [HideInInspector] public float velocityZ = 0.0f;
    [HideInInspector] public float velocityX = 0.0f;
    public float acceleration = 2.0f;
    public float deceleration = 2.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;
    public float gravity = -9.81f;
    
    public bool IsRunning { get; private set; }
    
    public FootstepManager leftFootstepManager;
    public FootstepManager rightFootstepManager;
    
    int VelocityZHash;
    int VelocityXHash;
    private float velocityY = 0f;

    // --- OVERRIDE SYSTEM ---
    // Allows other scripts to "Press W/A/S/D" virtually
    public bool inputOverride = false;
    public float overrideVertical = 0f;   
    public float overrideHorizontal = 0f; 
    public bool overrideRun = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        VelocityZHash = Animator.StringToHash("Velocity Z");
        VelocityXHash = Animator.StringToHash("Velocity X");
    }

    public void StepLeft() => leftFootstepManager?.Step();
    public void StepRight() => rightFootstepManager?.Step();

    void Update()
    {
        bool forwardPressed, leftPressed, rightPressed, runPressed;

        if (inputOverride)
        {
            forwardPressed = overrideVertical > 0.1f;
            leftPressed = overrideHorizontal < -0.1f;
            rightPressed = overrideHorizontal > 0.1f;
            runPressed = overrideRun;
        }
        else
        {
            forwardPressed = Input.GetKey(KeyCode.W);
            leftPressed = Input.GetKey(KeyCode.A);
            rightPressed = Input.GetKey(KeyCode.D);
            runPressed = Input.GetKey(KeyCode.LeftShift);
        }

        float currentMaxVelocity = runPressed ? maximumRunVelocity : maximumWalkVelocity;

        // Z Axis Logic
        if (forwardPressed && velocityZ < currentMaxVelocity)
            velocityZ += Time.deltaTime * acceleration;
        else if (!forwardPressed && velocityZ > 0.0f)
            velocityZ -= Time.deltaTime * deceleration;
        
        if (!forwardPressed && velocityZ < 0.05f) velocityZ = 0.0f;

        // X Axis Logic
        if (leftPressed && velocityX > -currentMaxVelocity)
            velocityX -= Time.deltaTime * acceleration;
        else if (rightPressed && velocityX < currentMaxVelocity)
            velocityX += Time.deltaTime * acceleration;
        else if (!leftPressed && !rightPressed && velocityX != 0.0f)
        {
            if (velocityX > 0) velocityX -= Time.deltaTime * deceleration;
            if (velocityX < 0) velocityX += Time.deltaTime * deceleration;
            if (Mathf.Abs(velocityX) < 0.05f) velocityX = 0f;
        }

        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
        
        if (characterController.isGrounded) velocityY = 0f;
        else velocityY += gravity * Time.deltaTime;

        Vector3 movement = new Vector3(velocityX, velocityY, velocityZ);
        movement = transform.TransformDirection(movement);
        characterController.Move(movement * Time.deltaTime);

        IsRunning = runPressed && (forwardPressed || leftPressed || rightPressed);

        if (!inputOverride && Input.GetKeyDown(KeyCode.E))
        {
            animator.SetTrigger("Wave");
        }
    }
}