using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System.Security.Cryptography.X509Certificates;

public class WFCRegion {
    private WFCGrid grid;
    private WFCResult result;
    public bool hasUpNeighbor;
    public bool hasLeftNeighbor;
    public event Action<WFCResult,Coordinates> onRegionComplete; // Define the onComplete event

    public Coordinates regionIndex;
    public Offset regionOffsetConst;

     public WFCRegion(int width, int height, List<WFCRule> rules, Coordinates _regionIndex) {
        EvaluateNeighbors();//determine if border needs to be adjusted
        grid = new WFCGrid(width, height, rules, this);

        //const
        regionOffsetConst.X = width;
        regionOffsetConst.Y = height;
        //were the region resides in the overall regions 2darray
        regionIndex.X = _regionIndex.X; 
        regionIndex.Y = _regionIndex.Y;

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
    public void EvaluateNeighbors(){
		//if either is 0, set that value to false, otherwise its true
		hasUpNeighbor = regionIndex.X != 0;
		hasLeftNeighbor = regionIndex.Y != 0;

	}
}