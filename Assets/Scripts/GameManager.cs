using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import TextMeshPro

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
    public GameObject borderBlockPrefab; 

    private int score = 0;
    private TextMeshPro scoreTextMesh; // Changed to TextMeshPro
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
            return;
        }
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
        {
            Debug.LogError("Tetromino Prefabs are not assigned in the GameManager inspector!");
            return;
        }

        Camera.main.backgroundColor = Color.black;
        gameState = GameState.MainMenu;
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        mainMenuHolder = new GameObject("MainMenu");

        // --- Font Asset Loading ---
        // TextMeshPro requires a Font Asset. We'll try to load a default one.
        // If you create your own Font Asset, you should load it here.
        // Example: TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts/MyCustomFont SDF");
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(mainMenuHolder.transform);
        titleGo.transform.position = new Vector3(width / 2, height / 2 + 4, 0);
        TextMeshPro titleTm = titleGo.AddComponent<TextMeshPro>(); // Use TextMeshPro
        titleTm.font = fontAsset; // Assign the font asset
        titleTm.text = "Tetris84";
        titleTm.fontSize = 10; // TMP uses a different scaling. Start with a reasonable size.
        titleTm.alignment = TextAlignmentOptions.Center; // Use TMP alignment
        titleTm.color = Color.white;
        titleTm.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 2); // Set rect size

        // Easy Button
        GameObject easyButtonGo = new GameObject("EasyButton");
        easyButtonGo.transform.SetParent(mainMenuHolder.transform);
        easyButtonGo.transform.position = new Vector3(width / 2, height / 2, 0);
        TextMeshPro easyButtonTm = easyButtonGo.AddComponent<TextMeshPro>(); // Use TextMeshPro
        easyButtonTm.font = fontAsset; // Assign the font asset
        easyButtonTm.text = "EASY";
        easyButtonTm.fontSize = 8;
        easyButtonTm.alignment = TextAlignmentOptions.Center; // Use TMP alignment
        easyButtonTm.color = Color.cyan;
        easyButtonTm.GetComponent<RectTransform>().sizeDelta = new Vector2(7, 2); // Set rect size
        BoxCollider easyBc = easyButtonGo.AddComponent<BoxCollider>();
        easyBc.size = new Vector3(7, 2, 1);

        // Hard Button
        GameObject hardButtonGo = new GameObject("HardButton");
        hardButtonGo.transform.SetParent(mainMenuHolder.transform);
        hardButtonGo.transform.position = new Vector3(width / 2, height / 2 - 3, 0);
        TextMeshPro hardButtonTm = hardButtonGo.AddComponent<TextMeshPro>(); // Use TextMeshPro
        hardButtonTm.font = fontAsset; // Assign the font asset
        hardButtonTm.text = "HARD";
        hardButtonTm.fontSize = 8;
        hardButtonTm.alignment = TextAlignmentOptions.Center; // Use TMP alignment
        hardButtonTm.color = Color.red;
        hardButtonTm.GetComponent<RectTransform>().sizeDelta = new Vector2(7, 2); // Set rect size
        BoxCollider hardBc = hardButtonGo.AddComponent<BoxCollider>();
        hardBc.size = new Vector3(7, 2, 1);
    }

    void StartGame(GameDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);

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

        GameObject oldBorder = GameObject.Find("Border");
        if (oldBorder) Destroy(oldBorder);
        GameObject oldScore = GameObject.Find("ScoreText");
        if (oldScore) Destroy(oldScore);
        if (nextQueueHolder) Destroy(nextQueueHolder);
        GameObject oldGameOverShadow = GameObject.Find("GameOverText_Shadow");
        if (oldGameOverShadow) Destroy(oldGameOverShadow);
        GameObject oldGameOverFG = GameObject.Find("GameOverText_FG");
        if (oldGameOverFG) Destroy(oldGameOverFG);
        GameObject oldRestartButton = GameObject.Find("RestartButton");
        if (oldRestartButton) Destroy(oldRestartButton);

        DrawBorder();
        
        // --- Font Asset Loading --- (Can be done once in Start)
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Create Score Display
        GameObject scoreGo = new GameObject("ScoreText");
        scoreGo.transform.position = new Vector3(-10f, 1f, 0);
        scoreTextMesh = scoreGo.AddComponent<TextMeshPro>(); // Use TextMeshPro
        scoreTextMesh.font = fontAsset; // Assign font
        scoreTextMesh.fontSize = 5;
        scoreTextMesh.alignment = TextAlignmentOptions.BottomLeft; // Use TMP alignment
        scoreTextMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 5); // Set rect size

        score = 0;
        speedLevel = 0;
        currentFallTime = baseFallTime;
        UpdateScoreDisplay();

        tetrominoQueue.Clear();
        if (selectedDifficulty == GameDifficulty.Easy)
        {
            currentFallTime = 1.2f;
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
                    StartGame(selectedDifficulty);
                }
            }
        }
    }
    
    void DrawBorder()
    {
        GameObject borderHolder = new GameObject("Border");
        for (int x = -1; x <= width; x++)
        {
            Instantiate(borderBlockPrefab, new Vector3(x, -1, 0), Quaternion.identity, borderHolder.transform);
        }
        for (int y = 0; y < height; y++)
        {
            Instantiate(borderBlockPrefab, new Vector3(-1, y, 0), Quaternion.identity, borderHolder.transform);
        }
        for (int y = 0; y < height; y++)
        {
            Instantiate(borderBlockPrefab, new Vector3(width, y, 0), Quaternion.identity, borderHolder.transform);
        }
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
        GameObject[] nextPieces = tetrominoQueue.ToArray();
        for (int i = 0; i < 2 && i < nextPieces.Length; i++)
        {
            GameObject piece = Instantiate(nextPieces[i], nextQueueHolder.transform);
            piece.transform.localPosition = new Vector3(0, -i * 4, 0);
            piece.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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
            nextTetrominoPrefab = tetrominoQueue.Dequeue();
            tetrominoQueue.Enqueue(tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Length)]);
            UpdateNextQueueDisplay();
        }
        else
        {
            nextTetrominoPrefab = tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Length)];
        }
        Instantiate(nextTetrominoPrefab,
                    new Vector3(width / 2, height + 2, 0),
                    Quaternion.identity);
    }

    public void GameOver()
    {
        gameState = GameState.GameOver;
        Debug.Log("GAME OVER");
        Camera.main.backgroundColor = Color.black;

        if (nextQueueHolder) Destroy(nextQueueHolder);
        
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // --- Shadow Text ---
        GameObject shadowGo = new GameObject("GameOverText_Shadow");
        shadowGo.transform.position = new Vector3(width / 2 + 0.05f, height / 2 - 0.05f, -0.9f);
        TextMeshPro shadowTm = shadowGo.AddComponent<TextMeshPro>();
        shadowTm.font = fontAsset;
        shadowTm.text = "GAME OVER";
        shadowTm.fontSize = 8;
        shadowTm.color = Color.black;
        shadowTm.alignment = TextAlignmentOptions.Center;
        shadowTm.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 2);

        // --- Foreground Text ---
        GameObject go = new GameObject("GameOverText_FG");
        go.transform.position = new Vector3(width / 2, height / 2, -1);
        TextMeshPro tm = go.AddComponent<TextMeshPro>();
        tm.font = fontAsset;
        tm.text = "GAME OVER";
        tm.fontSize = 8;
        tm.color = new Color(0.85f, 0.2f, 0.2f);
        tm.alignment = TextAlignmentOptions.Center;
        tm.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 2);

        // --- Restart Button ---
        GameObject restartButtonGo = new GameObject("RestartButton");
        restartButtonGo.transform.position = new Vector3(width / 2, height / 2 - 3, -1);
        TextMeshPro restartButtonTm = restartButtonGo.AddComponent<TextMeshPro>();
        restartButtonTm.font = fontAsset;
        restartButtonTm.text = "Restart";
        restartButtonTm.fontSize = 8;
        restartButtonTm.color = Color.cyan;
        restartButtonTm.alignment = TextAlignmentOptions.Center;
        restartButtonTm.GetComponent<RectTransform>().sizeDelta = new Vector2(7, 2);
        BoxCollider bc = restartButtonGo.AddComponent<BoxCollider>();
        bc.size = new Vector3(7, 2, 1);
    }

    public void CheckForLines()
    {
        List<int> fullLines = new List<int>();
        for (int y = 0; y < height; y++)
        {
            if (IsLineFull(y))
            {
                fullLines.Add(y);
            }
        }
        if (fullLines.Count > 0)
        {
            AddScore(fullLines.Count);
            UpdateScoreDisplay();
            foreach (int y in fullLines)
            {
                DeleteLine(y);
            }
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
            case 1: score += 100; break;
            case 2: score += 300; break;
            case 3: score += 500; break;
            case 4: score += 800; break;
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
                return false;
            }
        }
        return true;
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
        for (int i = y; i < height; i++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, i] != null)
                {
                    grid[x, i - 1] = grid[x, i];
                    grid[x, i - 1].position += new Vector3(0, -1, 0);
                    grid[x, i] = null;
                }
            }
        }
    }
}