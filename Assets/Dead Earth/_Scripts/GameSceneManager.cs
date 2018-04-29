using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class is used as a database of objects
public class GameSceneManager : MonoBehaviour {

    private static GameSceneManager _instance = null;
    public static GameSceneManager instance
    {
        get
        {   // Ensure class is a singleton
            if (_instance==null)
            {
                _instance = FindObjectOfType<GameSceneManager>();
            }
            return _instance;
        }
    }

    private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();
    
    // Stores the passed state machine in the dictionary using its unique id.
    public void RegisterAIStateMachine(int key, AIStateMachine stateMachine)
    {
        if (!_stateMachines.ContainsKey(key))
        {
            _stateMachines[key] = stateMachine;
        }
    }

    // return statemachine for the supplied uid if registered. 
    public AIStateMachine GetAIStateMachine(int key)
    {
        AIStateMachine machine = null;
        if (_stateMachines.TryGetValue(key, out machine))
        {
            return machine;
        }
        return null;
    }

}
