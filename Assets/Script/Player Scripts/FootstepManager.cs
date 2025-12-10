using UnityEngine;
using System.Collections.Generic;

public class FootstepManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private List<AudioClip> footstepClips = new List<AudioClip>();
    [SerializeField] private float volume = 0.7f;
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private string[] groundTags = { "Terrain"};
    [SerializeField] private float raycastDistance = 0.2f;
    [SerializeField] private Vector3 raycastOffset = Vector3.zero;
    
    [Header("Footstep Decal Settings")]
    [SerializeField] private GameObject footstepPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float decalLifetime = 5f;
    [SerializeField] private Vector3 decalOffset = new Vector3(0, 0.01f, 0);
    
    public AudioSource audioSource;
    private bool _isGrounded = false;
    
    // Object pooling for footstep decals
    private readonly Queue<GameObject> _footstepPool = new Queue<GameObject>();
    private readonly List<GameObject> _activeFootsteps = new List<GameObject>();

    void Start()
    {
        InitializePool();
    }

    void Update()
    {
        CheckGround();
    }

    // Create a pool of footstep decal objects for reuse
    private void InitializePool()
    {
        if (footstepPrefab == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject footstep = Instantiate(footstepPrefab);
            footstep.SetActive(false);
            _footstepPool.Enqueue(footstep);
        }
    }

    // Get a footstep decal from the pool, or create a new one if pool is empty
    private GameObject GetPooledFootstep()
    {
        if (_footstepPool.Count > 0)
        {
            GameObject footstep = _footstepPool.Dequeue();
            footstep.SetActive(true);
            _activeFootsteps.Add(footstep);
            return footstep;
        }
        
        // Fallback if pool is empty
        GameObject newFootstep = Instantiate(footstepPrefab);
        _activeFootsteps.Add(newFootstep);
        return newFootstep;
    }

    // Return a footstep decal to the pool for reuse
    private void ReturnFootstepToPool(GameObject footstep)
    {
        footstep.SetActive(false);
        _activeFootsteps.Remove(footstep);
        _footstepPool.Enqueue(footstep);
    }

    // Check if character is standing on ground
    private void CheckGround()
    {
        Vector3 rayOrigin = transform.position + raycastOffset;
        Ray ray = new Ray(rayOrigin, Vector3.down);
        RaycastHit hit;

        bool wasGrounded = _isGrounded;
        _isGrounded = Physics.Raycast(ray, out hit, raycastDistance, groundLayerMask);

        if (_isGrounded)
        {
            // Check if the ground has one of the valid tags
            bool validGround = false;
            foreach (string tag in groundTags)
            {
                if (hit.collider.CompareTag(tag))
                {
                    validGround = true;
                    break;
                }
            }

            if (validGround)
            {
                // Play footstep when first touching the ground
                if (!wasGrounded)
                {
                    PlayRandomFootstep();
                    SpawnFootstepDecal(hit);
                }
            }
            else
            {
                _isGrounded = false;
            }
        }
    }

    // Play a random footstep sound with variation
    private void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0)
            return;

        // Don't interrupt currently playing footsteps
        if (audioSource.isPlaying)
            return;
        
        AudioClip randomClip = footstepClips[Random.Range(0, footstepClips.Count)];
        
        // Add some randomness to make footsteps sound more natural
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = volume;
        
        audioSource.PlayOneShot(randomClip);
    }

    // Create a footstep visual effect at the hit position
    private void SpawnFootstepDecal(RaycastHit hit)
    {
        if (footstepPrefab == null) return;

        GameObject footstep = GetPooledFootstep();
        if (footstep == null) return;
        
        // Position the decal slightly above the ground surface
        footstep.transform.position = hit.point + decalOffset;
        footstep.transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);
        
        // Add slight random rotation for variety
        footstep.transform.Rotate(0, 0, Random.Range(0, 5), Space.Self);
        
        // Return to pool after lifetime expires
        StartCoroutine(ReturnFootstepAfterTime(footstep, decalLifetime));
    }

    // Returns footstep decal to pool after delay
    private System.Collections.IEnumerator ReturnFootstepAfterTime(GameObject footstep, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnFootstepToPool(footstep);
    }

    // Visualize the ground detection ray in editor - Useful for debugging
    private void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + raycastOffset;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
    }
    
    // Clean up pooled objects when this object is destroyed to not have a ton of objects when destroying and respawning the character again
    private void OnDestroy()
    {
        StopAllCoroutines();
        
        // Destroy all pooled objects
        foreach (var footstep in _footstepPool)
        {
            if (footstep != null)
                Destroy(footstep);
        }
        
        foreach (var footstep in _activeFootsteps)
        {
            if (footstep != null)
                Destroy(footstep);
        }
        
        _footstepPool.Clear();
        _activeFootsteps.Clear();
    }
}