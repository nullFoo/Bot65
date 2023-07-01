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

        if(closestSlot.pieces.Count > 0) { // check for other player's pieces on that slot
            if(closestSlot.pieces[0].player != this.player) {
                if(closestSlot.pieces.Count > 1) {
                    // this isn't a legal move, go back to starting slot
                    slot.AddPiece(this);
                    return;
                }
                Manager.instance.CapturePiece(closestSlot.pieces[0]);
            }
        }

        closestSlot.AddPiece(this);
        Debug.Log(slot.index);
    }

    public List<Slot> LegalMoves() {
        List<Slot> moves = new List<Slot>();

        // I will have to figure out how to figure out if one dice has already been used
        // I don't think I will allow undoing moves as it's too much hassle
        // Anyways, if you make a move you need to undo you're just bad + L + ratio + why are you reading code comments instead of doing something productive
        int dice1 = Manager.instance.dice1;
        int dice2 = Manager.instance.dice2;


    }

}
