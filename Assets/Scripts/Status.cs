using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using com.migenius.rs4.viewport;
using com.migenius.rs4.unity;
using com.migenius.rs4.core;
using Logger = com.migenius.rs4.core.Logger;

public class Status : MonoBehaviour {

    public UnityViewport Viewport = null;
    public string Text = "Loading Scene";
    private string OldText = null;
    public GUIStyle Style;
    public bool Hide = true;

    private float paddingX = Screen.width * 0.02f;
    private float paddingY = Screen.height * 0.04f;
    private bool addedStatusCallback = false;
	
    void Start () 
    {
        if (Viewport == null)
        {
            Logger.Log("error", "Status component needs a UnityViewport to work with.");
            this.enabled = false;
            return;
        }
        if (Viewport.enabled == false)
        {
            Logger.Log("error", "Status component needs the UnityViewport to be enabled to work with it.");
            this.enabled = false;
            return;
        }
	}

    void OnStatus(string type, string message)
    {
        Logger.Log("info", "Status - " + type + ": " + message);
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
        if (OldText != Text && GetComponent<Image>() != null)
        {
            // Save the old text and calculate how big the new text is
            OldText = Text;
            GUIContent content = new GUIContent(Text);
            Vector2 size = Style.CalcSize(content);
            float width = size.x + Style.padding.left;
            float height = size.y + Style.padding.top + 2.0f;

            // Resize the component to fit around the new text size
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width + paddingX, height + paddingY);
            rect.anchoredPosition = new Vector2((width / 2.0f) + paddingX, -((height / 2.0f) + paddingY));
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

        // Fade out the text
        Color textColour = Style.normal.textColor;
        textColour.a = CalcAlpha(!Hide, textColour.a, 1.0f, 0.25f);
        Style.normal.textColor = textColour;
        
        // Fade out the image background
        if (GetComponent<Image>() != null)
        {
            Color textureColour = GetComponent<Image>().color;
            textureColour.a = CalcAlpha(!Hide, textColour.a, 0.5f, 0.25f);
            GetComponent<Image>().color = textureColour;
        }
	}
}
