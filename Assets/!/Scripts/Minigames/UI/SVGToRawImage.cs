using UnityEngine;
using UnityEngine.UI;

public class SVGToRawImage : MonoBehaviour {
    public Sprite svgSprite;
    public RawImage rawImage;

    void Start() {
        if (svgSprite != null && rawImage != null) {
            rawImage.texture = svgSprite.texture;
        }
    }
}
