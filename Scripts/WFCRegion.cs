using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System.Security.Cryptography.X509Certificates;

public class WFCRegion {
    private WFCGrid grid;
    private WFCResult result;
    public event Action<WFCResult,int> onRegionComplete; // Define the onComplete event

    public int regionNumber;
    public Offset regionOffsetConst;

     public WFCRegion(int width, int height, List<WFCRule> rules, Offset _regionOffset, int passedRegionNum) {
        grid = new WFCGrid(width, height, rules, this);
        regionOffsetConst = _regionOffset;
        regionNumber = passedRegionNum; 
        if(regionOffsetConst.X != 0 || regionOffsetConst.Y !=0){
            //if not zero then a cell has been added
            grid.updateCellCoordinates(regionOffsetConst);//this is kinda messy, should clean up, currently a different
            //method for first cell update, then a new one each time spacebaris pressed
        }
        
        
    }
    public void InitCellCoordinates(int _regionNumber){
        Offset updateRegionOffset;
        updateRegionOffset.X = (_regionNumber * regionOffsetConst.X);
        updateRegionOffset.Y = (_regionNumber * regionOffsetConst.Y);
        grid.updateCellCoordinates(updateRegionOffset);
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
    public Offset GetOffset(){

        return regionOffsetConst;
    }

    public void Collapse(bool wrap) {
        grid.TryCollapse(wrap);
    }
     public bool IsBusy() {
        return grid.Busy;
    }
}