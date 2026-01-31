using UnityEngine;

public class Tetromino : MonoBehaviour
{
    private float previousTime;
    private GameManager gameManager; // Cached GameManager instance

    public float lockDelay = 0.5f;
    private float lockTime = 0;

    public float dasDelay = 0.3f;
    public float dasSpeed = 0.1f;
    private float dasTimerLeft = 0f;
    private float dasTimerRight = 0f;

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in scene!");
            this.enabled = false; // Disable this script if GameManager is not found
        }
    }

    void Start()
    {
        // Check if the spawn position is valid, otherwise Game Over
        if (!IsValidMove())
        {
            AddToGridAndCheckGameOver(); // This will handle hiding invalid parts and calling GameOver
            this.enabled = false;
        }

        UpdateVisibility();
    }

    void Update()
    {
        if (FindObjectOfType<GameManager>().gameState != GameManager.GameState.Playing)
        {
            return;
        }

        // --- Player Input ---
        // --- LEFT ---
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += new Vector3(-1, 0, 0);
            if (!IsValidMove())
                transform.position -= new Vector3(-1, 0, 0);
            else
                lockTime = 0;

            dasTimerLeft = Time.time + dasDelay;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) && Time.time > dasTimerLeft)
        {
            transform.position += new Vector3(-1, 0, 0);
            if (!IsValidMove())
                transform.position -= new Vector3(-1, 0, 0);
            else
                lockTime = 0;

            dasTimerLeft = Time.time + dasSpeed;
        }

        // --- RIGHT ---
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += new Vector3(1, 0, 0);
            if (!IsValidMove())
                transform.position -= new Vector3(1, 0, 0);
            else
                lockTime = 0;

            dasTimerRight = Time.time + dasDelay;
        }
        else if (Input.GetKey(KeyCode.RightArrow) && Time.time > dasTimerRight)
        {
            transform.position += new Vector3(1, 0, 0);
            if (!IsValidMove())
                transform.position -= new Vector3(1, 0, 0);
            else
                lockTime = 0;

            dasTimerRight = Time.time + dasSpeed;
        }
        
        // Rotate
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (!IsValidMove())
                transform.Rotate(0, 0, 90);
            else
                lockTime = 0; // Reset lock on successful move
        }

        // --- Automatic Falling ---
        if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? GameManager.currentFallTime / 10 : GameManager.currentFallTime))
        {
            transform.position += new Vector3(0, -1, 0);

            if (!IsValidMove())
            {
                transform.position -= new Vector3(0, -1, 0); // Hit something

                // Start lock delay if not already started
                if (lockTime == 0)
                {
                    lockTime = Time.time + lockDelay;
                }

                // Check if lock delay has expired
                if (Time.time > lockTime)
                {
                    LockPiece(); // New function to lock the piece
                }
            }
            else
            {
                // If it fell successfully, it's not on the ground, so reset lock timer
                lockTime = 0;
            }

            previousTime = Time.time;
        }

        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        foreach (Transform child in transform)
        {
            if (child.transform.position.y >= GameManager.height)
            {
                foreach(var renderer in child.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.enabled = false;
                }
            }
            else
            {
                foreach(var renderer in child.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.enabled = true;
                }
            }
        }
    }

    void LockPiece()
    {
        AddToGridAndCheckGameOver();
        this.enabled = false;

        if (FindObjectOfType<GameManager>().gameState == GameManager.GameState.GameOver)
        {
            return;
        }

        FindObjectOfType<GameManager>().CheckForLines();
        FindObjectOfType<GameManager>().SpawnNext();
    }

    void AddToGridAndCheckGameOver()
    {
        bool gameOver = false;
        foreach (Transform child in transform)
        {
            int roundedX = Mathf.RoundToInt(child.transform.position.x);
            int roundedY = Mathf.RoundToInt(child.transform.position.y);

            if (roundedY >= GameManager.height)
            {
                gameOver = true;
                child.gameObject.SetActive(false); // Hide the part of the block that is out of bounds
            }
            else
            {
                GameManager.grid[roundedX, roundedY] = child;
            }
        }

        if (gameOver)
        {
            FindObjectOfType<GameManager>().GameOver();
        }
    }

    bool IsValidMove()
    {
        foreach (Transform child in transform)
        {
            int roundedX = Mathf.RoundToInt(child.transform.position.x);
            int roundedY = Mathf.RoundToInt(child.transform.position.y);

            // 1. Check lateral and bottom boundaries
            if (roundedX < 0 || roundedX >= GameManager.width || roundedY < 0)
            {
                return false;
            }

            // 2. Check for collision with other blocks, but only if inside the grid
            if (roundedY < GameManager.height)
            {
                if (GameManager.grid[roundedX, roundedY] != null)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
