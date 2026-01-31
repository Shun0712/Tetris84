using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { MainMenu, Playing, GameOver }
    public enum GameDifficulty { Easy, Hard }
    public GameState gameState;
    public GameDifficulty selectedDifficulty;

    public static int width = 10;
    public static int height = 20;
    public static float currentFallTime = 0.8f;

    public static Transform[,] grid = new Transform[width, height];

    public GameObject[] tetrominoPrefabs;
    public GameObject borderBlockPrefab; // Variable for the border block

    private int score = 0;
    private TextMesh scoreTextMesh;
    private GameObject mainMenuHolder;
    private GameObject nextQueueHolder;
    private Queue<GameObject> tetrominoQueue = new Queue<GameObject>();

    private float baseFallTime = 0.8f;
    private int speedLevel = 0;

    void Start()
    {
        if (borderBlockPrefab == null)
        {
            Debug.LogError("Border Block Prefab is not assigned in the GameManager inspector!");
            return; // Stop execution to prevent further errors
        }
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
        {
            Debug.LogError("Tetromino Prefabs are not assigned in the GameManager inspector!");
            return; // Stop execution to prevent further errors
        }

        Camera.main.backgroundColor = Color.black;
        gameState = GameState.MainMenu;
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        mainMenuHolder = new GameObject("MainMenu");

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(mainMenuHolder.transform);
        titleGo.transform.position = new Vector3(width / 2, height / 2 + 4, 0);
        TextMesh titleTm = titleGo.AddComponent<TextMesh>();
        titleTm.text = "Tetris84";
        titleTm.fontSize = 400; // Increased font size for clarity
        titleTm.characterSize = 0.1f; // Use characterSize to scale
        titleTm.anchor = TextAnchor.MiddleCenter;
        titleTm.alignment = TextAlignment.Center;
        titleTm.color = Color.white;

        // Easy Button
        GameObject easyButtonGo = new GameObject("EasyButton");
        easyButtonGo.transform.SetParent(mainMenuHolder.transform);
        easyButtonGo.transform.position = new Vector3(width / 2, height / 2, 0);
        TextMesh easyButtonTm = easyButtonGo.AddComponent<TextMesh>();
        easyButtonTm.text = "EASY";
        easyButtonTm.fontSize = 300; // Increased font size for clarity
        easyButtonTm.characterSize = 0.1f; // Use characterSize to scale
        easyButtonTm.anchor = TextAnchor.MiddleCenter;
        easyButtonTm.alignment = TextAlignment.Center;
        easyButtonTm.color = Color.cyan;
        BoxCollider easyBc = easyButtonGo.AddComponent<BoxCollider>();
        easyBc.size = new Vector3(7, 2, 1); // Reverted to original size

        // Hard Button
        GameObject hardButtonGo = new GameObject("HardButton");
        hardButtonGo.transform.SetParent(mainMenuHolder.transform);
        hardButtonGo.transform.position = new Vector3(width / 2, height / 2 - 3, 0);
        TextMesh hardButtonTm = hardButtonGo.AddComponent<TextMesh>();
        hardButtonTm.text = "HARD";
        hardButtonTm.fontSize = 300; // Increased font size for clarity
        hardButtonTm.characterSize = 0.1f; // Use characterSize to scale
        hardButtonTm.anchor = TextAnchor.MiddleCenter;
        hardButtonTm.alignment = TextAlignment.Center;
        hardButtonTm.color = Color.red;
        BoxCollider hardBc = hardButtonGo.AddComponent<BoxCollider>();
        hardBc.size = new Vector3(7, 2, 1); // Reverted to original size
    }

    void StartGame(GameDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Set game background color

        // Clear the grid and destroy old blocks
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }

        // Destroy old game elements if they exist
        GameObject oldBorder = GameObject.Find("Border");
        if (oldBorder) Destroy(oldBorder);
        GameObject oldScore = GameObject.Find("ScoreText");
        if (oldScore) Destroy(oldScore);
        if (nextQueueHolder) Destroy(nextQueueHolder);
        
        // Destroy game over screen elements if they exist
        GameObject oldGameOverShadow = GameObject.Find("GameOverText_Shadow");
        if (oldGameOverShadow) Destroy(oldGameOverShadow);
        GameObject oldGameOverFG = GameObject.Find("GameOverText_FG");
        if (oldGameOverFG) Destroy(oldGameOverFG);
        GameObject oldRestartButton = GameObject.Find("RestartButton");
        if (oldRestartButton) Destroy(oldRestartButton);


        // Draw new game elements
        DrawBorder();

        // Create Score Display
        GameObject scoreGo = new GameObject("ScoreText");
        scoreGo.transform.position = new Vector3(-10f, 1f, 0); // Adjusted position further left
        scoreTextMesh = scoreGo.AddComponent<TextMesh>();
        scoreTextMesh.fontSize = 200; // Increased font size for clarity
        scoreTextMesh.characterSize = 0.1f; // Use characterSize to scale
        scoreTextMesh.anchor = TextAnchor.LowerLeft;

        // Reset Score and Speed
        score = 0;
        speedLevel = 0;
        currentFallTime = baseFallTime;
        UpdateScoreDisplay();

        // --- Game Mode Specific Setup ---
        tetrominoQueue.Clear();
        if (selectedDifficulty == GameDifficulty.Easy)
        {
            // Pre-populate queue for Easy mode
            currentFallTime = 1.2f; // Slower speed for easy mode
            for(int i = 0; i < 3; i++)
            {
                tetrominoQueue.Enqueue(tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Length)]);
            }
        }

        gameState = GameState.Playing;
        if(mainMenuHolder) Destroy(mainMenuHolder);
        SpawnNext();

        if(selectedDifficulty == GameDifficulty.Easy)
        {
            UpdateNextQueueDisplay();
        }
    }

    void Update()
    {
        if (gameState == GameState.MainMenu && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.name == "EasyButton")
                {
                    StartGame(GameDifficulty.Easy);
                }
                else if (hit.collider.name == "HardButton")
                {
                    StartGame(GameDifficulty.Hard);
                }
            }
        }
        else if (gameState == GameState.GameOver && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.name == "RestartButton")
                {
                    // Restart with the same difficulty
                    StartGame(selectedDifficulty);
                }
            }
        }
    }
    
    void DrawBorder()
    {
        GameObject borderHolder = new GameObject("Border");
        // Floor
        for (int x = -1; x <= width; x++)
        {
            Instantiate(borderBlockPrefab, new Vector3(x, -1, 0), Quaternion.identity, borderHolder.transform);
        }

        // Left Wall
        for (int y = 0; y < height; y++)
        {
            Instantiate(borderBlockPrefab, new Vector3(-1, y, 0), Quaternion.identity, borderHolder.transform);
        }

        // Right Wall
        for (int y = 0; y < height; y++)
        {
            Instantiate(borderBlockPrefab, new Vector3(width, y, 0), Quaternion.identity, borderHolder.transform);
        }

        // Top Wall
        for (int x = -1; x <= width; x++)
        {
            Instantiate(borderBlockPrefab, new Vector3(x, height, 0), Quaternion.identity, borderHolder.transform);
        }
    }

    void UpdateNextQueueDisplay()
    {
        if (nextQueueHolder != null)
        {
            Destroy(nextQueueHolder);
        }

        nextQueueHolder = new GameObject("NextQueue");
        nextQueueHolder.transform.position = new Vector3(width + 3, height - 2, 0);

        // Display the next 2 pieces
        GameObject[] nextPieces = tetrominoQueue.ToArray();
        for (int i = 0; i < 2 && i < nextPieces.Length; i++)
        {
            GameObject piece = Instantiate(nextPieces[i], nextQueueHolder.transform);
            piece.transform.localPosition = new Vector3(0, -i * 4, 0);
            piece.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            // Disable the script on the preview pieces
            if(piece.GetComponent<Tetromino>() != null)
            {
                piece.GetComponent<Tetromino>().enabled = false;
            }
        }
    }

    public void SpawnNext()
    {
        if (gameState != GameState.Playing)
        {
            return;
        }

        GameObject nextTetrominoPrefab;

        if (selectedDifficulty == GameDifficulty.Easy)
        {
            // Dequeue the next piece and enqueue a new one
            nextTetrominoPrefab = tetrominoQueue.Dequeue();
            tetrominoQueue.Enqueue(tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Length)]);
            UpdateNextQueueDisplay();
        }
        else // Hard mode
        {
            // Original random spawning
            nextTetrominoPrefab = tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Length)];
        }

        // Spawn the chosen tetromino
        Instantiate(nextTetrominoPrefab,
                    new Vector3(width / 2, height + 2, 0),
                    Quaternion.identity);
    }

    public void GameOver()
    {
        gameState = GameState.GameOver;
        Debug.Log("GAME OVER");
        Camera.main.backgroundColor = Color.black; // Reset background to black on game over;

        // Clean up next queue display on game over
        if (nextQueueHolder) Destroy(nextQueueHolder);

        // --- Create a background/shadow text to make it look bolder ---
        GameObject shadowGo = new GameObject("GameOverText_Shadow");
        shadowGo.transform.position = new Vector3(width / 2 + 0.05f, height / 2 - 0.05f, -0.9f); // Offset and slightly behind
        TextMesh shadowTm = shadowGo.AddComponent<TextMesh>();
        shadowTm.text = "GAME OVER";
        shadowTm.fontSize = 240; // Increased font size
        shadowTm.characterSize = 0.1f; // Use characterSize to scale
        shadowTm.color = Color.black; // Shadow color
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.alignment = TextAlignment.Center;
        
        // --- Create the main foreground text ---
        GameObject go = new GameObject("GameOverText_FG");
        go.transform.position = new Vector3(width / 2, height / 2, -1); // In front of shadow
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = "GAME OVER";
        tm.fontSize = 240; // Increased font size
        tm.characterSize = 0.1f; // Use characterSize to scale
        tm.color = new Color(0.85f, 0.2f, 0.2f); // A softer, less saturated red
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        
        // --- Create Restart Button ---
        GameObject restartButtonGo = new GameObject("RestartButton");
        restartButtonGo.transform.position = new Vector3(width / 2, height / 2 - 3, -1);
        TextMesh restartButtonTm = restartButtonGo.AddComponent<TextMesh>();
        restartButtonTm.text = "Restart";
        restartButtonTm.fontSize = 300; // Increased font size
        restartButtonTm.characterSize = 0.1f; // Use characterSize to scale
        restartButtonTm.anchor = TextAnchor.MiddleCenter;
        restartButtonTm.alignment = TextAlignment.Center;
        restartButtonTm.color = Color.cyan;
        BoxCollider bc = restartButtonGo.AddComponent<BoxCollider>();
        bc.size = new Vector3(5, 2, 1); // Reverted to original size
    }

    public void CheckForLines()
    {
        // Find all full lines
        List<int> fullLines = new List<int>();
        for (int y = 0; y < height; y++)
        {
            if (IsLineFull(y))
            {
                fullLines.Add(y);
            }
        }

        // If any lines were cleared
        if (fullLines.Count > 0)
        {
            AddScore(fullLines.Count);
            UpdateScoreDisplay();

            // Delete the lines
            foreach (int y in fullLines)
            {
                DeleteLine(y);
            }

            // Collapse the grid
            for (int i = fullLines.Count - 1; i >= 0; i--)
            {
                MoveLinesDown(fullLines[i]);
            }
        }
    }

    void UpdateScoreDisplay()
    {
        scoreTextMesh.text = "Score:\n" + score;
    }

    void AddScore(int lineCount)
    {
        switch (lineCount)
        {
            case 1:
                score += 100;
                break;
            case 2:
                score += 300;
                break;
            case 3:
                score += 500;
                break;
            case 4:
                score += 800;
                break;
        }

        if (selectedDifficulty == GameDifficulty.Hard)
        {
            int newSpeedLevel = score / 1000;
            if (newSpeedLevel > speedLevel)
            {
                speedLevel = newSpeedLevel;
                UpdateSpeed();
            }
        }
    }

    void UpdateSpeed()
    {
        currentFallTime = baseFallTime / Mathf.Pow(1.5f, speedLevel);
        Debug.Log("New Fall Time: " + currentFallTime);
    }

    bool IsLineFull(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] == null)
            {
                return false; // Not full
            }
        }
        return true; // Full line
    }

    void DeleteLine(int y)
    {
        for (int x = 0; x < width; x++)
        {
            Destroy(grid[x, y].gameObject);
            grid[x, y] = null;
        }
    }

    void MoveLinesDown(int y)
    {
        for (int i = y; i < height; i++) // From the cleared line upwards
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, i] != null)
                {
                    grid[x, i - 1] = grid[x, i]; // Move reference in grid
                    grid[x, i - 1].position += new Vector3(0, -1, 0); // Move actual game object
                    grid[x, i] = null; // Clear old position
                }
            }
        }
    }
}