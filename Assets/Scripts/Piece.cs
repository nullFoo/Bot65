using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Piece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    bool _player = false;
    public bool player {
        get { return _player; }
        set {
            if(image == null)
                image = GetComponent<Image>();
            image.color = value ? Color.red : Color.white;
            _player = value;
        }
    }

    public RectTransform canvasRT;
    RectTransform rt;

    public Slot slot;

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
    bool canOut {
        get {
            if(player)
                return Manager.instance.RedCanOut;
            else
                return Manager.instance.WhiteCanOut;
        }
    }

    Image image;
    void Start() {
        image = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        canvasRT = this.transform.root.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(Manager.instance.whoseTurn != player || !Manager.instance.game) // can't move pieces if it's not our turn
            return;

        Manager.instance.ClearHighlights();
        foreach((Slot, int) s in LegalMoves()) {
            s.Item1.Highlight(true);
        }
        
        slot.pieces.Remove(this);
        this.transform.parent = Manager.instance.topLayerParent;
    }

    public void OnDrag(PointerEventData data)
    {
        if(Manager.instance.whoseTurn != player || !Manager.instance.game) // can't move pieces if it's not our turn
            return;

        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRT, data.position, data.pressEventCamera, out globalMousePos))
        {
            rt.position = globalMousePos;
            rt.rotation = canvasRT.rotation;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(Manager.instance.whoseTurn != player || !Manager.instance.game) // can't move pieces if it's not our turn
            return;
        
        Slot closestSlot = slot;
        float closestDist = Mathf.Infinity;
        foreach (Slot s in Manager.instance.slots)
        {
            float dist = Vector2.Distance(s.transform.position, this.transform.position);
            if(dist < closestDist) {
                closestDist = dist;
                closestSlot = s;
            }
        }

        if(TupleHasItem1(LegalMoves(), closestSlot)) { // if moved to a legal slot, move
            // see which dice was used and set it as used
            int diceNum = GetItemFromSlot(LegalMoves(), closestSlot).Item2; 
            if(player)
                diceNum = -diceNum;
            Manager.instance.diceRolls.Remove(diceNum); // will remove the first instance of num in the dice list
            
            if(closestSlot.pieces.Count == 1) { // check for capturable pieces
                if(closestSlot.pieces[0].player != this.player) {
                    Manager.instance.CapturePiece(closestSlot.pieces[0]);
                }
            }

            if(isCaptured) {
                isCaptured = false;
                Manager.instance.player1Captured.Remove(this);
                Manager.instance.player2Captured.Remove(this);
            }

            if(closestSlot.index > 900) { // the out slot
                isOut = true;
                if(player)
                    Manager.instance.player2Out.Add(this);
                else
                    Manager.instance.player1Out.Add(this);
            }

            closestSlot.AddPiece(this);
        }
        
        // if not a legal move, go back to starting slot
        slot.AddPiece(this);
        
        Manager.instance.PieceMoved();
    }

    bool TupleHasItem1(List<(Slot, int)> list, Slot item1) {
        return list.Any(m => m.Item1 == item1);
    }
    bool TupleHasItem2(List<(Slot, int)> list, int item2) {
        return list.Any(m => m.Item2 == item2);
    }
    (Slot, int) GetItemFromSlot(List<(Slot, int)> list, Slot item1) {
        return list.Where(m => m.Item1  == item1).First();
    }
    (Slot, int) GetItemFromDice(List<(Slot, int)> list, int item2) {
        return list.Where(m => m.Item2  == item2).First();
    }

    public List<(Slot, int)> LegalMoves() {
        List<(Slot, int)> moves = new List<(Slot, int)>();

        if(!Manager.instance.game) // if the game is over or not started, can't move anything
            return moves;

        List<Piece> capturedCheck = this.player ? Manager.instance.player2Captured : Manager.instance.player1Captured;
        if(capturedCheck.Count > 0 && !isCaptured) // if this player has a piece captured and it's not this one, we can't move
            return moves;

        if(isOut) // out pieces are done, no moving
            return moves;

        List<int> diceRolls = new List<int>(Manager.instance.diceRolls);
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
                Slot s = Manager.instance.slots[slot.index + diceRoll];
                if(s.pieces.Count > 0) { // check for other player's pieces on that slot
                    if(s.pieces[0].player != this.player) {
                        if(s.pieces.Count <= 1) { // if there's more than 1, we can't move there
                            moves.Add((s, diceRoll)); // we can move to this slot and capture
                        }
                    }
                    else {
                        moves.Add((s, diceRoll)); // there's pieces on there but they belong to us so we can move there
                    }
                }
                else {
                    moves.Add((s, diceRoll)); // it's a free slot we can move to
                }
            }

            if(canOut) {
                bool legalOut = false;
                // todo: when less than dice roll, but no occupied slots higher - could loop up from current slot index
                if(player) {
                    legalOut = slot.index == -diceRoll;
                    if(!legalOut) {
                        if(-diceRoll > slot.index) {
                            // check if there's any pieces above us
                            for (int i = slot.index + 1; i <= 6; i++)
                            {
                                if(Manager.instance.slots[i].pieces.Count > 0) {
                                    if(Manager.instance.slots[i].pieces[0].player == this.player) {
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
                                if(Manager.instance.slots[i].pieces.Count > 0) {
                                    if(Manager.instance.slots[i].pieces[0].player == this.player) {
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
                    Slot s = player ? Manager.instance.redOutSlot : Manager.instance.whiteOutSlot;
                    moves.Add((s, diceRoll));
                }
            }
        }

        return moves;
    }

}
