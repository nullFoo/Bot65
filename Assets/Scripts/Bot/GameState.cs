using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Bot;

public class GameState
{
    public class SlotAbstract {
        public int index;
        public List<PieceAbstract> pieces;

        public SlotAbstract(Slot template, bool includePieces, GameState gameState) { // create this abstract class from a slot game object
            this.index = template.index;

            pieces = new List<PieceAbstract>();
            if(includePieces) {
                foreach(Piece piece in template.pieces) {
                    this.pieces.Add(new PieceAbstract(piece, gameState, this));
                }
            }
        }

        public SlotAbstract(SlotAbstract copy, bool includePieces, GameState gameState) {
            this.index = copy.index;

            pieces = new List<PieceAbstract>();
            if(includePieces) {
                foreach(PieceAbstract piece in copy.pieces) {
                    this.pieces.Add(new PieceAbstract(piece, gameState, this));
                }
            }
        }

        public void AddPiece(PieceAbstract p) {
            if(!pieces.Contains(p))
                pieces.Add(p);
            p.slot = this;
        }
    }
    public class PieceAbstract {
        public bool player;
        public SlotAbstract slot;
        public bool isOut;
        public bool isCaptured;
        
        public bool inBase {
            get {
                if(isOut)
                    return true;

                if(slot == null)
                    return false;

                if(player)
                    return slot.index <= 6;
                else
                    return slot.index >= 18;
            }
        }

        public bool canOut {
            get {
                if(player)
                    return gameState.RedCanOut;
                else
                    return gameState.WhiteCanOut;
            }
        }

        public GameState gameState;

        public PieceAbstract(Piece template, GameState gameState, SlotAbstract slot = null) { // create this abstract class from a piece game object
            this.player = template.player;
            this.isOut = template.isOut;
            this.isCaptured = template.isCaptured;

            if(slot == null) {
                this.slot = new SlotAbstract(template.slot, false, gameState);
            }
            else {
                this.slot = slot;
            }

            this.gameState = gameState;
        }

        public PieceAbstract(PieceAbstract copy, GameState gameState, SlotAbstract slot = null) {
            this.player = copy.player;
            this.isOut = copy.isOut;
            this.isCaptured = copy.isCaptured;

            if(slot == null) {
                this.slot = new SlotAbstract(copy.slot, false, gameState);
            }
            else {
                this.slot = slot;
            }

            this.gameState = gameState;
        }

