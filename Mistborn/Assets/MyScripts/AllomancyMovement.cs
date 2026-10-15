using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
//using UnityEngine.Events;

public class AllomancyMovement : MonoBehaviour
{
    #region Variable Declarations

    //references
    private Rigidbody rb;
    public Transform AllomanticLineOrigin; // The origin point for the Allomantic line
    public GameObject user; // The character using Allomancy
    [SerializeField] private LayerMask metalLayer; // Layer mask for metal objects
    private SphereCollider detectionTrigger;
    public GameObject CoinPrefab; // Prefab for the coin to be instantiated

    // Allomancy variables
    public float detectionRadius = 10f; // Radius to detect nearby metal objects
    public Material lineMaterial; // Material for the Allomantic line
    private bool isPushing = false;
    private bool isPulling = false;
    public float pushForce = 10f;
    public float pullForce = 10f;
    private bool duraluminActive = false;
    public float duraluminCD = 5f; // Cooldown time for Duralumin
    private float duraluminTimer = 0f; // Timer to track Duralumin cooldown
    private bool canUseDuralumin = true; // Flag to check if Duralumin can be used
    private Vector3 movementDirection;

    //Coin variables
    public int maxCoinCount = 10; // Maximum number of coins the player can carry

    [SerializeField] private int coinCount = 0; // Number of coins the player currently has
    

    private List<GameObject> nearbyMetalObjects = new List<GameObject>();

    public GameObject debugGameObject; // object for testing
    private bool isDebugging = false; // flag to toggle debugging


    #endregion
    #region Start/Update
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //startup code here
        rb = GetComponent<Rigidbody>();
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
        foreach (GameObject metalObject in nearbyMetalObjects)
        {
            if (metalObject != null)
            {
                DrawAllomanticLine(metalObject);
            }
        }

        //Turns on/off debugging mode and draws the line between the player and the metal object
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isDebugging = !isDebugging;
            debugGameObject.SetActive(isDebugging);
        }
        if (isDebugging)//does the actual drawing of the line between the player and the metal object
        {
            DrawAllomanticLine(debugGameObject);
        }
       
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

    #endregion


    #region Functions

    void SteelPush()
    {
        // Implement Steel Push logic here
        print("Steel Push activated");
    }

    void IronPull()
    {
        // Implement Iron Pull logic here
        print("Iron Pull activated");
    }

    void ActivateDuralumin()
    {
        // Implement Duralumin activation logic here
        print("Duralumin activated");
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
