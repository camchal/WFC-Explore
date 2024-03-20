using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics.Metrics;


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
	
	public WFCRegion[,] GetRegionsArray(){
		return regions;
	}
	public WFCRegion AddRegion(int width, int height, List<WFCRule> rules, Coordinates _regionIndex, bool suppressNotifications = false) {

		WFCRegion newRegion = new WFCRegion(width, height, rules, _regionIndex);
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
	

   public void CollapseRegions(bool wrap)
	{
		//resets these 2d arrays
		//they are used as metrics to see if all regions are done generating
		regionCompletionStatus = new bool[regionDimensions.X, regionDimensions.Y];
		regionResults = new WFCResult[regionDimensions.X, regionDimensions.Y];
		foreach (WFCRegion region in regions)
		{
			region.Collapse(wrap);
		}
	}

}
