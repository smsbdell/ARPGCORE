using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TMP_Text text;

    public float lifetime = 0.6f;
    public float floatSpeed = 1.5f;

    private float _timeRemaining;
    private Color _startColor;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        if (text != null)
            _startColor = text.color;

        _timeRemaining = lifetime;
    }

    public void Setup(float damage)
    {
        if (text != null)
        {
            text.text = Mathf.RoundToInt(damage).ToString();
            text.color = _startColor;
        }

        _timeRemaining = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        _timeRemaining -= Time.deltaTime;
        float t = Mathf.Clamp01(_timeRemaining / lifetime);

        if (text != null)
        {
            Color c = text.color;
            c.a = t;
            text.color = c;
        }

        if (_timeRemaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
