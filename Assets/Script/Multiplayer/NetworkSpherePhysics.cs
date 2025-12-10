using UnityEngine;
using Photon.Pun;

public class NetworkSpherePhysics : MonoBehaviourPun, IPhysical
{
    public Vector3 previousPosition, V0;
    public Vector3 velocity;
    Vector3 acceleration;

    float gravity = 9.81f;
    float CoR = 0.5f;
    internal float mass = 1.0f;

    public bool isHeld = false;

    void OnEnable()
    {
        NetworkManager.allObjects.Add(this);
    }

    void OnDisable()
    {
        NetworkManager.allObjects.Remove(this);
    }

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || isHeld) return;

        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
            return;
        }

        V0 = velocity;
        previousPosition = transform.position;
        acceleration = gravity * Vector3.down;
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }

    public void SetLaunchVelocity(Vector3 newVelocity)
    {
        this.velocity = newVelocity;
    }

    public bool IsColliding(IPhysical other)
    {
        if (other is NetworkPlanePhysics)
        {
            NetworkPlanePhysics plane = other as NetworkPlanePhysics;
            Vector3 v = transform.position - plane.transform.position;
            Vector3 p = Utils.ParallelTo(v, plane.normal);
            return p.magnitude < Radius;
        }

        if (other is NetworkSpherePhysics otherSphere)
        {
            return Vector3.Distance(transform.position, otherSphere.transform.position) < (Radius + otherSphere.Radius);
        }

        return false;
    }

    public void ResolveCollision(IPhysical other, ref Vector3 otherPos, ref Vector3 otherVel)
    {
        float TimeInterval = Time.deltaTime;

        if (other is NetworkPlanePhysics)
        {
            NetworkPlanePhysics plane = other as NetworkPlanePhysics;

            float D0 = Utils.NetworkDistanceToPlane(previousPosition, plane) - Radius;
            float D1 = Utils.NetworkDistanceToPlane(transform.position, plane) - Radius;
            float speed = (D1 - D0) / TimeInterval;

            if (Mathf.Abs(speed) < 0.0001f) return;

            float ToI = -D0 / speed;

            Vector3 vel_ToI = V0 + acceleration * ToI;
            Vector3 pos_ToI = previousPosition + vel_ToI * ToI;

            Vector3 vel_parallel = Utils.ParallelTo(vel_ToI, plane.normal);
            Vector3 vel_perpendicullar = Utils.PerpendicularTo(vel_ToI, plane.normal);
            Vector3 vel_res = vel_perpendicullar - CoR * vel_parallel;

            velocity = vel_res + acceleration * (TimeInterval - ToI);
            transform.position = pos_ToI + velocity * (TimeInterval - ToI);

            float d = Utils.NetworkDistanceToPlane(transform.position, plane) - Radius;
            if (d < 0)
            {
                transform.position -= d * plane.normal;
            }
        }

        if (other is NetworkSpherePhysics)
        {
            NetworkSpherePhysics sphere = (NetworkSpherePhysics)other;

            float D0 = Vector3.Distance(previousPosition, sphere.previousPosition) - Radius - sphere.Radius;
            float D1 = Vector3.Distance(transform.position, sphere.transform.position) - Radius - sphere.Radius;
            float speed = (D1 - D0) / TimeInterval;

            if (Mathf.Abs(speed) < 0.0001f) return;

            float ToI = -D0 / speed;

            Vector3 vel_ToI = V0 + acceleration * ToI;
            Vector3 pos_ToI = previousPosition + vel_ToI * ToI;

            Vector3 vel_ToIOther = sphere.V0 + sphere.acceleration * ToI;
            Vector3 pos_ToIOther = sphere.previousPosition + vel_ToIOther * ToI;

            Vector3 normal = (pos_ToI - pos_ToIOther).normalized;
            Vector3 vel_parallel = Utils.ParallelTo(vel_ToI, normal);
            Vector3 vel_parallelOther = Utils.ParallelTo(vel_ToIOther, normal);

            Vector3 velPerpAfter = ElasticCollision(vel_parallel, vel_parallelOther, mass, sphere.mass);
            Vector3 velPerpAfterOther = ElasticCollision(vel_parallelOther, vel_parallel, sphere.mass, mass);

            // Mass ratio: small mass = 0.1, big mass = 1.2 → ratio ~ 0.083
            float massRatio = mass / sphere.mass;

            // Mass-based restitution (bounciness)
            // Heavy ball = low bounce
            // Small ball = high bounce
            float CoR_self = Mathf.Lerp(0.05f, CoR, massRatio);
            float CoR_other = Mathf.Lerp(CoR, 0.05f, massRatio);

            // Apply CoR to the parallel components
            Vector3 velAfter = -CoR_self * vel_parallel + velPerpAfter;
            Vector3 velAfterOther = -CoR_other * vel_parallelOther + velPerpAfterOther;

            float dampingSelf = Mathf.Lerp(0.90f, 0.99f, mass / sphere.mass);
            float dampingOther = Mathf.Lerp(0.99f, 0.90f, mass / sphere.mass);

            // Heavy ball loses less velocity, small ball loses slightly more
            velAfter *= dampingSelf;
            velAfterOther *= dampingOther;

            float remainingTime = TimeInterval - ToI;
            velocity = velAfter + acceleration * remainingTime;
            transform.position = pos_ToI + velocity * remainingTime;

            otherVel = velAfterOther + sphere.acceleration * remainingTime;
            otherPos = pos_ToIOther + otherVel * remainingTime;
        }
    }

    private Vector3 ElasticCollision(Vector3 vel_parallel, Vector3 vel_parallelOther, float mass1, float mass2)
    {
        float calc1 = ((mass1 - mass2) / (mass1 + mass2));
        Vector3 x = calc1 * vel_parallel;
        float calc2 = ((2 * mass2) / (mass1 + mass2));
        Vector3 y = calc2 * vel_parallelOther;

        return x + y;
    }

    public void overrideAfterCollision(Vector3 pos, Vector3 vel)
    {
        if (isHeld) return;

        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
        }

        transform.position = pos;
        this.velocity = vel;
    }

    public float Radius
    {
        get
        {
            float diameter = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            return diameter / 2f;
        }
        set { transform.localScale = value * 2f * Vector3.one; }
    }

    public int rank => 1;
}