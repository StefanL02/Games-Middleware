using System.Collections.Generic;
using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    private AudioSource source;
    public List<AudioClip> clips;
    public GameObject decalPrefab;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void Step()
    {
        if (!source || clips.Count == 0) return;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
            Vector3.down, out RaycastHit hit, 1.5f))
        {
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(clips[Random.Range(0, clips.Count)]);

            if (decalPrefab)
            {
                var decal = Instantiate(
                    decalPrefab,
                    hit.point + Vector3.up * 0.01f,
                    Quaternion.Euler(90, transform.eulerAngles.y, 0)
                );
                Destroy(decal, 5f);
            }
        }
    }
}
