using UnityEngine;
using System.Collections.Generic;
using Unity.WebRTC;

namespace JWebRTC
{
    public class WRTCCore : MonoBehaviour
    {
        public static WRTCCore Instance = null;

        [SerializeField]
        int streamSizeX, streamSizeY;

        MediaStream sendStream;

        [SerializeField]
        Dictionary<int, GameObject> serverPeers;

        [SerializeField]
        Dictionary<int, GameObject> clientPeers;

        int serverIndexCounter, clientIndexCounter;


        #region WEBRTC - Signaling  이 처리는 자체적으로 해야 한다. 지원 안함.


        //temp
        public static string myUserKey;
        // 사용자 키를 생성 한다.
        public static string CreateUserIDKey()
        {
            return "11";
        }


        // 아래 로직은 서버 node js로 이동해야 한다. 
        public class RTCPeerInfo
        { 
            public enum Type { Server, Client};
            public Type type;
            public int index;

            public string otherPeerKey;
            public int otherPeerIndex;

            // 추후에 여러 정보 넣는다.
        }

        // 아래 로직은 서버 node js로 이동해야 한다. 
        //
        Dictionary<string, List<RTCPeerInfo>> listServerPeers;
        Dictionary<string, List<RTCPeerInfo>> listClientPeers;

        
        public void SignalingRTCSendRegisteryServerPeer(string myUserKey, int myServerIndex)
        {
            // 아래 로직은 서버 node js로 이동해야 한다.  여기서는 패킷 send만 있어야 한다.

            if (!listServerPeers.ContainsKey(myUserKey))
            {
                List<RTCPeerInfo> peerList = new List<RTCPeerInfo>();
                listServerPeers.Add(myUserKey, peerList);
            }

            RTCPeerInfo peer = new RTCPeerInfo();
            peer.type = RTCPeerInfo.Type.Server;
            peer.index = myServerIndex;

            listServerPeers[myUserKey].Add(peer);
        }

        public void SignalingRTCSendRegisteryClientPeer(string myUserKey, int myClientIndex)
        {
            // 아래 로직은 서버 node js로 이동해야 한다. 여기서는 패킷 send만 있어야 한다.

            if (!listClientPeers.ContainsKey(myUserKey))
            {
                List<RTCPeerInfo> peerList = new List<RTCPeerInfo>();
                listClientPeers.Add(myUserKey, peerList);
            }

            RTCPeerInfo peer = new RTCPeerInfo();
            peer.type = RTCPeerInfo.Type.Client;
            peer.index = myClientIndex;

            listClientPeers[myUserKey].Add(peer);
        }

        
        // 클라이언트에게 서버 목록은 서버 키.  알려주는 프로토콜 필요.
        public void SignalingRTCGetWantServerPeerKey(string myUserKey, int myClientIndex, System.Action<string, int> action)
        {
            // 아래 로직은 서버 node js로 이동해야 한다. 여기서는 패킷 send만 있어야 한다.

            // 어떤 서버를 찾을지 로직 필요..


            // 우선은 임시..
            foreach (var key in listServerPeers.Keys)
            {
                if(listServerPeers[key].Count > 0)
                {
                    action.Invoke(key, listServerPeers[key][0].index);
                }
            }
        }

        // 클라이언트에게 해당 서버 접속 원한다는 메시지 옴.
        public void SignalingRTCSendRequestFromClientPeer(  string myClientKey, int myClientIndex, 
                                                            string wantServerKey, int wantServerIndex, 
                                                            System.Action<bool> action)
        {
            // 아래 로직은 서버 node js로 이동해야 한다. 여기서는 패킷 send만 있어야 한다.

            if (listServerPeers.ContainsKey(wantServerKey) 
                && listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[wantServerKey];
                List<RTCPeerInfo> peerClientList = listClientPeers[wantServerKey];

                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == wantServerIndex);
                if (peerServer != null)
                {
                    peerServer.otherPeerKey = myClientKey;
                    peerServer.otherPeerIndex = myClientIndex;

                    //
                    RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                    if (peerClient != null)
                    {
                        peerClient.otherPeerKey = wantServerKey;
                        peerClient.otherPeerIndex = wantServerIndex;

                        action.Invoke(true);
                        return;
                    }
                }
            }

            action.Invoke(false);
        }

