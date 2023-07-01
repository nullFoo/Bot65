using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public int index;
    public List<Piece> pieces;
    Transform piecesParent;

    GameObject highlight;

    void Start() {
        pieces = new List<Piece>();
        piecesParent = this.transform.GetChild(1);
        highlight = this.transform.GetChild(0).GetChild(0).gameObject; // I know this is bad practice, I don't care
    }

    public void Highlight(bool h) {
        highlight.SetActive(h);
    }

    public void AddPiece(Piece p) {
        pieces.Add(p);
        p.slot = this;
        p.transform.parent = piecesParent;
    }

    public bool WhichPlayersPieces() {
        if(pieces.Count > 0) {
            return pieces[0].player;
        }
        
        return false;
    }
}
