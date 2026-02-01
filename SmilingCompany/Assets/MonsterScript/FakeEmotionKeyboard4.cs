using UnityEngine;

public class FakeEmotionKeyboard4 : MonoBehaviour, IEmotionProvider4
{
    // Default mapping:
    // 1 = Neutral, 2 = Smile, 3 = Sad, 4 = Angry
    public KeyCode neutralKey = KeyCode.Alpha1;
    public KeyCode smileKey   = KeyCode.Alpha2;
    public KeyCode sadKey     = KeyCode.Alpha3;
    public KeyCode angryKey   = KeyCode.Alpha4;

    public Emotion4 Current
    {
        get
        {
            // priority if multiple keys pressed
            if (Input.GetKey(angryKey)) return Emotion4.Angry;
            if (Input.GetKey(sadKey))   return Emotion4.Sad;
            if (Input.GetKey(smileKey)) return Emotion4.Smile;
            if (Input.GetKey(neutralKey)) return Emotion4.Neutral;

            return Emotion4.Neutral;
        }
    }
}
