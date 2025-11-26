using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class BodyPickupController : MonoBehaviour
{
    [Header("Rig Constraints")]
    [SerializeField] private TwoBoneIKConstraint leftHandRig;
    [SerializeField] private TwoBoneIKConstraint rightHandRig;
    [SerializeField] private TwoBoneIKConstraint backRig;
    
    [Header("Rig Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform backTarget;

    [Header("Pickup Settings")]
    [SerializeField] private string pickupTag = "Pickup";
    [SerializeField] private float pickupRadius = 0.5f;
    [SerializeField] private float pickupHeightOffset = 0.1f;
    [SerializeField] private float weightTransitionSpeed = 2f;
    [SerializeField] private float maxPickupAngle = 20f; // Increased angle for better usability
    [SerializeField] private float minPickupDot = 0.3f; // Minimum dot product for front check
    [SerializeField] private float frontCheckDistance = 0.5f;
    [SerializeField] private LayerMask pickupLayerMask = -1; // Layer mask for pickup objects
    [SerializeField] private LayerMask obstacleLayerMask = 1; // Layer mask for obstacles that block pickup
    
    [Header("Advanced Pickup Detection")]
    [SerializeField] private bool useRaycastCheck = true; // Use raycasts for precise obstacle detection
    [SerializeField] private float raycastHeightOffset = 0.5f; // Raycast from character's chest level
    [SerializeField] private int raycastsPerObject = 3; // Multiple raycasts for larger objects
    [SerializeField] private float objectClearanceCheck = 0.2f; // Additional clearance around object
    
    [Header("Hand Settings")]
    [SerializeField] private Vector3 handOffset = new Vector3(0.2f, 0.1f, 0f);
    [SerializeField] private Vector3 handRotation = new Vector3(180f, 90f, 90f);

    [Header("Holding Settings")]
    [SerializeField] private Transform holdPosition;
    [SerializeField] private float holdHeight = 0.8f;
    [SerializeField] private float holdForwardOffset = 0.4f;

    [Header("Back Target Settings")]
    [SerializeField] private Vector3 backBendLocalPosition = new Vector3(0f, 1.3f, 0.4f);
    [SerializeField] private Vector3 backBendLocalRotation = new Vector3(-75f, 180f, 0f);
    [SerializeField] private Vector3 backHoldLocalPosition = new Vector3(0f, 1.3f, -0.12f);
    [SerializeField] private Vector3 backHoldLocalRotation = new Vector3(-9f, 180f, 0f);
    
    // Current pickup state
    private GameObject _currentPickupObject;
    private bool _isHoldingObject = false;
    private bool _isPickingUp = false;
    private float _bodyWeight = 0f;
    private float _backWeight = 0f;
    
    // Original positions for resetting
    private Vector3 _leftHandOriginalPos;
    private Vector3 _rightHandOriginalPos;
    private Quaternion _leftHandOriginalRot;
    private Quaternion _rightHandOriginalRot;
    private Vector3 _backOriginalLocalPos;
    private Quaternion _backOriginalLocalRot;
    
    // Object physics components
    private Collider[] _objectColliders;
    private Rigidbody _objectRigidbody;
    private Vector3 _objectPickupPosition;
    private Quaternion _objectPickupRotation;
    
    // Public properties for external access
    public bool IsHoldingObject => _isHoldingObject;

    void Start()
    {
        // Validate required components
        if (leftHandRig == null || rightHandRig == null || backRig == null ||
            leftHandTarget == null || rightHandTarget == null || backTarget == null)
        {
            Debug.LogError("Missing rig or target references!");
            enabled = false;
            return;
        }
        
        // Create hold position if not assigned
        if (holdPosition == null)
        {
            GameObject holdObj = new GameObject("HoldPosition");
            holdObj.transform.SetParent(transform);
            holdPosition = holdObj.transform;
        }
        
        // Store original positions for resetting
        _leftHandOriginalPos = leftHandTarget.position;
        _rightHandOriginalPos = rightHandTarget.position;
        _leftHandOriginalRot = leftHandTarget.rotation;
        _rightHandOriginalRot = rightHandTarget.rotation;
        _backOriginalLocalPos = backTarget.localPosition;
        _backOriginalLocalRot = backTarget.localRotation;
        
        UpdateRigWeights();
    }

    void Update()
    {
        HandleInput();
        UpdateRigWeights();
        UpdateHoldingPosition();
        
        if (_isHoldingObject && _currentPickupObject != null && !_isPickingUp)
        {
            UpdateHeldObjectPosition();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.P) && !_isPickingUp)
        {
            if (!_isHoldingObject)
            {
                TryPickupObject();
            }
            else
            {
                DropObject();
            }
        }
    }

    /// <summary>
    /// Improved pickup detection with better front checking and obstacle detection
    /// </summary>
    private void TryPickupObject()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayerMask);
        GameObject bestPickup = null;
        float bestScore = float.MinValue;

        foreach (Collider col in nearbyObjects)
        {
            if (!col.CompareTag(pickupTag) || !col.enabled) continue;

            float score = CalculatePickupScore(col.transform);
            
            if (score > bestScore && score > 0)
            {
                bestScore = score;
                bestPickup = col.gameObject;
            }
        }

        if (bestPickup != null)
        {
            StartPickup(bestPickup);
        }
        else
        {
            Debug.Log("No valid pickup objects found nearby!");
        }
    }

    /// <summary>
    /// Calculates a pickup score based on angle, distance, and clear path
    /// </summary>
    private float CalculatePickupScore(Transform objectTransform)
    {
        Vector3 toObject = objectTransform.position - transform.position;
        float distance = toObject.magnitude;
        
        // Normalize direction
        Vector3 directionToObject = toObject.normalized;
        
        // 1. Angle score (higher score for objects directly in front)
        float angle = Vector3.Angle(transform.forward, directionToObject);
        float angleScore = Mathf.Clamp01(1f - (angle / maxPickupAngle));
        
        // 2. Distance score (prefer closer objects)
        float distanceScore = Mathf.Clamp01(1f - (distance / pickupRadius));
        
        // 3. Dot product score (ensure object is in front)
        float dotScore = Mathf.Clamp01(Vector3.Dot(transform.forward, directionToObject));
        if (dotScore < minPickupDot) return -1f; // Object is behind character
        
        // 4. Clear path check
        float clearanceScore = CheckClearPathToObject(objectTransform) ? 1f : 0f;
        
        // 5. Height check (prefer objects at reasonable height)
        float heightDifference = Mathf.Abs(objectTransform.position.y - transform.position.y);
        float heightScore = Mathf.Clamp01(1f - (heightDifference / 2f)); // Adjust 2f as needed
        
        // Combined score with weights
        float totalScore = (angleScore * 0.4f) + (distanceScore * 0.3f) + (clearanceScore * 0.2f) + (heightScore * 0.1f);
        
        return totalScore;
    }

    /// <summary>
    /// Comprehensive path checking using multiple raycasts
    /// </summary>
    private bool CheckClearPathToObject(Transform objectTransform)
    {
        if (!useRaycastCheck) return true;

        Collider objectCollider = objectTransform.GetComponent<Collider>();
        if (objectCollider == null) return false;

        Bounds bounds = objectCollider.bounds;
        Vector3 characterCheckPosition = transform.position + Vector3.up * raycastHeightOffset;
        
        // Check multiple points on the object for better coverage
        Vector3[] checkPoints = new Vector3[raycastsPerObject];
        checkPoints[0] = bounds.center; // Center of object
        checkPoints[1] = bounds.center + transform.right * (bounds.extents.x - objectClearanceCheck); // Right side
        checkPoints[2] = bounds.center - transform.right * (bounds.extents.x - objectClearanceCheck); // Left side
        
        if (raycastsPerObject > 3)
        {
            checkPoints[3] = bounds.center + transform.forward * (bounds.extents.z - objectClearanceCheck); // Front
            if (raycastsPerObject > 4)
            {
                checkPoints[4] = bounds.center - transform.forward * (bounds.extents.z - objectClearanceCheck); // Back
            }
        }

        int clearHits = 0;
        float requiredClearHits = Mathf.Ceil(raycastsPerObject * 0.6f); // Require 60% of raycasts to be clear

        foreach (Vector3 checkPoint in checkPoints)
        {
            Vector3 rayDirection = checkPoint - characterCheckPosition;
            float rayDistance = rayDirection.magnitude;
            
            RaycastHit hit;
            bool hasHit = Physics.Raycast(characterCheckPosition, rayDirection.normalized, out hit, 
                                        rayDistance, obstacleLayerMask);
            
            if (!hasHit || hit.collider.transform == objectTransform)
            {
                clearHits++;
            }
            else
            {
                // Debug visualization
                Debug.DrawLine(characterCheckPosition, hit.point, Color.red, 1f);
            }
            
            // Debug visualization
            Debug.DrawLine(characterCheckPosition, checkPoint, hasHit ? Color.red : Color.green, 1f);
        }

        return clearHits >= requiredClearHits;
    }

    /// <summary>
    /// Simplified front check for quick validation
    /// </summary>
    private bool IsObjectInFront(Transform objectTransform)
    {
        Vector3 directionToObject = (objectTransform.position - transform.position).normalized;
        
        // Quick dot product check
        float dotProduct = Vector3.Dot(transform.forward, directionToObject);
        if (dotProduct < minPickupDot) return false;
        
        // Angle check
        float angle = Vector3.Angle(transform.forward, directionToObject);
        if (angle > maxPickupAngle) return false;
        
        return true;
    }

    /// <summary>
    /// Gets all valid pickup candidates for debugging or external use
    /// </summary>
    public List<GameObject> GetValidPickupCandidates()
    {
        List<GameObject> candidates = new List<GameObject>();
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayerMask);

        foreach (Collider col in nearbyObjects)
        {
            if (col.CompareTag(pickupTag) && col.enabled && IsObjectInFront(col.transform))
            {
                candidates.Add(col.gameObject);
            }
        }

        return candidates;
    }

    /// <summary>
    /// Gets the best pickup candidate with score for debugging
    /// </summary>
    public GameObject GetBestPickupCandidate(out float bestScore)
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayerMask);
        GameObject bestPickup = null;
        bestScore = float.MinValue;

        foreach (Collider col in nearbyObjects)
        {
            if (!col.CompareTag(pickupTag) || !col.enabled) continue;

            float score = CalculatePickupScore(col.transform);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestPickup = col.gameObject;
            }
        }

        return bestPickup;
    }
 
    /// <summary>
    /// Initiates the pickup process for a specific object
    /// </summary>
    /// <param name="pickupObject">The GameObject to pick up</param>
    private void StartPickup(GameObject pickupObject)
    {
        _currentPickupObject = pickupObject;
        _isPickingUp = true;
        
        // Cache object components
        _objectColliders = pickupObject.GetComponentsInChildren<Collider>();
        _objectRigidbody = pickupObject.GetComponent<Rigidbody>();
        _objectPickupPosition = pickupObject.transform.position;
        _objectPickupRotation = pickupObject.transform.rotation;
        
        // Calculate hand positions for pickup
        float objectHeight = GetObjectHeight(pickupObject);
        Vector3 boxTop = pickupObject.transform.position + Vector3.up * (objectHeight * 0.5f + pickupHeightOffset);
        
        Vector3 leftHandPos = boxTop - transform.right * handOffset.x;
        Vector3 rightHandPos = boxTop + transform.right * handOffset.x;

        StartCoroutine(PickupAnimation(leftHandPos, rightHandPos));
    }

    /// <summary>
    /// Animates the hands moving from current position to the pickup object
    /// </summary>
    /// <param name="leftTarget">Target position for left hand</param>
    /// <param name="rightTarget">Target position for right hand</param>
    private IEnumerator PickupAnimation(Vector3 leftTarget, Vector3 rightTarget)
    {
        float t = 0f;
        Vector3 leftStart = leftHandTarget.position;
        Vector3 rightStart = rightHandTarget.position;
        Quaternion leftStartRot = leftHandTarget.rotation;
        Quaternion rightStartRot = rightHandTarget.rotation;
        Vector3 backStartPos = backTarget.localPosition;
        Quaternion backStartRot = backTarget.localRotation;

        Quaternion backBendRot = Quaternion.Euler(backBendLocalRotation);
        Quaternion leftHandTargetRot = CalculateHandRotation(true);
        Quaternion rightHandTargetRot = CalculateHandRotation(false);
        
        // Animate hands moving to pickup position
        while (t < 1f)
        {
            t += Time.deltaTime * weightTransitionSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            leftHandTarget.position = Vector3.Lerp(leftStart, leftTarget, smoothT);
            rightHandTarget.position = Vector3.Lerp(rightStart, rightTarget, smoothT);
            leftHandTarget.rotation = Quaternion.Lerp(leftStartRot, leftHandTargetRot, smoothT);
            rightHandTarget.rotation = Quaternion.Lerp(rightStartRot, rightHandTargetRot, smoothT);
            backTarget.localPosition = Vector3.Lerp(backStartPos, backBendLocalPosition, smoothT);
            backTarget.localRotation = Quaternion.Lerp(backStartRot, backBendRot, smoothT);
            
            _bodyWeight = Mathf.Lerp(0f, 1f, smoothT);
            _backWeight = Mathf.Lerp(0f, 1f, smoothT);
            
            // Keep object in place during animation
            if (_currentPickupObject != null)
            {
                _currentPickupObject.transform.position = _objectPickupPosition;
                _currentPickupObject.transform.rotation = _objectPickupRotation;
            }
            
            yield return null;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        AttachObject();
        yield return StartCoroutine(TransitionToHoldPosition());
        
        _isPickingUp = false;
        _isHoldingObject = true;
    }

    /// <summary>
    /// Transitions the object from pickup position to holding position
    /// </summary>
    private IEnumerator TransitionToHoldPosition()
    {
        float t = 0f;
        Vector3 leftStart = leftHandTarget.position;
        Vector3 rightStart = rightHandTarget.position;
        Quaternion leftStartRot = leftHandTarget.rotation;
        Quaternion rightStartRot = rightHandTarget.rotation;
        Vector3 backStartPos = backTarget.localPosition;
        Quaternion backStartRot = backTarget.localRotation;

        // Calculate hold positions
        Vector3 holdPos = CalculateHoldPosition();
        Vector3 leftHoldPos = holdPos - transform.right * handOffset.x;
        Vector3 rightHoldPos = holdPos + transform.right * handOffset.x;
        Quaternion backHoldRot = Quaternion.Euler(backHoldLocalRotation);
        Quaternion leftHandHoldRot = CalculateHandRotation(true);
        Quaternion rightHandHoldRot = CalculateHandRotation(false);

        // Animate transition to holding position
        while (t < 1f)
        {
            t += Time.deltaTime * weightTransitionSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            leftHandTarget.position = Vector3.Lerp(leftStart, leftHoldPos, smoothT);
            rightHandTarget.position = Vector3.Lerp(rightStart, rightHoldPos, smoothT);
            leftHandTarget.rotation = Quaternion.Lerp(leftStartRot, leftHandHoldRot, smoothT);
            rightHandTarget.rotation = Quaternion.Lerp(rightStartRot, rightHandHoldRot, smoothT);
            backTarget.localPosition = Vector3.Lerp(backStartPos, backHoldLocalPosition, smoothT);
            backTarget.localRotation = Quaternion.Lerp(backStartRot, backHoldRot, smoothT);
            
            _bodyWeight = 1f;
            _backWeight = Mathf.Lerp(1f, 0f, smoothT);
            
            UpdateHeldObjectPosition();
            yield return null;
        }
        
        _backWeight = 0f;
    }

    /// <summary>
    /// Calculates the appropriate hand rotation for pickup and holding
    /// </summary>
    /// <param name="isLeftHand">Whether this is for the left hand</param>
    /// <returns>The target hand rotation</returns>
    private Quaternion CalculateHandRotation(bool isLeftHand)
    {
        return Quaternion.Euler(handRotation);
    }

    /// <summary>
    /// Attaches the object to the character by disabling its physics
    /// </summary>
    private void AttachObject()
    {
        if (_currentPickupObject == null) return;
        
        // Disable object physics while holding
        if (_objectColliders != null)
        {
            foreach (Collider col in _objectColliders)
            {
                col.enabled = false;
            }
        }
        
        if (_objectRigidbody != null)
        {
            _objectRigidbody.isKinematic = true;
        }
    }

    /// <summary>
    /// Updates the position of the held object to match the hand positions
    /// </summary>
    private void UpdateHeldObjectPosition()
    {
        if (_currentPickupObject == null) return;
        
        // Position object between hands
        Vector3 targetPosition = (leftHandTarget.position + rightHandTarget.position) / 2f;
        _currentPickupObject.transform.position = targetPosition;
        _currentPickupObject.transform.rotation = holdPosition.rotation;
    }

    /// <summary>
    /// Initiates the object dropping process
    /// </summary>
    private void DropObject()
    {
        if (_currentPickupObject == null) return;
        StartCoroutine(DropAnimation());
    }

    /// <summary>
    /// Handles the complete drop animation sequence
    /// </summary>
    private IEnumerator DropAnimation()
    {
        ReleaseObject();
        yield return StartCoroutine(ReturnToNeutralPosition());
    }

    /// <summary>
    /// Returns the hands and back to their original neutral positions
    /// </summary>
    private IEnumerator ReturnToNeutralPosition()
    {
        float t = 0f;
        Vector3 leftStart = leftHandTarget.position;
        Vector3 rightStart = rightHandTarget.position;
        Quaternion leftStartRot = leftHandTarget.rotation;
        Quaternion rightStartRot = rightHandTarget.rotation;
        Vector3 backStartPos = backTarget.localPosition;
        Quaternion backStartRot = backTarget.localRotation;
        float startBackWeight = _backWeight;
        
        _isHoldingObject = false;
        _currentPickupObject = null;
        _objectRigidbody = null;
        _objectColliders = null;
        
        // Animate hands returning to neutral position
        while (t < 1f)
        {
            t += Time.deltaTime * weightTransitionSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            leftHandTarget.position = Vector3.Lerp(leftStart, _leftHandOriginalPos, smoothT);
            rightHandTarget.position = Vector3.Lerp(rightStart, _rightHandOriginalPos, smoothT);
            leftHandTarget.rotation = Quaternion.Lerp(leftStartRot, _leftHandOriginalRot, smoothT);
            rightHandTarget.rotation = Quaternion.Lerp(rightStartRot, _rightHandOriginalRot, smoothT);
            backTarget.localPosition = Vector3.Lerp(backStartPos, _backOriginalLocalPos, smoothT);
            backTarget.localRotation = Quaternion.Lerp(backStartRot, _backOriginalLocalRot, smoothT);
            
            _bodyWeight = Mathf.Lerp(1f, 0f, smoothT);
            _backWeight = Mathf.Lerp(startBackWeight, 0f, smoothT);
            yield return null;
        }

        _bodyWeight = 0f;
        _backWeight = 0f;
    }

    /// <summary>
    /// Re-enables physics on the dropped object
    /// </summary>
    private void ReleaseObject()
    {
        if (_currentPickupObject == null) return;
        
        // Re-enable object physics
        if (_objectColliders != null)
        {
            foreach (Collider col in _objectColliders)
            {
                col.enabled = true;
            }
        }
        
        if (_objectRigidbody != null)
        {
            _objectRigidbody.isKinematic = false;
        }
    }

    /// <summary>
    /// Updates the weight values for all rig constraints
    /// </summary>
    private void UpdateRigWeights()
    {
        // Apply weights to rig constraints
        if (leftHandRig != null) leftHandRig.weight = _bodyWeight;
        if (rightHandRig != null) rightHandRig.weight = _bodyWeight;
        if (backRig != null) backRig.weight = _backWeight;
    }

    /// <summary>
    /// Updates the hand positions when holding an object
    /// </summary>
    private void UpdateHoldingPosition()
    {
        if (!_isHoldingObject || _isPickingUp) return;

        // Update hand positions for holding
        Vector3 holdPos = CalculateHoldPosition();
        holdPosition.position = holdPos;
        holdPosition.rotation = transform.rotation;
        
        Vector3 leftHandPos = holdPos - transform.right * handOffset.x;
        Vector3 rightHandPos = holdPos + transform.right * handOffset.x;
        Quaternion leftHandRot = CalculateHandRotation(true);
        Quaternion rightHandRot = CalculateHandRotation(false);
        
        leftHandTarget.position = leftHandPos;
        rightHandTarget.position = rightHandPos;
        leftHandTarget.rotation = leftHandRot;
        rightHandTarget.rotation = rightHandRot;
        backTarget.localPosition = backHoldLocalPosition;
        backTarget.localRotation = Quaternion.Euler(backHoldLocalRotation);
        _backWeight = 0f;
    }

    /// <summary>
    /// Calculates the position where objects should be held
    /// </summary>
    /// <returns>The world position for holding objects</returns>
    private Vector3 CalculateHoldPosition()
    {
        return transform.position + transform.up * holdHeight + transform.forward * holdForwardOffset;
    }

    /// <summary>
    /// Gets the approximate height of a GameObject for positioning calculations
    /// </summary>
    /// <param name="item">The GameObject to measure</param>
    /// <returns>The height of the object</returns>
    private float GetObjectHeight(GameObject item)
    {
        if (item == null) return 1f;
        
        // Try to get height from renderer bounds
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        
        // Fallback to collider bounds
        Collider col = item.GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds.size.y;
        }
        
        return 1f;
    }

    /// <summary>
    /// Cleanup method to release any held objects when destroyed
    /// </summary>
    void OnDestroy()
    {
        // Clean up any remaining objects
        if (_currentPickupObject != null)
        {
            ReleaseObject();
        }
    }

    /// <summary>
    /// Draws visualization gizmos in the editor for pickup radius and angles
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Visualize pickup radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        // Draw pickup angle visualization
        Gizmos.color = Color.blue;
        Quaternion leftAngle = Quaternion.AngleAxis(-maxPickupAngle, Vector3.up);
        Quaternion rightAngle = Quaternion.AngleAxis(maxPickupAngle, Vector3.up);
        Vector3 leftDir = leftAngle * transform.forward * pickupRadius;
        Vector3 rightDir = rightAngle * transform.forward * pickupRadius;
        Gizmos.DrawLine(transform.position, transform.position + leftDir);
        Gizmos.DrawLine(transform.position, transform.position + rightDir);
        
        // Draw front check area
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Vector3 frontCenter = transform.position + transform.forward * (frontCheckDistance * 0.5f);
        Gizmos.DrawCube(frontCenter, new Vector3(pickupRadius, 1f, frontCheckDistance));
        
        // Show hold position in play mode
        if (Application.isPlaying && holdPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(holdPosition.position, 0.1f);
        }
    }
}