        public List<Move> LegalMoves() {
            List<Move> moves = new List<Move>();

            List<PieceAbstract> capturedCheck = this.player ? gameState.redPiecesCaptured : gameState.whitePiecesCaptured;
            if(capturedCheck.Count > 0 && !isCaptured) // if this player has a piece captured and it's not this one, we can't move
                return moves;

            if(isOut) // out pieces are done, no moving
                return moves;

            List<int> diceRolls = new List<int>(gameState.diceRolls);
            if(player) { // since red moves counter-clockwise, moves need to be going down in index rather than up
                for (int i = 0; i < diceRolls.Count; i++)
                {
                    diceRolls[i] = -diceRolls[i];
                }
            }

            // check dice moves
            foreach (int diceRoll in diceRolls)
            {
                if(!(slot.index + diceRoll > 23) && !(slot.index + diceRoll < 0)) {
                    SlotAbstract s = gameState.slots[slot.index + diceRoll];
                    if(s.pieces.Count > 0) { // check for other player's pieces on that slot
                        if(s.pieces[0].player != this.player) {
                            if(s.pieces.Count <= 1) { // if there's more than 1, we can't move there
                                moves.Add(new Move(gameState, this, s, diceRoll)); // we can move to this slot and capture
                            }
                        }
                        else {
                            moves.Add(new Move(gameState, this, s, diceRoll)); // there's pieces on there but they belong to us so we can move there
                        }
                    }
                    else {
                        moves.Add(new Move(gameState, this, s, diceRoll)); // it's a free slot we can move to
                    }
                }

                if(canOut) {
                    bool legalOut = false;
                    // todo: when less than dice roll, but no occupied slots higher - could loop up from current slot index
                    if(player) {
                        legalOut = slot.index == -diceRoll;
                        if(!legalOut) {
                            if(-diceRoll > slot.index) {
                                Debug.Log((-diceRoll) + " > " + slot.index);
                                // check if there's any pieces above us
                                for (int i = slot.index + 1; i <= 6; i++)
                                {
                                    Debug.Log("checking" + i);
                                    if(gameState.slots[i].pieces.Count > 0) {
                                        if(gameState.slots[i].pieces[0].player == this.player) {
                                            break;
                                        }
                                    }
                                }
                                legalOut = true; // if there aren't, we can get this piece out
                            }
                        }
                        
                    }
                    else {
                        legalOut = slot.index - 24 == -diceRoll;
                        if(!legalOut) {
                            if(-diceRoll < slot.index - 24) {
                                Debug.Log((-diceRoll) + " < " + (slot.index - 24));

                                // check if there's any pieces above us
                                bool arePieces = false;
                                Debug.Log(slot.index - 1);
                                for (int i = slot.index - 1; i >= 18; i--)
                                {
                                    Debug.Log("checking" + i);
                                    if(gameState.slots[i].pieces.Count > 0) {
                                        if(gameState.slots[i].pieces[0].player == this.player) {
                                            Debug.Log(i + "has our pieces on it");
                                            arePieces = true;
                                            break;
                                        }
                                    }
                                }
                                legalOut = !arePieces; // if there aren't, we can get this piece out
                                Debug.Log(legalOut);
                        }

                        }
                        
                    }

                    if(legalOut) {
                        SlotAbstract s = player ? gameState.outSlotRed : gameState.outSlotWhite;
                        moves.Add(new Move(gameState, this, s, diceRoll));
                    }
                }
            }

            return moves;
        }
    }

    public SlotAbstract[] slots;
    public List<PieceAbstract> allPieces;

    public SlotAbstract capturedSlotRed;
    public SlotAbstract capturedSlotWhite;

    public SlotAbstract outSlotRed;
    public SlotAbstract outSlotWhite;

    public List<PieceAbstract> redPiecesCaptured;
    public List<PieceAbstract> redPiecesOut;
    public List<PieceAbstract> whitePiecesCaptured;
    public List<PieceAbstract> whitePiecesOut;

    public List<int> diceRolls;
    
    public bool WhiteCanOut {
        get {
            foreach (PieceAbstract p in allPieces)
            {
                if(!p.player) {
                    if(!p.inBase) { // if any of them aren't in the base, we can't
                        return false;
                    }
                }
            }
            return true; // if we haven't come across one that isn't, they must all be
        }
    }
    public bool RedCanOut {
        get {
            foreach (PieceAbstract p in allPieces)
            {
                if(p.player) {
                    if(!p.inBase) { // if any of them aren't in the base, we can't
                        return false;
                    }
                }
            }
            return true; // if we haven't come across one that isn't, they must all be
        }
    }

    public GameState() {
        this.allPieces = new List<PieceAbstract>();

        this.redPiecesCaptured = new List<PieceAbstract>();
        this.redPiecesOut = new List<PieceAbstract>();
        this.whitePiecesCaptured = new List<PieceAbstract>();
        this.whitePiecesOut = new List<PieceAbstract>();

        this.diceRolls = new List<int>();
    }

