using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceCommand : MonoBehaviour
{
    [SerializeField] private BodyPickupController pickupController;
    
    private KeywordRecognizer _recognizer;
    private Dictionary<string, System.Action> _keywords = new Dictionary<string, System.Action>();

    void Start()
    {
        _keywords.Add("Lift", LiftBox);
        _keywords.Add("Pick Up", LiftBox);
        _keywords.Add("Drop", DropBox);
        _keywords.Add("Drop the box", DropBox);

        _recognizer = new KeywordRecognizer(_keywords.Keys.ToArray());
        _recognizer.OnPhraseRecognized += OnPhraseRecognized;
        _recognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string spoken = args.text.ToLower();
        foreach (var key in _keywords)
        {
            if (spoken.Contains(key.Key.ToLower()))
            {
                key.Value.Invoke();
                break;
            }
        }

    }

    private void LiftBox()
    {
        if (pickupController && !pickupController.IsHoldingObject) 
            pickupController.TriggerPickup();
    }

    private void DropBox()
    {
        if (pickupController && pickupController.IsHoldingObject) 
            pickupController.TriggerDrop();
    }

    void OnDestroy()
    {
        if (_recognizer != null && _recognizer.IsRunning)
        {
            _recognizer.Stop();
            _recognizer.Dispose();
        }
    }

}