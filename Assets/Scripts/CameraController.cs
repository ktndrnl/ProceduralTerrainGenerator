using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 5f;
    public float slowSpeed = 5f;
    public float normalSpeed = 10f;
    public float fastSpeed = 20f;
    public event Action OnCameraMove;
    public event Action OnCameraStop;
    private float _currentSpeed;

    private void Update()
    {
        if (Input.GetMouseButton(1)) // right mouse button
        {
            OnCameraMove?.Invoke();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Movement();
            Rotation();
        }
        else
        {
            OnCameraStop?.Invoke();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
   private void Movement()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
    
        if (input.magnitude > 0.1f)
        {
            input.Normalize();
        }
    
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _currentSpeed = fastSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            _currentSpeed = slowSpeed;
        }
        else
        {
            _currentSpeed = normalSpeed;
        }
    
        Vector3 direction = transform.TransformDirection(input);
        float distance = _currentSpeed * Time.deltaTime * 2f;
    
        if (!Physics.Raycast(transform.position, direction, distance))
        {
            transform.Translate(input * _currentSpeed * Time.deltaTime);
        }
    }
    private void Rotation()
    {
        var mouseInput = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        transform.Rotate(mouseInput * sensitivity * Time.deltaTime * 50);
        Vector3 eulerRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, 0);
    }
}
