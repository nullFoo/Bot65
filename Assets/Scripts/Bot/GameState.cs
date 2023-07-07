using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameState
{
    public class SlotAbstract {
        public int index;
        public List<PieceAbstract> pieces;

        public SlotAbstract(Slot template, bool includePieces) { // create this abstract class from a slot game object
            this.index = template.index;

            pieces = new List<PieceAbstract>();
            if(includePieces) {
                foreach(Piece piece in template.pieces) {
                    this.pieces.Add(new PieceAbstract(piece, this));
                }
            }
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

        public PieceAbstract(Piece template, SlotAbstract slot = null) { // create this abstract class from a piece game object
            this.player = template.player;
            this.isOut = template.isOut;
            this.isCaptured = template.isCaptured;
            if(slot == null) {
                this.slot = new SlotAbstract(template.slot, false);
            }
            else {
                this.slot = slot;
            }
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

    public GameState() {
        this.allPieces = new List<PieceAbstract>();

        this.redPiecesCaptured = new List<PieceAbstract>();
        this.redPiecesOut = new List<PieceAbstract>();
        this.whitePiecesCaptured = new List<PieceAbstract>();
        this.whitePiecesOut = new List<PieceAbstract>();

        this.diceRolls = new List<int>();
    }

    public static GameState GameStateFromCurrentBoard() {
        GameState game = new GameState();

        Manager m = Manager.instance;
        
        game.slots = new SlotAbstract[m.slots.Length];
        for (int i = 0; i < m.slots.Length; i++)
        {
            game.slots[i] = new SlotAbstract(m.slots[i], true);
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
        game.capturedSlotWhite = new SlotAbstract(m.whiteCaptured, true);
        foreach(PieceAbstract piece in game.capturedSlotWhite.pieces) {
            game.whitePiecesCaptured.Add(piece);
        }
        game.capturedSlotRed = new SlotAbstract(m.redCaptured, true);
        foreach(PieceAbstract piece in game.capturedSlotRed.pieces) {
            game.redPiecesCaptured.Add(piece);
        }

        game.diceRolls = m.diceRolls;

        return game;
    }
}