        // 서버가 클라이언트에게 Reply -> Description 정보를 전달.
        public void SignalingRTCSendSetRemoteDescriptionFromServer(string myServerKey, int myServerIndex, string sdpPacketWRTC)
        {
            // 아래 로직은 서버 node js로 이동해야 한다.  여기서는 패킷 send만 있어야 한다.
            if (listServerPeers.ContainsKey(myServerKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[myServerKey];
                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == myServerIndex);
                if(peerServer != null)
                {
                    // 네트웍 호출.. 우선은 직접 호출
                    SignalingRTCRecvSetRemoteDescriptionFromServer(peerServer.otherPeerKey, peerServer.otherPeerIndex, sdpPacketWRTC);
                }
            }
        }

        // 클라이언트가 Description  정보 받음.
        public void SignalingRTCRecvSetRemoteDescriptionFromServer(string wantClientKey, int wantClientIndex, string sdpPacketWRTC)
        {
            RecvWRTCSetRemoteDescription_SC(wantClientIndex, sdpPacketWRTC);
        }


        // 클라이언트가 서버에게 - 생성한후 바로 Description 정보를 전달.
        public void SignalingRTCSendSetRemoteDescriptionFromClient(string myClientKey, int myClientIndex, string sdpPacketWRTC)
        {
            // 아래 로직은 서버 node js로 이동해야 한다.  여기서는 패킷 send만 있어야 한다.
            if (listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerClientList = listClientPeers[myClientKey];
                RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                if (peerClient != null)
                {
                    // 네트웍 호출.. 우선은 직접 호출
                    SignalingRTCRecvSetRemoteDescriptionFromClient(peerClient.otherPeerKey, peerClient.otherPeerIndex, sdpPacketWRTC);
                }
            }
        }

        // 서버가 Description  정보 받음.
        public void SignalingRTCRecvSetRemoteDescriptionFromClient(string wantServerKey, int wantServerIndex, string sdpPacketWRTC)
        {
            RecvWRTCSetRemoteDescription_CS(wantServerIndex, sdpPacketWRTC);
        }


        // 클라이언트가 서버에게 Candidate 정보를 보낸다.  시그널링 로직상 클라이언트가 먼저 호출한다.
        public void SignalingRTCSendCandidateFromClient(string myClientKey, int myClientIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            // 아래 로직은 서버 node js로 이동해야 한다.  여기서는 패킷 send만 있어야 한다.
            if (listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerClientList = listClientPeers[myClientKey];
                RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                if (peerClient != null)
                {
                    // 네트웍 호출.. 우선은 직접 호출
                    SignalingRTCRecvCandidateFromClient(peerClient.otherPeerKey, peerClient.otherPeerIndex, Candidate, SdpMid, SdpMLineIndex);
                }
            }
        }

        // 서버가 Candidate 정보 받음
        public void SignalingRTCRecvCandidateFromClient(string wantServerKey, int wantServerIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            RecvWRTCAddIceCandidate_CS(wantServerIndex, Candidate, SdpMid, SdpMLineIndex);
        }


