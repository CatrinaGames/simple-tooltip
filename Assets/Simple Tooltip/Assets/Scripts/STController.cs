using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for the Simple Tooltip system.
/// Handles text resizing, styling, and positioning relative to the mouse cursor with screen boundary checks.
/// </summary>
public class STController : MonoBehaviour
{
    // Enum to define text alignment options
    public enum TextAlign { Left, Right };

    [Header("Offset Settings (Relative to Mouse)")]
    [Tooltip("Normal: Tooltip appears at the top right of the mouse")]
    public Vector2 offsetTopRight = new Vector2(10f, 10f);

    [Tooltip("Right Border: Tooltip appears above and to the left of the mouse")]
    public Vector2 offsetTopLeft = new Vector2(-10f, 10f);

    [Tooltip("Top Border: Tooltip appears below and to the right of the mouse")]
    public Vector2 offsetBottomRight = new Vector2(50f, -20f);

    [Tooltip("Top Right Corner: Tooltip appears below and to the left of the mouse")]
    public Vector2 offsetBottomLeft = new Vector2(-10f, -20f);

    // UI References
    private Image panel;
    private TextMeshProUGUI toolTipTextLeft;
    private TextMeshProUGUI toolTipTextRight;
    private RectTransform rect;

    private int showInFrames = -1;
    private bool showNow = false;

    private void Awake()
    {
        // Load up both text layers
        var tmps = GetComponentsInChildren<TextMeshProUGUI>();
        for(int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i].name == "_left")
                toolTipTextLeft = tmps[i];

            if (tmps[i].name == "_right")
                toolTipTextRight = tmps[i];
        }

        // Keep a reference for the panel image and transform
        panel = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        // Hide at the start
        HideTooltip();
    }

    void Update()
    {
        // Adjust size and visibility every frame
        ResizeToMatchText();
        UpdateShow();
    }

    // Adjusts the tooltip panel height to fit the text content
    private void ResizeToMatchText()
    {
        // Find the biggest height between both text layers
        var bounds = toolTipTextLeft.textBounds;
        float biggestY = toolTipTextLeft.textBounds.size.y;
        float rightY = toolTipTextRight.textBounds.size.y;
        if (rightY > biggestY)
            biggestY = rightY;

        // Dont forget to add the margins
        var margins = toolTipTextLeft.margin.y * 2;

        // Update the height of the tooltip panel
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, biggestY + margins);
    }

    // Handles the delay before showing the tooltip and updates its position
    private void UpdateShow()
    {
        if (showInFrames == -1)
            return;

        if (showInFrames == 0)
            showNow = true;

        if (showNow)
        {
            FollowMouseAndClamp();
        }

        showInFrames -= 1;
    }

    // --- NEW FEATURE: Border Control ---
    // Calculates the tooltip position relative to the mouse, ensuring it stays within screen bounds
    private void FollowMouseAndClamp()
    {
        Vector2 mousePos = Input.mousePosition;

        // 1. Calculate the real size on screen
        float realWidth = rect.sizeDelta.x * transform.lossyScale.x;
        float realHeight = rect.sizeDelta.y * transform.lossyScale.y;

        // 2. Determine if we need to flip
        bool flipX = false;
        bool flipY = false;

        // If it goes out of bounds on the right -> Flip X
        if (mousePos.x + realWidth > Screen.width)
            flipX = true;

        // If it goes out of bounds on the top -> Flip Y
        if (mousePos.y + realHeight > Screen.height)
            flipY = true;

        // 3. Select the correct Offset and Pivot based on the case
        Vector2 finalPivot;
        Vector2 finalOffset;

        if (!flipX && !flipY)
        {
            // Normal Case (Top-Right)
            finalPivot = new Vector2(0, 0);
            finalOffset = offsetTopRight;
        }
        else if (flipX && !flipY)
        {
            // Right Border (Top-Left)
            finalPivot = new Vector2(1, 0);
            finalOffset = offsetTopLeft;
        }
        else if (!flipX && flipY)
        {
            // Top Border (Bottom-Right)
            // Special adjustment for top border
            finalPivot = new Vector2(0, 1);
            finalOffset = offsetBottomRight;
        }
        else
        {
            // Corner (Bottom-Left)
            finalPivot = new Vector2(1, 1);
            finalOffset = offsetBottomLeft;
        }

        // 4. Apply changes
        rect.pivot = finalPivot;
        rect.position = new Vector3(mousePos.x + finalOffset.x, mousePos.y + finalOffset.y, 0f);
    }
    // ----------------------------------------

    // Sets the text content without changing the style
    public void SetRawText(string text, TextAlign align = TextAlign.Left)
    {
        // Doesn't change style, just the text
        if(align == TextAlign.Left)
            toolTipTextLeft.text = text;
        if (align == TextAlign.Right)
            toolTipTextRight.text = text;
        ResizeToMatchText();
    }

    // Sets the text content and applies a specific visual style
    public void SetCustomStyledText(string text, SimpleTooltipStyle style, TextAlign align = TextAlign.Left)
    {
        // Update the panel sprite and color
        panel.sprite = style.slicedSprite;
        panel.color = style.color;

        // Update the font asset, size and default color
        toolTipTextLeft.font = style.fontAsset;
        toolTipTextLeft.color = style.defaultColor;
        toolTipTextRight.font = style.fontAsset;
        toolTipTextRight.color = style.defaultColor;

        // Convert all tags to TMPro markup
        var styles = style.fontStyles;
        for(int i = 0; i < styles.Length; i++)
        {
            string addTags = "</b></i></u></s>";
            addTags += "<color=#" + ColorToHex(styles[i].color) + ">";
            if (styles[i].bold) addTags += "<b>";
            if (styles[i].italic) addTags += "<i>";
            if (styles[i].underline) addTags += "<u>";
            if (styles[i].strikethrough) addTags += "<s>";
            text = text.Replace(styles[i].tag, addTags);
        }
        if (align == TextAlign.Left)
            toolTipTextLeft.text = text;
        if (align == TextAlign.Right)
            toolTipTextRight.text = text;
        ResizeToMatchText();
    }

    // Helper function to convert a Unity Color to a Hex string
    public string ColorToHex(Color color)
    {
        int r = (int)(color.r * 255);
        int g = (int)(color.g * 255);
        int b = (int)(color.b * 255);
        return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
    }

    public void ShowTooltip()
    {
        // After 2 frames, showNow will be set to TRUE
        // after that the frame count wont matter
        if (showInFrames == -1)
            showInFrames = 2;
    }

    public void HideTooltip()
    {
        showInFrames = -1;
        showNow = false;
        // Move the tooltip off-screen
        rect.anchoredPosition = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
    }
}
