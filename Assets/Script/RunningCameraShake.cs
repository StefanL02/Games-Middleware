using UnityEngine;

public class RunningCameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float walkShakeIntensity = 0.05f;
    public float runShakeIntensity = 0.1f;
    public float shakeFrequency = 8f;
    
    [Header("Movement Detection")]
    public twoDimensionalStateController characterController;
    
    private Vector3 _originalPosition;
    private float _shakeTimer = 0f;
    
    void Start()
    {
        // Store the camera's original position
        _originalPosition = transform.localPosition;
        
        // Try to find the character controller if not assigned
        if (characterController == null)
        {
            characterController = FindObjectOfType<twoDimensionalStateController>();
        }
    }
    
    void Update()
    {
        if (characterController == null) return;
        
        float currentIntensity = 0f;
        
        // Determine shake intensity based on movement state
        if (characterController.IsRunning)
        {
            currentIntensity = runShakeIntensity;
        }
        else if (IsCharacterWalking())
        {
            currentIntensity = walkShakeIntensity;
        }
        
        if (currentIntensity > 0)
        {
            // Apply camera shake when moving
            _shakeTimer += Time.deltaTime * shakeFrequency;
            ApplyMovementShake(currentIntensity);
        }
        else
        {
            // Smoothly return to original position when not moving
            transform.localPosition = Vector3.Lerp(transform.localPosition, _originalPosition, Time.deltaTime * 8f);
            _shakeTimer = 0f;
        }
    }
    
    private bool IsCharacterWalking()
    {
        // Check if character is moving but not running
        return (Mathf.Abs(characterController.velocityZ) > 0.1f || Mathf.Abs(characterController.velocityX) > 0.1f) && !characterController.IsRunning;
    }
    
    private void ApplyMovementShake(float intensity)
    {
        // Create different shake patterns for more natural movement
        float verticalBounce = Mathf.Sin(_shakeTimer * 2f) * intensity * 0.6f;
        float horizontalWobble = Mathf.Sin(_shakeTimer * 0.7f) * intensity * 0.4f;
        float slightRoll = Mathf.Sin(_shakeTimer * 1.3f) * intensity * 0.2f;
        
        Vector3 shakeOffset = new Vector3(horizontalWobble, verticalBounce, 0f);
        
        // Only apply rotation during running for more intense effect
        if (intensity >= runShakeIntensity)
        {
            transform.localRotation = Quaternion.Euler(0, 0, slightRoll);
        }
        
        transform.localPosition = _originalPosition + shakeOffset;
    }
}