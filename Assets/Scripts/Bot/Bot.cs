using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static GameState;

public class Bot : MonoBehaviour
{
    public static Bot instance;

    public bool isPlayingRed = true;

    [SerializeField] TextMeshProUGUI debugTextEvaluation;

    float[] stackPositionScores = new float[]
    {9.5f, 9.4f, 9.3f, 9.2f, 9.1f, 9f, 8.5f, 6, 5, 5, 5, 5, 3, 3, 3, 3, 3, 5, -1, -1, -1, -1, -1, -1};
    // todo: different scores at different points in the game (like when you're in the base)

    List<Move> movesToPlay = new List<Move>(); // the list of calculated moves, for the bot to play through slowly instead of instantly so the player can see what's going on
    bool hasCombo; // if false, we're doing individual moves

    [SerializeField] Slider botPlaySpeed;
    float playDelay = 0.5f;

    void Awake() {
        instance = this;
    }

    public void UpdateDebugText() {
        GameState currentGameState = GameState.GameStateFromCurrentBoard();

        // float throwawayDebug = EvaluateGameState(currentGameState, true);
        float evaluation = EvaluateGameState(); // use this function instead, so it updates debug texts properly

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

    public void BotsTurn() {
        playDelay = 2 / botPlaySpeed.value;

        movesToPlay = new List<Move>();

        if(Manager.instance.whoseTurn != isPlayingRed) // if this function has somehow been called when it isn't the bot's turn, ignore
            return;
        
        GameState currentGameState = GameState.GameStateFromCurrentBoard();
        List<Move> legalMoves = currentGameState.GetAllLegalMoves(isPlayingRed);

        if(legalMoves.Count == 0) {
            Debug.Log("no legal moves");
            Invoke("PlayMove", playDelay);
            hasCombo = false;
            return;
        }

        hasCombo = true;

        List<List<Move>> moveCombos = GetAllMoveCombinations(currentGameState);

        if(moveCombos.Count == 0) {
            // something failed with the move combo checking, just do best individual move instead
            Debug.Log("something failed with checking move combos this turn, just doing best individual moves instead");
            
            hasCombo = false;
        }
        else {
            List<Move> best = moveCombos[0];
            float bestEval = -1000;
            foreach(List<Move> m in moveCombos) {
                // if(m.Count == 0)
                //     continue;
                float eval = EvaluateGameState(m[m.Count - 1].after); // game state after the end of the combination
                if(!isPlayingRed)
                    eval = -eval; // since the evaluation is positive for red and negative for white, flip that if we are white

                if(eval > bestEval) {
                    bestEval = eval;
                    best = m;
                }
            }

            movesToPlay.AddRange(best);
        }

        Invoke("PlayMove", playDelay);
    }
    public void PlayMove() {
        if(Manager.instance.whoseTurn != isPlayingRed)
            return;
        if(Manager.instance.diceRolls.Count == 0)
            return;

        if(hasCombo) {
            if(movesToPlay.Count == 0)
                return;
            DoMoveInGame(movesToPlay[0]);
            movesToPlay.RemoveAt(0);
        }
        else {
            Debug.Log("no combo, doing best singular move");
            Debug.Log(Manager.instance.diceRolls.Count);
            GameState currentGameState = GameState.GameStateFromCurrentBoard();
            List<Move> legalMoves = currentGameState.GetAllLegalMoves(isPlayingRed);
            
            if(legalMoves.Count == 0) {
                Debug.Log("no legal moves");
                Manager.instance.NextTurn(); // pass turn
                return;
            }

            Move best = legalMoves[0];
            float bestEval = -1000;
            foreach(Move m in legalMoves) {
                float eval = EvaluateGameState(m.after); // game state after the end of the combination
                if(!isPlayingRed)
                    eval = -eval; // since the evaluation is positive for red and negative for white, flip that if we are white

                if(eval > bestEval) {
                    bestEval = eval;
                    best = m;
                }
            }

            DoMoveInGame(best);
        }

        Invoke("PlayMove", playDelay);
    }

    public void DebugFuncTemp() { // called from the "bot" button in debug ui
        BotsTurn();
        return;
        
        isPlayingRed = Manager.instance.whoseTurn;

        GameState currentGameState = GameState.GameStateFromCurrentBoard();
        List<Move> legalMoves = currentGameState.GetAllLegalMoves(isPlayingRed);

        if(legalMoves.Count == 0) {
            // pass turn
            Debug.Log("no legal moves");
            return;
        }

        List<List<Move>> moveCombos = GetAllMoveCombinations(currentGameState);

        if(moveCombos.Count == 0) {
            // something failed with the move combo checking, just do best individual move instead
            Debug.Log("something failed with checking move combos, just doing best individual moves instead");
            Move best = legalMoves[0];
            float bestEval = -1000;
            foreach(Move m in legalMoves) {
                // if(m.Count == 0)
                //     continue;
                float eval = EvaluateGameState(m.after); // game state after the end of the combination
                if(!isPlayingRed)
                    eval = -eval; // since the evaluation is positive for red and negative for white, flip that if we are white

                if(eval > bestEval) {
                    bestEval = eval;
                    best = m;
                }
            }

            DoMoveInGame(best);

            return;
        }
        else {
            List<Move> best = moveCombos[0];
            float bestEval = -1000;
            foreach(List<Move> m in moveCombos) {
                // if(m.Count == 0)
                //     continue;
                float eval = EvaluateGameState(m[m.Count - 1].after); // game state after the end of the combination
                if(!isPlayingRed)
                    eval = -eval; // since the evaluation is positive for red and negative for white, flip that if we are white

                if(eval > bestEval) {
                    bestEval = eval;
                    best = m;
                }
            }

            DoMovesInGame(best);
        }

    }

    public void DoMovesInGame(List<Move> moveCombo) { // find the actual Piece and Slot objects from the move combo and do it
        if(moveCombo.Count == 0 || moveCombo.Count > 4) {
            Debug.Log("Something probably went wrong with the move combo checking, combination has a count of " + moveCombo.Count);
            return;
        }
        foreach(Move move in moveCombo) {
            Slot slot = Manager.instance.slots.First(s => s.index == move.targetSlot.index);
            if(slot == null) {
                return;
            }
            Piece piece = Manager.instance.allPieces.First(p => p.slot.index == move.piece.slot.index); // pieces on the same slot are functionally identical, so just the first one that's on the same slot
            if(piece == null) {
                return;
            }

            Manager.instance.diceRolls.Remove(Mathf.Abs(move.moveNumber));
            piece.MoveTo(slot);
        }
    }
    public void DoMoveInGame(Move move) { // find the actual Piece and Slot objects from the move and do it

        Slot slot = Manager.instance.slots.First(s => s.index == move.targetSlot.index);
        if(slot == null) {
            return;
        }
        Piece piece = Manager.instance.allPieces.First(p => p.slot.index == move.piece.slot.index); // pieces on the same slot are functionally identical, so just the first one that's on the same slot
        if(piece == null) {
            return;
        }

        Manager.instance.diceRolls.Remove(Mathf.Abs(move.moveNumber));
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
            PieceAbstract newPiece = after.allPieces.First(pi => pi.slot.index == p.slot.index);
            SlotAbstract newSlot = after.slots.First(sl => sl.index == s.index);
            after.MovePiece(newPiece, newSlot);
            after.diceRolls.Remove(Mathf.Abs(moveNumber));
        }
    }

