using TMPro;
using UnityEngine;

public class DamageNumberController : MonoBehaviour
{

    public float lifetime = 1f;
    public bool crit = false;
    [Range(0f, 1f)]
    private float alpha = 1f;

    private float bucket = 0;

    public void Initialize(string text, bool _crit)
    {
        bucket = 0;
        crit = _crit;
        alpha = 1f;
        if( crit )
        {
            GetComponentInChildren<TMP_Text>().color = Color.yellow;
            GetComponentInChildren<TMP_Text>().fontSize = 14;
        }
        GetComponentInChildren<TMP_Text>().text = text;
        GetComponentInChildren<TMP_Text>().fontSize = 10;
        transform.GetChild(0).localPosition = Vector3.zero;
    }

    private void Update()
    {
        bucket += Time.deltaTime;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if( bucket > lifetime)
        {
            GetComponent<PooledObject>().DestroyObject();
            return;
        }

        float percent = bucket / lifetime;

        float posOffset = 2f * percent;

        transform.GetChild(0).localPosition = new Vector3(0,0, posOffset);
      
    }
}
