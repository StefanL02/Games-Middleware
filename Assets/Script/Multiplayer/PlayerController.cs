using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviourPun
{
    public CharacterController controller;
    public Camera playerCamera;
    public Transform handPosition;

    [Header("Settings")] public float speed = 3f;
    public float mouseSensitivity = 100f;
    public float throwForce = 1f;
    public string ballPrefabName = "Ball";

    private float xRotation = 0f;
    private Ball currentBall;

    void Start()
    {
        if (!photonView.IsMine)
        {
            if (playerCamera) Destroy(playerCamera.gameObject);
            Destroy(controller);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        HandleLook();
        HandleMove();
        HandleInteraction();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.Q) && currentBall == null)
        {
            SpawnBall();
        }

        if (Input.GetKeyDown(KeyCode.E) && currentBall == null)
        {
            TryPickup();
        }

        if (Input.GetMouseButtonDown(0) && currentBall != null)
        {
            ThrowBall();
        }
    }

    void SpawnBall()
    {
        GameObject ballObj = PhotonNetwork.Instantiate(ballPrefabName, handPosition.position, Quaternion.identity);
        float randomScale = Random.Range(0.15f, 1.15f);
        ballObj.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

        NetworkSpherePhysics nsp = ballObj.GetComponent<NetworkSpherePhysics>();
        if (nsp != null)
        {
            float volumeScale = randomScale * randomScale * randomScale; // scale³ for realism
            nsp.mass = 1.0f * volumeScale;
        }


        Collider ballCollider = ballObj.GetComponent<Collider>();
        if (ballCollider != null)
        {
            if (controller != null)
            {
                Physics.IgnoreCollision(controller, ballCollider);
            }

            ballCollider.enabled = false;
        }

        Ball ballScript = ballObj.GetComponent<Ball>();
        if (ballScript != null)
        {
            currentBall = ballScript;
            ballScript.Pickup(photonView.ViewID);
        }
    }

    void TryPickup()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 4f))
        {
            Ball ball = hit.collider.GetComponent<Ball>();

            if (ball != null && ball.IsFree())
            {
                currentBall = ball;
                ball.Pickup(photonView.ViewID);
            }
        }
    }

    void ThrowBall()
    {
        NetworkSpherePhysics nsp = currentBall.GetComponent<NetworkSpherePhysics>();
        float mass = nsp != null ? nsp.mass : 1f;

        // Heavy ball = slower throw
        float speed = throwForce / mass;

        // Prevent tiny balls from being too fast
        speed = Mathf.Clamp(speed, 3f, throwForce);

        Vector3 throwVelocity = playerCamera.transform.forward * speed;

        Collider ballCollider = currentBall.GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.enabled = true;
        }

        currentBall.Throw(throwVelocity);
        currentBall = null;
    }

}