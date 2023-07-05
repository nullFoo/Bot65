using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public int index;
    public List<Piece> pieces;
    [SerializeField] Transform piecesParent;

    [SerializeField] GameObject highlight;
    // temporary? debug
    public Image highlightImage {
        get {
            return highlight.GetComponent<Image>();
        }
    }

    void Start() {
        pieces = new List<Piece>();
        if(piecesParent == null)
            piecesParent = this.transform.GetChild(1);
        if(highlight == null)
            highlight = this.transform.GetChild(0).GetChild(0).gameObject; // I know this is bad practice, I don't care
    }

    public void Highlight(bool h) {
        highlight.SetActive(h);
    }

    public void AddPiece(Piece p) {
        if(!pieces.Contains(p))
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
