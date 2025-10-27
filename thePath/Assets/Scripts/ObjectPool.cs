using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class ObjectInPool
{
    public PoolObjectID ObjectID;
    public GameObject Prefab;
    public int PreCookedAmount = 0;
    public List<GameObject> AliveObjects;
    public List<GameObject> DeadObjects;
    [HideInInspector]
    public bool Initialized = false;
}

public enum PoolObjectID
{
    DAMAGE_NUMBER,
    ENEMY_ONE,
    ENEMY_TWO,
    ENEMY_THREE,
    MAX
}

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    public ObjectInPool[] Pool = new ObjectInPool[(int)PoolObjectID.MAX];

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        } else
        {
            // initialize yourself
        }
        Instance = this;
    }

    public void SpawnDamageNumber(int _damage, bool _crit, Vector3 position)
    {
        ObjectInPool poolObj = Pool[(int)PoolObjectID.DAMAGE_NUMBER];

        if (!poolObj.Initialized)
        {
            poolObj.AliveObjects = new List<GameObject>();
            poolObj.DeadObjects = new List<GameObject>();
            poolObj.Initialized = true;
        } 
        
        if(poolObj.DeadObjects.Count > 0 )
        {
            // pop off a dmg number and add it to the alive list
            GameObject obj = poolObj.DeadObjects[0];
            poolObj.DeadObjects.Remove(obj);
            obj.GetComponent<DamageNumberController>().Initialize(_damage.ToString(), _crit);
            obj.GetComponent<PooledObject>().ConnectedPoolID = PoolObjectID.DAMAGE_NUMBER;
            obj.transform.position = position;
            poolObj.AliveObjects.Add(obj);
            obj.gameObject.SetActive(true);
        } else
        {
            // create a new object and add to alive
            GameObject obj = Instantiate(poolObj.Prefab);
            obj.GetComponent<DamageNumberController>().Initialize(_damage.ToString(), _crit);
            obj.AddComponent<PooledObject>();
            obj.GetComponent<PooledObject>().ConnectedPoolID = PoolObjectID.DAMAGE_NUMBER;
            obj.transform.position = position;
            poolObj.AliveObjects.Add(obj);
        }
    }

    public GameObject SpawnEnemyFromPoolID(PoolObjectID id)
    {
        ObjectInPool poolObj = Pool[(int)id];

        if (!poolObj.Initialized)
        {
            poolObj.AliveObjects = new List<GameObject>();
            poolObj.DeadObjects = new List<GameObject>();
            poolObj.Initialized = true;
        }

        if (poolObj.DeadObjects.Count > 0)
        {
            // pop off a dmg number and add it to the alive list
            GameObject obj = poolObj.DeadObjects[0];
            poolObj.DeadObjects.Remove(obj);
            obj.GetComponent<PooledObject>().ConnectedPoolID = id;
            poolObj.AliveObjects.Add(obj);
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            // create a new object and add to alive
            GameObject obj = Instantiate(poolObj.Prefab);
            obj.AddComponent<PooledObject>();
            obj.GetComponent<PooledObject>().ConnectedPoolID = id;
            poolObj.AliveObjects.Add(obj);
            return obj;
        }
    }
}
