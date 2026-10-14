using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class AllomancyMovement : MonoBehaviour
{
    #region Variables

    //references
    private Rigidbody rb;
    public Transform AllomanticLineOrigin; // The origin point for the Allomantic line
    public GameObject user; // The character using Allomancy
    [SerializeField] private LayerMask metalLayer; // Layer mask for metal objects
    public GameObject CoinPrefab; // Prefab for the coin to be instantiated

    // Allomancy variables
    public float detectionRadius = 10f; // Radius to detect nearby metal objects
    private bool isPushing = false;
    private bool isPulling = false;
    public float pushForce = 10f;
    public float pullForce = 10f;
    private bool duraluminActive = false;
    public float duraluminCD = 5f; // Cooldown time for Duralumin
    private float duraluminTimer = 0f; // Timer to track Duralumin cooldown
    private bool canUseDuralumin = true; // Flag to check if Duralumin can be used
    private Vector3 movementDirection;
    private List<GameObject> nearbyMetalObjects = new List<GameObject>();

    //LineRenderer variables
    [SerializeField, Range(0.01f, 0.1f)] private float lineWidth = 0.01f;
    public Material lineMaterial; // Material for the Allomantic line
    [SerializeField] private Color nearRangeColor = Color.white;
    [SerializeField] private Color farRangeColor = Color.gray;
    [SerializeField, Min(0f)] private float nearRange = 0f;
    [SerializeField, Min(0.01f)] private float farRange = 10f;
    [SerializeField, Range(4, 32)] private int curveSegments = 12;
    [SerializeField, Min(0f)] private float maxCurveOffset = 0.5f;
    [SerializeField, Min(0.01f)] private float speedForMaxCurve = 5f;

    public GameObject debugGameObject; // object for testing
    private LineRenderer debugLineRenderer;
    private MaterialPropertyBlock linePropertyBlock;
    private int lineColorPropertyId;




    #endregion
    #region Start/Update
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (debugGameObject == null || AllomanticLineOrigin == null || lineMaterial == null)
            {
                Debug.LogError("Assign the debug GameObject, Allomantic Line Origin, and line material.", this);
                return;
            }

            if (!debugGameObject.TryGetComponent(out debugLineRenderer))
            {
                Debug.LogError("The debug GameObject must have a LineRenderer component.", debugGameObject);
                return;
            }

            if (lineMaterial.HasProperty("_BaseColor"))
                lineColorPropertyId = Shader.PropertyToID("_BaseColor");
            else if (lineMaterial.HasProperty("_Color"))
                lineColorPropertyId = Shader.PropertyToID("_Color");
            else
            {
                Debug.LogError("The line material shader has no _BaseColor or _Color property.", lineMaterial);
                debugLineRenderer = null;
                return;
            }

            debugLineRenderer.useWorldSpace = true;
            debugLineRenderer.positionCount = curveSegments + 1;
            debugLineRenderer.startWidth = lineWidth;
            debugLineRenderer.endWidth = lineWidth;
            debugLineRenderer.sharedMaterial = lineMaterial;
            debugLineRenderer.enabled = true;
            linePropertyBlock = new MaterialPropertyBlock();
        }

        if (debugLineRenderer != null && debugLineRenderer.enabled &&
            debugGameObject != null && AllomanticLineOrigin != null)
        {
            Vector3 objectPosition = debugGameObject.transform.position;
            Vector3 originPosition = AllomanticLineOrigin.position;
            debugLineRenderer.positionCount = curveSegments + 1;

            Vector3 lineDirection = (originPosition - objectPosition).normalized;
            Vector3 lateralVelocity = rb != null
                ? Vector3.ProjectOnPlane(rb.linearVelocity, lineDirection)
                : Vector3.zero;
            Vector3 curveDirection = lateralVelocity.sqrMagnitude > 0.001f
                ? -lateralVelocity.normalized
                : Vector3.zero;
            float speedFactor = rb != null
                ? Mathf.Clamp01(lateralVelocity.magnitude / speedForMaxCurve)
                : 0f;
            Vector3 controlPoint = (objectPosition + originPosition) * 0.5f
                + curveDirection * (maxCurveOffset * speedFactor * 2f);

            for (int i = 0; i <= curveSegments; i++)
            {
                float t = (float)i / curveSegments;
                float inverseT = 1f - t;
                Vector3 point = inverseT * inverseT * objectPosition
                    + 2f * inverseT * t * controlPoint
                    + t * t * originPosition;
                debugLineRenderer.SetPosition(i, point);
            }

            float distance = Vector3.Distance(objectPosition, originPosition);
            float range = Mathf.InverseLerp(nearRange, Mathf.Max(nearRange + 0.01f, farRange), distance);
            Color rangeColor = Color.Lerp(nearRangeColor, farRangeColor, range);

            debugLineRenderer.GetPropertyBlock(linePropertyBlock);
            linePropertyBlock.SetColor(lineColorPropertyId, rangeColor);
            debugLineRenderer.SetPropertyBlock(linePropertyBlock);
        }
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



    #endregion
}
