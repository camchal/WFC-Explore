using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;

public class WFCRegion {
    private WFCGrid grid;

     public WFCRegion(int width, int height, List<WFCRule> rules, Offset regionOffset) {
        grid = new WFCGrid(width, height, rules);
        if(regionOffset.X != 0 && regionOffset.Y !=0){
            //if not zero then a cell has been added
            grid.updateCellCoordinates(regionOffset);
        }
        
    }
    public int this[int x, int y] {
        get { return grid[x, y].TileIndex; }
    }
    public WFCGrid GetGrid() {
        return grid;
    }

    public void Collapse(bool wrap) {
        grid.TryCollapse(wrap);
    }
     public bool IsBusy() {
        // Implement this method if needed
        return grid.Busy;
    }
}