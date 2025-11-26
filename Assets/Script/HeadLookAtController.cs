using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadLookAtController : MonoBehaviour
{
    [Header("Head Rig Settings")]
    [SerializeField] private MultiAimConstraint headRig;
    [SerializeField] private Transform headTarget;
    [SerializeField] private string[] interestPointTags = { "InterestPoint" };
    [SerializeField] private float lookAtDistance = 10f;
    [SerializeField] private float lookAtAngle = 60f;
    [SerializeField] private float weightTransitionSpeed = 2f;

    [Header("Head Movement")]
    [SerializeField] private float maxHeadTurnAngle = 80f;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private Vector3 targetRotationOffset = new Vector3(0f, 180f, 0f);

    private Transform _currentInterestPoint;
    private float _currentWeight = 0f;
    private Vector3 _smoothedLookPosition;
    private Vector3 _lookVelocity;
    
    // Cache interest points for better performance
    private readonly List<Transform> _cachedInterestPoints = new List<Transform>();
    private float _cacheRefreshTimer = 0f;
    private readonly float _cacheRefreshInterval = 1f;

    void Start()
    {
        if (headTarget == null)
        {
            Debug.LogError("Head target not assigned!");
            enabled = false;
            return;
        }
        
        _smoothedLookPosition = headTarget.position;
        RefreshInterestPointCache();
    }

    void Update()
    {
        if (headRig == null || headTarget == null) return;
        
        // Periodically refresh the interest point cache
        _cacheRefreshTimer += Time.deltaTime;
        if (_cacheRefreshTimer >= _cacheRefreshInterval)
        {
            RefreshInterestPointCache();
            _cacheRefreshTimer = 0f;
        }

        FindInterestPoint();
        UpdateHeadLookAt();
    }

    private void RefreshInterestPointCache()
    {
        _cachedInterestPoints.Clear();
        foreach (string tag in interestPointTags)
        {
            GameObject[] points = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject point in points)
            {
                if (point != null && point != gameObject)
                {
                    _cachedInterestPoints.Add(point.transform);
                }
            }
        }
    }

    private void FindInterestPoint()
    {
        Transform closestPoint = null;
        float closestDistance = float.MaxValue;

        // Find the closest valid interest point
        foreach (Transform point in _cachedInterestPoints)
        {
            if (point == null) continue;

            float distance = Vector3.Distance(transform.position, point.position);
            if (distance <= lookAtDistance && IsWithinViewAngle(point))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }
        }

        _currentInterestPoint = closestPoint;
    }

    private bool IsWithinViewAngle(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= lookAtAngle;
    }

    private void UpdateHeadLookAt()
    {
        if (_currentInterestPoint != null)
        {
            // Smoothly increase look-at weight when target is found
            _currentWeight = Mathf.MoveTowards(_currentWeight, 1f, weightTransitionSpeed * Time.deltaTime);
            
            Vector3 targetPosition = _currentInterestPoint.position;
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            
            // Apply rotation offset to adjust look direction
            Quaternion offsetRotation = Quaternion.Euler(targetRotationOffset);
            Vector3 offsetDirection = offsetRotation * directionToTarget;
            
            float distance = Vector3.Distance(transform.position, targetPosition);
            Vector3 adjustedTargetPosition = transform.position + offsetDirection * distance;
            
            // Smoothly move the look target position
            _smoothedLookPosition = Vector3.SmoothDamp(_smoothedLookPosition, adjustedTargetPosition, ref _lookVelocity, smoothTime);
            
            headTarget.position = _smoothedLookPosition;
        }
        else
        {
            // Smoothly return to neutral position when no target
            _currentWeight = Mathf.MoveTowards(_currentWeight, 0f, weightTransitionSpeed * Time.deltaTime);
            
            Vector3 neutralPosition = transform.position + transform.forward * 2f + transform.up * 0.5f;
            _smoothedLookPosition = Vector3.SmoothDamp(_smoothedLookPosition, neutralPosition, ref _lookVelocity, smoothTime);
            headTarget.position = _smoothedLookPosition;
        }

        // Apply the calculated weight to the head rig
        if (headRig != null)
            headRig.weight = _currentWeight;
    }
}