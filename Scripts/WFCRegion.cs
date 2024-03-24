using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System.Security.Cryptography.X509Certificates;
using System.Net.Cache;

public class WFCRegion {
    private RegionManager parentRegionManager;
    private WFCGrid grid;
    private WFCResult result;
    public Coordinates lowerNeighbor;
    public Coordinates rightNeighbor;
    public event Action<WFCResult,Coordinates> onRegionComplete; // Define the onComplete event

    public Coordinates regionIndex;
    public Offset regionOffsetConst;

     public WFCRegion(int width, int height, List<WFCRule> rules, Coordinates _regionIndex, RegionManager _parentRegionManager ,Coordinates _rightNeighbor, Coordinates _lowerNeighbor ) {
        parentRegionManager = _parentRegionManager; //keep a reference to the manager
         //const
        regionOffsetConst.X = width;
        regionOffsetConst.Y = height;
        //were the region resides in the overall regions 2darray
        regionIndex.X = _regionIndex.X; 
        regionIndex.Y = _regionIndex.Y;
        //references to nearby neighbors (-1,-1) if theres no neighbor
        lowerNeighbor = _lowerNeighbor; 
        rightNeighbor = _rightNeighbor; 

        EvaluateNeighbors();//determine if border needs to be adjusted
        grid = new WFCGrid(width, height, rules, this);

    }

   

    public void ChildGridCompleted(WFCResult _result){
        result = _result; 

        onRegionComplete?.Invoke(result,regionIndex);
    }
    public int this[int x, int y] {
        get { return grid[x, y].TileIndex; }
    }
    public WFCGrid GetGrid() {
        return grid;
    }
    public WFCResult GetResult(){
        return result;
    }
    public Offset GetOffset(){

        return regionOffsetConst;
    }

    public void Collapse(bool wrap) {
        grid.TryCollapse(wrap);
    }
     public bool IsBusy() {
        return grid.Busy;
    }
    public WFCCell getCellFromRegionManager(char _cellType, int x, int y){
        Coordinates requestedRegion = new Coordinates(-1,-1);
        switch(_cellType){
        case 'l': //lower
            requestedRegion = lowerNeighbor;
        break;
        case 'r'://right
            requestedRegion = rightNeighbor;
        break;
        }
        if(requestedRegion.X == -1){ //there is no region there
            
            return new WFCCell(requestedRegion, new int [0]);
        }
        else return parentRegionManager.getCellFromRegion(requestedRegion, x, y);
    }
    public void EvaluateNeighbors(){
    // Store the index of the lower neighbor if it exists, otherwise (-1, -1)
    lowerNeighbor = regionIndex.X != 0 - 1 ? new Coordinates(regionIndex.X + 1, regionIndex.Y) : new Coordinates(-1, -1);
    // Store the index of the right neighbor if it exists, otherwise (-1, -1)
    rightNeighbor = regionIndex.Y != 0 - 1 ? new Coordinates(regionIndex.X, regionIndex.Y + 1) : new Coordinates(-1, -1);
    }

}