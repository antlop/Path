using AML.Survivors;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class GameUIController : MonoBehaviour
{

    public static GameUIController instance;

    public TMP_Text XPOrbText;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // this will 'Pause' the Entities
    private void SetEscEnabled(bool shouldEnable)
    {
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null) return;

        var initSystemGroup = defaultWorld.GetExistingSystemManaged<InitializationSystemGroup>();
        initSystemGroup.Enabled = shouldEnable;

        var simSystemGroup = defaultWorld.GetExistingSystemManaged<SimulationSystemGroup>();
        simSystemGroup.Enabled = shouldEnable;
    }

    public void UpdateEXOrbsCollectedText(int count)
    {
        XPOrbText.text = count.ToString();
    }
}
