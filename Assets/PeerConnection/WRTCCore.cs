using UnityEngine;
using System.Collections.Generic;

public class WRTCCore : MonoBehaviour
{
    public static WRTCCore Instance = null;

    [SerializeField]
    List<GameObject> serverPeers;

    [SerializeField]
    List<GameObject> clientPeers;


    private void Awake()
    {
        Instance = this;
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
