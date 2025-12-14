using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class BodyPickupController : MonoBehaviour
{ 
    [SerializeField] private TwoBoneIKConstraint leftHandIK;

    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private ChainIKConstraint spineChainIK;
    [SerializeField] private Transform spineTarget;
    [SerializeField] private Transform holdPivot;
    [SerializeField] private MultiAimConstraint headRig;

    public Vector3 leftHandRotOffset = new Vector3(90, 0, 0);
    public Vector3 rightHandRotOffset = new Vector3(90, 0, 0);
    public float elbowWidth = 0.4f;
    
    [SerializeField] private float spineBendAngle = 50f;

    [SerializeField] private float spineForwardOffset = 0.4f;
    [SerializeField] private float gripOffset = 0.05f;

    [SerializeField] private float reachDuration = 0.6f;
    [SerializeField] private float arcHeight = 0.15f;
    [SerializeField] private float stopDistance = 0.45f;

    private twoDimensionalStateController _moveScript;
    private Rigidbody _heldBody;
    private BoxCollider _heldBox;
    private bool _isBusy;
    private Vector3 _defaultSpineLocalPos;
    private Quaternion _defaultSpineLocalRot;

    private Vector3 _lhFinal, _rhFinal;
    private Quaternion _lhRotFinal, _rhRotFinal;
    private Vector3 _lhStart, _rhStart;
    private Quaternion _lhRotStart, _rhRotStart;

    public bool IsHoldingObject => _heldBody != null;

    void Start()
    {
        _moveScript = GetComponent<twoDimensionalStateController>();
        SetRigWeight(0f);
        
        if (spineTarget)
        {
            _defaultSpineLocalPos = spineTarget.localPosition;
            _defaultSpineLocalRot = spineTarget.localRotation;
        }
    }

    public void TriggerPickup()
    {
        if (!_isBusy && !IsHoldingObject)
        {
            StartCoroutine(PickupRoutine());
        }
    }

    public void TriggerDrop()
    {
        if (!_isBusy && IsHoldingObject)
        {
            StartCoroutine(DropRoutine());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (IsHoldingObject)
            {
                TriggerDrop();
            }
            else
            {
                TriggerPickup();
            }
        }

        if (IsHoldingObject || _isBusy)
        {
            SolveElbows();
        }
    }

    private IEnumerator PickupRoutine()
    {
        BoxCollider box = FindBestBox();
        if (!box)
        {
            yield break;
        }

        _isBusy = true;
        if (headRig)
        {
            headRig.weight = 0f;
        }

        if (_moveScript)
        {
            _moveScript.inputOverride = true;
        }
        
        Vector3 boxCenter = box.transform.TransformPoint(box.center);
        Vector3 dirToBox = (boxCenter - transform.position).normalized;
        Vector3 standPos = boxCenter - (dirToBox * stopDistance);

        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                   new Vector2(standPos.x, standPos.z)) > 0.1f)
        {
            Vector3 walkDir = (standPos - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(new Vector3(walkDir.x, 0, walkDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 180f * Time.deltaTime);
            
            if (_moveScript)
            {
                _moveScript.overrideVertical = 1f;
                _moveScript.overrideHorizontal = 0f;
            }

            
            yield return null;
        }

        if (_moveScript)
        {
            _moveScript.overrideVertical = 0f;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        if (_moveScript)
        {
            _moveScript.inputOverride = false;
        }

        Quaternion finalRot = Quaternion.LookRotation(new Vector3(dirToBox.x, 0, dirToBox.z));
        float t = 0;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, t);
            yield return null;
        }

        CalculateSmartGrip(box);

        Vector3 startSpinePos = spineTarget.localPosition;
        Quaternion startSpineRot = spineTarget.localRotation;
        Quaternion targetSpineRot = Quaternion.Euler(spineBendAngle, 0, 0) * _defaultSpineLocalRot;
        Vector3 targetSpinePos = _defaultSpineLocalPos + (Vector3.forward * spineForwardOffset);

        Vector3 lLocalPos = leftHandIK.data.root.InverseTransformPoint(leftHandIK.data.tip.position);
        Vector3 rLocalPos = rightHandIK.data.root.InverseTransformPoint(rightHandIK.data.tip.position);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / reachDuration;
            float smooth = Mathf.SmoothStep(0, 1, t);

            if (spineTarget)
            {
                spineTarget.localPosition = Vector3.Lerp(startSpinePos, targetSpinePos, smooth);
                spineTarget.localRotation = Quaternion.Slerp(startSpineRot, targetSpineRot, smooth);
            }

            Vector3 currentStartL = leftHandIK.data.root.TransformPoint(lLocalPos);
            Vector3 currentStartR = rightHandIK.data.root.TransformPoint(rLocalPos);

            leftHandIK.data.target.position = CalculateArcPoint(currentStartL, _lhFinal, arcHeight, smooth);
            rightHandIK.data.target.position = CalculateArcPoint(currentStartR, _rhFinal, arcHeight, smooth);

            leftHandIK.data.target.rotation = Quaternion.Slerp(_lhRotStart, _lhRotFinal, smooth);
            rightHandIK.data.target.rotation = Quaternion.Slerp(_rhRotStart, _rhRotFinal, smooth);

            SetRigWeight(smooth);
            yield return null;
        }

        Grab(box);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.6f;
            float smooth = Mathf.SmoothStep(0, 1, t);

            if (spineTarget)
            {
                spineTarget.localPosition = Vector3.Lerp(targetSpinePos, _defaultSpineLocalPos, smooth);
                spineTarget.localRotation = Quaternion.Slerp(targetSpineRot, _defaultSpineLocalRot, smooth);
            }

            CalculateSmartGrip(_heldBox);
            if (spineChainIK) spineChainIK.weight = 1f - smooth;

            leftHandIK.data.target.position = _lhFinal;
            leftHandIK.data.target.rotation = _lhRotFinal;
            rightHandIK.data.target.position = _rhFinal;
            rightHandIK.data.target.rotation = _rhRotFinal;

            yield return null;
        }

        _isBusy = false;
        
        if (headRig)
        {
            headRig.weight = 1f;
        }
    }

    private IEnumerator DropRoutine()
    {
        _isBusy = true;
        Drop();
        Vector3 sL = leftHandIK.data.target.position;
        Vector3 sR = rightHandIK.data.target.position;
        Vector3 eL = transform.position + Vector3.up * 0.9f - transform.right * 0.35f;
        Vector3 eR = transform.position + Vector3.up * 0.9f + transform.right * 0.35f;
        float t = 0;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 2;
            float s = Mathf.SmoothStep(0, 1, t);
            leftHandIK.data.target.position = Vector3.Lerp(sL, eL, s);
            rightHandIK.data.target.position = Vector3.Lerp(sR, eR, s);
            SetRigWeight(1f - s);
            yield return null;
        }

        _isBusy = false;
    }

    private void SolveElbows()
    {
        if (leftHandIK.data.root && leftHandIK.data.target)
        {
            Vector3 mid = Vector3.Lerp(leftHandIK.data.root.position, leftHandIK.data.target.position, 0.5f);
            leftHandIK.data.hint.position = mid - (transform.right * elbowWidth) + (transform.up * 0.2f);
        }

        if (rightHandIK.data.root && rightHandIK.data.target)
        {
            Vector3 mid = Vector3.Lerp(rightHandIK.data.root.position, rightHandIK.data.target.position, 0.5f);
            rightHandIK.data.hint.position = mid + (transform.right * elbowWidth) + (transform.up * 0.2f);
        }
    }

    private void CalculateSmartGrip(BoxCollider box)
    {
        Vector3 s = Vector3.Scale(box.size, box.transform.lossyScale);
        Vector3 c = box.transform.TransformPoint(box.center);
        Vector3 r = box.transform.right;
        Vector3 f = box.transform.forward;
        Vector3 ax = r;
        float w = s.x;
        
        if (Mathf.Abs(Vector3.Dot(transform.right, f)) > Mathf.Abs(Vector3.Dot(transform.right, r)))
        {
            ax = f;
            w = s.z;
        }

        float hw = (w * 0.5f) + gripOffset;
        _lhFinal = c - ax * hw;
        _rhFinal = c + ax * hw;

        _lhRotFinal = Quaternion.LookRotation(c - _lhFinal, Vector3.up) * Quaternion.Euler(leftHandRotOffset);
        _rhRotFinal = Quaternion.LookRotation(c - _rhFinal, Vector3.up) * Quaternion.Euler(rightHandRotOffset);
    }

    private Vector3 CalculateArcPoint(Vector3 s, Vector3 e, float h, float t)
    {
        Vector3 m = Vector3.Lerp(s, e, 0.5f) + Vector3.up * h;
        return Vector3.Lerp(Vector3.Lerp(s, m, t), Vector3.Lerp(m, e, t), t);
    }

    private void Grab(BoxCollider box)
    {
        _heldBox = box;
        _heldBody = box.GetComponent<Rigidbody>();
        _heldBody.isKinematic = true;
        _heldBox.enabled = false;
        _heldBody.transform.SetParent(holdPivot);
        _heldBody.transform.localPosition = Vector3.forward * 0.45f;
        _heldBody.transform.localRotation = Quaternion.identity;
    }

    private void Drop()
    {
        if (!_heldBody) return;
        _heldBody.transform.SetParent(null);
        _heldBody.isKinematic = false;
        _heldBox.enabled = true;
        _heldBody = null;
        _heldBox = null;
    }

    private void SetRigWeight(float w)
    {
        leftHandIK.weight = w;
        rightHandIK.weight = w;
        
        if (spineChainIK)
        {
            spineChainIK.weight = w;
        }
    }

    private BoxCollider FindBestBox()
    {
        Collider[] c = Physics.OverlapSphere(transform.position, 3f);
        float bestDist = float.MaxValue;
        BoxCollider best = null;
        
        foreach (var col in c)
        {
            BoxCollider b = col.GetComponent<BoxCollider>();
            if (b && col.GetComponent<Rigidbody>() && col.transform != transform)
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = b;
                }
            }
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        if (leftHandIK && leftHandIK.data.hint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftHandIK.data.hint.position, 0.1f);
        }

        if (rightHandIK && rightHandIK.data.hint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(rightHandIK.data.hint.position, 0.1f);
        }
    }
}