    List<List<Move>> GetAllMoveCombinations(GameState startState) {
        List<List<Move>> allCombinations = new List<List<Move>>(); // this will be referenced, not copied, allowing us to add to it from the recursive function
        GenerateMoveCombination(startState, new List<Move>(), allCombinations);
        return allCombinations;
    }

    void GenerateMoveCombination(GameState gameState, List<Move> currentCombination, List<List<Move>> allCombinations) {
        if(gameState.diceRolls.Count == 0 && currentCombination.Count > 0) { // gone through all dice rolls, combination done
            allCombinations.Add(currentCombination); // add it to the overall list
            return;
        }

        List<Move> legalMoves = new List<Move>(gameState.GetAllLegalMoves(this.isPlayingRed));
        foreach(Move move in legalMoves) {
            List<Move> current = new List<Move>(currentCombination); // make a copy so it doesn't affect the other moves
            current.Add(move); // add this move to the current combinations

            if(move.after.diceRolls.Count == 0) { // gone through all dice rolls, combination done
                allCombinations.Add(current); // add it to the overall list
                continue;
            }

            GenerateMoveCombination(move.after, current, allCombinations); // continue the recursive search
        }
    }
    
    // the bot will be playing red
    public float EvaluateGameState() {        
        float overallScore = 0;

        // scores for each piece
        foreach(Piece p in GameObject.FindObjectsOfType<Piece>()) {
            float pieceValue = EvaluatePiece(p);

            if(p.player)
                overallScore += pieceValue; // red
            else
                overallScore -= pieceValue; // white

                
            p.debugText.text = pieceValue.ToString("F2");
        }
        // more: score based on how many stacks each player has in their base
        float redStacksScore = 0;
        for (int i = 0; i <= 6; i++) // red base
        {
            Slot s = Manager.instance.slots[i];
            if(s.pieces.Count > 1) { // it's a stack
                if(s.pieces[0].player) {
                    redStacksScore++;
                }
            }
        }
        float whiteStacksScore = 0;
        for (int i = 18; i <= 23; i++) // white base
        {
            Slot s = Manager.instance.slots[i];
            if(s.pieces.Count > 1) { // it's a stack
                if(!s.pieces[0].player) {
                    whiteStacksScore++;
                }
            }
        }
        overallScore += redStacksScore;
        overallScore -= whiteStacksScore;

        return overallScore;
    }

