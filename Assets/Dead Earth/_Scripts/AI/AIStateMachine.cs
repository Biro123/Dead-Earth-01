using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead };
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio };
public enum AITriggerEventType { Enter, Stay, Exit};

// ----------------------------------------------------------------------
// Class: AITarget
// Desc:  Describes a potential traget to the AI system
// ----------------------------------------------------------------------
public struct AITarget
{
    private AITargetType _type;
    private Collider _collider;
    private Vector3 _position;
    private float _distanceFromPlayer;
    private float _timeLastPinged;

    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float time { get { return _timeLastPinged; } }

    public float distance {
        get { return _distanceFromPlayer; }
        set { _distanceFromPlayer = value; }
    }

    public void Set(AITargetType type, Collider collider, Vector3 position, float distance)
    {
        _type = type;
        _collider = collider;
        _position = position;
        _distanceFromPlayer = distance;
        _timeLastPinged = Time.time;
    }

    public void Clear()
    {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _distanceFromPlayer = Mathf.Infinity;
        _timeLastPinged = 0.0f;
    }
}

// ----------------------------------------------------------------------
// Class: AIStateMachine
// Desc:  Base class for all AI State Machines
// ----------------------------------------------------------------------
public abstract class AIStateMachine : MonoBehaviour {

    public AITarget VisualThreat = new AITarget();
    public AITarget AudioThreat = new AITarget();

    protected AIState _currentState = null;
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;

    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;  
    [SerializeField] protected SphereCollider _targetTrigger = null;
    [SerializeField] protected SphereCollider _sensorTrigger = null;

    [SerializeField] [Range(0, 15)] protected float _stoppingDistance = 1.0f; 

    // Component Cache
    protected Animator      _animator = null;
    protected NavMeshAgent  _navAgent = null;
    protected Collider      _collider = null;
    protected Transform     _transform = null;

    // Public Properties
    public Animator     animator { get { return _animator; } }
    public NavMeshAgent navAgent { get { return _navAgent; } }
    public Vector3 sensorPosition
    {
        get
        {
            if (!_sensorTrigger) return Vector3.zero;
            // Converts the local postion into a world position accounting for scale of parents
            Vector3 worldPosition = _sensorTrigger.transform.position;
            worldPosition.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            worldPosition.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            worldPosition.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return worldPosition;
        }
    }
    public float sensorRadius
    {
        get
        {
            if (!_sensorTrigger) return 0.0f;
            float radius = Mathf.Max(
                _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x,
                _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y
                );
            return Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }

    public bool useRootPosition { get { return _rootPositionRefCount > 0; } }
    public bool useRootRotaion { get { return _rootRotationRefCount > 0; } }

    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();

        if (GameSceneManager.instance)
        {
            // Register State Machine's colliders with scene database
            if (_collider)
            {
                GameSceneManager.instance.RegisterAIStateMachine(_collider.GetInstanceID(), this);
            }
            if(_sensorTrigger)
            {
                GameSceneManager.instance.RegisterAIStateMachine(_sensorTrigger.GetInstanceID(), this);
            }
        }
    }

    protected virtual void Start ()
    {
        if(_sensorTrigger != null)
        {
            AISensor sensor = _sensorTrigger.GetComponent<AISensor>(); 
            if(sensor)
            {
                sensor.parentStateMachine = this;
            }
        }

        // Add all AI States for this object to the dictionary
        // and tell each state what the stateMachine is (this)
        AIState[] states = GetComponents<AIState>();
        foreach (AIState aiState in states)
        {
            if (aiState && !_states.ContainsKey(aiState.GetStateType()))
            {
                _states[aiState.GetStateType()] = aiState;
                aiState.SetStateMachine(this);
            }
        }

        // If the state (script object) is stored in the dictionary for the current state type
        // retrieve it.
        if (_states.ContainsKey(_currentStateType))
        {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState();
        } else
        {
            _currentState = null;
        }
    }

    public void SetTarget(AITargetType type, Collider collider, Vector3 position, float distance)
    {
        _target.Set(type, collider, position, distance);

        if (_targetTrigger)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITargetType type, Collider collider, Vector3 position, float distance, float stoppingDistance)
    {
        _target.Set(type, collider, position, distance);

        if (_targetTrigger)
        {
            _targetTrigger.radius = stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITarget target)
    {
        _target = target;

        if (_targetTrigger)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void ClearTarget()
    {
        _target.Clear();
        if(_targetTrigger)
        {
            _targetTrigger.enabled = false;
        }
    }

    // Called each tick of the Physics system
    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear();
        AudioThreat.Clear();

        if(_target.type != AITargetType.None)
        {
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }

    }

    protected virtual void Update()
    {
        if (!_currentState) { return; }
        
        // Execute the OnUpdate of the current state
        AIStateType newStateType = _currentState.OnUpdate();

        // On change of state, tell the old one to exit and start the new.
        if (newStateType != _currentStateType)
        {
            AIState newState = null;
            // If new state in dictionary, return its type
            if (_states.TryGetValue(newStateType, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            else // if state doesn't exist/isn't built, fall back to Idle 
            if (_states.TryGetValue(AIStateType.Idle, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            _currentStateType = newStateType;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // enusre it's our own target trigger
        if (_targetTrigger == null || other != _targetTrigger) { return; }

        if(_currentState)        
            _currentState.OnDestinationReached(true); // notify child state        
    }
    protected virtual void OnTriggerExit(Collider other)
    {
        // enusre it's our own target trigger
        if (_targetTrigger == null || other != _targetTrigger) { return; }

        if (_currentState)        
            _currentState.OnDestinationReached(false); // notify child state        
    }

    // Called by the sensor script attached to the sensor object 
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        if (_currentState)
            _currentState.OnTriggerEvent(eventType, other);
    }

    protected virtual void OnAnimatorMove()
    {
        if (_currentState)
            _currentState.OnAnimatorUpdated();
    }
    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState)
            _currentState.OnAnimatorIKUppdated();
    }

    public void NavAgentControl(bool positionUpdate, bool rotationUpdate)
    {
        if(_navAgent)
        {
            _navAgent.updatePosition = positionUpdate;
            _navAgent.updateRotation = rotationUpdate;
        }
    }

    // Called by the state machine behaviours to enable/disable root motion
    public void AddRootMotionRequest(int rootPosition, int rootRotation)
    {
        _rootPositionRefCount += rootPosition;
        _rootRotationRefCount += rootRotation;
    }
}
