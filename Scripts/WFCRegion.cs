using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System.Security.Cryptography.X509Certificates;

public class WFCRegion {
    private WFCGrid grid;
    private WFCResult result;
    public event Action<WFCResult,Coordinates> onRegionComplete; // Define the onComplete event

    public Coordinates regionIndex;
    public Offset regionOffsetConst;

     public WFCRegion(int width, int height, List<WFCRule> rules, Coordinates _regionIndex) {
        grid = new WFCGrid(width, height, rules, this);

        //const
        regionOffsetConst.X = width;
        regionOffsetConst.Y = height;
        //were the region resides in the overall regions 2darray
        regionIndex.X = _regionIndex.X; 
        regionIndex.Y = _regionIndex.Y;

        if(regionOffsetConst.X != 0 || regionOffsetConst.Y !=0){
            //REMEMBER I USED TO OFFSET COORDS HERE
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
}