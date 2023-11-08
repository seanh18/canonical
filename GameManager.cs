using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public struct OpenSpace{
    public OpenSpace(int i, int j) {
        index = i;
        next = j;
    }
    public int index;
    public int next;
}
    
public class GameManager : MonoBehaviour
{
    private GameObject player;
    public GameObject[] startBlocks;
    public GameObject[] deepBlocks;
    public GameObject[] gameOverMenu; //0-Endless 1-LevelLoss 2-LevelWin
    public GameObject[] backgrounds;
    public GameObject[] gemPrefabs;
    public GameObject[] powerUpPrefabs;
    public GameObject startMenu;
    public GameObject cloudPoof;
    public GameObject cloud;
    public GameObject lightObject;
    public GameObject heart;
    public Queue<GameObject> clumps;
    private Light lighting;
    private LevelManager levelManager;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI[] lootTexts;
    private Quaternion blockRot;
    public float generateSize = 50f;
    public float endlessSpeed;
    public float scoreIncr;
    public float speed;
    private int startHeight = 10;
    private int depthCnt = 0;
    public int score;
    public bool gameOver;
    public bool endless;
    private List<Vector3> closedBlock;
    public Queue<Queue<int>> spawnRows;
    public PlayerTraits traits;

    void Start() {
        //instantiates variables
        spawnRows = new Queue<Queue<int>>();
        clumps = new Queue<GameObject>();
        blockRot = startBlocks[0].gameObject.transform.rotation;
        levelManager = gameObject.GetComponent<LevelManager>();
        lighting = lightObject.GetComponent<Light>();
        
        Input.multiTouchEnabled = false;
        Application.targetFrameRate = 60;

        //initializes spawn vector list
        closedBlock = new List<Vector3>();
        for(int i = 0; i < 7; ++i) {
            closedBlock.Add(new Vector3(i - 3, -5, 0.5f));
        }
        levelManager.StartLevelManager(closedBlock.Count);
    }
    
    // StartGame is called after one of the start buttons is clicked
    public void StartGame() {
        //remove start UI and swap background
        startMenu.SetActive(false);
        cloud.gameObject.SetActive(false);
        cloudPoof.gameObject.SetActive(true);
        heart.gameObject.SetActive(true);

        // load in playerTraits
        endlessSpeed = traits.endlessSpeed;

        //other game start variables
        score = 0;
        gameOver = false;
        player = GameObject.FindWithTag("Player");
        player.GetComponent<PlayerController>().gameActive = true;
        scoreIncr = 1 / speed;
        SpawnStart();

        if (endless) {
            scoreText.gameObject.SetActive(true);
            StartCoroutine(UpdateScore());
            StartCoroutine(IncrementSpeed());
        }

        SpawnObstacle();
        StartCoroutine(ChangeColor());
    }