    public float EvaluatePiece(Piece piece) {
        float positionValue = 0;

        if(piece.isOut) {
            positionValue = 4;
            return positionValue;
        }
        

        if(piece.isCaptured) { // captured
            positionValue = -2.5f;
        }
        else {
            // score based on proximity to end
            positionValue += (piece.player ? 23 - piece.slot.index : piece.slot.index) / 23f;
            if(piece.inBase) { // in base = worth more
                positionValue *= 1.5f;
            }
            
            if(piece.slot.pieces.Count > 1) { // score based on where it's good to have a stack
                positionValue += 0.1f; // good to be in a stack
                if(piece.player)
                    positionValue += stackPositionScores[piece.slot.index] / 23f;
                else
                    positionValue += stackPositionScores[23 - piece.slot.index] / 23f;
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
            if((piece.player && positionValue > 17) || positionValue < 6) { // in/near enemy base
                positionValue -= 0.05f; // it's not that big of a risk, we won't get set back far
            }
            else
                positionValue -= 0.2f;
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
    public float EvaluateGameState(GameState state, bool isCurrent = false) {        
        float overallScore = 0;

        foreach(PieceAbstract p in state.allPieces) {
            if(isCurrent)
                Debug.Log(p.isCaptured);
            float pieceValue = EvaluatePiece(p, state);

            if(p.player)
                overallScore += pieceValue; // red
            else
                overallScore -= pieceValue; // white
        }
        // more: score based on how many stacks each player has in their base
        float redStacksScore = 0;
        for (int i = 0; i <= 6; i++) // red base
        {
            SlotAbstract s = state.slots[i];
            if(s.pieces.Count > 1) { // it's a stack
                if(s.pieces[0].player) {
                    redStacksScore++;
                }
            }
        }
        float whiteStacksScore = 0;
        for (int i = 18; i <= 23; i++) // white base
        {
            SlotAbstract s = state.slots[i];
            if(s.pieces.Count > 1) { // it's a stack
                if(!s.pieces[0].player) {
                    whiteStacksScore++;
                }
            }
        }
        overallScore += redStacksScore;
        overallScore -= whiteStacksScore;

        return overallScore;
    }

    public float EvaluatePiece(PieceAbstract piece, GameState gameState) {
        float positionValue = 0;

        if(piece.isOut) {
            positionValue = 4;
            return positionValue;
        }
        

        if(piece.isCaptured) { // captured
            positionValue = -2.5f; // 500 is temporary for debugging, should be -2.5
        }
        else {
            // score based on proximity to end
            positionValue += (piece.player ? 23 - piece.slot.index : piece.slot.index) / 23f;
            if(piece.inBase) { // in base = worth more
                positionValue *= 1.5f;
            }
            
            if(piece.slot.pieces.Count > 1) { // score based on where it's good to have a stack
                positionValue += 0.1f; // good to be in a stack
                if(piece.player)
                    positionValue += stackPositionScores[piece.slot.index] / 23f;
                else
                    positionValue += stackPositionScores[23 - piece.slot.index] / 23f;
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

        if(piece.slot.pieces.Count == 1 && !piece.isCaptured) { // we are exposed
            if((piece.player && positionValue > 17) || positionValue < 6) { // in/near enemy base
                positionValue -= 0.05f; // it's not that big of a risk, we won't get set back far
            }
            else
                positionValue -= 0.2f;
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
