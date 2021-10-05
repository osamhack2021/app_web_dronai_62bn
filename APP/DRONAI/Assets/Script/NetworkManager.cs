using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Dronai.Data;
using WebSocketSharp;



namespace Dronai.Network
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        private WebSocket ws;


        // Resources
        [SerializeField] private string serverPath = "ds.linearjun.com";
        [SerializeField] private string fileName = string.Empty;


        private void Awake()
        {
            // 준비
            Initialize();
        }
        private void Initialize(Action onFinished = null)
        {
            ws = new WebSocket("ws://" + serverPath);
            ws.OnMessage += (sender, e) =>
            {
                Debug.Log("Message received from " + ((WebSocket)sender).Url + ", Data: " + e.Data);
            };
            ws.Connect();

            onFinished?.Invoke();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                StartCoroutine(AddEvent("Drone_Test", "ADD API 테스트입니다", Application.dataPath + "/Design/Images/" + fileName));
            }
        }

        public void TestConnection(Action<bool> success)
        {
            StartCoroutine(TestConnectionRoutine(success));
        }

        private IEnumerator TestConnectionRoutine(Action<bool> success = null)
        {
            if (ws == null)
            {
                success?.Invoke(false);
                yield break;
            }

            // 웹 Reqeust 시작
            using (UnityWebRequest www = UnityWebRequest.Get(serverPath + "/api/event/test"))
            {
                // Request 요청
                yield return www.SendWebRequest();


                // Request 결과 출력
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    print("[실패] " + www.error);
                }
                else
                {
                    if (www.isDone)
                    {
                        // print(www.downloadHandler.text);
                        success?.Invoke(true);
                        yield break;
                    }
                    else
                    {
                        print("[실패] 이벤트 삽입을 완료하지 못했습니다!");
                    }
                }
            }
            success?.Invoke(false);
            yield break;
        }

        /// <summary>
        /// 원격 데이터베이스에 이벤트를 추가해주는 함수
        /// </summary>
        /// <param name="droneId">드론 아이디</param>
        /// <param name="detail">이벤트 상세 설명</param>
        /// <param name="localImgPath">업로드 할 이미지 경로</param>
        /// <returns></returns>
        private IEnumerator AddEvent(string droneId, string detail, string localImgPath)
        {
            bool working = false;
            string remoteImgPath = string.Empty;

            // 이미지 업로드 시작
            working = true;
            StartCoroutine(Upload(new string[] { localImgPath }, serverPath + "/api/event/upload", (List<string> results) =>
            {
                remoteImgPath = results[0];
                working = false;
            }));

            // 이미지 업로드 대기
            while (working)
            {
                yield return null;
            }

            // 이벤트 요청 시작
            working = true;

            // Json data 생성
            var jsonData = JsonUtility.ToJson(new DroneEvent(droneId, detail, remoteImgPath));

            // 웹 Reqeust 시작
            using (UnityWebRequest www = UnityWebRequest.Post(serverPath + "/api/event/add", jsonData))
            {
                // Reqeust 정의
                www.SetRequestHeader("content-type", "application/json");
                www.uploadHandler.contentType = "application/json";
                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));

                // Request 요청
                yield return www.SendWebRequest();


                // Request 결과 출력
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    print("[실패] " + www.error);
                }
                else
                {
                    if (www.isDone)
                    {
                        print(www.downloadHandler.text);
                    }
                    else
                    {
                        print("[실패] 이벤트 삽입을 완료하지 못했습니다!");
                    }
                }
            }

            // 작업 종료
            working = false;
            yield break;
        }

        private IEnumerator Upload(string[] path, string url, Action<List<string>> OnFinished)
        {
            foreach (string target in path)
            {
                if (!File.Exists(target)) yield break;
            }

            // 결과 변수 정의
            List<string> results = new List<string>();

            // 전송할 이미지 정의
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            foreach (string target in path)
            {
                // 바이트 스트림
                byte[] bytes = File.ReadAllBytes(target);
                form.Add(new MultipartFormFileSection("file", bytes, "3_2.jpg", "image/jpg"));
            }

            byte[] boundary = UnityWebRequest.GenerateBoundary();

            //serialize form fields into byte[] => requires a bounday to put in between fields 
            byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\\ n-- ", Encoding.UTF8.GetString(boundary), "--"));

            //Make my complete body from the two byte arrays 
            byte[] body = new byte[formSections.Length + terminate.Length];
            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

            //Set the content type-NO QUOTES around the boundary 
            string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));

            using (UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                UploadHandlerRaw uploadHandlerFile = new UploadHandlerRaw(body);
                www.uploadHandler = (UploadHandler)uploadHandlerFile;
                www.uploadHandler.contentType = contentType;
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    //Show results as text
                    if (www.responseCode == 200)
                    {
                        results.Add(www.downloadHandler.text);
                    }
                }
            }

            // Finalize
            OnFinished?.Invoke(results);
            yield break;
        }

        private IEnumerator DownLoad(string desFileName, string url)
        {
            string url1 = url;
            if (File.Exists(desFileName)) File.Delete(desFileName);
            if (!File.Exists(desFileName))
            {
                UnityWebRequest request = UnityWebRequest.Get(url1);
                yield return request.SendWebRequest();
                if (request.isDone)
                {
                    int packLength = 1024 * 20;
                    byte[] data = request.downloadHandler.data;
                    int nReadSize = 0;
                    byte[] nbytes = new byte[packLength];
                    using (FileStream fs = new FileStream(desFileName, FileMode.Create))
                    using (Stream netStream = new MemoryStream(data))
                    {
                        nReadSize = netStream.Read(nbytes, 0, packLength);
                        while (nReadSize > 0)
                        {
                            fs.Write(nbytes, 0, nReadSize);
                            nReadSize = netStream.Read(nbytes, 0, packLength);
                            double dDownloadedLength = fs.Length * 1.0 / (1024); //* 1024 
                            double dTotalLength = data.Length * 1.0 / (1024); //* 1024 
                            string ss = string.Format("Downloaded {0:F}K/{1:F}K", dDownloadedLength, dTotalLength);

                            // if (OnDownloadProgressEvent != null)
                            // {
                            //     OnDownloadProgressEvent.Invoke(ss);
                            // }

                            Debug.Log(ss);
                            yield return null;
                        }

                    }

                    byte[] bytes = request.downloadHandler.data;
                    Texture2D texture2D = new Texture2D(256, 256);
                    texture2D.LoadImage(bytes);
                    texture2D.Apply();
                    // _rawImage.texture = texture2D;
                }

            }
            // if (OnDownloadCompleteEvent != null)
            // {
            //     Debug.Log("download finished");
            //     OnDownloadCompleteEvent.Invoke();
            // }
        }
    }
}
