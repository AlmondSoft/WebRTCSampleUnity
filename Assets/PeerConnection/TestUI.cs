using UnityEngine;
using UnityEngine.UI;
using JWebRTC;

using TMPro;

public class TestUI : MonoBehaviour
{
    public static TestUI Instance = null;

    [SerializeField] private Button startButton;
    [SerializeField] private Button serverSendDescButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button serverHangUpButton;

    [SerializeField]
    private Button client0_CallButton, client0_HangUpButton;

    [SerializeField]
    private Button client1_CallButton, client1_HangUpButton;

    [SerializeField] public Text localCandidateId;
    [SerializeField] public Text remoteCandidateId;
    //[SerializeField] private Dropdown dropDownProtocol;

    [SerializeField] private Camera cam;
    [SerializeField] public RawImage sourceImage;
    [SerializeField] private Transform rotateObject;

    [SerializeField] private RawImage client0_receiveImage;
    [SerializeField] private RawImage client1_receiveImage;


    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(OnStart);
        
        serverSendDescButton.onClick.AddListener(SendWRTCSetRemoteDescription);
        
        restartButton.onClick.AddListener(RestartIce);
        serverHangUpButton.onClick.AddListener(HangUpServer);

        client0_CallButton.onClick.AddListener(CallClient_0);
        client0_HangUpButton.onClick.AddListener(HangUpClient_0);

        client1_CallButton.onClick.AddListener(CallClient_1);
        client1_HangUpButton.onClick.AddListener(HangUpClient_1);
    }

    void Start()
    {
        client0_CallButton.interactable = true;
        client0_HangUpButton.interactable = false;

        client1_CallButton.interactable = true;
        client1_HangUpButton.interactable = false;
        
        serverSendDescButton.interactable = false;

        restartButton.interactable = false;
        serverHangUpButton.interactable = false;

        //
        WRTCCore.Instance.CreateClient((texture) =>
        {
            client1_receiveImage.texture = texture;
        });

    }

    // Update is called once per frame
    void Update()
    {
        if (rotateObject != null)
        {
            float t = Time.deltaTime;
            rotateObject.Rotate(100 * t, 200 * t, 300 * t);
        }
    }


    private void OnStart()
    {
        startButton.interactable = false;

        //
        WRTCCore.Instance.CreateServer(cam, (camera) =>
        {
            sourceImage.texture = camera.targetTexture;
            sourceImage.color = Color.white;
        });
    }


    private void CallClient_0()
    {
        client0_CallButton.interactable = false;
        client0_HangUpButton.interactable = true;
        serverSendDescButton.interactable = true;

        client0_receiveImage.color = Color.white;

        WRTCCore.Instance.RequestByClient(0);
    }
    public void HangUpClient_0()
    {
        client0_HangUpButton.interactable = false;
        client0_receiveImage.color = Color.black;

        WRTCCore.Instance.CloseClient(0);
    }

    private void CallClient_1()
    {
        client1_CallButton.interactable = false;
        client1_HangUpButton.interactable = true;
        serverSendDescButton.interactable = true;

        client1_receiveImage.color = Color.white;

        WRTCCore.Instance.RequestByClient(1);
    }
    public void HangUpClient_1()
    {
        client1_HangUpButton.interactable = false;
        client1_receiveImage.color = Color.black;

        WRTCCore.Instance.CloseClient(1);
    }

    private void SendWRTCSetRemoteDescription()
    {
        serverSendDescButton.interactable = false;

        WRTCCore.Instance.ReplyByServer(1);
    }

    private void RestartIce()
    {
        restartButton.interactable = false;

        WRTCCore.Instance.RestartIceServer(1);
    }

    public void HangUpServer()
    {
        restartButton.interactable = false;
        sourceImage.color = Color.black;

        WRTCCore.Instance.CloseServer(1);
    }

}

