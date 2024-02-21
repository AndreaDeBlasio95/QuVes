using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MainCharacterController : MonoBehaviour
{
    // MOVEMENTS INPUT
    [Header("MOVEMENTS INPUT")]
    private DefaultPlayerActions _defaultPlayerActions;

    private InputAction _moveAction;
    private InputAction _lookAction;


    // PLAYER STATS MANAGER
    [Header("PLAYER STATS MANAGER")]

    [SerializeField]
    private bool _canMove;

    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private Vector2 _moveDir;

    [SerializeField]
    private Vector3 _vel;

    [SerializeField]
    private float _jumpForce = 5f;

    [SerializeField]
    private float _initSpeed = 15f;

    [SerializeField]
    private float _currentSpeed = 15f;

    [SerializeField]
    private float _acceleration = 5f;

    [SerializeField]
    private float _maxSpeed = 30f;

    // CUSTOM MOVEMENTS
    [Header("CUSTOM MOVEMENTS")]

    [SerializeField]
    private bool _dump;

    // ANIMATIONS
    [Header("ANIMATIONS")]

    [SerializeField]
    private bool _isRunning;

    [SerializeField]
    private bool _isDumping;

    [SerializeField]
    private Animator _animator;

    // CAMERA
    [Header("AIM")]
    [SerializeField]
    private bool _aim;

    [SerializeField]
    private GameObject moveCamera;

    [SerializeField]
    private GameObject aimCamera;

    [SerializeField]
    private GameObject aimTargetObj;

    // HOOK
    [Header("HOOK")]
    [SerializeField]
    private bool _hook;

    [SerializeField]
    private HookController hookController;

    [SerializeField]
    private Vector3? _hookTargetPosition;

    [SerializeField]
    private float _hookMaxSpeed;

    [SerializeField]
    private bool _isHooked;

    [SerializeField]
    private float rotationSpeed = 5f;

    private void Awake()
    {
        _canMove = true;
        _dump = false;
        _aim = false;
        _hook = false;
        _isHooked = false;

        aimTargetObj.SetActive(false);

        _defaultPlayerActions = new DefaultPlayerActions();
        _rigidbody = GetComponent<Rigidbody>();
        _currentSpeed = _initSpeed;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Aim();
        }
        if (Input.GetMouseButtonUp(1))
        {
            NotAim();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Dump();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Hook();
        }
    }

    private void Aim()
    {
        _canMove = false;
        _aim = true;
        moveCamera.SetActive(false);
        aimCamera.SetActive(true);
        aimTargetObj.SetActive(true);

    }
    private void NotAim()
    {
        _canMove = true;
        _aim = false;
        moveCamera.SetActive(true);
        aimCamera.SetActive(false);
        aimTargetObj.SetActive(false);
    }
    private void Hook()
    {
        _hookTargetPosition = null;
        _hookTargetPosition = hookController.HookCheck();
        if (_hookTargetPosition != null)
        {
            _hook = true;
        } else
        {
            _hook = false;
        }
        _animator.SetBool("isRunning", false);
    }

    private void OnEnable()
    {
        _moveAction = _defaultPlayerActions.Player.Move;
        _moveAction.Enable();
        _lookAction = _defaultPlayerActions.Player.Look;
        _lookAction.Enable();

        _defaultPlayerActions.Player.Jump.performed += OnJump;
        _defaultPlayerActions.Player.Jump.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _lookAction.Disable();

        _defaultPlayerActions.Player.Jump.performed -= OnJump;
        _defaultPlayerActions.Player.Jump.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        ResetPhysics();
    }

    private void Dump ()
    {
        _dump = true;
        _rigidbody.AddForce(Vector3.down * _jumpForce * 1.1f, ForceMode.Impulse);
    }
    private void DumpFallBack()
    {
        _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }
    private void ResetPhysics ()
    {
        _rigidbody.useGravity = true;
        _hook = false;
        _isHooked = false;
    }
    private void FixedUpdate()
    {
        // --- MOVEMENT ---
        if (_canMove)
        {
            if (!_isHooked)
            {
                
                if (!_hook)
                {
                    #region Player Movements on the Ground 
                    _moveDir = _moveAction.ReadValue<Vector2>();

                    // Gradually increase the speed until it reaches the maxSpeed.
                    if (_currentSpeed < _maxSpeed && (_moveDir.x != 0 || _moveDir.y != 0))
                    {
                        _currentSpeed += _acceleration * Time.fixedDeltaTime;
                        _currentSpeed = Mathf.Min(_currentSpeed, _maxSpeed);
                    }

                    _vel = _rigidbody.velocity;
                    _vel.x = _currentSpeed * _moveDir.x;
                    _vel.z = _currentSpeed * _moveDir.y;
                    _rigidbody.velocity = _vel;


                    // --- ROTATION ---
                    //Vector2 lookDir = _lookAction.ReadValue<Vector2>();
                    if (_moveDir != Vector2.zero) // Ensure there is movement input to rotate towards.
                    {
                        // Calculate the angle to rotate towards. Convert from 2D to 3D direction.
                        Vector3 directionToLookAt = new Vector3(_moveDir.x, 0, _moveDir.y);
                        Quaternion targetRotation = Quaternion.LookRotation(directionToLookAt, Vector3.up);

                        // Apply the rotation smoothly.
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * _currentSpeed);
                    }

                    // --- ANIMATION ---
                    if (!_isRunning && (_vel.x != 0 || _vel.z != 0) && !_aim)
                    {
                        _isRunning = true;
                        _animator.SetBool("isRunning", true);
                    }
                    if (_isRunning && (_vel.x == 0 && _vel.z == 0) || _aim)
                    {
                        _isRunning = false;
                        _animator.SetBool("isRunning", false);
                        _currentSpeed = _initSpeed;
                    }
                    #endregion Player Movements ends -
                }
                else
                {
                    #region Hooking system - Player will try to hook a target
                    if (_hookTargetPosition != null)
                    {
                        // Move our position a step closer to the target.
                        float step = _currentSpeed * Time.deltaTime; // Calculate distance to move
                                                                     // Gradually increase the speed until it reaches the maxSpeed.
                        if (_currentSpeed < _hookMaxSpeed)
                        {
                            _currentSpeed += _acceleration * 2 * Time.fixedDeltaTime;
                            _currentSpeed = Mathf.Min(_currentSpeed, _hookMaxSpeed);
                        }
                        _rigidbody.useGravity = false;
                        transform.position = Vector3.MoveTowards(transform.position, (Vector3)_hookTargetPosition, step);
                    }
                    #endregion Hooking system ends -
                }
            } else
            {
                #region Wall Walking System
                float input = Input.GetAxis("Vertical");

                Vector3 moveDirection = hookController.HookedObject().transform.forward * input; // Use the wall's forward direction for movement
                transform.position += moveDirection * _currentSpeed * Time.deltaTime;

                // Rotate the player to face the direction of movement
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                #endregion Wall Walking System End
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (_dump)
            {
                _dump = false;
                DumpFallBack();
            }
        }
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_isHooked == false)
            {
                _rigidbody.velocity = new Vector3(0,0,0);
                _hook = false;
                _isHooked = true;
                transform.position = (Vector3)_hookTargetPosition;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            if (_isHooked == true)
            {
                Debug.Log("Exit from Wall");
                ResetPhysics();
                Dump();
            }
        }

    }
}