using UnityEngine;
using System.Collections;

public class TunnelArrowRotateChoice : MonoBehaviour {

    bool startedDrag = false;

    float GetAngle(TunnelChoice choice)
    {
        Vector3 forwardXZPlane = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (choice == TunnelChoice.ALLOCENTRIC)
        {
            float dir = TunnelEnvironmentManager.Inst.GetTunnelAngle() > 0 ? 1f : -1f;
            Quaternion alloRotation = Quaternion.AngleAxis(Vector3.Angle(TunnelEnvironmentManager.Inst.EndTunnelDirection(), TunnelEnvironmentManager.Inst.StartTunnelDirection()), dir * Vector3.up);
            Vector3 correctAnswer = alloRotation * (TunnelEnvironmentManager.Inst.StartTunnelPosition() - transform.position).normalized;
            correctAnswer.y = 0f;
            return Vector3.Angle(correctAnswer, forwardXZPlane);
        }
        else // choice == EGOCENTRIC
        {
            Vector3 correctAnswer = (TunnelEnvironmentManager.Inst.StartTunnelPosition() - transform.position).normalized;
            correctAnswer.y = 0f;
            return Vector3.Angle(correctAnswer, forwardXZPlane);
        }
    }

    void RegisterChoice()
    {
        TunnelGameManager.Inst.RegisterAngleOffsets(GetAngle(TunnelChoice.ALLOCENTRIC), GetAngle(TunnelChoice.EGOCENTRIC));
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseDown && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            TunnelGameManager.Inst.RegisterEvent(TunnelEvent.DECISION_START);
            startedDrag = true;
        }

        if (Event.current != null && Event.current.type == EventType.MouseUp)
        {
            if (startedDrag)
                RegisterChoice();
            startedDrag = false;
        }
        RegisterChoice();

        GUILayout.BeginArea(new Rect(0.25f * Screen.width, 0.25f * Screen.height, 0.5f * Screen.width, 0.5f * Screen.height));
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.fontSize = Mathf.CeilToInt(Screen.height * 0.05f);
        GUILayout.Space(Mathf.Max(Screen.height * 0.01f, 4));
        GUILayout.Label("Where did the tunnel start?", GUILayout.Height(Mathf.Max(GUI.skin.label.fontSize + 6, Mathf.CeilToInt(Screen.height * 0.06f))));
        GUILayout.Space(Mathf.Max(Screen.height * 0.01f, 4));
        GUI.skin.label.fontSize = Mathf.CeilToInt(Screen.height * 0.02f);
        GUILayout.Space(Mathf.Max(Screen.height * 0.005f, 4));
        GUILayout.Label("(Click and drag the arrow)", GUILayout.Height(Mathf.Max(GUI.skin.label.fontSize + 4, Mathf.CeilToInt(Screen.height * 0.03f))));
        GUI.skin.label.fontSize = 12;
        GUILayout.EndArea();
    }
}
