using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead };
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio };

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

    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();

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

    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
    }

    protected virtual void Start ()
    {
        // Add all AI States for this object to the dictionary
        AIState[] states = GetComponents<AIState>();
        foreach (AIState state in states)
        {
            if (state && !_states.ContainsKey(state.GetStateType()))
            {
                _states[state.GetStateType()] = state;
            }
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

}