        // 위의 프로토콜과의 일종의 핸드쉐이킹 관계
        // 서버가 클라이언트에게 Candidate 정보를 보낸다.  시그널링 로직상 클라이언트가 먼저 호출한다.
        public void SignalingRTCSendCandidateFromServer(string myServerKey, int myServerIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            // 아래 로직은 서버 node js로 이동해야 한다.  여기서는 패킷 send만 있어야 한다.
            if (listServerPeers.ContainsKey(myServerKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[myServerKey];
                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == myServerIndex);
                if (peerServer != null)
                {
                    // 네트웍 호출.. 우선은 직접 호출
                    SignalingRTCRecvCandidateFromServer(peerServer.otherPeerKey, peerServer.otherPeerIndex, Candidate, SdpMid, SdpMLineIndex);
                }
            }
        }
        public void SignalingRTCRecvCandidateFromServer(string wantClientKey, int wantClientIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            RecvWRTCAddIceCandidate_SC(wantClientIndex, Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion


        private void Awake()
        {
            Instance = this;

            serverIndexCounter = 0;
            clientIndexCounter = 0;

            serverPeers = new Dictionary<int, GameObject>();
            clientPeers = new Dictionary<int, GameObject>();

            listServerPeers = new Dictionary<string, List<RTCPeerInfo>>();
            listClientPeers = new Dictionary<string, List<RTCPeerInfo>>();

            // TEMP..
            myUserKey = CreateUserIDKey();
        }



        #region 외부 인터페이스 UI - 서버
        public int CreateServer(Camera cam, System.Action<Camera> updateTextureCallback)
        {
            //
            GameObject serverPeer = new GameObject("serverPeer");
            serverPeer.transform.parent = gameObject.transform;
            serverPeer.AddComponent<WRTCServerPeer>();

            serverPeers.Add(++serverIndexCounter, serverPeer);

            //
            if (sendStream == null)
            {
                sendStream = cam.CaptureStream(streamSizeX, streamSizeY);
            }

            serverPeers[serverIndexCounter].GetComponent<WRTCServerPeer>().SetIndexKey(myUserKey, serverIndexCounter);
            serverPeers[serverIndexCounter].GetComponent<WRTCServerPeer>().OnCreate(cam, sendStream);
            updateTextureCallback.Invoke(cam);

            //
            SignalingRTCSendRegisteryServerPeer(myUserKey, serverIndexCounter);

            return serverIndexCounter;
        }

        public void ReplyByServer(int index)
        {
            if(serverPeers.ContainsKey(index))
                serverPeers[index].GetComponent<WRTCServerPeer>().SendWRTCSetRemoteDescription();
        }


        public void RestartIceServer(int index)
        {
            if (serverPeers.ContainsKey(index))
                serverPeers[index].GetComponent<WRTCServerPeer>().OnRestartIce();
        }

        public void CloseServer(int index)
        {
            if (serverPeers.ContainsKey(index))
            {
                GameObject server = serverPeers[index];
                server.GetComponent<WRTCServerPeer>().OnClose();
                serverPeers.Remove(index);

                Destroy(server);
                server = null;
            }
        }

        #endregion


        #region 외부 인터페이스 UI - 클라이언트
        public int CreateClient(System.Action<Texture> func)
        {
            //
            GameObject clientPeer = new GameObject("clientPeer");
            clientPeer.transform.parent = gameObject.transform;
            clientPeer.AddComponent<WRTCClientPeer>();

            clientPeers.Add(++clientIndexCounter, clientPeer);

            clientPeers[serverIndexCounter].GetComponent<WRTCClientPeer>().SetIndexKey(myUserKey, clientIndexCounter);
            clientPeers[clientIndexCounter].GetComponent<WRTCClientPeer>().InitReceiveStream(func);

            SignalingRTCSendRegisteryClientPeer(myUserKey, clientIndexCounter);

            return clientIndexCounter;
        }

        
        public void RequestByClient(int index, string wantServerKey, int wantServerIndex)
        {
            if (clientPeers.ContainsKey(index))
            {
                //
                SignalingRTCSendRequestFromClientPeer(myUserKey, index, wantServerKey, wantServerIndex, (isSuccess) =>
                {
                    if (isSuccess)
                        clientPeers[index].GetComponent<WRTCClientPeer>().OnRequest();
                    else
                    {
                        CloseClient(index);
                    }
                });

                //
            }
        }

        public void CloseClient(int index)
        {
            if (clientPeers.ContainsKey(index))
            {
                GameObject client = clientPeers[index];
                client.GetComponent<WRTCClientPeer>().OnClose();
                clientPeers.Remove(index);

                Destroy(client);
                client = null;
            }
        }

       

        #endregion


        #region 서버 ->  클라이언트  : 네트워크 프로토콜  

        public void RecvWRTCSetRemoteDescription_SC(int clientIndex, string sdpPacket)
        {
            if (clientPeers.ContainsKey(clientIndex))
                clientPeers[clientIndex].GetComponent<WRTCClientPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_SC(int clientIndex, string CandidateWRTC, string SdpMidWRTC, int SdpMLineIndexWRTC)
        {
            if (clientPeers.ContainsKey(clientIndex))
                clientPeers[clientIndex].GetComponent<WRTCClientPeer>().RecvWRTCAddIceCandidate(CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        public void ChangeStatusServer_SC(int key, RTCIceConnectionState state)
        {
            Debug.Log($"ChangeStatusServer >> {state}");
        }

        #endregion

        #region 클라이언트 -> 서버 : 네트워크 프로토콜

        public void RecvWRTCSetRemoteDescription_CS(int serverIndex, string sdpPacket)
        {
            if (serverPeers.ContainsKey(serverIndex))
                serverPeers[serverIndex].GetComponent<WRTCServerPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_CS(int serverIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            if (serverPeers.ContainsKey(serverIndex))
                serverPeers[serverIndex].GetComponent<WRTCServerPeer>().RecvWRTCAddIceCandidate(Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion

    }

}
