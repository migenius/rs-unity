using UnityEngine;
using System.Collections;
using com.migenius.rs4.viewport;
using com.migenius.rs4.unity;
using com.migenius.rs4.core;

public class Status : MonoBehaviour {

    public UnityViewport Viewport = null;
    public string Text = "Loading Scene";
    private string OldText = null;
    public GUIStyle Style;
    public bool Hide = true;

    private float paddingX = 0.0f;
    private float paddingY = 0.0f;
    private bool addedStatusCallback = false;
	
    void Start () 
    {
        if (Viewport == null)
        {
            Debug.Log("- Status component needs a UnityViewport to work with.");
            this.enabled = false;
            return;
        }
        if (Viewport.enabled == false)
        {
            Debug.Log("- Status component needs the UnityViewport to be enabled to work with it.");
            this.enabled = false;
            return;
        }
        // Number of pixels from the top of the screen that the gui texture current is located at.
        if (GetComponent<GUITexture>() != null)
        {
            paddingX = GetComponent<GUITexture>().transform.position.x * Screen.height;
            paddingY = (1.0f - GetComponent<GUITexture>().transform.position.y) * Screen.height - GetComponent<GUITexture>().pixelInset.height;
        }

	}

    void OnStatus(string type, string message)
    {
        Debug.Log("Status - " + type + ": " + message);
        if (type == "render")
        {
            Hide = true;
            return;
        }

        Hide = false;
        if (type != "info")
        {
            Text = type.ToUpper() + ": " + message;
        }
        else
        {
            Text = message;
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(paddingX, paddingY, 200, 20), Text, Style);
        if (OldText != Text && GetComponent<GUITexture>() != null)
        {
            OldText = Text;
            GUIContent content = new GUIContent(Text);
            Vector2 size = Style.CalcSize(content);
            float width = size.x + Style.padding.left;
            float height = size.y + Style.padding.top + 2.0f;
            GetComponent<GUITexture>().pixelInset = new Rect(0, 0, width, height);

            float top = 1.0f - ((paddingY + height) / Screen.height);
            Vector3 pos = GetComponent<GUITexture>().transform.position;
            pos.y = top;
            GetComponent<GUITexture>().transform.position = pos;
        }
    }

    float CalcAlpha(bool display, float alpha, float maxAlpha, float timeScale)
    {
        if (display && alpha < maxAlpha)
        {
            alpha += (Time.deltaTime / timeScale) * maxAlpha;
        }
        else if (!display && alpha > 0.0f)
        {
            alpha -= (Time.deltaTime / timeScale) * maxAlpha;
        }
        return Mathf.Clamp(alpha, 0.0f, maxAlpha);
    }

	// Update is called once per frame
	void Update () 
    {
        if (!addedStatusCallback && Viewport != null)
        {
            Viewport.Scene.OnStatus += new StatusUpdateCallback(OnStatus);
            addedStatusCallback = true;
            //Logger.OnLog += new Logger.LogHandler(OnLog);
        }

        Color textColour = Style.normal.textColor;
        textColour.a = CalcAlpha(!Hide, textColour.a, 1.0f, 0.25f);
        Style.normal.textColor = textColour;
        
        if (GetComponent<GUITexture>() != null)
        {
            Color textureColour = GetComponent<GUITexture>().color;
            textureColour.a = CalcAlpha(!Hide, textColour.a, 0.5f, 0.25f);
            GetComponent<GUITexture>().color = textureColour;
        }
	}
}
