using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;




public class ExplorerStateManager : MonoBehaviour, ICommunicator
{
    public enum ExplorerState { Idle, Walk, Search, Danger, Dead, Success }
    public ExplorerState currentState;

    public float visionRange = 2f;

    public float moveSpeed = 1.5f; // speed of explorer
    private Vector2 randomDirection;
    private float changeDirectionTime = 3f; // how often to pick new random direction
    private float directionTimer;

    private bool canMove = false;

    public TextMeshProUGUI gameOverText;

    public TextMeshProUGUI messageText;

    private Transform dangerSource;

    private Animator animator;

    private bool isSandstorm = false;
    private float sandstormTimer = 0f;
    [SerializeField]
    private float timeBetweenSandstorms = 25f; 
    private float sandstormDuration = 10f; 

    private float originalVisionRange; 

    public GameObject sandstormOverlay;

    private bool isNight = false;
    private float dayNightTimer = 0f;
    private float dayDuration = 120f; 
    private float nightDuration = 15f; 
    private Camera mainCamera; 

    public GameObject sunImage;
    public GameObject moonImage;

    public int maxHealth = 100;
    private int currentHealth;

    public TextMeshProUGUI healthText; // To show health on screen

    public GameObject sandstormParticles;

    private CanvasGroup sandstormCanvasGroup;

    public GameObject nightOverlay;
    private CanvasGroup nightCanvasGroup;

    private float damageCooldown = 1f; 
    private float lastDamageTime = -1f; 

    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    public float memoryBiasStrength = 5f;  
    public float cellSize = 2f;

    private float fearLevel = 0f;
    public float maxFear = 100f;
    public float fearIncreaseRate = 20f;
    public float fearDecayRate = 5f;

    private Transform waterSource;

    public static event System.Action<ExplorerState> OnStateChanged;
    private ExplorerState lastState;

    [Header("Map Bounds (set these in the Inspector)")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    public Slider fearSlider;              
    public TextMeshProUGUI fearText;       

    [HideInInspector] public Vector2 seekTarget;
    [HideInInspector] public bool hasSeekTarget = false;

    private void Start()
    {
        canMove = true;
        directionTimer = changeDirectionTime;    
        ChooseNewDirection();
        animator = GetComponent<Animator>();
        currentState = ExplorerState.Walk; 
        originalVisionRange = visionRange;

        if (sandstormOverlay != null)
        {
            sandstormCanvasGroup = sandstormOverlay.GetComponent<CanvasGroup>();
            sandstormOverlay.SetActive(true); 
            if (sandstormCanvasGroup != null)
                sandstormCanvasGroup.alpha = 0f; 
        }

        if (sandstormParticles != null)
            sandstormParticles.SetActive(false);


        mainCamera = Camera.main;
        SetDay();

        currentHealth = maxHealth;
        UpdateHealthUI();

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        if (nightOverlay != null)
        {
            nightCanvasGroup = nightOverlay.GetComponent<CanvasGroup>();
            nightOverlay.SetActive(true); 
            if (nightCanvasGroup != null)
            {
                nightCanvasGroup.alpha = 0f; 
            }
        }
        randomDirection = Random.insideUnitCircle.normalized;
    }

    private void Update()
    {
        HandleState();
        CheckVision();
        HandleSandstorm();
        HandleDayNightCycle();
        UpdateVision();

    }

    void HandleState()
    {
        if (currentState != lastState)
        {
            UpdateMessage(currentState.ToString());
            OnStateChanged?.Invoke(currentState);
            lastState = currentState;
        }
        UpdateFear(currentState == ExplorerState.Danger);

        switch (currentState)
        {
            case ExplorerState.Idle:
                animator.SetFloat("Speed", 0f);
                break;

            case ExplorerState.Walk:
                UpdateMessage("Searching...");
                WalkAround();
                break;

            case ExplorerState.Search:
                UpdateMessage("Searching...");
                SearchForWater();
                break;

            case ExplorerState.Danger:
                UpdateMessage("Enemy spotted!!");
                RunFromDanger();
                break;

            case ExplorerState.Dead:
                Die();
                break;

            case ExplorerState.Success:
                Celebrate();
                break;
        }
    }

    void WalkAround()
    {
        RandomMovement();
    }

    void SearchForWater()
    {
        Debug.Log($"[Search] hasSeekTarget={hasSeekTarget}, target={seekTarget}");
        if (!hasSeekTarget)
        {
            RandomMovement();
            return;
        }

        // 1) Compute direction & move
        Vector2 pos = transform.position;
        Vector2 dir = (seekTarget - pos).normalized;
        float speed = CurrentSpeed();
        Vector2 newPos = Vector2.MoveTowards(pos, seekTarget, speed * Time.deltaTime);

        Debug.Log($"[Explorer] Moving from {pos} → {seekTarget} (now at {newPos})");

        transform.position = newPos;
        UpdateAnimator(dir, speed);

        // 2) Check arrival
        if (Vector2.Distance(newPos, seekTarget) < 0.1f)
        {
            currentState = ExplorerState.Success;
            hasSeekTarget = false;
        }
    }


    void Die()
    {
        Debug.Log("Explorer died.");
        canMove = false;
        if (gameOverText != null)
        {
            ShowMessage(gameOverText, "Game Over");
        }
        Invoke("RestartGame", 3f); //restart the game after 3 seconds
    }
    void Celebrate()
    {
        RecordVisit();
        if (waterSource != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                waterSource.position,
                CurrentSpeed() * Time.deltaTime
            );
            if (Vector2.Distance(transform.position, waterSource.position) < 0.1f)
                canMove = false;
                UpdateMessage("You've found the water. You’ve reached the oasis!");
        }
    }


