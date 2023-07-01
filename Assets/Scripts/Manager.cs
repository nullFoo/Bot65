using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Manager : MonoBehaviour
{
    public bool whoseTurn; // false = white, true = red

    public int dice1;
    public int dice2;
    public bool dice1Used;
    public bool dice2Used;
    [SerializeField] Image dice1Image;
    [SerializeField] Image dice2Image;
    [SerializeField] Sprite[] diceSprites;

    [SerializeField] TextMeshProUGUI turnText;

    public Slot[] slots;

    public List<Piece> player1Out;
    public List<Piece> player2Out;
    public List<Piece> player1Middle;
    public List<Piece> player2Middle;

    [SerializeField] GameObject piecePrefab;
    public Transform topLayerParent;

    List<Piece> allPieces;

    public static Manager instance;
    void Awake() {
        instance = this;
    }

    void Start() {
        slots = GameObject.FindObjectsOfType<Slot>();
        List<Slot> slotList = new List<Slot>(slots);
        slotList = slotList.OrderBy(s=>s.index).ToList();
        slots = slotList.ToArray();
    }

    public void StartGame() {
        // board setup
        LoadGameStateFromString("%!!!!,!(!!!+,!!!'!+!!!!&!!!!");
        // Debug.Log(SaveGameStateToString());

        // roll for who goes first and start the round
        while(dice1 == dice2)
            RollDice();
        whoseTurn = dice2 > dice1;
        
        turnText.text = TurnBoolToString(whoseTurn) + "'s turn";
        turnText.color = whoseTurn ? Color.red : Color.white;
    }

    public void NextTurn() {
        // update turn and roll the dice
        whoseTurn = !whoseTurn;
        turnText.text = TurnBoolToString(whoseTurn) + "'s turn";
        turnText.color = whoseTurn ? Color.red : Color.white;
        RollDice();
        dice1Used = false;
        dice2Used = false;

        // highlight pieces that can be moved
        HighlightLegalMoves();
    }

    public void HighlightLegalMoves() {
        ClearHighlights();
        foreach (Piece p in allPieces)
        {
            if(p.player == whoseTurn) {
                if(p.LegalMoves().Count > 0) {
                    // do some graphical thing
                    p.slot.Highlight(true);
                    continue;
                }
            }
        }
    }
    public void ClearHighlights() {
        foreach (Slot s in slots)
        {
            s.Highlight(false);
        }
    }

    string TurnBoolToString(bool turn) {
        return turn ? "Red" : "White";
    }

    void RollDice() {
        dice1 = UnityEngine.Random.Range(1, 7);
        dice1Image.sprite = diceSprites[dice1 - 1];
        dice2 = UnityEngine.Random.Range(1, 7);
        dice2Image.sprite = diceSprites[dice2 - 1];
    }

    public void CapturePiece(Piece p) {
        p.slot.pieces.Remove(p);
        p.transform.parent = null; // todo: some transform in the center
    }

    #region saving and loading

    string SaveGameStateToString() {
        string state = "";

        Debug.Log(slots.Length);
        foreach(Slot slot in slots) {
            Debug.Log(slot.index);

            byte b = SaveSlotByte(slot);
            Debug.Log(ByteToVisualStringForDebugging(b));

            Debug.Log((char)(b + 33)); // add 33 to get an actual character instead of headings, whitespace etc
            state += (char)(b + 33); // char representation of these bits
        }
        
        state += (char)(player1Out.Count + 33);
        state += (char)(player2Out.Count + 33);
        state += (char)(player1Middle.Count + 33);
        state += (char)(player2Middle.Count + 33);

        return state;
    }

    byte SaveSlotByte(Slot slot) {
        byte numPieces = (byte)slot.pieces.Count; // how many pieces on this slot (e.g. 00000101)
        Debug.Log(ByteToVisualStringForDebugging(numPieces));
        byte whichType = Convert.ToByte(slot.WhichPlayersPieces()); // which player these pieces belong too (e.g. 00000001)
        Debug.Log(ByteToVisualStringForDebugging(whichType));

        byte combine = (byte)((numPieces << 1) | whichType); // combine these into one byte (e.g. 00001011)
        // ^ this first moves the pieces number left by one (e.g. 00001010) then bitwise ors it with the player type

        return combine;
    }

    void LoadGameStateFromString(string data) {
        if(data.Length != 28) // something is wrong with the string
            return;

        // destroy all current pieces
        allPieces = new List<Piece>();
        foreach (Piece p in GameObject.FindObjectsOfType<Piece>())
        {
            Destroy(p.gameObject);
        }
        foreach (Slot s in slots)
        {
            s.pieces = new List<Piece>();
        }

        for (int i = 0; i < 24; i++)
        {
            LoadSlotByte(data[i], slots[i]);
        }

        // todo out & middle
    }
    void LoadSlotByte(char c, Slot s) {
        // wacky bit hijinks
        Debug.Log(c);
        Debug.Log((int)c);
        int charInt = (int)c - 33;
        Debug.Log(charInt);
        byte b = (byte)charInt;
        Debug.Log(ByteToVisualStringForDebugging(b));

        bool playerType = ((b & 1) != 0);
        Debug.Log(playerType);

        int pieceCount = (int)(b >> 1);
        Debug.Log(pieceCount);

        // actually setting up the slot
        for (int i = 0; i < pieceCount; i++)
        {
            Piece piece = Instantiate(piecePrefab, s.transform).GetComponent<Piece>();
            s.AddPiece(piece);
            piece.player = playerType;
            allPieces.Add(piece);
        }
    }

    string ByteToVisualStringForDebugging(byte b) {
        string s = "";
        for (int i = 7; i > -1; i--)
        {
            s += ((b & (1 << i)) == 0) ? "0" : "1";
        }
        return s;
    }

    #endregion
}