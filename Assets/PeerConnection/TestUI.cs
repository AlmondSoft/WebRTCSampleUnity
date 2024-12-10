using UnityEngine;
using UnityEngine.UI;
using JWebRTC;

using TMPro;

public class TestUI : MonoBehaviour
{
    public static TestUI Instance = null;

    [SerializeField] private Button startButton;
    [SerializeField] private Button serverCallButton, clientCallButton;
    [SerializeField] private Button serverSendDescButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button serverHangUpButton, clientHangUpButton;

    [SerializeField] public Text localCandidateId;
    [SerializeField] public Text remoteCandidateId;
    [SerializeField] public TMP_Text midText, candidaiteText, lineIndexText;

    [SerializeField] public TMP_Text c_midText, c_candidaiteText, c_lineIndexText;
    //[SerializeField] private Dropdown dropDownProtocol;

    [SerializeField] private Camera cam;
    [SerializeField] public RawImage sourceImage;
    [SerializeField] private RawImage receiveImage;
    [SerializeField] private Transform rotateObject;


    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(OnStart);
        
        clientCallButton.onClick.AddListener(CallClient);
        serverCallButton.onClick.AddListener(CallServer);
        serverSendDescButton.onClick.AddListener(SendWRTCSetRemoteDescription);

        restartButton.onClick.AddListener(RestartIce);
        clientHangUpButton.onClick.AddListener(HangUpClient);
        serverHangUpButton.onClick.AddListener(HangUpServer);

    }

    void Start()
    {
        clientCallButton.interactable = false;
        serverCallButton.interactable = false;
        serverSendDescButton.interactable = false;

        restartButton.interactable = false;

        clientHangUpButton.interactable = false;
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


    private void CallClient()
    {
        clientCallButton.interactable = false;
        clientHangUpButton.interactable = true;

        serverSendDescButton.interactable = true;

        WRTCClientPeer.Instance.OnCall();
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

        clientCallButton.interactable = true;

        WRTCServerPeer.Instance.OnCall();
    }

    private void RestartIce()
    {
        restartButton.interactable = false;

        WRTCServerPeer.Instance.OnRestartIce();
    }


    public void HangUpClient()
    {
        clientHangUpButton.interactable = false;
        receiveImage.color = Color.black;

        WRTCClientPeer.Instance.OnHangUp();
    }

    public void HangUpServer()
    {
        serverCallButton.interactable = true;
        restartButton.interactable = false;

        sourceImage.color = Color.black;

        WRTCServerPeer.Instance.OnHangUp();
    }


    //

    public void SetReceiveImage(Texture tex)
    {
        receiveImage.texture = tex;
        receiveImage.color = Color.white;
    }

}

