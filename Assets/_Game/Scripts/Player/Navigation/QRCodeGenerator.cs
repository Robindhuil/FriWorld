using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Collections;

public class QRCodeGenerator
{
    private RawImage qrRawImage;
    private UnityEngine.UIElements.Image qrUIImage;
    private bool useUIToolkit;
    private MonoBehaviour runner;

    public QRCodeGenerator(RawImage qrImage, MonoBehaviour runner)
    {
        this.qrRawImage = qrImage;
        this.useUIToolkit = false;
        this.runner = runner;
    }

    public QRCodeGenerator(UnityEngine.UIElements.Image qrImage, MonoBehaviour runner)
    {
        this.qrUIImage = qrImage;
        this.useUIToolkit = true;
        this.runner = runner;
    }

    public void GenerateQRCode(string text)
    {
        string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=256x256&data={UnityWebRequest.EscapeURL(text)}";
        runner.StartCoroutine(DownloadQRCode(qrCodeUrl));
    }


    private IEnumerator DownloadQRCode(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[QRCodeGenerator] Error: {request.error}");
            }
            else
            {
                Texture2D qrTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                
                if (useUIToolkit)
                {
                    qrUIImage.image = qrTexture;
                }
                else
                {
                    qrRawImage.texture = qrTexture;
                }
            }
        }
    }

}
