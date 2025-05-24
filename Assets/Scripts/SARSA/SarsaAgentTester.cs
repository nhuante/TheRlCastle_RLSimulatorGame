using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

[Serializable]
public class AgentPolicyData
{
    public List<SerializedQEntry> qEntries = new List<SerializedQEntry>();
    public float alpha, gamma, epsilon;
    public float averageReward, catchRate;
    public int totalEpisodes;
}

[Serializable]
public class SerializedQEntry
{
    public int enemyX, enemyY, playerX, playerY;
    public string action;
    public float value;
}


public enum RLAlgorithm
{
    SARSA,
    QLearning
}


public enum AgentPlayMode
{
    UntrainedSARSA,
    UntrainedQLearning,
    TrainedSARSA,
    TrainedQLearning
}


public class SarsaAgentTester : MonoBehaviour
{


    // PUBLIC VARIABLES
    [Header("RL Algorithm Chosen")]
    // public enum RLAgorithm { SARSA, QLearning }
    private RLAlgorithm currentAlgorithm = RLAlgorithm.SARSA;
    public Canvas gridHiderCanvas;

    [Header("Character Objects")]
    public GameObject enemy_object;
    // private Transform enemy_transform;
    public GameObject player_object;

    [Header("Speed and Debug Logs")]
    public float move_interval;      // time between steps
    public float episode_interval;   // time between episodes 
    public bool logs_within_episodes;

    // [Header("Training Settings")]
    // public int total_episodes;
    // public Vector2Int enemy_start_position;
    // public Vector2Int player_start_position;

    [Header("Training UI Components - Right Panel")]
    // public TMP_InputField playerXInput;
    // public TMP_InputField playerYInput;

    public TMP_Text Epsiode_Text;
    public TMP_Text Step_Text;
    public TMP_Text CatchRatio_Text;
    public Button toggleHeatmapButton;
    public TMP_InputField finalPlayerXInput;
    public TMP_InputField finalPlayerYInput;
    public Button updateFinalPolicyButton;
    public TMP_Text updateFinalPolicyButtonText; // drag in the button's child text component
    public TMP_Text stepRewardText;
    public TMP_Text averageRewardText;

    [Header("Training UI Components - Left Panel")]
    public Button sarsaButton;
    public Button qLearningButton;
    public TMP_Text sarsaButtonText;
    public TMP_Text qLearningButtonText;
    public Slider alphaSlider;
    public Slider gammaSlider;
    public Slider moveIntervalSlider;
    public Slider episodeIntervalSlider;
    public Slider epsilonSlider;
    public Slider episodeCountSlider;

    // text displays for real-time feedback
    public TMP_Text alphaValueText;
    public TMP_Text gammaValueText;
    public TMP_Text moveIntervalText;
    public TMP_Text episodeIntervalText;
    public TMP_Text epsilonValueText;
    public TMP_Text episodeCountText;


    [Header("Training UI Components - Bottom Panel")]
    public Button startTrainingButton;
    public TMP_Text startTrainingText;
    public Button pauseTrainingButton;
    public TMP_Text pauseTrainingText;

    public GameObject arrowPrefab;

    [Header("Canvas UI Components")]
    public Canvas trainingCanvas;
    public Canvas playingCanvas;
    public TitleScreenController titleScreen;

    [Header("Playing UI Components")]
    public TMP_Text policyStatsText;
    public Button SarsaUntrained_Button;
    public Button SarsaTrained_Button;
    public Button QLearningUntrained_Button;
    public Button QLearningTrained_Button;
    public TMP_Text turnTrackerText;
    public TMP_Text outcomeText;
    public TMP_Text startPlayButtonText;


    private enum TurnState { PlayerTurn, AgentTurn }
    private TurnState turnState;
    private bool inPlaySession = false;
    private Vector2Int playerPos, enemyPos;


    private AgentPlayMode currentPlayMode = AgentPlayMode.UntrainedSARSA;
    


    // PRIVATE VARIABLES
    private SarsaAgent agent;
    private QLearningAgent qLearningAgent;

    private const int GRID_WIDTH = 5;
    private const int GRID_HEIGHT = 5;
    private GameObject[,] policyArrows = new GameObject[GRID_WIDTH, GRID_HEIGHT];
    private bool heatmapVisible = false;
    private bool trainingComplete = false;
    private int numTimesPlayerCaught = 0;
    private Coroutine trainingCoroutine = null;
    private bool isPaused = false;
    private float cumulativeReward = 0f;
    private float averageReward = 0f;
    private int episodeCount = 0;
    private int[] episodeOptions = { 50, 100, 500, 1000, 2000 };
    private int total_episodes = 100;


