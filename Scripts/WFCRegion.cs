using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;

public class WFCRegion {
    private WFCGrid grid;
    private WFCResult result;
    public event Action<WFCResult,int> onRegionComplete; // Define the onComplete event

    public int regionNumber;

     public WFCRegion(int width, int height, List<WFCRule> rules, Offset regionOffset, int passedRegionNum) {
        grid = new WFCGrid(width, height, rules, this);
        regionNumber = passedRegionNum; 
        if(regionOffset.X != 0 && regionOffset.Y !=0){
            //if not zero then a cell has been added
            grid.updateCellCoordinates(regionOffset);
        }
        
    }

    public void ChildGridCompleted(WFCResult _result){
        result = _result;
        //GD.Print("now inside region.cs");
        //GD.Print($"  Result: {result.Grid}, Success: {result.Success}, Attempts: {result.Attempts}, ElapsedMilliseconds: {result.ElapsedMilliseconds}");

        onRegionComplete?.Invoke(result,regionNumber);
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


    public void Collapse(bool wrap) {
        grid.TryCollapse(wrap);
    }
     public bool IsBusy() {
        return grid.Busy;
    }
}