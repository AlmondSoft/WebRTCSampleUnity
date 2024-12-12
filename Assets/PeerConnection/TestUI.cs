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
    private void CallClient_0()
    {
    }
    public void HangUpClient_0()
    {
    }

    int serverIndex;
    private void OnStart()
    {
        startButton.interactable = false;

        //
        serverIndex = WRTCCore.Instance.CreateServer(cam, (camera) =>
        {
            sourceImage.texture = camera.targetTexture;
            sourceImage.color = Color.white;
        });
    }

    private void SendWRTCSetRemoteDescription()
    {
        serverSendDescButton.interactable = false;

        WRTCCore.Instance.ReplyByServer(serverIndex);
    }

    private void RestartIce()
    {
        restartButton.interactable = false;

        WRTCCore.Instance.RestartIceServer(serverIndex);
    }

    public void HangUpServer()
    {
        restartButton.interactable = false;
        sourceImage.color = Color.black;

        WRTCCore.Instance.CloseServer(serverIndex);
    }


    int clientIndex;
    ///
    private void CallClient_1()
    {
        client1_CallButton.interactable = false;
        client1_HangUpButton.interactable = true;
        serverSendDescButton.interactable = true;
        client1_receiveImage.color = Color.white;

       
        WRTCCore.Instance.SignalingRTCGetWantServerPeerKey(WRTCCore.myUserKey, clientIndex, (serverKey, serverIndex) =>
        {
            //
            clientIndex = WRTCCore.Instance.CreateClient((texture) =>
            {
                /////////////////////////////////////////
                //  확인! 확인 !
                //
                // 업데이트 함수임 리턴 함수 아님.
                client1_receiveImage.texture = texture;
                //
            });

            WRTCCore.Instance.RequestByClient(clientIndex, serverKey, serverIndex);
        });
    }

    public void HangUpClient_1()
    {
        client1_HangUpButton.interactable = false;
        client1_receiveImage.color = Color.black;

        WRTCCore.Instance.CloseClient(clientIndex);
    }

}

