using UnityEngine;

public class DamageNumberController : MonoBehaviour
{

    public float lifetime = 1f;
    public float speed = 1f;
    public bool crit = false;
    [Range(0f, 1f)]
    private float alpha = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
}
