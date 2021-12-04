using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class MarioPlayerController : MonoBehaviour
{
    [Header("Animator Atributes")]
    [Range(0,1)]
    [SerializeField] private float _animSpeed;
    private bool _grounded;

    private Animator _animator;
    private CharacterController _controller;

    [Header("Rotation")]
    private Vector3 _currRotation;
    private Vector3 _rSmoothV;
    [SerializeField] private float _rSmoothTime;

    [Header("Jump")]
    [SerializeField] private KeyCode _jumpKey;
    private float _vSpeed = 0;
    private float _startingGravity;
    [SerializeField] private float _height;
    [SerializeField] private float _timeToMaxHeight;
    private float _gravity;
    private float _jumpSpeed;
    [SerializeField] private float _jumpCooldown;
    private float _currentJumpTime;
    private int _nJump;

    [Header("Movement")]
    [SerializeField] private Camera _cam;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _runSpeed;
    [SerializeField] float smoothMoveTime = 0.5f;
    private Vector3 _speed;
    private Vector3 smoothV;
    private int _specialIdleTime = 5;
    private float _currentSpecialIdleTime;
    private bool _resetGame;

    [Header("Punch")]
    [SerializeField] private KeyCode _punchKey;
    [SerializeField] private float _punchCooldown;
    private float _currentPunchTime;
    private int _nPunch;


    void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _startingGravity = -2 * _height / (_timeToMaxHeight * _timeToMaxHeight);
        _gravity = _startingGravity;
        _jumpSpeed = 2 * _height / _timeToMaxHeight;
        _nJump = 0;
        _currentSpecialIdleTime = _specialIdleTime;
        _currentJumpTime = 0;
    }

    private void Update()
    {

        _animator.SetFloat("Speed", _animSpeed);
        _currentJumpTime -= Time.deltaTime;
        _currentSpecialIdleTime -= Time.deltaTime;
        _currentPunchTime -= Time.deltaTime;

        if (Input.GetKeyDown(_jumpKey) && _grounded)
        {
            Jump();
            _currentSpecialIdleTime = _specialIdleTime;
        }
        if (Input.GetKeyDown(_punchKey) && !Input.GetKey(KeyCode.LeftControl))
        {
            Punch();
            _currentSpecialIdleTime = _specialIdleTime;
        }
        
    }

    void FixedUpdate()
    {
        _currRotation = new Vector3(transform.forward.x, 0, transform.forward.z);
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input != Vector2.zero)
            _currentSpecialIdleTime = _specialIdleTime;
        Vector3 inputDir = new Vector3(input.x, 0, input.y).normalized;
        Vector3 worldInputDir = _cam.transform.TransformDirection(inputDir).normalized;

        Vector3 _lookTargetDirection = new Vector3(worldInputDir.x, 0, worldInputDir.z).normalized;
        _currRotation = Vector3.SmoothDamp(_currRotation, _lookTargetDirection, ref _rSmoothV, _rSmoothTime);
        if (_currRotation != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_currRotation, Vector3.up);
        }

        float currentSpeed = (Input.GetKey(KeyCode.LeftShift)) ? _runSpeed : _moveSpeed;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            currentSpeed *= 0.5f;
            _animator.SetBool("Crouch",true);
        } else
        {
            _animator.SetBool("Crouch", false);
        }
        Vector3 targetVelocity = worldInputDir * currentSpeed;
        _speed = Vector3.SmoothDamp(_speed, targetVelocity, ref smoothV, smoothMoveTime);

        _vSpeed += _gravity * Time.deltaTime;
        if (_currentSpecialIdleTime < 0)
        {
            _animSpeed = -1;
        } else
        {
            _animSpeed = new Vector2(_speed.x, _speed.z).magnitude;
        }
        _speed = new Vector3(_speed.x, _vSpeed, _speed.z);
        var cf = _controller.Move(_speed * Time.deltaTime);
        bool onGround = (cf & CollisionFlags.Below) != 0;
        bool onContactCeiling = (cf & CollisionFlags.Above) != 0;
        if (onGround)
        {
            _grounded = true;
            _vSpeed = -1.0f;
        } 
        else
        {
            _grounded = false;
            if ((cf & CollisionFlags.Above) != 0 && _vSpeed > 0)
            {
                _vSpeed = 0;
            }
        }
        if (onContactCeiling && _vSpeed > 0.0f) _vSpeed = 0.0f;
        if (_vSpeed < 0.0f)
        {
            _gravity = _startingGravity * 2f;
        }

        _animator.SetBool("Grounded",_grounded);

    }

    private void Jump()
    {
        if (_currentJumpTime > 0 && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            _nJump++;
            if (_nJump == 1)
            {
                _vSpeed = _jumpSpeed * 1.05f;
                _gravity = _startingGravity;
                _animator.SetTrigger("SecondJump");
                _currentJumpTime = _jumpCooldown;
            } else if (_nJump == 2)
            {
                _vSpeed = _jumpSpeed * 1.15f;
                _gravity = _startingGravity;
                _animator.SetTrigger("TripleJump");
                _currentJumpTime = -1;
                _nJump = 0;
            }
        } else if (Input.GetKey(KeyCode.LeftControl)) 
        {
            _nJump = 0;
            _vSpeed = _jumpSpeed;
            _gravity = _startingGravity;
            _animator.SetTrigger("FirstJump");
        } else
        {
            _nJump = 0;
            _vSpeed = _jumpSpeed;
            _gravity = _startingGravity;
            _animator.SetTrigger("FirstJump");
            _currentJumpTime = _jumpCooldown;
        }
        
    }

    private void Punch()
    {
        if(_currentPunchTime > 0)
        {
            _nPunch++; 
            if (_nPunch == 1)
            {
                _animator.SetTrigger("punch_double");
                _currentPunchTime = _punchCooldown;
            } else if (_nPunch == 2)
            {
                _animator.SetTrigger("kick_triple"); 
                _currentPunchTime = -1;
                _nPunch = 0; 
            }
        } else 
        {
            _nPunch = 0;
            _animator.SetTrigger("punch_single");
            _currentPunchTime = _punchCooldown;
        }
    }

    private CheckPoint _currentCheckPoint;
    public void setCheckPoint(CheckPoint checkpoint)
    {
        _currentCheckPoint = checkpoint;
    }

    public void RestartGame()
    {
        //ResetGame
        _resetGame = true;
    }

    private void LateUpdate()
    {
        if (_resetGame)
        {
            Transform tr = _currentCheckPoint.getCheckPointPos();
            transform.position = tr.position;
            transform.rotation = tr.rotation;
            _resetGame = false;
        }
    }
}
