using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QLearningAgent
{
    // q-table holds each state:action pair with a q-value 
    private Dictionary<StateActionPair, float> q_table = new Dictionary<StateActionPair, float>();

    // hyperparameters 
    public float alpha = 0.1f;     // learning rate (0=ignore new info, 1=only use new info)
    public float gamma = 0.9f;     // discount factor (0=only immediate reward, 1=long-term)
    public float epsilon = 0.2f;   // 20% explore, 80% exploit (can be tweaked later)

    // action space (simplified for now)
    private List<string> actions = new List<string> { "UP", "DOWN", "LEFT", "RIGHT" };



    // get a value from the q-table 
    private float get_qvalue(State state, string action)
    {
        // returns 0 if that pair doesn't exist, otherwise returns its q-value
        StateActionPair key = new StateActionPair(state, action);
        return q_table.ContainsKey(key) ? q_table[key] : 0.1f;
    }

    // set a value in the q-table 
    private void set_qvalue(State state, string action, float value)
    {
        StateActionPair key = new StateActionPair(state, action);
        q_table[key] = value;
    }

    // performs the qlearning update based on the current transition (our big formula)
    public void update_qtable_via_qLearning_update(State currentState, string currentAction, float reward, State nextState)
    {
        // get the maximum q value for all possible next actions instead of getting the one from the action 
        // we actually took (that is what sarsa does)
        float currentStateQ = get_qvalue(currentState, currentAction);
        float maxNextStateQ = float.MinValue;
        foreach (var a in actions)
        {
            float q = get_qvalue(nextState, a);
            if (q > maxNextStateQ) maxNextStateQ = q;
        }

        float newCurrentStateQ = currentStateQ + alpha * (reward + gamma * maxNextStateQ - currentStateQ);

        set_qvalue(currentState, currentAction, newCurrentStateQ);
    }


    // will choose the next action based on the e-greedy policy 
    public string choose_action(State state)
    {
        float rand = UnityEngine.Random.value;
        // Debug.Log($"random number generated: {rand}");

        // exploration - pick a random action 
        if (rand < epsilon)
        {
            // Debug.Log($"    ....exploring");
            int rand_action_index = UnityEngine.Random.Range(0, actions.Count);
            return actions[rand_action_index];
        }

        // exploitation - find the best action according to the q-table
        // Debug.Log($"    ....exploiting");
        string best_action = actions[0];
        float best_qvalue = get_qvalue(state, best_action);

        foreach (var action in actions)
        {
            float new_qvalue = get_qvalue(state, action);
            if (new_qvalue > best_qvalue)
            {
                best_qvalue = new_qvalue;
                best_action = action;
            }
        }

        return best_action;

    }

    public string GetBestAction(State state)
    {
        string bestAction = actions[0];
        float bestQ = get_qvalue(state, bestAction);

        foreach (var action in actions)
        {
            float q = get_qvalue(state, action);
            if (q > bestQ)
            {
                bestQ = q;
                bestAction = action;
            }
        }

        return bestAction;
    }


    // used in testing the update via sarsa function 
    public float get_qvalue_debugging(State state, string action)
    {
        return get_qvalue(state, action);
    }




    public void SavePolicy(float avgReward, float catchRate, int totalEpisodes)
    {
        #if UNITY_EDITOR
        var data = new AgentPolicyData
        {
            alpha = alpha,
            gamma = gamma,
            epsilon = epsilon,
            averageReward = avgReward,
            catchRate = catchRate,
            totalEpisodes = totalEpisodes,
        };

        foreach (var kvp in q_table)
        {
            data.qEntries.Add(new SerializedQEntry
            {
                enemyX = kvp.Key.state.enemy_pos.x,
                enemyY = kvp.Key.state.enemy_pos.y,
                playerX = kvp.Key.state.player_pos.x,
                playerY = kvp.Key.state.player_pos.y,
                action = kvp.Key.action,
                value = kvp.Value
            });
        }
        // Write to Resources folder (relative to Unity project)
        string path = Application.dataPath + "/Resources/Policies/qLearning_policy.json";
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"✅ Policy saved to {path}. Will be bundled into build.");
        #else
            Debug.LogWarning("This method should only be used in the Unity Editor to save policies.");
        #endif
    }

    public void LoadPolicyFromResources(string resourcePath, out float avgReward, out float catchRate, out int episodes)
    {
        avgReward = catchRate = 0f;
        episodes = 0;

        // Load from Resources/Policies/<filename>.json WITHOUT the .json extension
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath); // e.g., "Policies/sarsa_policy"
        if (jsonAsset == null)
        {
            Debug.LogError($"❌ Policy not found at /Resources/Policies/{resourcePath}.json");
            return;
        }

        var data = JsonUtility.FromJson<AgentPolicyData>(jsonAsset.text);
        alpha = data.alpha;
        gamma = data.gamma;
        epsilon = data.epsilon;
        avgReward = data.averageReward;
        catchRate = data.catchRate;
        episodes = data.totalEpisodes;

        q_table.Clear();
        foreach (var entry in data.qEntries)
        {
            var state = new State(new Vector2Int(entry.enemyX, entry.enemyY), new Vector2Int(entry.playerX, entry.playerY));
            q_table[new StateActionPair(state, entry.action)] = entry.value;
        }

        Debug.Log($"✅ Loaded policy: {resourcePath} | Episodes: {episodes}, Catch Rate: {catchRate:P0}");
    }




}
