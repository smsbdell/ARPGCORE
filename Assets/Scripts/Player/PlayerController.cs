using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterStats))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private CharacterStats _stats;

    private Vector2 _input;
    private Vector2 _velocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<CharacterStats>();
    }

    private void Update()
    {
        _input = Vector2.zero;

        if (Keyboard.current == null)
            return;

        var keyboard = Keyboard.current;

        // Horizontal
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            _input.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            _input.x += 1f;

        // Vertical
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            _input.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            _input.y += 1f;

        if (_input.sqrMagnitude > 1f)
            _input = _input.normalized;
    }

    private void FixedUpdate()
    {
        _velocity = _input * _stats.moveSpeed;
        _rb.linearVelocity = _velocity;
    }
}
