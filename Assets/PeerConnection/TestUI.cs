using UnityEngine;
using UnityEngine.UI;
using JWebRTC;


public class TestUI : MonoBehaviour
{
    public static TestUI Instance = null;

    [SerializeField] private Button startButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button hangUpButton;
    [SerializeField] public Text localCandidateId;
    [SerializeField] public Text remoteCandidateId;
    [SerializeField] private Dropdown dropDownProtocol;

    [SerializeField] private Camera cam;
    [SerializeField] public RawImage sourceImage;
    [SerializeField] private RawImage receiveImage;
    [SerializeField] private Transform rotateObject;


    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(OnStart);
        callButton.onClick.AddListener(Call);
        restartButton.onClick.AddListener(RestartIce);
        hangUpButton.onClick.AddListener(HangUp);

    }

    void Start()
    {
        callButton.interactable = false;
        restartButton.interactable = false;
        hangUpButton.interactable = false;


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
        callButton.interactable = true;


        //
        WRTCClientPeer.Instance.OnStart(cam);
        WRTCServerPeer.Instance.OnStart(cam);

        // 반드시 여기서 
        sourceImage.texture = cam.targetTexture;
        sourceImage.color = Color.white;
    }


    private void Call()
    {
        callButton.interactable = false;
        hangUpButton.interactable = true;
        restartButton.interactable = true;

        WRTCClientPeer.Instance.OnCall();
        WRTCServerPeer.Instance.OnCall();
    }

    private void RestartIce()
    {
        restartButton.interactable = false;

        WRTCClientPeer.Instance.OnRestartIce();
        WRTCServerPeer.Instance.OnRestartIce();
    }


    public void HangUp()
    {
        callButton.interactable = true;
        restartButton.interactable = false;
        hangUpButton.interactable = false;
        receiveImage.color = Color.black;

        WRTCClientPeer.Instance.OnHangUp();
        WRTCServerPeer.Instance.OnHangUp();
    }


    //

    public void SetReceiveImage(Texture tex)
    {
        receiveImage.texture = tex;
        receiveImage.color = Color.white;
    }

}

