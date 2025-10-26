using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    private List<GameObject> AliveDamageNumbers;
    private List<GameObject> DeadDamageNumbers;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        } else
        {
            // initialize yourself
            AliveDamageNumbers = new List<GameObject>();
            DeadDamageNumbers = new List<GameObject>();
        }
        Instance = this;
    }

    public void SpawnDamageNumber(int _damage, bool _crit)
    {
        if( DeadDamageNumbers.Count > 0 )
        {
            // pop off a dmg number and add it to the alive list
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
