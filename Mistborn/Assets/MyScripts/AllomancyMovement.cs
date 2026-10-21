using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
//using UnityEngine.Events;

public class AllomancyMovement : MonoBehaviour
{
    #region Variable Declarations

    //references
    private Rigidbody rb;
    [SerializeField] private StarterAssets.ThirdPersonController thirdPersonController;
    public Transform AllomanticLineOrigin; // The origin point for the Allomantic line
    public GameObject user; // The character using Allomancy
    [SerializeField] private LayerMask metalLayer; // Layer mask for metal objects
    private SphereCollider detectionTrigger;
    public GameObject CoinPrefab; // Prefab for the coin to be instantiated

    // Allomancy variables
    public float detectionRadius = 10f; // Radius to detect nearby metal objects
    public Material lineMaterial; // Material for the Allomantic line
    public Material arrowMaterial; // Material for the debug arrow
    public Material selectedMaterial; // Material for the selected metal object
    private bool isPushing = false;
    private bool isPulling = false;
    public float pushForce = 10f;
    public float pullForce = 10f;
    private bool duraluminActive = false;
    public float duraluminCD = 5f; // Cooldown time for Duralumin
    private float duraluminTimer = 0f; // Timer to track Duralumin cooldown
    private bool canUseDuralumin = true; // Flag to check if Duralumin can be used
    private Vector3 movementDirection;
    [SerializeField] private float debugArrowLength = 2f;
    //[SerializeField] private Color debugArrowColor = Color.yellow;
    [SerializeField] private float debugArrowHeight = 1f;
    private LineRenderer debugArrowShaft;
    private LineRenderer debugArrowHeadLeft;
    private LineRenderer debugArrowHeadRight;

    //Coin variables
    public int maxCoinCount = 10; // Maximum number of coins the player can carry

    [SerializeField] private int coinCount = 0; // Number of coins the player currently has


    private List<GameObject> nearbyMetalObjects = new List<GameObject>();
    public GameObject selectedMetalObject = null; // The closest metal object in the direction of arrow
    [Range(0f, 180f)]
    public float selectionAngleThreshold = 30f; // Angle threshold for selecting the closest metal object


    #endregion
    #region Start/Update
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //startup code here
        rb = GetComponent<Rigidbody>();
        if (thirdPersonController == null)
            thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
        if (thirdPersonController == null)
            Debug.LogError("AllomancyMovement requires a ThirdPersonController reference.", this);

        if (lineMaterial == null)
            Debug.LogError("Assign a line material to render the movement debug arrow.", this);
        else
        {
            debugArrowShaft = CreateDebugArrowLine("Movement Debug Arrow Shaft");
            debugArrowHeadLeft = CreateDebugArrowLine("Movement Debug Arrow Head Left");
            debugArrowHeadRight = CreateDebugArrowLine("Movement Debug Arrow Head Right");
        }

        detectionTrigger = GetComponent<SphereCollider>();
        detectionTrigger.isTrigger = true;
        detectionTrigger.radius = detectionRadius;
        coinCount = maxCoinCount; // Initialize coin count to max at the start

        Collider[] metalColliders = Physics.OverlapSphere(
            detectionTrigger.bounds.center,
            detectionTrigger.bounds.extents.x,
            metalLayer,
            QueryTriggerInteraction.Collide);

