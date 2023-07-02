using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Bot : MonoBehaviour
{
    public static Bot instance;

    [SerializeField] TextMeshProUGUI debugTextEvaluation;

    float[] stackPositionScores = new float[]
    {10, 9, 8, 7, 6, 5, 8, 4, 3, 2, 2, 2, 2, 2, 2, 2, 2, 5, 0, 1, 1, 2, 2, 3};

    void Awake() {
        instance = this;
    }

    public void UpdateDebugText() {
        float evaluation = EvaluateGameState();
        debugTextEvaluation.text = "Game state score: " + evaluation 
                                    + "\n" + (evaluation < 0 ? "White" : "Red") + " is winning.";
        debugTextEvaluation.color = (evaluation < 0 ? Color.white : Color.red);
    }

    // the bot will be playing red
    public float EvaluateGameState() {
        List<Piece> redPieces = new List<Piece>();
        List<Piece> whitePieces = new List<Piece>();
        
        foreach(Piece p in GameObject.FindObjectsOfType<Piece>()) {
            if(p.player)
                redPieces.Add(p);
            else
                whitePieces.Add(p);
        }

        float overallScore = 0;

        // evaluate red's position
        foreach(Piece piece in redPieces) {
            float positionValue = 0;

            if(piece.isOut) {
                positionValue = 4;
                Debug.Log("piece (out) value: " + positionValue);
                overallScore += positionValue;
                continue;
            }

            if(piece.slot.pieces.Count > 1) // position score based on where it's good to have a stack
                positionValue = stackPositionScores[23 - piece.slot.index] / 23f;
            
            // position score based on proximity to base
            positionValue += (23 - piece.slot.index) / 23f;
            if(positionValue <= 6) { // in base = worth more
                positionValue *= 2f;
            }

            Debug.Log("proximity score: " + positionValue);

            if(piece.isCaptured) { // captured
                positionValue = -2f;
            }
            
            for (int i = piece.slot.index - 1; i > piece.slot.index - 6; i--) // places it can move to within 1 dice roll
            {
                if(i >= 0) { // to avoid errors
                    Slot s = Manager.instance.slots[i];
                    if(s.pieces.Count > 0) {
                        if(s.pieces[0].player) { // one of our pieces
                            if(s.pieces.Count == 1 || piece.slot.pieces.Count == 1) { // we may be able to close next turn, so slight plus
                                positionValue += 0.05f;
                            }
                        }
                        else {
                            if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                                positionValue += 0.2f;
                            }
                            else { // a position is blocked - bad
                                positionValue -= 0.1f;
                            }
                        }
                    }
                }
            }

            if(piece.slot.pieces.Count == 1) { // we are exposed
                positionValue -= 1f;
                float enemyPiecesAheadScore = 0; // score based on enemy pieces that are ahead - especially if they can reach us within 1 dice roll
                for (int i = piece.slot.index - 1; i >= 0; i--) // loop through all slots ahead of this one
                {
                    Slot s = Manager.instance.slots[i];
                    if(s.pieces.Count > 0) {
                        if(!s.pieces[0].player) {
                            if(piece.slot.pieces.Count == 1) { // it's an exposed one too
                                // currently just ignore, but maybe do something based on who's turn it is?
                            }
                            else {
                                if(piece.slot.index - i <= 6) {
                                    enemyPiecesAheadScore += 1;
                                }
                                else {
                                    enemyPiecesAheadScore += (Mathf.Abs(piece.slot.index - i) % 6) / 4; // 0-1 based on how many dice rolls it'll take to reach us
                                }
                            }
                        }
                    }
                }
                positionValue -= enemyPiecesAheadScore * 0.2f;
                Debug.Log("enemy pieces ahead: " + enemyPiecesAheadScore);
            }

            Debug.Log("piece value: " + positionValue);
            overallScore += positionValue;
        }
        
        // evaluate white's position
        foreach(Piece piece in whitePieces) {
            float positionValue = 0;

            if(piece.isOut) {
                positionValue = 4;
                Debug.Log("white (out) piece value: " + positionValue);
                overallScore -= positionValue;
                continue;
            }

            if(piece.slot.pieces.Count > 1) // position score based on where it's good to have a stack
                positionValue = stackPositionScores[23 - piece.slot.index] / 23f;
            
            positionValue = piece.slot.index / 23f;
            if(positionValue <= 6) { // in base = worth more
                positionValue *= 2f;
            }

            if(positionValue >= 18) { // in base = worth more
                positionValue *= 4f;
            }

            if(piece.isCaptured) { // captured
                positionValue = -2f;
            }
            
            for (int i = piece.slot.index + 1; i < piece.slot.index + 6; i++) // places it can move to within 1 dice roll
            {
                if(i <= 23) { // to avoid errors
                    Slot s = Manager.instance.slots[i];
                    if(s.pieces.Count > 0) {
                        if(!s.pieces[0].player) { // one of pir pieces
                            if(s.pieces.Count == 1 || piece.slot.pieces.Count == 1) { // we may be able to close next turn, so slight plus
                                positionValue += 0.05f;
                            }
                        }
                        else {
                            if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                                positionValue += 0.2f;
                            }
                            else { // a position is blocked - bad
                                positionValue -= 0.1f;
                            }
                        }
                    }
                }
            }

            if(piece.slot.pieces.Count == 1) { // we are exposed
                positionValue -= 0.1f;
                float enemyPiecesAheadScore = 0; // score based on enemy pieces that are ahead - especially if they can reach us within 1 dice roll
                for (int i = piece.slot.index + 1; i <= 23; i++) // loop through all slots ahead of this one
                {
                    Slot s = Manager.instance.slots[i];
                    if(s.pieces.Count > 0) {
                        if(!s.pieces[0].player) {
                            if(piece.slot.pieces.Count == 1) { // it's an exposed one too
                                // currently just ignore, but maybe do something based on who's turn it is?
                                continue;
                            }
                            else {
                                if(Mathf.Abs(i - piece.slot.index) <= 6) {
                                    enemyPiecesAheadScore += 1;
                                }
                                else {
                                    enemyPiecesAheadScore += (Mathf.Abs(piece.slot.index - i) % 6) / 4; // 0-1 based on how many dice rolls it'll take to reach us
                                }
                            }
                        }
                    }
                }
                positionValue -= enemyPiecesAheadScore * 0.2f;
            }

            Debug.Log("white piece value: " + positionValue);
            overallScore -= positionValue; // subtract white's position from red's position to get overall score for red
        }

        return overallScore;
    }
}
