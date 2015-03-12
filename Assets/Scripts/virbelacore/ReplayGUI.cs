using UnityEngine;

public class ReplayGUI : MonoBehaviour {

    bool timeBasedSlider = true;
    static public bool showMessageNum = false;

    private string GetLabelText()
    {
        string label = "";
        if (showMessageNum)
            label += (ReplayManager.Inst.MessageOffset + ReplayManager.Inst.NextMsgIdx);
        label += "\n" + ReplayManager.Inst.PlaybackTime.ToString();
        return label;
    }

    public void DrawGUI(int x, int y)
    {
        if (!ReplayManager.Initialized || !this.enabled)
            return;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.alignment = TextAnchor.UpperCenter;

        GUI.Label(new Rect(x, y, Screen.width, Screen.height), GetLabelText(), textStyle);
        float sliderWidth = 0.6f;
        if (timeBasedSlider)
        {
            float endSliderVal = 500.0f;
            float currentSliderVal = endSliderVal * ReplayManager.Inst.GetPlaybackPercent();
            float sliderPos = GUI.HorizontalSlider(new Rect((0.5f * (1.0f - sliderWidth)) * Screen.width, y + 40, sliderWidth * Screen.width, 20), currentSliderVal, 0.0f, endSliderVal);
            if (sliderPos == currentSliderVal)
                return;

            ReplayManager.Inst.SetPlaybackPercent(sliderPos / endSliderVal);

        }
        else
        {
            int newNextMsgIdx = (int)GUI.HorizontalSlider(new Rect((0.5f * (1.0f - sliderWidth)) * Screen.width, y + 40, sliderWidth * Screen.width, 20), (float)ReplayManager.Inst.NextMsgIdx, 0.0f, (float)ReplayManager.Inst.NumMessagesToPlay);
            if (newNextMsgIdx != ReplayManager.Inst.NextMsgIdx)
                ReplayManager.Inst.SetCurrentMessage(newNextMsgIdx);
        }
    }
}
