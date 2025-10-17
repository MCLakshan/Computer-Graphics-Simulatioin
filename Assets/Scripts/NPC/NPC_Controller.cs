using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public enum NPC_State
{
    Idle,
    Patrol,
    Chase,
    Attack
}

public class NPC_Controller : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("NPC Settings")]
    [SerializeField] private NPC_State currentState = NPC_State.Idle;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private Transform eyesTransform;
    
    [Header("Detection Status")]
    [SerializeField] bool isInDetectionRange = false;
    [SerializeField] bool isInAttackRange = false;
    [SerializeField] bool isInVisionAngle = false;
    
    [Header("Navi Mesh Agent Settings")]
    [SerializeField] private Transform[] roamPoints;
    [SerializeField] private float roamSpeed = 4f;
    [SerializeField] private float chaseSpeed = 5f;
    
    [Header("Animation Settings")]
    
    
    [Header("Debug Settings")]
    [SerializeField] bool isDebug = false;
    [SerializeField] private bool showDetectionRange = true;
    
    private NavMeshAgent agent;
    private int currentRoamIndex = 0;
    private Animator animator;
    private float npcVelocity;      // For animation blend tree
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component is missing on the NPC.");
            return;
        }
        
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component is missing on the NPC or its children.");
            return;
        }
    }
    
    private void Update()
    {
        CkeckRanges();
        CheckNpcState();
        PerformActions();
        
        // Update animator with the current velocity
        npcVelocity = agent.velocity.magnitude;
        animator.SetFloat("Velocity", npcVelocity / chaseSpeed);
        
    }
    
    private void CkeckRanges()
    {
        // Here no serious logic, just simple distance and angle checks
        
        if (target == null) return;
        
        // Ignore the y-axis for distance calculation
        Vector2 npcPosition2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetPosition2D = new Vector2(target.position.x, target.position.z);
        float distance = Vector2.Distance(npcPosition2D, targetPosition2D);
        
        isInDetectionRange = distance <= detectionRange;
        
        // Check the vision angle
        if (isInDetectionRange)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            isInVisionAngle = angleToTarget <= visionAngle / 2f;
        }
        else
        {
            isInVisionAngle = false;
        }
        
        // Check attack range
        isInAttackRange = distance <= attackRange;
    }
    
    private void CheckNpcState()
    {
        // Simple state machine logic
        
        // idle state
        if (!isInDetectionRange || !isInVisionAngle)
        {
            currentState = NPC_State.Patrol;
            return;
        }
        
        // chase state
        if (isInDetectionRange && isInVisionAngle && !isInAttackRange)
        {
            // is the target is in the detection range and vision angle 
            // check if there are obstacles between NPC and target (line of sight)
            
            Vector3 rayCastStart = eyesTransform.position; // use eyes position for better accuracy 
            Vector3 rayCastEnd = new Vector3(target.position.x, rayCastStart.y, target.position.z); // keep the same height as eyes
            
            // Cast a ray to check for obstacles
            RaycastHit hit;
            if (Physics.Raycast(rayCastStart, (rayCastEnd - rayCastStart).normalized, out hit, detectionRange))
            {
                if (hit.transform != target)
                {
                    // There is an obstacle between NPC and target
                    currentState = NPC_State.Patrol;
                    return;
                }
            }

            currentState = NPC_State.Chase;
            return;
        }
        
        // attack state
        if (isInVisionAngle && isInAttackRange)
        {
            currentState = NPC_State.Attack;
            return;
        }
    }
    
    // Check the state and perform actions accordingly
    private void PerformActions()
    {
        // roam state
        if (currentState == NPC_State.Patrol)
        {
            // Simple roaming logic
            if (roamPoints.Length == 0) return;
            
            // Roaming logic is running on (x,z) plane
            // NavMeshAgent is in the current gameObject
            agent.speed = roamSpeed;
            if (!agent.hasPath || agent.remainingDistance < agent.stoppingDistance)
            {
                // Roam through the points in a loop in the array
                currentRoamIndex = (currentRoamIndex + 1) % roamPoints.Length;
                agent.SetDestination(roamPoints[currentRoamIndex].position);
            }
        }
        
        // chase state
        if (currentState == NPC_State.Chase)
        {
            if (target == null) return;
            agent.speed = chaseSpeed;
            agent.SetDestination(target.position);
        }
        
        // attack state
        if (currentState == NPC_State.Attack)
        {
            agent.ResetPath();
            animator.SetBool("Attack", true);
        }
        else
        {
            animator.SetBool("Attack", false);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!isDebug) return;
        
        // Draw detection range
        if (showDetectionRange)
        {
            Gizmos.color = isInDetectionRange ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = isInVisionAngle ? Color.green : Color.red;
            Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRange);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRange);
        }
        
        // Draw attack range lets use another color to differentiate
        Gizmos.color = isInAttackRange ? Color.blue : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw roam points and paths
        if (roamPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < roamPoints.Length; i++)
            {
                Vector3 currentPoint = roamPoints[i].position;
                Vector3 nextPoint = roamPoints[(i + 1) % roamPoints.Length].position;
                Gizmos.DrawLine(currentPoint, nextPoint);
                
                // Draw arrow head
                Vector3 direction = (nextPoint - currentPoint).normalized;
                Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
                Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
                Gizmos.DrawLine(nextPoint, nextPoint + right * 0.5f);
                Gizmos.DrawLine(nextPoint, nextPoint + left * 0.5f);
            }
        }
        
        // Draw the obstacle check ray
        // Change color based on whether the target is detected or not
        if (target != null)
        {
            Gizmos.color = (currentState == NPC_State.Chase) ? Color.green : Color.red;
            Vector3 rayCastStart = eyesTransform.position; // use eyes position for better accuracy 
            Vector3 rayCastEnd = new Vector3(target.position.x, rayCastStart.y, target.position.z); // keep the same height as eyes
            Gizmos.DrawLine(rayCastStart, rayCastEnd);
        }
    }
}