    public GameState(GameState copy) {
        
        this.allPieces = new List<PieceAbstract>();
        this.redPiecesCaptured = new List<PieceAbstract>();
        this.redPiecesOut = new List<PieceAbstract>();
        this.whitePiecesCaptured = new List<PieceAbstract>();
        this.whitePiecesOut = new List<PieceAbstract>();

        this.slots = new SlotAbstract[copy.slots.Length];
        for (int i = 0; i < copy.slots.Length; i++)
        {
            this.slots[i] = new SlotAbstract(copy.slots[i], true, this);
        }
        foreach(SlotAbstract slot in this.slots) {
            foreach(PieceAbstract piece in slot.pieces) {
                this.allPieces.Add(piece);
                if(piece.isOut) {
                    List<PieceAbstract> listToAdd = piece.player ? this.redPiecesOut : this.whitePiecesOut; // first time that list = being reference instead of copy is actually useful instead of a hinderance
                    listToAdd.Add(piece);
                }
            }
        }

        this.outSlotWhite = this.slots.First(s => s.index == copy.outSlotWhite.index);
        this.outSlotRed = this.slots.First(s => s.index == copy.outSlotRed.index);
        this.capturedSlotWhite = new SlotAbstract(copy.capturedSlotWhite, true, this);
        foreach(PieceAbstract piece in this.capturedSlotWhite.pieces) {
            this.whitePiecesCaptured.Add(piece);
        }
        this.capturedSlotRed = new SlotAbstract(copy.capturedSlotRed, true, this);
        foreach(PieceAbstract piece in this.capturedSlotRed.pieces) {
            this.redPiecesCaptured.Add(piece);
        }

        this.diceRolls = new List<int>(copy.diceRolls);
    }

    public void MovePiece(PieceAbstract piece, SlotAbstract newSlot) {
        piece.slot.pieces.Remove(piece);
        newSlot.AddPiece(piece);
        
        if(newSlot.pieces.Count == 1) { // check for capturable pieces
            if(newSlot.pieces[0].player != piece.player) {
                CapturePiece(newSlot.pieces[0]);
            }
        }
    }

    public void CapturePiece(PieceAbstract p) {
        if(p.slot != null)
            p.slot.pieces.Remove(p);
        if(p.player) {
            capturedSlotRed.AddPiece(p);
            redPiecesCaptured.Add(p);
        }
        else {
            capturedSlotWhite.AddPiece(p);
            whitePiecesCaptured.Add(p);
        }

        p.isCaptured = true;
    }

    public List<Move> GetAllLegalMoves() {
        List<Move> moves = new List<Move>();
        foreach(PieceAbstract piece in allPieces) {
            Debug.Log(piece.slot.index);
            List<Move> movesForThisPiece = piece.LegalMoves();
            if(movesForThisPiece.Count > 0) {
                Debug.Log(movesForThisPiece[0].moveNumber);
                Debug.Log(movesForThisPiece[0].piece.slot.index);
                moves.AddRange(movesForThisPiece);
            }
        }
        return moves;
    }


    public static GameState GameStateFromCurrentBoard() {
        GameState game = new GameState();

        Manager m = Manager.instance;
        
        game.slots = new SlotAbstract[m.slots.Length];
        for (int i = 0; i < m.slots.Length; i++)
        {
            game.slots[i] = new SlotAbstract(m.slots[i], true, game);
        }
        foreach(SlotAbstract slot in game.slots) {
            foreach(PieceAbstract piece in slot.pieces) {
                game.allPieces.Add(piece);
                if(piece.isOut) {
                    List<PieceAbstract> listToAdd = piece.player ? game.redPiecesOut : game.whitePiecesOut; // first time that list = being reference instead of copy is actually useful instead of a hinderance
                    listToAdd.Add(piece);
                }
            }
        }

        game.outSlotWhite = game.slots.First(s => s.index == m.whiteOutSlot.index);
        game.outSlotRed = game.slots.First(s => s.index == m.redOutSlot.index);
        game.capturedSlotWhite = new SlotAbstract(m.whiteCaptured, true, game);
        foreach(PieceAbstract piece in game.capturedSlotWhite.pieces) {
            game.whitePiecesCaptured.Add(piece);
        }
        game.capturedSlotRed = new SlotAbstract(m.redCaptured, true, game);
        foreach(PieceAbstract piece in game.capturedSlotRed.pieces) {
            game.redPiecesCaptured.Add(piece);
        }

        game.diceRolls = new List<int>(m.diceRolls);

        return game;
    }
}