    public void LevelSelect(int selection, string name) {
        switch(selection) {
            //Endless
            case 1:
                levelManager.GenerateList(2000);
                endless = true;
                break;
            //Levels , temporarily to generate
            case 2:
                levelManager.GenerateLevels(500);
                break;
            //levels to play
            case 3:
                levelManager.LoadLevel(name);
                endless = false;
                break;
        }
    }
    //Spawns the first 10 rows
    void SpawnStart() {
        int blockIndex = 0;
        int wallXPos = 4;
        GameObject block = startBlocks[blockIndex];

        GameObject spawnClump = new GameObject();
        spawnClump.transform.position = Vector3.up * -15;
        spawnClump.tag = "Moves";

        //for the first 10 rows
        for(int j = 0; j < startHeight; ++j) {
            Queue<int> currRow = spawnRows.Dequeue();

            block = startBlocks[j];
            
            int rowCount = currRow.Count;
            //instantiates row
            for(int k = 0; k < rowCount; ++k) {
                Instantiate(block, closedBlock[currRow.Dequeue()] 
                        + new Vector3(0, -j, 0), blockRot, spawnClump.transform);
            }

            //instantiates side walls, closedBlock[3] has xPos = 0
            Instantiate(startBlocks[j + 10], closedBlock[3] + new Vector3(wallXPos, -j, 0), 
                    blockRot, spawnClump.transform);
            Instantiate(startBlocks[j + 10], closedBlock[3] + new Vector3(-wallXPos, -j, 0), 
                    blockRot, spawnClump.transform);
        }
        clumps.Enqueue(spawnClump);
    }
    //spawns all but the first 10 rows
    public void SpawnObstacle() {
        int blockIndex = 0;
        int wallXPos = 4;
        float depth = depthCnt * generateSize;
        GameObject block = deepBlocks[blockIndex];

        GameObject spawnClump = new GameObject(); 
        spawnClump.tag = "Moves";
        
        //for the next min {generateSize (endless) or spawnRows.count rows ()
        float spawnCount = endless ? generateSize > spawnRows.Count ? spawnRows.Count : generateSize : spawnRows.Count;

        spawnClump.transform.position = Vector3.down * (depth + spawnCount + 15);

        for(int j = 0; j < spawnCount; ++j) {
            Queue<int> currRow = spawnRows.Dequeue();

            //spawns powerups & gems
            if (traits.powerUpSpawnRate > 0 && j % traits.powerUpSpawnRate == 0) {
                int[] powerUpInfo = SpawnPowerUp(currRow);
                Instantiate(powerUpPrefabs[powerUpInfo[1]], closedBlock[powerUpInfo[0]] + new Vector3
                        (0, -(j + startHeight + depth), 0), powerUpPrefabs[0].transform.rotation, spawnClump.transform);
            } else if (j % 5 == 0) {
                int[] gemInfo = SpawnGem(currRow, j);
                Instantiate(gemPrefabs[gemInfo[1]], closedBlock[gemInfo[0]] + new Vector3
                        (0, -(j + startHeight + depth), 0), gemPrefabs[0].transform.rotation, spawnClump.transform);
            }

            //spawns backgrounds
            if (j % 30 == 0) {
                Instantiate(backgrounds[0], backgrounds[0].transform.position - (Vector3.up * (j + depth)), 
                        backgrounds[0].transform.rotation, spawnClump.transform);
                Instantiate(backgrounds[1], backgrounds[1].transform.position - (Vector3.up * (j + depth)), 
                        backgrounds[1].transform.rotation, spawnClump.transform);
                Instantiate(backgrounds[2], backgrounds[2].transform.position - (Vector3.up * (j + depth)), 
                        backgrounds[2].transform.rotation, spawnClump.transform);
            }
        
            int rowCount = currRow.Count;
            //instantiates row
            for(int k = 0; k < rowCount; ++k) {
                block = deepBlocks[UnityEngine.Random.Range(0, 20)];
                Instantiate(block, closedBlock[currRow.Dequeue()] 
                        + new Vector3(0, -(j + startHeight + depth), -(0.5f - block.transform.localScale.z/2)), blockRot, spawnClump.transform);
            }

            //instantiates side walls, closedBlock[3] has xPos = 0
            Instantiate(deepBlocks[20], closedBlock[3] + new Vector3(wallXPos, -(j + startHeight + depth), 0),
                     blockRot, spawnClump.transform);
            Instantiate(deepBlocks[20], closedBlock[3] + new Vector3(-wallXPos, -(j + startHeight + depth), 0), blockRot,
                     spawnClump.transform);  

            
        }
        depthCnt++;

        //Spawn end of level
        if (!endless) {
            //side walls
            int height = 25;
            for(int i = 0; i < height; ++i) {
                Instantiate(block, closedBlock[3] + new Vector3(wallXPos, 
                        -(spawnCount + i + startHeight), 0),blockRot, spawnClump.transform);
                Instantiate(block, closedBlock[3] + new Vector3(-wallXPos,
                        -(spawnCount + i + startHeight), 0), blockRot, spawnClump.transform); 
            }
            //floor
            for (int j = 1; j <= 16; ++ j) {
                for(int i = 0; i < closedBlock.Count; ++i) {
                    Instantiate(block, closedBlock[i] + new Vector3(0, -(spawnCount + height / 2.5f + startHeight + j - 2),
                        0), blockRot, spawnClump.transform);
                }    
            }
            
            //water
            for(int i = 0; i < closedBlock.Count; ++i) {
                Instantiate(deepBlocks[21], closedBlock[i] + new Vector3(0, -(spawnCount + height / 2.5f + startHeight - 3.625f),
                 0), blockRot, spawnClump.transform);
                Instantiate(deepBlocks[22], closedBlock[i] + new Vector3(0, -(spawnCount + height / 2.5f + startHeight - 3),
                 0), blockRot, spawnClump.transform);
                Instantiate(deepBlocks[22], closedBlock[i] + new Vector3(0, -(spawnCount + height / 2.5f + startHeight - 2),
                 0), blockRot, spawnClump.transform);
            }
        }
        clumps.Enqueue(spawnClump);
    }

