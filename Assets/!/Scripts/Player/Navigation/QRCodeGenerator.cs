using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class QRCodeGenerator
{
    private RawImage qrImage;

    public QRCodeGenerator(RawImage qrImage)
    {
        this.qrImage = qrImage;
    }

    public void GenerateQRCode(string text)
    {
        string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=256x256&data={UnityWebRequest.EscapeURL(text)}";
        CoroutineRunner.Instance.RunCoroutine(DownloadQRCode(qrCodeUrl));
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
                qrImage.texture = qrTexture;
            }
        }
    }

}