    void Start()
    {
        Debug.Log(Application.persistentDataPath);
        // listen for button clicks
        toggleHeatmapButton.onClick.AddListener(ToggleHeatmap);
        updateFinalPolicyButton.onClick.AddListener(ShowFinalPolicyMap);
        startTrainingButton.onClick.AddListener(StartTraining);
        pauseTrainingButton.onClick.AddListener(PauseOrResumeTraining);
        sarsaButton.onClick.AddListener(() => SelectAlgorithm(RLAlgorithm.SARSA));
        qLearningButton.onClick.AddListener(() => SelectAlgorithm(RLAlgorithm.QLearning));

        ApplyAlgorithmUI(); // ensures correct startup state

        updateFinalPolicyButton.interactable = false;
        updateFinalPolicyButtonText.color = Color.gray;

        pauseTrainingButton.interactable = false;

    }

    void Update()
    {
        // slider text values to reflect the position of the slider
        alphaValueText.text = "Alpha (Learning Rate) = " + alphaSlider.value.ToString("F2");
        gammaValueText.text = "Gamma (Discount Factor) =" + gammaSlider.value.ToString("F2");
        moveIntervalText.text = "Time Between Steps = " + moveIntervalSlider.value.ToString("F2") + "s";
        episodeIntervalText.text = "Time Between Episodes = " + episodeIntervalSlider.value.ToString("F2") + "s";
        epsilonValueText.text = "Epsilon Value = " + epsilonSlider.value.ToString("F2");
        int index = Mathf.RoundToInt(episodeCountSlider.value);
        total_episodes = episodeOptions[index];
        episodeCountText.text = $"Episodes: {total_episodes}";

        // Show correct canvas based on game mode
        if (titleScreen.gameMode == "train")
        {
            trainingCanvas.enabled = true;
            playingCanvas.enabled = false;
        }
        else if (titleScreen.gameMode == "play") // handle player input for moving 
        {
            trainingCanvas.enabled = false;
            playingCanvas.enabled = true;
            if ((turnState == TurnState.PlayerTurn) && (inPlaySession))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) TryMovePlayer(Vector2Int.up);
                if (Input.GetKeyDown(KeyCode.DownArrow)) TryMovePlayer(Vector2Int.down);
                if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMovePlayer(Vector2Int.left);
                if (Input.GetKeyDown(KeyCode.RightArrow)) TryMovePlayer(Vector2Int.right);
            }
        }
    }
    Color green = new Color(68 / 255f, 161 / 255f, 40 / 255f);
    public void PlayUntrainedSARSA()
    {
        currentPlayMode = AgentPlayMode.UntrainedSARSA;

        agent = new SarsaAgent();
        qLearningAgent = null;

        SarsaTrained_Button.GetComponent<Image>().color = Color.grey;
        SarsaUntrained_Button.GetComponent<Image>().color = green;
        QLearningTrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningUntrained_Button.GetComponent<Image>().color = Color.grey; 
        policyStatsText.text = $"Blob will follow SARSA, but without any training. AKA he moves pretty randomly!";

    }
    public void PlayUntrainedQLearning()
    {
        currentPlayMode = AgentPlayMode.UntrainedQLearning;

        qLearningAgent = new QLearningAgent();
        agent = null;

        SarsaTrained_Button.GetComponent<Image>().color = Color.grey;
        SarsaUntrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningTrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningUntrained_Button.GetComponent<Image>().color = green; 
        policyStatsText.text = $"Blob will follow Q-Learning, but without any training. AKA he moves pretty randomly!";

    }

    public void PlayTrainedSARSA()
    {
        currentPlayMode = AgentPlayMode.TrainedSARSA;
        float avg, catchRate;
        int episodes;

        qLearningAgent = null;

        agent = new SarsaAgent();
        agent.LoadPolicyFromResources("Policies/sarsa_policy", out avg, out catchRate, out episodes);
        policyStatsText.text = $"SARSA (Trained)\n" +
                                $"Episodes: {episodes}\n" +
                                $"Catch Rate: {catchRate:P0}\n" +
                                $"Avg Reward: {avg:F2}\n" +
                                $"Alpha: {agent.alpha:F2}\n" +
                                $"Gamma: {agent.gamma:F2}\n" +
                                $"Epsilon: {agent.epsilon:F2}";
        SarsaTrained_Button.GetComponent<Image>().color = green;
        SarsaUntrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningTrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningUntrained_Button.GetComponent<Image>().color = Color.grey;
    }
    public void PlayTrainedQLearning()
    {
        currentPlayMode = AgentPlayMode.TrainedQLearning;
        float avg, catchRate;
        int episodes;

        agent = null;

        qLearningAgent = new QLearningAgent();
        qLearningAgent.LoadPolicyFromResources("Policies/qLearning_policy", out avg, out catchRate, out episodes);
        policyStatsText.text = $"Q-Learning (Trained)\n"  +
                                $"Episodes: {episodes}\n" +
                                $"Catch Rate: {catchRate:P0}\n" +
                                $"Avg Reward: {avg:F2}\n" +
                                $"Alpha: {qLearningAgent.alpha:F2}\n" +
                                $"Gamma: {qLearningAgent.gamma:F2}\n" +
                                $"Epsilon: {qLearningAgent.epsilon:F2}";
        SarsaTrained_Button.GetComponent<Image>().color = Color.grey;
        SarsaUntrained_Button.GetComponent<Image>().color = Color.grey;
        QLearningTrained_Button.GetComponent<Image>().color = green; 
        QLearningUntrained_Button.GetComponent<Image>().color = Color.grey; 
    }

    public void StartPlaySession()
    {
        Color green = new Color(33 / 255f, 96 / 255f, 14 / 255f);

        float avgReward, catchRate;
        int episodes;
        if (turnTrackerText != null)
        {
            turnTrackerText.text = "Your Turn (Use arrow keys to move once)";
            turnTrackerText.color = green;
        }
        if (outcomeText != null) outcomeText.text = "";

        switch (currentPlayMode)
        {
            case AgentPlayMode.UntrainedSARSA:
                agent = new SarsaAgent();
                break;

            case AgentPlayMode.UntrainedQLearning:
                qLearningAgent = new QLearningAgent();
                break;

            case AgentPlayMode.TrainedSARSA:
                agent = new SarsaAgent();
                agent.LoadPolicyFromResources("Policies/sarsa_policy", out avgReward, out catchRate, out episodes);
                break;

            case AgentPlayMode.TrainedQLearning:
                qLearningAgent = new QLearningAgent();
                qLearningAgent.LoadPolicyFromResources("Policies/qLearning_policy", out avgReward, out catchRate, out episodes);
                break;
        }

        playerPos = new Vector2Int(0, 0);  // Or wherever you want player to start
        enemyPos = new Vector2Int(GRID_WIDTH  - 1, GRID_HEIGHT - 1); // Enemy start

        player_object.transform.position = GridToWorld(playerPos);
        enemy_object.transform.position = GridToWorld(enemyPos);

        inPlaySession = true;
        startPlayButtonText.text = "reset game";
        turnState = TurnState.PlayerTurn;

    }

    private void TryMovePlayer(Vector2Int dir)
    {
        Color blue = new Color(0 / 255f, 106 / 255f, 182 / 255f);

        Vector2Int newPos = playerPos + dir;
        if (IsValid(newPos))
        {
            playerPos = newPos;
            player_object.transform.position = GridToWorld(playerPos);
            if (enemyPos == playerPos)
            {
                Debug.Log("Enemy caught the player!");
                inPlaySession = false;
                startPlayButtonText.text = "start playing";
                if (outcomeText != null)
                {
                    outcomeText.text = "Blob caught you!";
                    outcomeText.color = Color.red;
                }
            }
            turnState = TurnState.AgentTurn;
            if (turnTrackerText != null)
            {
                turnTrackerText.text = "Blob's Turn";
                turnTrackerText.color = blue;
            }
            StartCoroutine(AgentMove());

        }
    }

    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < GRID_WIDTH && pos.y >= 0 && pos.y < GRID_HEIGHT;
    }


    private IEnumerator AgentMove()
    {
        yield return new WaitForSeconds(1f);

        if (!inPlaySession) yield break; // in case game ended during wait

        // Clear previous arrow if needed
        GameObject existingArrow = GameObject.FindWithTag("Arrow");
        if (existingArrow) Destroy(existingArrow);

        State s = new State(enemyPos, playerPos);
        string action = currentPlayMode switch
        {
            AgentPlayMode.UntrainedSARSA => agent.choose_action(s),
            AgentPlayMode.TrainedSARSA => agent.choose_action(s),
            AgentPlayMode.UntrainedQLearning => qLearningAgent.choose_action(s),
            AgentPlayMode.TrainedQLearning => qLearningAgent.GetBestAction(s),
            _ => "UP"
        };

        Vector3 basePos = enemy_object.transform.position; 

        enemyPos = apply_action(enemyPos, action, out bool invalidMove);
        enemy_object.transform.position = GridToWorld(enemyPos);

        // Show direction arrow
        
        Vector3 arrowOffset = Vector3.zero;
        switch (action)
        {
            case "UP": arrowOffset = new Vector3(0, 1, 0); break;
            case "DOWN": arrowOffset = new Vector3(0, -1, 0); break;
            case "LEFT": arrowOffset = new Vector3(-1, 0, 0); break;
            case "RIGHT": arrowOffset = new Vector3(1, 0, 0); break;
        }
        Vector3 arrowPos = invalidMove
            ? basePos + arrowOffset * 0.6f // pushes arrow outside the grid
            : basePos + new Vector3(0, 0f, 0); // slight vertical offset for visibility

        Quaternion arrowRot = GetRotationForAction(action);

        GameObject arrow = Instantiate(arrowPrefab, arrowPos, arrowRot);
        arrow.tag = "Arrow";
        Destroy(arrow, 1.0f);

        if (enemyPos == playerPos)
        {
            Debug.Log("Enemy caught the player!");
            inPlaySession = false;
            startPlayButtonText.text = "start playing";
            if (outcomeText != null)
            {
                outcomeText.text = "Blob caught you!";
                outcomeText.color = Color.red;
            }
            yield break;
        }
        else
        {
            turnState = TurnState.PlayerTurn;
            if (turnTrackerText != null) turnTrackerText.text = "Your Turn\n(Use arrow keys to move one tile)";
        }
    }

    public void setCharacterPositions(string gameMode)
    {
        if (gameMode == "train")
        {
            enemy_object.transform.position = new Vector3(-2, 3, 0);
            player_object.transform.position = new Vector3(2, 3, 0);
        } else if (gameMode == "play")
        {
            enemy_object.transform.position = new Vector3(-2, 3, 0);
            player_object.transform.position = new Vector3(2, 3, 0);
        }
    }


    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void SelectAlgorithm(RLAlgorithm algo)
    {
        if (currentAlgorithm == algo) return;

        // If switching while training, stop it and reset everything
        if (trainingCoroutine != null)
        {
            StopCoroutine(trainingCoroutine);
            trainingCoroutine = null;
        }
        isPaused = false;
        trainingComplete = false;
        averageReward = 0f;
        episodeCount = 0;
        averageRewardText.text = "Avg Reward = 0";
        pauseTrainingButton.interactable = false;
        startTrainingButton.interactable = true;
        startTrainingText.color = Color.black;
        updateFinalPolicyButton.interactable = false;

        // Destroy heatmap arrows
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (policyArrows[x, y] != null)
                {
                    Destroy(policyArrows[x, y]);
                    policyArrows[x, y] = null;
                }
            }
        }

        currentAlgorithm = algo;
        ApplyAlgorithmUI();

        // You may want to clear/reset here if changing mid-training
    }

    private void ApplyAlgorithmUI()
    {
        // Button visual updates
        bool isSARSA = currentAlgorithm == RLAlgorithm.SARSA;

        sarsaButtonText.color = isSARSA ? Color.black : Color.gray;
        qLearningButtonText.color = !isSARSA ? Color.black : Color.gray;

        Color green = new Color(68 / 255f, 161 / 255f, 40 / 255f);
        sarsaButton.GetComponent<Image>().color = isSARSA ? green : Color.white;
        qLearningButton.GetComponent<Image>().color = !isSARSA ? green : Color.white;

        // if (currentAlgorithm == RLAlgorithm.SARSA)
        //     gridHiderCanvas.gameObject.SetActive(false);
        // else
        //     gridHiderCanvas.gameObject.SetActive(true);

        // Enable/disable sliders if needed (for future)
        // For example, if Q-learning doesn’t use epsilon the same way:
        // epsilonSlider.interactable = isSARSA; 
    }

    public void StartTraining()
    {
        // Reset tracking values
        episodeCount = 0;
        averageReward = 0f;
        averageRewardText.text = "Avg Rewards: 0";
        stepRewardText.text = "Curr Ep Rewards: 0";

        // Destroy previous policy arrows if visible
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (policyArrows[x, y] != null)
                {
                    Destroy(policyArrows[x, y]);
                    policyArrows[x, y] = null;
                }
            }
        }


        // Disable start button to prevent duplicates
        startTrainingButton.interactable = false;
        startTrainingText.color = Color.gray;

        pauseTrainingButton.interactable = true;
        pauseTrainingText.color = Color.black;

        if (currentAlgorithm == RLAlgorithm.SARSA)
        {
            // Create the SARSA agent
            agent = new SarsaAgent();
            agent.alpha = alphaSlider.value;
            agent.gamma = gammaSlider.value;
            agent.epsilon = epsilonSlider.value;
            // Debug.Log("new sarsa agent created")
        }
        else if (currentAlgorithm == RLAlgorithm.QLearning)
        {
            // Create the Q-Learning Agent
            qLearningAgent = new QLearningAgent();
            qLearningAgent.alpha = alphaSlider.value;
            qLearningAgent.gamma = gammaSlider.value;
            qLearningAgent.epsilon = epsilonSlider.value;
        }
        // get updated valued from sliders 
        move_interval = moveIntervalSlider.value;
        episode_interval = episodeIntervalSlider.value;

        // UI and control
        trainingComplete = false;

        startTrainingButton.interactable = false;
        startTrainingText.color = Color.gray;

        pauseTrainingButton.interactable = true;
        pauseTrainingText.color = Color.black;

        pauseTrainingButton.GetComponentInChildren<TMP_Text>().text = "Pause Training";

        isPaused = false;

        // Start training
        trainingCoroutine = StartCoroutine(train_and_move());
    }

    public void PauseOrResumeTraining()
    {
        isPaused = !isPaused;
        pauseTrainingButton.GetComponentInChildren<TMP_Text>().text = isPaused ? "Resume Training" : "Pause Training";
    }

    public void backToTitle()
    {
        // Stop training coroutine
        if (trainingCoroutine != null)
        {
            StopCoroutine(trainingCoroutine);
            trainingCoroutine = null;
        }

        // Reset training state
        trainingComplete = false;
        isPaused = false;
        averageReward = 0f;
        episodeCount = 0;
        numTimesPlayerCaught = 0;
        cumulativeReward = 0f;
        heatmapVisible = false;

        // Reset UI
        averageRewardText.text = "Avg Reward = 0";
        stepRewardText.text = "Curr Ep Rewards: 0";
        Epsiode_Text.text = "Episode 0";
        Step_Text.text = "Step 0";
        CatchRatio_Text.text = "Catch Rate 0";

        updateFinalPolicyButton.interactable = false;
        updateFinalPolicyButtonText.color = Color.gray;

        pauseTrainingButton.interactable = false;
        pauseTrainingText.color = Color.grey;

        startTrainingButton.interactable = true;
        startTrainingText.color = Color.black;
        

        // Reset playing mode state
        inPlaySession = false;
        turnState = TurnState.PlayerTurn;

        if (turnTrackerText != null) turnTrackerText.text = "";
        if (outcomeText != null) outcomeText.text = "";

        // Clear arrows if visible
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (policyArrows[x, y] != null)
                {
                    Destroy(policyArrows[x, y]);
                    policyArrows[x, y] = null;
                }
            }
        }

        // Hide agent direction arrow (if it exists)
        GameObject existingArrow = GameObject.FindWithTag("Arrow");
        if (existingArrow) Destroy(existingArrow);

        // Reset characters to default spot (optional)
        enemy_object.transform.position = GridToWorld(new Vector2Int(0, 0));
        player_object.transform.position = GridToWorld(new Vector2Int(4, 4));
    }


    // test for debugging 
    public void test_sarsa_update()
    {
        // two example states 
        // enemy at (1, 1), player at (4, 4)
        State state1 = new State(new Vector2Int(1, 1), new Vector2Int(4, 4));
        // enemy at (2, 1), player at (4, 4) aka moved right by one 
        State state2 = new State(new Vector2Int(2, 1), new Vector2Int(4, 4));

        // choose actions (normally from choose_action(), but we’ll hardcode for now)
        string action1 = "RIGHT";
        string action2 = "UP";

        // example reward 
        float reward = -0.1f;

        // log q-value before the update 
        Debug.Log($"Before update: Q({state1.enemy_pos}, {action1}) = {agent.get_qvalue_debugging(state1, action1)}");

        // apply the sarsa update 
        agent.update_qtable_via_sarsa_update(state1, action1, reward, state2, action2);

        // log q-value after the update 
        Debug.Log($"After update: Q({state1.enemy_pos}, {action1}) = {agent.get_qvalue_debugging(state1, action1)}");

    }

    // runs a single episode - used for testing, currently not being called 
    private string run_episode(int max_steps = 20)
    {
        // enemy starts at (0,0), player at (4,4)
        State current_state = new State(new Vector2Int(0, 0), new Vector2Int(4, 4));
        string current_action = agent.choose_action(current_state);

        // episode truncates at 20 steps 
        for (int step = 0; step < max_steps; step++)
        {
            // take the action 
            Vector2Int new_enemy_pos = apply_action(current_state.enemy_pos, current_action, out bool invalidMove);
            State next_state = new State(new_enemy_pos, current_state.player_pos);

            // check if we reached the player 
            bool caught_player = new_enemy_pos == current_state.player_pos;
            float reward = caught_player ? 1f : -0.1f;

            // choose the next best action for the next state
            string next_action = agent.choose_action(next_state);

            // update the q-table 
            agent.update_qtable_via_sarsa_update(current_state, current_action, reward, next_state, next_action);
            if (logs_within_episodes)
            {
                Debug.Log($"Step {step + 1}: Enemy at {new_enemy_pos}, action = {current_action}, reward = {reward}");
            }

            // if the agent (enemy) won, stop the episode 
            if (caught_player)
            {
                // Debug.Log("🎯 Enemy caught the player! Episode finished.");
                return $"Caught enemy at {step + 1} steps";
            }
            // otherwise, continue to the next step 
            current_state = next_state;
            current_action = next_action;
        }
        return $"{max_steps}";
    }



    // master coroutine 
    private IEnumerator train_and_move()
    {

        Epsiode_Text.text = $"Episode 0 / {total_episodes}";
        CatchRatio_Text.text = $"Catch Rate 0 / {total_episodes}";

        for (int episode = 0; episode < total_episodes; episode++)
        {
            // respect paused 
            while (isPaused)
            {
                yield return null;
            }

            // update training parameters 
            if (currentAlgorithm == RLAlgorithm.SARSA)
            {
                agent.alpha = alphaSlider.value;
                agent.gamma = gammaSlider.value;
                agent.epsilon = epsilonSlider.value;
            }
            else if (currentAlgorithm == RLAlgorithm.QLearning)
            {
                qLearningAgent.alpha = alphaSlider.value;
                qLearningAgent.gamma = gammaSlider.value;
                qLearningAgent.epsilon = epsilonSlider.value;
            }
            move_interval = moveIntervalSlider.value;
            episode_interval = episodeIntervalSlider.value;

            int steps_taken = 0;
            yield return StartCoroutine(run_episode_Coroutine((steps) => steps_taken = steps));

            Debug.Log($"-------- Episode {episode + 1}, Steps Taken {steps_taken} --------");
            Epsiode_Text.text = $"Episode {episode + 1} / {total_episodes}";
            CatchRatio_Text.text = $"Catch Rate {numTimesPlayerCaught} / {total_episodes}";

            yield return new WaitForSeconds(episodeIntervalSlider.value);
        }
        trainingComplete = true;
        updateFinalPolicyButton.interactable = true;
        updateFinalPolicyButtonText.color = Color.black;

        startTrainingButton.interactable = true;
        startTrainingText.color = Color.black;

        pauseTrainingButton.interactable = false;
        pauseTrainingText.color = Color.grey;

        if (currentAlgorithm == RLAlgorithm.SARSA)
        {
            float finalCatchRate = (float)numTimesPlayerCaught / total_episodes;
            agent.SavePolicy(averageReward, finalCatchRate, total_episodes);
            Debug.Log("Policy saved to: " + Application.persistentDataPath);

        }
        else if (currentAlgorithm == RLAlgorithm.QLearning)
        {
            float finalCatchRate = (float)numTimesPlayerCaught / total_episodes;
            qLearningAgent.SavePolicy(averageReward, finalCatchRate, total_episodes);
            Debug.Log("Policy saved to: " + Application.persistentDataPath);

        }

    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x - 2, gridPos.y - 2, 0);
    }


    // [coroutine version] runs a single episode - this one is being called
    private IEnumerator run_episode_Coroutine(System.Action<int> callback, int max_steps = 20)
    {

        // random start positions 
        Vector2Int playerPos = new Vector2Int(UnityEngine.Random.Range(0, GRID_WIDTH), UnityEngine.Random.Range(0, GRID_HEIGHT));
        Vector2Int enemyPos = new Vector2Int(UnityEngine.Random.Range(0, GRID_WIDTH), UnityEngine.Random.Range(0, GRID_HEIGHT));

        // prevent same start positions 
        while (enemyPos == playerPos)
            enemyPos = new Vector2Int(UnityEngine.Random.Range(0, GRID_WIDTH), UnityEngine.Random.Range(0, GRID_HEIGHT));


        //  initialize state and action 
        State current_state = new State(new Vector2Int(enemyPos[0], enemyPos[1]),
                                        new Vector2Int(playerPos[0], playerPos[1]));

        // string current_action = agent.choose_action(current_state);
        string current_action = (currentAlgorithm == RLAlgorithm.SARSA)
            ? agent.choose_action(current_state)
            : qLearningAgent.choose_action(current_state);

        int lastDistance = ManhattanDistance(enemyPos, playerPos);


        // place characters at starting positions
        enemy_object.transform.position = GridToWorld(new Vector2Int(current_state.enemy_pos[0], current_state.enemy_pos[1]));

        player_object.transform.position = GridToWorld(new Vector2Int(current_state.player_pos[0], current_state.player_pos[1]));

        cumulativeReward = 0f;

        // episode truncates at max steps 
        for (int step = 0; step < max_steps; step++)
        {
            while (isPaused)
                yield return null;
            move_interval = moveIntervalSlider.value;


            Step_Text.text = $"Step {step + 1} / {max_steps}";
            if (heatmapVisible)
            {
                UpdatePolicyMap(playerPos);
            }

            // take the action and get the next state
            Vector2Int new_enemy_pos = apply_action(current_state.enemy_pos, current_action, out bool invalidMove);
            State next_state = new State(new_enemy_pos, current_state.player_pos);

            // calculate reward 
            int newDistance = ManhattanDistance(new_enemy_pos, playerPos);
            float reward = -0.01f; // step cost

            if (new_enemy_pos == playerPos)
            {
                reward = 10f; // caught the player
                numTimesPlayerCaught += 1;
            }
            // else if (playerPos == targetGoal)
            // {
            //     reward = -1f; // player escaped
            // }
            else if (newDistance < lastDistance)
            {
                reward += 0.05f; // moved closer
            }
            else if (newDistance > lastDistance)
            {
                reward -= 0.05f; // moved away
            }
            if (invalidMove)
            {
                reward -= 0.2f;
            }


            string next_action = "";
            // update the q-table 
            if (currentAlgorithm == RLAlgorithm.SARSA && agent != null)
            {
                // choose the next best action for the next state
                next_action = agent.choose_action(next_state);
                agent.update_qtable_via_sarsa_update(current_state, current_action, reward, next_state, next_action);
            }
            else if (currentAlgorithm == RLAlgorithm.QLearning && qLearningAgent != null)
            {
                qLearningAgent.update_qtable_via_qLearning_update(current_state, current_action, reward, next_state);
            }
            if (logs_within_episodes)
            {
                Debug.Log($"Step {step + 1}: Enemy at {new_enemy_pos}, action = {current_action}, reward = {reward}");
            }

            // move to the next tiles 
            enemy_object.transform.position = GridToWorld(new_enemy_pos);
            cumulativeReward += reward;
            stepRewardText.text = $"Curr Ep Rewards: {cumulativeReward:F2}";

            // if the agent (enemy) won, stop the episode 
            if (playerPos == new_enemy_pos)
            {
                Debug.Log("Caught Player");
                callback(step + 1);
                episodeCount++;
                averageReward = ((averageReward * (episodeCount - 1)) + cumulativeReward) / episodeCount;
                averageRewardText.text = $"Avg Reward: {averageReward:F2}";
                yield break;
            }

            // otherwise, continue to the next step 
            current_state = next_state;
            if (currentAlgorithm == RLAlgorithm.SARSA)
            {
                current_action = next_action;
            }
            lastDistance = newDistance;
            enemyPos = new_enemy_pos;


            // time cushion 
            yield return new WaitForSeconds(move_interval);
        }
        episodeCount++;
        averageReward = ((averageReward * (episodeCount - 1)) + cumulativeReward) / episodeCount;
        averageRewardText.text = $"Avg Reward: {averageReward:F2}";

        Debug.Log("Episode Terminated");
        callback(max_steps);
    }

    // simulates the agent taking the action 
    private Vector2Int apply_action(Vector2Int position, string action, out bool invalidMove)
    {
        Vector2Int newPos = position;
        invalidMove = false;

        switch (action)
        {
            case "UP":
                newPos += Vector2Int.up;
                break;
            case "DOWN":
                newPos += Vector2Int.down;
                break;
            case "LEFT":
                newPos += Vector2Int.left;
                break;
            case "RIGHT":
                newPos += Vector2Int.right;
                break;
        }

        // check bounds
        if (newPos.x < 0 || newPos.x >= GRID_WIDTH || newPos.y < 0 || newPos.y >= GRID_HEIGHT)
        {
            invalidMove = true;
            return position; // stay on the same tile as before, no update 
        }

        return newPos;
    }

    private Quaternion GetRotationForAction(string action)
    {
        switch (action)
        {
            case "UP": return Quaternion.Euler(0, 0, 0);
            case "RIGHT": return Quaternion.Euler(0, 0, -90);
            case "DOWN": return Quaternion.Euler(0, 0, 180);
            case "LEFT": return Quaternion.Euler(0, 0, 90);
            default: return Quaternion.identity;
        }
    }

    public void UpdatePolicyMap(Vector2Int playerPos)
    {
        // Debug.Log("🧭 UpdatePolicyMap called");

        if (currentAlgorithm == RLAlgorithm.SARSA && agent == null) return;
        if (currentAlgorithm == RLAlgorithm.QLearning && qLearningAgent == null) return;

        // Clear existing arrows
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (policyArrows[x, y] != null)
                {
                    Destroy(policyArrows[x, y]);
                    policyArrows[x, y] = null;
                }

                State state = new State(new Vector2Int(x, y), playerPos);

                // string bestAction = agent.choose_action(state);
                string bestAction = currentAlgorithm == RLAlgorithm.SARSA
                    ? agent.choose_action(state)
                    : qLearningAgent.GetBestAction(state);

                if (bestAction == null)
                {
                    // Debug.LogWarning($"No best action found for state: Enemy({x},{y}), Player({playerPos.x},{playerPos.y})");
                    continue;
                }
                GameObject arrow = Instantiate(arrowPrefab, GridToWorld(new Vector2Int(x, y)), GetRotationForAction(bestAction));
                arrow.SetActive(heatmapVisible); // match toggle
                policyArrows[x, y] = arrow;
            }
        }
    }

    // public void OnUpdatePolicyMapPressed()
    // {
    //     // Basic error handling: default to 0 if input is empty
    //     int x = int.TryParse(playerXInput.text, out var parsedX) ? Mathf.Clamp(parsedX, 0, GRID_WIDTH - 1) : 0;
    //     int y = int.TryParse(playerYInput.text, out var parsedY) ? Mathf.Clamp(parsedY, 0, GRID_HEIGHT - 1) : 0;

    //     UpdatePolicyMap(new Vector2Int(x, y));
    // }

    public void ToggleHeatmap()
    {
        heatmapVisible = !heatmapVisible;
        toggleHeatmapButton.GetComponentInChildren<TMP_Text>().text = heatmapVisible ? "Hide Heatmap" : "Show Heatmap";

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (policyArrows[x, y] != null)
                {
                    policyArrows[x, y].SetActive(heatmapVisible);
                }
            }
        }
    }

    public void ShowFinalPolicyMap()
    {
        if (!trainingComplete)
        {
            Debug.LogWarning("Training is not yet complete!");
            return;
        }


        int x = int.TryParse(finalPlayerXInput.text, out var parsedX) ? Mathf.Clamp(parsedX, 0, GRID_WIDTH - 1) : 0;
        int y = int.TryParse(finalPlayerYInput.text, out var parsedY) ? Mathf.Clamp(parsedY, 0, GRID_HEIGHT - 1) : 0;
        player_object.transform.position = GridToWorld(new Vector2Int(x, y));
        enemy_object.transform.position = new Vector2(0, 3);

        Vector2Int fixedPlayerPos = new Vector2Int(x, y);
        UpdatePolicyMap(fixedPlayerPos);
    }


}