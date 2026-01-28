using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public TextAsset dialogueJson; 

    public void Interact()
    {
        GameRoot.I.Dialogue.Open(dialogueJson);
    }
}
