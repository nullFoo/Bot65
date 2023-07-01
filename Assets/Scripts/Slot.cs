using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public int index;
    public List<Piece> pieces;
    Transform piecesParent;

    void Start() {
        pieces = new List<Piece>();
        piecesParent = this.transform.GetChild(1);
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
