using UnityEngine;

public class PooledObject : MonoBehaviour 
{
    public PoolObjectID ConnectedPoolID;

    public void DestroyObject()
    {
        ObjectPool.Instance.Pool[(int)ConnectedPoolID].AliveObjects.Remove(gameObject);
        ObjectPool.Instance.Pool[(int)ConnectedPoolID].DeadObjects.Add(gameObject);
        gameObject.SetActive(false);
    }
}
