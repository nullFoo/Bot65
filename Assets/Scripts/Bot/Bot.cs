using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using static GameState;

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
        GameState currentGameState = GameState.GameStateFromCurrentBoard();

        float evaluation = EvaluateGameState(currentGameState);
        string evalString = evaluation.ToString("F2");

        string whosWinning = (evaluation < 0 ? "White" : "Red") + " is winning.";
        Color colour = (evaluation < 0 ? Color.white : Color.red);
        if(Mathf.Abs(evaluation) < 0.1f) { // it's even or very close to even
            whosWinning = "Game is even";
            colour = new Color(200, 200, 200);
        }

        debugTextEvaluation.text = "Game state score: " + evalString 
                                    + "\n" + whosWinning;
        debugTextEvaluation.color = colour;
    }

    public void DebugFuncTemp() {
        GameState currentGameState = GameState.GameStateFromCurrentBoard();
        List<Move> legalMoves = currentGameState.GetAllLegalMoves();
        DoMoveInGame(legalMoves[0]);
    }

    public void DoMoveInGame(Move move) { // find the actual Piece and Slot objects from the move and do it
        Slot slot = Manager.instance.slots.First(s => s.index == move.targetSlot.index);
        if(slot == null) {
            return;
        }
        Debug.Log(move.piece.slot.index);
        Piece piece = Manager.instance.allPieces.First(p => p.slot.index == move.piece.slot.index); // pieces on the same slot are functionally identical, so just the first one that's on the same slot
        if(piece == null) {
            return;
        }

        Manager.instance.diceRolls.Remove(move.moveNumber);
        piece.MoveTo(slot);
    }

    public struct Move {
        public PieceAbstract piece;
        public SlotAbstract targetSlot;
        public int moveNumber;

        public GameState after;

        public Move(GameState gameState, PieceAbstract p, SlotAbstract s, int dice) {
            this.piece = p;
            this.targetSlot = s;
            this.moveNumber = dice;

            this.after = new GameState(gameState);
            // after.MovePiece(p, s);
            // after.diceRolls.Remove(moveNumber);
        }
    }

    // the bot will be playing red
    public float EvaluateGameState() {        
        float overallScore = 0;

        foreach(Piece p in GameObject.FindObjectsOfType<Piece>()) {
            float pieceValue = EvaluatePiece(p);

            if(p.player)
                overallScore += pieceValue; // red
            else
                overallScore -= pieceValue; // white

                
            p.debugText.text = pieceValue.ToString("F2");
        }

        return overallScore;
    }

    public float EvaluatePiece(Piece piece) {
        float positionValue = 0;

        if(piece.isOut) {
            positionValue = 4;
            return positionValue;
        }
        

        if(piece.isCaptured) { // captured
            positionValue = -2f;
        }
        else {
            if(piece.slot.pieces.Count > 1) { // position score based on where it's good to have a stack
                if(piece.player)
                    positionValue += stackPositionScores[piece.slot.index] / 23f;
                else
                    positionValue += stackPositionScores[23 - piece.slot.index] / 23f;
            }
            
            positionValue += (piece.player ? 23 - piece.slot.index : piece.slot.index) / 23f;
            if(piece.inBase) { // in base = worth more
                positionValue *= 2f;
            }
        }

        int d = piece.player ? -1 : 1; // direction of movement
        int amountBlocked = 0;
        for (int i = 0; (d == 1 ? (i < 6) : (i > -6)); i += d) // places it can move to within 1 dice roll
        {
            int index = piece.slot.index + i;
            if(index > 23 || index < 0)
                continue;

            Slot s = Manager.instance.slots[index];
            if(s.pieces.Count > 0) {
                if(s.pieces[0].player == piece.player) { // one of our pieces
                    if(s.pieces.Count == 1) { // we may be able to close next turn, so slight plus
                        positionValue += 0.05f;
                    }
                    if(piece.slot.pieces.Count == 1) {
                        positionValue += 0.05f;
                    }
                }
                else {
                    if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                        positionValue += 0.2f;
                    }
                    else { // a position is blocked - bad
                        positionValue -= 0.1f;
                        amountBlocked++;
                    }
                }
            }
        }
        bool blocked = (amountBlocked == 6);
        if(blocked) { // this piece can't move
            positionValue -= 0.1f;
        }

        for (int i = 0; (d == 1 ? (i < 12) : (i > -12)); i += d) // places it can move to with combinations of dice
        {
            float val = 0;

            int index = piece.slot.index + i;
            if(index > 23 || index < 0)
                continue;

            Slot s = Manager.instance.slots[index];
            if(s.pieces.Count > 0) {
                if(s.pieces[0].player == piece.player) { // one of or pieces
                    if(s.pieces.Count == 1) { // we may be able to close
                        val += 0.05f;
                    }
                    if(piece.slot.pieces.Count == 1) {
                        val += 0.05f;
                    }
                }
                else {
                    if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                        val += 0.2f;
                    }
                    else { // a position is blocked - bad
                        val -= 0.1f;
                    }
                }
            }
            else {
                val += 0.025f; // empty space that we can move to
            }

            float probabilityMult = 6 - Mathf.Abs(7 - Mathf.Abs(i)); // how many options there are
            probabilityMult /= 6;
            if(blocked && (Mathf.Abs(i) > 6))
                probabilityMult = 0; // we can't get to that square because this piece can't move at all
            
            val *= probabilityMult * 0.5f; // normalised by probability of that combination of dice, * 0.5 because this is less influential than single-dice possibilities
            
            positionValue += val;
        }

        if(piece.slot.pieces.Count == 1) { // we are exposed
            positionValue -= 0.1f;
            float enemyPiecesAheadScore = 0; // score based on enemy pieces that are ahead - especially if they can reach us within 1 dice roll
            for (int i = (d == 1 ? 0 : 23); ((d == 1 ? (i < 23) : (i > 0))); i += d) // loop through all slots ahead of this one
            {
                Slot s = Manager.instance.slots[i];
                if(s.pieces.Count > 0) {
                    if(!s.pieces[0].player == piece.player) {
                        if(piece.slot.pieces.Count == 1) { // it's an exposed one too
                            // currently just neutral/cancel out, but maybe do something based on who's turn it is?
                            continue;
                        }
                        else {
                            int dif = Mathf.Abs(i - piece.slot.index); // distance to that slot from this piece
                            if(dif <= 6) {
                                enemyPiecesAheadScore += 1;
                            }
                            else {
                                enemyPiecesAheadScore += (dif % 6) / 4; // 0-1 based on how many dice rolls it'll take to reach us
                            }
                        }
                    }
                }
            }
            positionValue -= enemyPiecesAheadScore * 0.2f;
        }

        return positionValue;
    }
    

    // same as the above two functions, but based on a GameState object instead of neccessarily the current board
    public float EvaluateGameState(GameState state) {        
        float overallScore = 0;

        foreach(PieceAbstract p in state.allPieces) {
            float pieceValue = EvaluatePiece(p, state);

            if(p.player)
                overallScore += pieceValue; // red
            else
                overallScore -= pieceValue; // white
        }

        return overallScore;
    }

    public float EvaluatePiece(PieceAbstract piece, GameState gameState) {
        float positionValue = 0;

        if(piece.isOut) {
            positionValue = 4;
            return positionValue;
        }
        

        if(piece.isCaptured) { // captured
            positionValue = -2f;
        }
        else {
            if(piece.slot.pieces.Count > 1) { // position score based on where it's good to have a stack
                if(piece.player)
                    positionValue += stackPositionScores[piece.slot.index] / 23f;
                else
                    positionValue += stackPositionScores[23 - piece.slot.index] / 23f;
            }
            
            positionValue += (piece.player ? 23 - piece.slot.index : piece.slot.index) / 23f;
            if(piece.inBase) { // in base = worth more
                positionValue *= 2f;
            }
        }

        int d = piece.player ? -1 : 1; // direction of movement
        int amountBlocked = 0;
        for (int i = 0; (d == 1 ? (i < 6) : (i > -6)); i += d) // places it can move to within 1 dice roll
        {
            int index = piece.slot.index + i;
            if(index > 23 || index < 0)
                continue;

            SlotAbstract s = gameState.slots[index];
            if(s.pieces.Count > 0) {
                if(s.pieces[0].player == piece.player) { // one of our pieces
                    if(s.pieces.Count == 1) { // we may be able to close next turn, so slight plus
                        positionValue += 0.05f;
                    }
                    if(piece.slot.pieces.Count == 1) {
                        positionValue += 0.05f;
                    }
                }
                else {
                    if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                        positionValue += 0.2f;
                    }
                    else { // a position is blocked - bad
                        positionValue -= 0.1f;
                        amountBlocked++;
                    }
                }
            }
        }
        bool blocked = (amountBlocked == 6);
        if(blocked) { // this piece can't move
            positionValue -= 0.1f;
        }

        for (int i = 0; (d == 1 ? (i < 12) : (i > -12)); i += d) // places it can move to with combinations of dice
        {
            float val = 0;

            int index = piece.slot.index + i;
            if(index > 23 || index < 0)
                continue;

            SlotAbstract s = gameState.slots[index];
            if(s.pieces.Count > 0) {
                if(s.pieces[0].player == piece.player) { // one of or pieces
                    if(s.pieces.Count == 1) { // we may be able to close
                        val += 0.05f;
                    }
                    if(piece.slot.pieces.Count == 1) {
                        val += 0.05f;
                    }
                }
                else {
                    if(s.pieces.Count == 1) { // we may be able to capture enemy piece next turn - good!
                        val += 0.2f;
                    }
                    else { // a position is blocked - bad
                        val -= 0.1f;
                    }
                }
            }
            else {
                val += 0.025f; // empty space that we can move to
            }

            float probabilityMult = 6 - Mathf.Abs(7 - Mathf.Abs(i)); // how many options there are
            probabilityMult /= 6;
            if(blocked && (Mathf.Abs(i) > 6))
                probabilityMult = 0; // we can't get to that square because this piece can't move at all
            
            val *= probabilityMult * 0.5f; // normalised by probability of that combination of dice, * 0.5 because this is less influential than single-dice possibilities
            
            positionValue += val;
        }

        if(piece.slot.pieces.Count == 1) { // we are exposed
            positionValue -= 0.1f;
            float enemyPiecesAheadScore = 0; // score based on enemy pieces that are ahead - especially if they can reach us within 1 dice roll
            for (int i = (d == 1 ? 0 : 23); ((d == 1 ? (i < 23) : (i > 0))); i += d) // loop through all slots ahead of this one
            {
                SlotAbstract s = gameState.slots[i];
                if(s.pieces.Count > 0) {
                    if(!s.pieces[0].player == piece.player) {
                        if(piece.slot.pieces.Count == 1) { // it's an exposed one too
                            // currently just neutral/cancel out, but maybe do something based on who's turn it is?
                            continue;
                        }
                        else {
                            int dif = Mathf.Abs(i - piece.slot.index); // distance to that slot from this piece
                            if(dif <= 6) {
                                enemyPiecesAheadScore += 1;
                            }
                            else {
                                enemyPiecesAheadScore += (dif % 6) / 4; // 0-1 based on how many dice rolls it'll take to reach us
                            }
                        }
                    }
                }
            }
            positionValue -= enemyPiecesAheadScore * 0.2f;
        }

        return positionValue;
    }
}