    public IEnumerator IncrementSpeed(){
        float incrementDelay = 5.001f;
        float speedIncrement = 0.25f;
        speed = endlessSpeed - speedIncrement;

        while (!gameOver) {
            speed += speedIncrement;
            scoreIncr = 1 / speed;
            yield return new WaitForSeconds(incrementDelay);
        }
    }

    IEnumerator ChangeColor() {
        float startH = 44f;
        int dir = 1;
        while(!gameOver){
            if (startH >= 360 || startH <= 0) {
                dir *= -1;
            }

            startH += dir;
            lighting.color = Color.HSVToRGB(startH/360f, 30/100f, 1);
            
            yield return new WaitForSeconds(0.25f);
        }
    }

    //updates score and generates lists if needed, it doesnt need checking every frame
    IEnumerator UpdateScore(){
        while(!gameOver && endless) {
            score++;
            scoreText.text = "Score\n" + score;

            //every ~2000 blocks spawned, the game will generate more to the list
            if(endless && (score + 500) % 2000 == 0) {
                levelManager.GenerateList(2000);
            }

            //generates new obstacles
            if (clumps != null && clumps.Count > 0 && clumps.Peek().transform.position.y - player.transform.position.y > 10) {
                SpawnObstacle();
                Destroy(clumps.Dequeue());
            }

            yield return new WaitForSeconds(scoreIncr);
        }
    }
    // Spawns a gem, random location in the row and value randomized
    // but weighted based on difficulty
    int[] SpawnGem(Queue<int> row, int depth) {
        int[] gemInfo = new int[] {0,0};
        depth += depthCnt * (int)generateSize;

        // gets a list with the indices of open spaces
        List<int> list = new List<int>();
        for(int i = 0; i < 7; ++i) {
            list.Add(i);
        }
        while(row.Count > 0) {
            list.Remove(row.Dequeue());
        }

        // random open space index
        gemInfo[0] = list[UnityEngine.Random.Range(0, list.Count)];

        // weighted random gem value
        if (endless){
            // use depth
            int gem = UnityEngine.Random.Range(0, 4000);
            if (gem < 4000 - depth * 5) {
                //green gem
                gemInfo[1] = 0;
            } else if (gem < 4000 - depth){
                //blue gem
                gemInfo[1] = 1;
            } else {
                //red gem
                gemInfo[1] = 2;
            }
        } else {
            // levels - use speed
            int gem = UnityEngine.Random.Range(0, 100);
            if (gem < 100 - speed * 5) {
                //green gem
                gemInfo[1] = 0;
            } else if (gem < 100 - speed){
                //blue gem
                gemInfo[1] = 1;
            } else {
                //red gem
                gemInfo[1] = 2;
            }
        }
        
        return gemInfo;
    }

