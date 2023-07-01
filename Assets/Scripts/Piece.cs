using System.Collections;
using System.Collections.Generic;
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

    Image image;
    void Start() {
        image = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        canvasRT = this.transform.root.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
            return;

        Manager.instance.ClearHighlights();
        Debug.Log(LegalMoves().Count);
        foreach(Slot s in LegalMoves()) {
            s.Highlight(true);
        }
        
        slot.pieces.Remove(this);
        this.transform.parent = Manager.instance.topLayerParent;
    }

    public void OnDrag(PointerEventData data)
    {
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
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
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
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

        Debug.Log(closestSlot.index);

        if(LegalMoves().Contains(closestSlot)) { // if moved to a legal slot, move
            // see which dice was used and set it as used
            int dice1 = Manager.instance.dice1;
            int dice2 = Manager.instance.dice2;

            if(player) { // since red moves counter-clockwise, moves need to be going down in index rather than up
                dice1 = -dice1;
                dice2 = -dice2;
            }

            if(closestSlot.index == slot.index + dice1)
                Manager.instance.dice1Used = true;
            if(closestSlot.index == slot.index + dice2)
                Manager.instance.dice2Used = true;

            
            closestSlot.AddPiece(this);
        }
        
        // if not a legal move, go back to starting slot
        slot.AddPiece(this);
        
        Debug.Log(slot.index);

        Manager.instance.HighlightLegalMoves();

        if(Manager.instance.dice1Used && Manager.instance.dice2Used) {
            Manager.instance.NextTurn();
        }
    }

    public List<Slot> LegalMoves() {
        List<Slot> moves = new List<Slot>();

        // I will have to figure out how to figure out if one dice has already been used
        // I don't think I will allow undoing moves as it's too much hassle
        // Anyways, if you make a move you need to undo you're just bad + L + ratio + why are you reading code comments instead of doing something productive
        int dice1 = Manager.instance.dice1;
        int dice2 = Manager.instance.dice2;

        if(player) { // since red moves counter-clockwise, moves need to be going down in index rather than up
            dice1 = -dice1;
            dice2 = -dice2;
        }

        // check first dice moves
        if(!Manager.instance.dice1Used && !(slot.index + dice1 < 0)) {
            if(!(slot.index + dice1 > 23)) {
                Slot s = Manager.instance.slots[slot.index + dice1];
                if(s.pieces.Count > 0) { // check for other player's pieces on that slot
                    if(s.pieces[0].player != this.player) {
                        if(s.pieces.Count <= 1) { // if there's more than 1, we can't move there
                            moves.Add(s); // we can move to this slot and capture
                        }
                    }
                    else {
                        moves.Add(s); // there's pieces on there but they belong to us so we can move there
                    }
                }
                else {
                    moves.Add(s); // it's a free slot we can move to
                }
            }
        }
        
        // check second dice moves
        if(!Manager.instance.dice2Used && !(slot.index + dice2 < 0)) {
            if(!(slot.index + dice2 > 23)) {
                Slot s = Manager.instance.slots[slot.index + dice2];
                if(s.pieces.Count > 0) { // check for other player's pieces on that slot
                    if(s.pieces[0].player != this.player) {
                        if(s.pieces.Count <= 1) { // if there's more than 1, we can't move there
                            moves.Add(s); // we can move to this slot and capture
                        }
                    }
                    else {
                        moves.Add(s); // there's pieces on there but they belong to us so we can move there
                    }
                }
                else {
                    moves.Add(s); // it's a free slot we can move to
                }
            }
        }

        return moves;
    }

}
