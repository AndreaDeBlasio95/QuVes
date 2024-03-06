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
    private int _countJump = 0; // to allow multiple jumps

    [SerializeField]
    private int _maxJumps = 2; // to allow multiple jumps

    [SerializeField]
    private bool _isOnAir = false;

    [SerializeField]
    private float _jumpForce = 5f;

    [SerializeField]
    private Vector3 _customGravity;

    [SerializeField]
    private float _jumpForceLimitController = 5f;

    [SerializeField]
    private float _jumpForceLimitControllerTimer = 0.5f;

    [SerializeField]
    private float _onAirSpeed = 4f;

    [SerializeField]
    private float _initSpeed = 7f;

    [SerializeField]
    private float _currentSpeed = 7f;

    [SerializeField]
    private float _acceleration = 5f;

    [SerializeField]
    private float _maxSpeed = 20f;

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
    private bool _canPerformHook;

    [SerializeField]
    private float _timerStartMovementToTarget;

    [SerializeField]
    private float _timerResetCanPerformHook;

    [SerializeField]
    private GameObject _hookLongTentacle;

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
        _canPerformHook = true;

        _timerStartMovementToTarget = 0f;

        _countJump = 0;

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
        //_animator.SetBool("isWallRunning", false);
        //_animator.SetBool("isRunning", false);

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
        if (_canPerformHook)
        {
            _hookTargetPosition = null;
            _hookTargetPosition = hookController.HookCheck();
            if (_hookTargetPosition != null)
            {
                _rigidbody.useGravity = false;
                Vector3 newVelocity = _rigidbody.velocity * 0.5f; // Reduce the velocity by half
                _rigidbody.velocity = newVelocity;                // Set the new velocity
                _hook = true;
                _hookLongTentacle.SetActive(true);
                StartCoroutine(ResetCanPerformHookAutomatically());

            }
            else
            {
                _hook = false;
            }
            //_animator.SetBool("isRunning", false);
        }
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
        if (_countJump < _maxJumps)
        {
            //ResetCanPerformHookManually();
            _countJump++;
            if (_countJump == 2)
            {
                int randomDoubleJumpAnim = Random.Range(1, 3);
                _animator.SetInteger("DoubleJump", randomDoubleJumpAnim);
                StartCoroutine(ResetDoubleJumpAnimation());
            }
            ResetPhysics();
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

            _isOnAir = true;
            StartCoroutine(JumpLimitController());
        }
    }

    private void Dump()
    {
        _dump = true;
        _animator.SetTrigger("Dump");
        _rigidbody.AddForce(Vector3.down * _jumpForce * 1.1f, ForceMode.Impulse);
    }
    private void DumpFallBack()
    {
        _rigidbody.AddForce(Vector3.up * _jumpForce * 0.2f, ForceMode.Impulse);
    }
    private void ResetPhysics()
    {
        _rigidbody.useGravity = true;
        _hook = false;
        _isHooked = false;
        //_animator.SetBool("isWallRunning", false);
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
                    if (!_isOnAir)
                    {
                        if (_currentSpeed < _maxSpeed && (_moveDir.x != 0 || _moveDir.y != 0))
                        {
                            _currentSpeed += _acceleration * Time.fixedDeltaTime;
                            _currentSpeed = Mathf.Min(_currentSpeed, _maxSpeed);
                        }

                    } else
                    {
                        // movement Speed
                        if (_currentSpeed > _initSpeed)
                        {
                            _currentSpeed -= _acceleration * Time.fixedDeltaTime;
                        }

                        // Rigidbody
                        _rigidbody.AddForce(_customGravity * _rigidbody.mass, ForceMode.Acceleration);
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
                    if (!_isRunning && (_vel.x != 0 || _vel.z != 0) && !_aim && !_isOnAir)
                    {
                        _isRunning = true;
                        //_animator.SetBool("isRunning", true);
                    }
                    if ((_isRunning && (_vel.x == 0 && _vel.z == 0)) || _aim)
                    {
                        _isRunning = false;
                        //_animator.SetBool("isRunning", false);
                        if (_currentSpeed >= (_initSpeed + 3f))
                        {
                            _currentSpeed -= 3f;
                        }
                    }
                    // --- 
                    #endregion Player Movements ends -
                }
                else
                {
                    #region Hooking system - Player will try to hook a target
                    if (_hookTargetPosition != null)
                    {
                        // Move our position a step closer to the target.
                        float step = _currentSpeed * Time.deltaTime; // Calculate distance to move
                        _timerStartMovementToTarget += 1 * Time.deltaTime;
                        _rigidbody.useGravity = false;
                        if (_timerStartMovementToTarget >= 0.7f)
                        {
                            _currentSpeed = _hookMaxSpeed;
                            transform.position = Vector3.MoveTowards(transform.position, (Vector3)_hookTargetPosition, step);
                        }
                    }
                    #endregion Hooking system ends -
                }
            } else
            {
                #region Wall Walking System
                float input = Input.GetAxis("Vertical");

                Vector3 moveDirection = hookController.HookedObject().transform.forward * input; // Use the wall's forward direction for movement
                // todo: change 10 with a wall running speed
                transform.position += moveDirection * 10 * Time.deltaTime;

                // Rotate the player to face the direction of movement
                if (moveDirection != Vector3.zero)
                {
                    // --- ANIMATION ---
                    if (!_isRunning)
                    {
                        _isRunning = true;
                        //ator.SetBool("isWallRunning", true);
                    }
                    // ---

                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                } else
                {
                    // --- ANIMATION ---
                    if (_isRunning)
                    {
                        _isRunning = false;
                        //_animator.SetBool("isWallRunning", false);
                    }
                    // ---
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
                ResetPhysics();
                DumpFallBack();
            }

            ResetJumpCount();
            _isOnAir = false;
        }
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_isHooked == false && _hook)
            {
                _rigidbody.velocity = new Vector3(0, 0, 0);
                _hook = false;
                _isHooked = true;
                transform.position = (Vector3)_hookTargetPosition;
                _currentSpeed = _initSpeed;
                //_animator.SetBool("isWallRunning", true);

                _isOnAir = false;
            }

            ResetJumpCount();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("ExitWall"))
        {
            if (_isHooked == true)
            {
                Debug.Log("Exit from Wall");
                _currentSpeed = _initSpeed;

                ResetPhysics();
                ResetJumpCount();

                _isHooked = false;
                _isOnAir = true;
            }

        }

    }

    private void ResetJumpCount()
    {
        _countJump = 0;
    }

    private IEnumerator JumpLimitController()
    {
        yield return new WaitForSeconds(_jumpForceLimitControllerTimer);
        //_rigidbody.AddForce(Vector3.down * _jumpForceLimitController, ForceMode.Impulse);
        
    }
    // Reset Anim State
    private IEnumerator ResetDoubleJumpAnimation()
    {
        yield return new WaitForSeconds(0.1f);
        _animator.SetInteger("DoubleJump", 0);
    }

    // Reset Can Perform Hook
    private IEnumerator ResetCanPerformHookAutomatically()
    {
        yield return new WaitForSeconds(_timerResetCanPerformHook);
        _canPerformHook = true;
        _timerStartMovementToTarget = 0.0f;
    }
    private void ResetCanPerformHookManually()
    {
        _canPerformHook = true;
        _timerStartMovementToTarget = 0.0f;
    }
}