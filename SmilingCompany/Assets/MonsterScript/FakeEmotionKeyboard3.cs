using UnityEngine;

public class FakeEmotionKeyboard3 : MonoBehaviour, IEmotionProvider3
{
    // Default mapping:
    // 1 = Neutral, 2 = Smile, 3 = Sad, 4 = Angry
    public KeyCode neutralKey = KeyCode.Alpha1;
    public KeyCode smileKey   = KeyCode.Alpha2;
    public KeyCode sadKey     = KeyCode.Alpha3;
    //public KeyCode angryKey   = KeyCode.Alpha4;

  public Emotion3 Current
    {
        get
        {
            if (Input.GetKey(sadKey))   return Emotion3.Sad;
            if (Input.GetKey(smileKey)) return Emotion3.Smile;
            if (Input.GetKey(neutralKey)) return Emotion3.Neutral;
            return Emotion3.Neutral;
        }
    }
}