    void UpdateMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }


    private void OnDrawGizmosSelected()
    {
        // Set Gizmo color to yellow
        Gizmos.color = Color.yellow;

        // Draw a wire circle around the player to show vision range
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
    void RandomMovement()
    {
        if (!canMove) return;

        RecordVisit();

        directionTimer += Time.deltaTime;
        if (directionTimer > changeDirectionTime)
        {
            ChooseNewDirection();
            directionTimer = 0f;
        }

        // 1) Compute a true unit‐direction and the current speed
        Vector2 dir = randomDirection.normalized;
        float speed = CurrentSpeed();

        // 2) Move
        transform.Translate(dir * speed * Time.deltaTime, Space.World);

        UpdateAnimator(dir, speed);
    }
    private void UpdateAnimator(Vector2 dir, float speed)
    {
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
        animator.SetFloat("Speed", speed);
    }
    void CheckVision()
    {
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Water"))
            {
                waterSource = hit.transform;
                currentState = ExplorerState.Success;
            }
            else if (hit.CompareTag("Enemy"))
            {
                currentState = ExplorerState.Danger;
                dangerSource = hit.transform; // Save the enemy transform
            }
        }
    }
    void RunFromDanger()
    {
        RecordVisit();
        UpdateFear(true);
        if (dangerSource == null)
        {
            // if he still know where the water is, go back into Search
            currentState = hasSeekTarget
                           ? ExplorerState.Search
                           : ExplorerState.Walk;
            return;
        }

        float distanceFromDanger = Vector2.Distance(transform.position, dangerSource.position);

        if (distanceFromDanger > visionRange * 2f)
        {
            dangerSource = null;
            currentState = ExplorerState.Walk;
            return;
        }

        // Still in danger, run away
        Vector2 directionAway = (transform.position - dangerSource.position).normalized;

        // 1) pull out the speed into a local
        float speed = CurrentSpeed();

        // 2) move
        transform.Translate(directionAway * speed * Time.deltaTime);

        // 3) animate
        UpdateAnimator(directionAway, speed);

        // take damage while close to the enemy
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            TakeDamage(1);
            lastDamageTime = Time.time;
        }
    }


    void HandleSandstorm()
    {
        sandstormTimer += Time.deltaTime;

        if (!isSandstorm && sandstormTimer > timeBetweenSandstorms)
        {
            // Start a new sandstorm
            isSandstorm = true;
            sandstormTimer = 0f;
            visionRange = originalVisionRange * 0.5f; // Reduce vision by half
           
            if (sandstormOverlay != null)
            StartCoroutine(FadeCanvas(sandstormCanvasGroup, 1f, 1f));

            if (sandstormParticles != null)
                sandstormParticles.SetActive(true);
            TakeDamage(2);
        }

        if (isSandstorm && sandstormTimer > sandstormDuration)
        {
            // End sandstorm
            isSandstorm = false;
            sandstormTimer = 0f;
            visionRange = originalVisionRange;

            if (messageText != null)
                messageText.text = "Searching...";

            if (sandstormOverlay != null)
                StartCoroutine(FadeCanvas(sandstormCanvasGroup, 0f, 1f));

            if (sandstormParticles != null)
                sandstormParticles.SetActive(false);
        }
    }
    void SetDay()
    {
        isNight = false;
        dayNightTimer = 0f;
        if (mainCamera != null)
            mainCamera.backgroundColor = new Color(0.5f, 0.8f, 1f); // Light blue sky
       
        if (sunImage != null) sunImage.SetActive(true);
        if (moonImage != null) moonImage.SetActive(false);
                
        if (nightCanvasGroup != null)
            StartCoroutine(FadeCanvas(nightCanvasGroup, 0f, 1f)); // fade out
    }

    void SetNight()
    {
        isNight = true;
        dayNightTimer = 0f;
        if (mainCamera != null)
            mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.2f); // Dark blue night sky
        
        if (sunImage!= null) sunImage.SetActive(false);
        if (moonImage != null) moonImage.SetActive (true);

        if (nightCanvasGroup != null)
            StartCoroutine(FadeCanvas(nightCanvasGroup, 1f, 1f));
        TakeDamage(3);
    }

    void HandleDayNightCycle()
    {
        dayNightTimer += Time.deltaTime;

        if (!isNight && dayNightTimer > dayDuration)
        {
            SetNight();
        }
        else if (isNight && dayNightTimer > nightDuration)
        {
            SetDay();
        }
    }
        void UpdateVision()
    {
        visionRange = originalVisionRange * (isSandstorm ? 0.5f : 1f) * (isNight ? 0.6f : 1f);
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "Health: " + currentHealth;

        if (fearSlider != null)
            fearSlider.value = fearLevel / maxFear;

        if (fearText != null)
            fearText.text = "Fear: " + Mathf.RoundToInt(fearLevel);
    }

    void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0)
            return; // Already dead, no more damage

        currentHealth -= damageAmount;
        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHealthUI();

        if (currentHealth == 0)
        {
            currentState = ExplorerState.Dead; // Move to Dead state
            if (animator != null)
                animator.SetTrigger("Die"); // Play dying animation!

            Die();
        }
    }
    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.
            GetActiveScene().buildIndex);
    }
    void ShowMessage(TextMeshProUGUI textElement, string message)
    {
        if (textElement != null)
        {
            textElement.text = message;
            if (!textElement.gameObject.activeSelf)
                textElement.gameObject.SetActive(true);
        }
    }

    IEnumerator FadeCanvas(CanvasGroup canvas, float targetAlpha, float duration)
    {
        float startAlpha = canvas.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        canvas.alpha = targetAlpha;
    }
    private void RecordVisit()
    {
        var cell = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / cellSize),
            Mathf.RoundToInt(transform.position.y / cellSize)
        );
        visitedCells.Add(cell);
    }
    private void ChooseNewDirection()
    {
        const int samples = 8;
        Vector2 bestDir = Vector2.zero;
        int bestScore = int.MaxValue;

        for (int i = 0; i < samples; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector2 targetPos = (Vector2)transform.position + dir * cellSize;
            var cell = new Vector2Int(
                Mathf.RoundToInt(targetPos.x / cellSize),
                Mathf.RoundToInt(targetPos.y / cellSize)
            );
            int visits = visitedCells.Contains(cell) ? 1 : 0;
            if (visits < bestScore)
            {
                bestScore = visits;
                bestDir = dir;
                if (visits == 0) break;
            }
        }
        randomDirection = bestDir * (1 + memoryBiasStrength * (1 - bestScore));
    }
    private void UpdateFear(bool inDanger)
    {
        if (inDanger)
            fearLevel = Mathf.Min(maxFear, fearLevel + fearIncreaseRate * Time.deltaTime);
        else
            fearLevel = Mathf.Max(0, fearLevel - fearDecayRate * Time.deltaTime);
        UpdateHealthUI();
    }
    private float CurrentSpeed()
    {
        // compute a fear‐based multiplier:
        float fearMultiplier = (currentState == ExplorerState.Danger)
            ? (1f + fearLevel / maxFear)        // speed up when fleeing
            : (1f - 0.5f * fearLevel / maxFear); // slow down when not in danger

        // apply it to your base moveSpeed
        return moveSpeed * fearMultiplier;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var cell in visitedCells)
        {
            var world = new Vector3(cell.x * cellSize, cell.y * cellSize, 0);
            Gizmos.DrawWireCube(world, Vector3.one * cellSize * 0.9f);
        }
    }
    private void LateUpdate()
    {
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }
    public void ReceiveMessage(string message, Transform from)
    {
        if (message.StartsWith("Water at"))
        {
            Debug.Log($"[Explorer] Received “{message}” from {from.name}");
            // parse "Water at x,y"
            var coords = message.Substring(9).Split(',');
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            seekTarget = new Vector2(x, y);
            hasSeekTarget = true;
            currentState = ExplorerState.Search;
        }
    }

}
