using UnityEngine;
using UnityEngine.UI;
using JWebRTC;

using TMPro;

public class TestUI : MonoBehaviour
{
    public static TestUI Instance = null;

    [SerializeField] private Button startButton;
    [SerializeField] private Button serverCallButton;
    [SerializeField] private Button serverSendDescButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button serverHangUpButton;

    [SerializeField]
    private Button client0_CallButton, client0_HangUpButton;


    [SerializeField] public Text localCandidateId;
    [SerializeField] public Text remoteCandidateId;
    //[SerializeField] private Dropdown dropDownProtocol;

    [SerializeField] private Camera cam;
    [SerializeField] public RawImage sourceImage;
    [SerializeField] private Transform rotateObject;

    [SerializeField] private RawImage client0_receiveImage;


    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(OnStart);
        
        serverCallButton.onClick.AddListener(CallServer);
        serverSendDescButton.onClick.AddListener(SendWRTCSetRemoteDescription);
        
        restartButton.onClick.AddListener(RestartIce);
        serverHangUpButton.onClick.AddListener(HangUpServer);

        client0_CallButton.onClick.AddListener(CallClient_0);
        client0_HangUpButton.onClick.AddListener(HangUpClient_0);
    }

    void Start()
    {
        client0_CallButton.interactable = true;
        client0_HangUpButton.interactable = false;

        serverCallButton.interactable = false;
        serverSendDescButton.interactable = false;

        restartButton.interactable = false;
        serverHangUpButton.interactable = false;

        //
        WRTCClientPeer.Instance.InitReceiveStream((texture) =>
        {
            client0_receiveImage.texture = texture;
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
        serverCallButton.interactable = true;
       

        //
        WRTCServerPeer.Instance.OnStart(cam);

        // 반드시 여기서 
        sourceImage.texture = cam.targetTexture;
        sourceImage.color = Color.white;
    }


    private void CallClient_0()
    {
        client0_CallButton.interactable = false;
        client0_HangUpButton.interactable = true;
        serverSendDescButton.interactable = true;

        client0_receiveImage.color = Color.white;

        WRTCClientPeer.Instance.OnCall();
    }
    public void HangUpClient_0()
    {
        client0_HangUpButton.interactable = false;
        client0_receiveImage.color = Color.black;

        WRTCClientPeer.Instance.OnHangUp();
    }


    private void SendWRTCSetRemoteDescription()
    {
        serverSendDescButton.interactable = false;

        WRTCServerPeer.Instance.SendWRTCSetRemoteDescription();
    }

    private void CallServer()
    {
        serverCallButton.interactable = false;
        serverHangUpButton.interactable = true;
        restartButton.interactable = true;
        

        WRTCServerPeer.Instance.OnCall();
    }

    private void RestartIce()
    {
        restartButton.interactable = false;

        WRTCServerPeer.Instance.OnRestartIce();
    }




    public void HangUpServer()
    {
        serverCallButton.interactable = true;
        restartButton.interactable = false;

        sourceImage.color = Color.black;

        WRTCServerPeer.Instance.OnHangUp();
    }

}

