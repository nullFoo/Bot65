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

    public struct Move {
        public Move(Piece p, Slot s, int dice) {
            piece = p;
            targetSlot = s;
            moveNumber = dice;
            gameEvaluationAfterMove = -10000;
        }

        public Piece piece;
        public Slot targetSlot;
        public int moveNumber;
        public float gameEvaluationAfterMove;
    }

    public void DebugFuncTemp() {
        List<Move> possibleMoves = GetPossibleMoves(Manager.instance.diceRolls);
        Move m = GetBestMove(possibleMoves);
        Debug.Log(m);
        Debug.Log(Manager.instance.diceRolls.Count);
        Manager.instance.diceRolls.Remove(Mathf.Abs(m.moveNumber));
        Debug.Log(Manager.instance.diceRolls.Count);
        m.piece.MoveTo(m.targetSlot);
    }

    // problem: we want to get the best combination of 2 (or 4, for double) moves - at the moment, we are just getting the best singular move
    public Move GetBestMove(List<Move> possibleMoves) {
        if(possibleMoves.Count <= 0)
            return new Move(null, null, 0);
        
        Move best = possibleMoves[0];

        List<Move> copyList = new List<Move>(possibleMoves);
        for (int i = 0; i < copyList.Count; i++)
        {
            Move m = possibleMoves[i];
            
            Slot start = m.piece.slot; // so we can return it after calculating - todo: allow evaluation for moves that aren't the current state
            m.piece.MoveTo(m.targetSlot);
            float eval = EvaluateGameState();
            m.gameEvaluationAfterMove = eval;
            if(eval > best.gameEvaluationAfterMove)
                best = m;
            m.piece.MoveTo(start);
        }

        return best;
    }

    // problem as mentioned above
    public List<Move> GetPossibleMoves(List<int> _diceRolls) {
        // this may have to be split across several frames

        List<Move> moves = new List<Move>();
        
        List<int> diceRolls = new List<int>(_diceRolls);
        for (int i = 0; i < diceRolls.Count; i++)
        {
            diceRolls[i] = -diceRolls[i];
        }

        foreach(Piece p in GameObject.FindObjectsOfType<Piece>()) {
            if(p.player) { // the bot can only move red pieces
                foreach((Slot, int) m in p.LegalMoves()) {
                    Move move = new Move(p, m.Item1, m.Item2);
                    moves.Add(move);
                }
            }
        }

        return moves;
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
}