        foreach (Collider metalCollider in metalColliders)
        {
            if (!nearbyMetalObjects.Contains(metalCollider.gameObject))
                nearbyMetalObjects.Add(metalCollider.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (thirdPersonController != null)
        {
            Vector3 inputDirection = thirdPersonController.MovementDirection;
            if (inputDirection.sqrMagnitude <= 0.0001f)
            {
                movementDirection = Vector3.up;
            }
            else if (thirdPersonController.IsJumpingOrSpaceHeld)
            {
                movementDirection = (inputDirection + Vector3.up).normalized;
            }
            else
            {
                movementDirection = inputDirection;
            }


        }
        UpdateDebugArrow();

        foreach (GameObject metalObject in nearbyMetalObjects)
        {
            if (metalObject != null)
            {
                DrawAllomanticLine(metalObject);
            }
        }
        selectedMetalObject = FindSteelPushTarget(-movementDirection);

    }

    void OnDrawGizmosSelected()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            return;

        Vector3 scale = transform.lossyScale;
        float worldRadius = detectionRadius * Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), worldRadius);
    }

    LineRenderer CreateDebugArrowLine(string objectName)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.04f;
        lineRenderer.endWidth = 0.04f;
        lineRenderer.sharedMaterial = arrowMaterial;
        //lineRenderer.startColor = debugArrowColor;
        //lineRenderer.endColor = debugArrowColor;
        lineRenderer.enabled = false;
        return lineRenderer;
    }

    void UpdateDebugArrow()
    {
        if (debugArrowShaft == null)
            return;

        bool hasMovementDirection = movementDirection.sqrMagnitude > 0.0001f;
        debugArrowShaft.enabled = hasMovementDirection;
        debugArrowHeadLeft.enabled = hasMovementDirection;
        debugArrowHeadRight.enabled = hasMovementDirection;
        if (!hasMovementDirection)
            return;

        Vector3 arrowDirection = -movementDirection.normalized;
        Vector3 arrowStart = transform.position + Vector3.up * debugArrowHeight;
        Vector3 arrowTip = arrowStart + arrowDirection * debugArrowLength;
        Vector3 arrowSide = Vector3.Cross(Vector3.up, arrowDirection).normalized;
        float arrowHeadLength = debugArrowLength * 0.2f;

        debugArrowShaft.SetPositions(new[] { arrowStart, arrowTip });
        debugArrowHeadLeft.SetPositions(new[]
        {
            arrowTip,
            arrowTip - arrowDirection * arrowHeadLength + arrowSide * arrowHeadLength * 0.5f
        });
        debugArrowHeadRight.SetPositions(new[]
        {
            arrowTip,
            arrowTip - arrowDirection * arrowHeadLength - arrowSide * arrowHeadLength * 0.5f
        });
    }

    #endregion


    #region Functions

    public void SteelPush(GameObject pushTarget)
    {
        if (pushTarget == null)
        {
            Debug.LogWarning("No metal object selected for Steel Push.");
            return;
        }

        if (thirdPersonController == null)
        {
            Debug.LogError("Cannot Steel Push because ThirdPersonController is missing.", this);
            return;
        }

        Vector3 pushDirection = transform.position - pushTarget.transform.position;
        Vector3 horizontalOffset = pushDirection;
        horizontalOffset.y = 0f;

        CharacterController characterController = GetComponent<CharacterController>(); //used to see if player above the target, if so, push up instead of away from the target
        Collider targetCollider = pushTarget.GetComponent<Collider>();
        float topPushDistance = characterController != null
            ? characterController.radius + (targetCollider != null ? targetCollider.bounds.extents.magnitude : 0f)
            : 0.5f;
        bool isAboveTarget = pushDirection.y > 0f &&
                             horizontalOffset.sqrMagnitude <= topPushDistance * topPushDistance;
        bool hasMovementInput = thirdPersonController.MovementDirection.sqrMagnitude > 0.0001f;

        if (isAboveTarget && !hasMovementInput)
            pushDirection = Vector3.up;

        if (pushDirection.sqrMagnitude <= 0.0001f)
            return;

        thirdPersonController.ApplyExternalImpulse(
            pushDirection.normalized * pushForce * Time.deltaTime);
    }

    public void IronPull(GameObject pullTarget)
    {
        // Implement Iron Pull logic here
        print("Iron Pull activated");
    }

    public void ActivateDuralumin()
    {
        // Implement Duralumin activation logic here
        print("Duralumin activated");
    }

    public void SpawnCoin()
    {
        if (CoinPrefab == null)
        {
            Debug.LogError("CoinPrefab is not assigned in the inspector.");
            return;
        }
        else if (coinCount != 0)
        {
            Instantiate(CoinPrefab, transform.position + Vector3.up, Quaternion.Euler(-90f, 0f, 0f));
            coinCount--;
        }
        else
        {
            Debug.Log("No coins left to spawn.");
        }
    }

    GameObject FindSteelPushTarget(Vector3 direction)//Function for finding the closest metal object in the direction of the arrow
    {
        GameObject closestMetalObject = null;
        if (nearbyMetalObjects.Count == 0)
            return null;

        foreach (GameObject metalObject in nearbyMetalObjects)
        {
            if (metalObject == null)
                continue;

            Vector3 toMetalObject = (metalObject.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(direction, toMetalObject);

            if (angle <= selectionAngleThreshold)
            {
                if (closestMetalObject == null || Vector3.Distance(transform.position, metalObject.transform.position) < Vector3.Distance(transform.position, closestMetalObject.transform.position))
                {
                    closestMetalObject = metalObject;
                }
            }
        }
        if (closestMetalObject != null)
        {
            LineRenderer lineRenderer = closestMetalObject.GetComponent<LineRenderer>();
            if (lineRenderer != null)
                lineRenderer.material = selectedMaterial;
            else
                Debug.LogError($"Metal object '{closestMetalObject.name}' needs a LineRenderer component.", closestMetalObject);
        }

        return closestMetalObject;
    }

    void DrawAllomanticLine(GameObject metalObject) //Draws the blue line between the player and the metal object
    {
        if (metalObject == null || AllomanticLineOrigin == null)
            return;

        LineRenderer lineRenderer = metalObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError($"Metal object '{metalObject.name}' needs a LineRenderer component.", metalObject);
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, AllomanticLineOrigin.position);
        lineRenderer.SetPosition(1, metalObject.transform.position);

        //LineRenderer Variable update
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & metalLayer) != 0)
        {
            if (!nearbyMetalObjects.Contains(other.gameObject)) //checks if already in the list
                nearbyMetalObjects.Add(other.gameObject);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & metalLayer) != 0)
        {
            if (nearbyMetalObjects.Remove(other.gameObject))
            {
                LineRenderer lineRenderer = other.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                    lineRenderer.positionCount = 0;
            }
        }
    }

    #endregion
}