     int[] SpawnPowerUp(Queue<int> row) {
        int[] powerUpInfo = new int[] {0,0}; // { where to spawn, what to spawn}

        // gets a list with the indices of open spaces
        List<int> list = new List<int>();
        for(int i = 0; i < 7; ++i) {
            list.Add(i);
        }
        while(row.Count > 0) {
            list.Remove(row.Dequeue());
        }

        // random open space index
        powerUpInfo[0] = list[UnityEngine.Random.Range(0, list.Count)];

        int powerUp = UnityEngine.Random.Range(0,20);
        //powerUpInfo[1] = 6;
        if (powerUp < traits.powerUpRates[0]) {
            //bomb
            powerUpInfo[1] = 0;
        } else if (powerUp < traits.powerUpRates[1]) {
            //slow
            powerUpInfo[1] = 1;
        } else if (powerUp < traits.powerUpRates[2]) {
            //flip
            powerUpInfo[1] = 2;
        } else if (powerUp < traits.powerUpRates[3]) {
            //double
            powerUpInfo[1] = 3;
        } else if (powerUp < traits.powerUpRates[4]) {
            //minimize
            powerUpInfo[1] = 4;
        } else if (powerUp < traits.powerUpRates[5]) {
            //mystery box
            powerUpInfo[1] = 5;
        } else {
            // Heart
            powerUpInfo[1] = 6;
        }
        
        return powerUpInfo;
    }

    // Called after game ends, handles and manages relevant information
    public void EndGame(int menu, int gems) {
        gameOver = true;
        StopAllCoroutines();
        gameOverMenu[menu].gameObject.SetActive(true);
        scoreText.gameObject.SetActive(false);
        heart.gameObject.SetActive(false);

        //stat update
        gameObject.GetComponent<ProgressManager>().currProgress.totalScore += score;
        
        if (endless) {
            int highScore = gameObject.GetComponent<ProgressManager>().currProgress.highScore;
            if (score > highScore) {
                highScore = score;
                gameObject.GetComponent<ProgressManager>().currProgress.highScore = score;
                gameObject.GetComponent<ProgressManager>().Save();
            }
            highScoreText.text = "Score: " + score + "\nHigh Score: " + highScore;
        }
        for(int i = 0; i < 3; ++i) {
            lootTexts[i].text = "+" + gems + " Gems";
        }
    }
    
    //Resets the objects in the game
    private void ResetGame(){
        gameOver = false;
        depthCnt = 0;
        spawnRows.Clear();
        clumps.Clear();

        //resets player
        GameObject player =  GameObject.FindWithTag("Player");
        player.transform.position = new Vector3(0, 10, 0.5f);
        player.GetComponent<PlayerController>().dirtSplat.gameObject.SetActive(false);
        player.GetComponent<PlayerController>().explosion.gameObject.SetActive(false);
        GameObject.FindWithTag("MainCamera").transform.position = new Vector3(0, 6, 25f);


        //deletes old blocks
        GameObject[] oldBlocks = GameObject.FindGameObjectsWithTag("Moves");
        for(int i = 0; i < oldBlocks.Length; ++i) {
            Destroy(oldBlocks[i]);
        }

        //resets lighting
        lighting.color = Color.HSVToRGB(44/360f, .3f, 1); 
        
        gameOverMenu[0].SetActive(false);
        gameOverMenu[1].SetActive(false);
        gameOverMenu[2].SetActive(false);
    }

    //After the player finishes a game or dies, this option resets the game to the main menu
    public void MainMenu() {
        ResetGame();
        
        GameObject.FindWithTag("Player").GetComponent<PlayerController>().resetAnim("Idle2");
        cloud.SetActive(true);
        startMenu.SetActive(true);
    }

    //After the player finishes a game or dies, this option resets the game to play again
    public void returnGame() {
        ResetGame();

        //if endless, else levels
        if (endless) {
            LevelSelect(1, null);
            speed = endlessSpeed;
        } else {
            LevelSelect(3, gameObject.GetComponent<LevelManager>().lastLevel);
        }
        
        GameObject.FindWithTag("Player").GetComponent<PlayerController>().resetAnim("Falling");
        StartGame();
    }

    //if the player wins the level, this function allows them to play the next one
    public void NextLevel() {
        ResetGame();
        string prev = gameObject.GetComponent<LevelManager>().lastLevel;
        string next = ProgressManager.IndexToLevel(ProgressManager.LevelToIndex(prev) + 1);
        LevelSelect(3, next);

        GameObject.FindWithTag("Player").GetComponent<PlayerController>().resetAnim("Falling");
        StartGame();
    }
}