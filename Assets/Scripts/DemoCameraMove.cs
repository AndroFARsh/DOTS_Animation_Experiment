using UnityEngine;

public class DemoCameraMove : MonoBehaviour
{
    [SerializeField] private float speed = 5;
    
    private InputController inputController;
    
    void Awake() => inputController = new InputController();

    private void OnEnable() => inputController.Enable();
    
    private void OnDisable() => inputController.Dispose();

    // Update is called once per frame
    void Update()
    {
        var dt = Time.deltaTime;
        var moveInput = inputController.Player.Move.ReadValue<Vector2>();
        
        var forward = transform.forward;
        var right = transform.right;

        var direction = forward * moveInput.y + right * moveInput.x;
        transform.position += dt * speed * direction;
    }
}
