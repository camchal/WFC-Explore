using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;


public class RegionManager {
	WFCRegion[,] regions;
	bool[,] regionCompletionStatus;
	WFCResult[,] regionResults;
	public event Action<WFCRegion[,]> AllRegionsComplete;

	private Offset regionOffsetConst;
	public Coordinates regionDimensions;

	private int completedRegions;
	

	public RegionManager(int regionWidth, int regionHeight, int _numRegionsRows, int _numRegionsCols, List<WFCRule> rules) {
		//dimensions of 2d array
		regionDimensions.X = _numRegionsRows; regionDimensions.Y = _numRegionsCols;
		//following regions will be offset by the designated size of regions at beggining(all regions will be same size)
		//safety push
		regionOffsetConst.X = regionWidth; regionOffsetConst.Y = regionHeight;


		//new 2d array management
		regions = new WFCRegion[_numRegionsRows, _numRegionsCols];
		regionCompletionStatus = new bool[_numRegionsRows, _numRegionsCols];
		regionResults = new WFCResult[_numRegionsRows, _numRegionsCols];	
		
		//InitializeLists(_numRegions); //fills regionResults and regionCompstat with empty vars that will be replaced
		//could be an issue
		
		//intialize and add regions to 2d array
		int regionCounter = 0;
		Coordinates regionIndex;
		for(int i = 0; i < _numRegionsRows; i++){
			for(int j = 0; j < _numRegionsCols; j++){
				regionIndex.X = i; regionIndex.Y = j;
				

				regions[i,j] = AddRegion(regionWidth, regionHeight,rules, regionIndex);
				regionCounter++;
			}
		}

	}
	public Coordinates calcNeighbor(int row, int col, char type){
		switch(type){
        case 'l': // Lower neighbor
            if(row < regionDimensions.X - 1)
                return new Coordinates(row + 1, col);
            break;
        case 'r': // Right neighbor
            if(col < regionDimensions.Y - 1)
                return new Coordinates(row, col + 1);
            break;
    }
    // Return (-1, -1) if no neighbor in the specified direction
    return new Coordinates(-1, -1);
	}
	
	public WFCRegion[,] GetRegionsArray(){
		return regions;
	}
	public WFCRegion AddRegion(int width, int height, List<WFCRule> rules, Coordinates _regionIndex, bool suppressNotifications = false) {
		Coordinates rightNeighbor = calcNeighbor(_regionIndex.X,_regionIndex.Y,'r');
		Coordinates lowerNeighbor = calcNeighbor(_regionIndex.X,_regionIndex.Y,'l');

		WFCRegion newRegion = new WFCRegion(width, height, rules, _regionIndex, this, rightNeighbor, lowerNeighbor);
		//GD.Print($"Region number {regionNumber} had an offset of {regionOffset.X}"); 
		newRegion.onRegionComplete += (result, index) => OnRegionComplete(result, _regionIndex);
		//USED TO POSITION NEXT REGION
		return newRegion;
	
	}

	private void OnRegionComplete(WFCResult result, Coordinates _regionIndex)
	{

		regionResults[_regionIndex.X,_regionIndex.Y] = result; 
	
		regionCompletionStatus[_regionIndex.X,_regionIndex.Y] = true; // Mark region as complete
		
		//LogRegionManagerState();

		if (CheckAllComplete(regionCompletionStatus))
{
			//VisualizeRegionCoordinates();
			if(CheckAllComplete(regionCompletionStatus)){
				GD.Print("All regions completed!");
			}
			
			OnAllRegionsComplete();
}
		}
	 private void OnAllRegionsComplete()
	{
		 // Raise the event (aka fill in animation the animation coords of all grids)
		 //if (!result.Success) return; <--remember this
		 AllRegionsComplete?.Invoke(regions);
	}
	public bool CheckAllComplete(bool[,] array)
{
	for (int i = 0; i < array.GetLength(0); i++)
	{
		for (int j = 0; j < array.GetLength(1); j++)
		{
			if (!array[i, j])
			{
				return false;
			}
		}
	}
	return true;
}

	public WFCRegion GetRegion(Coordinates _regionIndex) {
		return regions[_regionIndex.X,_regionIndex.Y];
	}
	// public WFCCell getCellFromRegion(Coordinates _requestedRegion, int row, int col){
	// 	  // Check if the requested region is within the bounds of the regions array
    // if (_requestedRegion.X < 0 || _requestedRegion.X >= regions.GetLength(0) ||
    //     _requestedRegion.Y < 0 || _requestedRegion.Y >= regions.GetLength(1))
    // {
    //     // Return a default WFCCell if the requested region is outside the array bounds
    //     return new WFCCell(new Coordinates(-1, -1), new int[0]);
    // }

    // // Get the cell from the requested region
    // return regions[_requestedRegion.X, _requestedRegion.Y].GetGrid().getCell(row, col);
	// }
	public void AppendBorCellUpdate(Coordinates _requestedRegion, BorderCellUpdate _borCellUpdate){
	  // Check if the requested region is within the bounds of the regions array
    if (_requestedRegion.X < 0 || _requestedRegion.X >= regions.GetLength(0) ||
        _requestedRegion.Y < 0 || _requestedRegion.Y >= regions.GetLength(1))
    {
        // Return  if the requested region is outside the region array bounds
        return;
    }

    // send update to the specified region
		regions[_requestedRegion.X, _requestedRegion.Y].RegionAppendBorCellUpdate(_borCellUpdate);
		GD.Print($"region ({_requestedRegion.X}, {_requestedRegion.Y}) had cell({_borCellUpdate.X},{_borCellUpdate.Y}) updated with {_borCellUpdate.OptionsRemovedList.Count} options removed. DTindex: {_borCellUpdate.determiningTileIndex}");
		return;
	}

	public bool IsAnyRegionBusy() {
		for (int i = 0; i < regionDimensions.X; i++){
			for (int j = 0; j < regionDimensions.Y; j++)
			{
				if (regions[i, j].IsBusy())
				{
					return true;
				}
			}
		}
		return false;
	}
	public void ResetRegionBorderUpdates(){
		foreach (WFCRegion region in regions)
		{
			region.GetGrid().bCellUpdates = new List<BorderCellUpdate>(); //reset their lists
		}
		
	}

   public void CollapseRegions(bool wrap)
	{
		//resets these 2d arrays
		//they are used as metrics to see if all regions are done generating
		regionCompletionStatus = new bool[regionDimensions.X, regionDimensions.Y];
		regionResults = new WFCResult[regionDimensions.X, regionDimensions.Y];
		ResetRegionBorderUpdates();
		foreach (WFCRegion region in regions)
		{
			
			region.Collapse(wrap);
		}
	}

}
