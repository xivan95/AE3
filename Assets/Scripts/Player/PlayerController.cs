using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Animator _animator;
    
    [SerializeField] private Transform _gunTip;
    private float _movementSpeed = 10.0f;
    private float _fireRate = 0.3f;
    private float _lastShotTime = 0;
    private float _rotationSpeed = 15.0f;
    private float _verticalRotation = 0f;
    private bool _isShooting = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _playerInput.actions["Jump"].performed += ctx => HandleJump();
        _playerInput.actions["Attack"].performed += ctx =>_isShooting = true;
        _playerInput.actions["Attack"].canceled += ctx => _isShooting = false;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();

        //Comprobamos que el jugador este manteniendo el botón de disparar
        // y que el tiempo actual menos el tiempo cuando ocurrió el anterior disparo es mayor o igual al ratio de fuego
        if (_isShooting && Time.time - _lastShotTime >= _fireRate)
        {
            _animator.SetTrigger("Shoot");
            Shoot();
            _lastShotTime = Time.time;
        }
        
    }

    void HandleMovement() 
    {
        // Get the movement input
        Vector2 movementInput = _playerInput.actions["Move"].ReadValue<Vector2>();

        // Convert to world-space movement relative to player rotation
        Vector3 movementDirection = transform.forward * movementInput.y + transform.right * movementInput.x;

        // Apply movement (ensuring it's physics-friendly)
        Vector3 movementVelocity = movementDirection.normalized * _movementSpeed;

        _rb.linearVelocity = new Vector3(movementVelocity.x, _rb.linearVelocity.y, movementVelocity.z); // Keeps gravity intact

    }

    void HandleRotation() 
    {
        // Get the mouse movement input
        Vector2 mouseDelta = _playerInput.actions["Look"].ReadValue<Vector2>();

        // Get the current rotation
        Quaternion currentRotation = _rb.rotation;

        Quaternion yawRotation = Quaternion.Euler(0f, mouseDelta.x * _rotationSpeed * Time.deltaTime, 0f);
        _rb.MoveRotation(currentRotation * yawRotation);

        _verticalRotation -= mouseDelta.y * _rotationSpeed * Time.deltaTime;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -89f, 89f); // Prevents looking too far up/down

        Camera.main.transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);

    }

    void Shoot() 
    {
        _audioSource.PlayOneShot(GameManager.Instance.ShootSound, 0.5f);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f,0.5f,0));

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the hit object implements IDamageable
            IDamageable damageable = hit.transform.GetComponent<IDamageable>();

            if(damageable != null ) damageable.Damage(hit);
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;

        // Check if there's a collider below the player
        return Physics.Raycast(origin, direction, 1.3f);
    }

    void HandleJump() 
    {
        
        if (!IsGrounded()) return;
        _rb.AddForce(Vector3.up * 6.0f, ForceMode.Impulse);
    }
}
