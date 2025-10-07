using UnityEngine;

namespace AML.Survivors
{
    public class CameraTargetSingleton : MonoBehaviour
    {
        public static CameraTargetSingleton instance;

        private void Awake()
        {
            if( instance != null )
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
    }
}