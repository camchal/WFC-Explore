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
		regionOffsetConst.X = regionWidth; regionOffsetConst.Y = regionHeight;


		//new 2d array management
		regions = new WFCRegion[_numRegionsRows, _numRegionsCols];
		regionCompletionStatus = new bool[_numRegionsRows, _numRegionsCols];
		regionResults = new WFCResult[_numRegionsRows, _numRegionsCols];	
		
		//InitializeLists(_numRegions); //fills regionResults and regionCompstat with empty vars that will be replaced
		//could be an issue
		
		GD.Print("in constructor!");
		//intialize and add regions to 2d array
		int regionCounter = 0;
		Coordinates regionIndex;
		for(int i = 0; i < _numRegionsRows; i++){
			for(int j = 0; j < _numRegionsCols; j++){
				regionIndex.X = i; regionIndex.Y = j;
				regions[i,j] = AddRegion(regionWidth, regionHeight,rules, regionIndex);
				regionCounter++;
			}
		}GD.Print("addreg");

		completedRegions = 0; //needed?

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
		//GD.Print("now inside regionmanager.cs");
		//GD.Print($"  Result: {result.Grid}, Success: {result.Success}, Attempts: {result.Attempts}, ElapsedMilliseconds: {result.ElapsedMilliseconds}");
		//GD.Print($"Region number: {regionNumber}, Region Results length: {regionResults.Length}");

		regionResults[_regionIndex.X,_regionIndex.Y] = result; //<--- somehow the problem for out of bounds, once done handle animation
		//GD.Print($"successfully replaced region result for region {_regionIndex.Y}");
		regionCompletionStatus[_regionIndex.X,_regionIndex.Y] = true; // Mark region as complete
		//GD.Print("Region Completion Status:");
		// for (int i = 0; i < regionDimensions.X; i++){
		// 	for(int j = 0; j <regionDimensions.Y; j++){
		// 		GD.Print($"Region({i},{j}) completion status: {result.Success}");
		// 	}
		// }
		//LogRegionManagerState();
		//failing somewhere around here, but it seems like 

		//GD.Print($"region {regionNumber} called on Region COmplete");

		if (CheckAllComplete(regionCompletionStatus))
{
			//VisualizeRegionCoordinates();
			GD.Print("All regions completed!");
			OnAllRegionsComplete();
}
		}
	 private void OnAllRegionsComplete()
	{
		 // Raise the event (aka fill in animation the animation coords of all grids)
		 //need to place the invoke on region complete on the end of the grid i believe
		 //if (!result.Success) return;
		 AllRegionsComplete?.Invoke(regions);
		 GD.Print("All regions completed!");
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

		foreach (WFCRegion region in regions)
		{
			region.Collapse(wrap);
		}
	}
	public void ResetRegionCellCoordinates()
{
	foreach (WFCRegion region in regions)
	{
		//region.InitCellCoordinates(region.regionNumber); //might not be neccesarry?
	}
}


/// <summary>
/// DEBUG FUNCTIONS
/// </summary>
	// 		public void LogRegionManagerState()
	// {
	// 	GD.Print("RegionManager State:");
	// 	for (int i = 0; i < regions.Count; i++)
	// 	{
	// 		GD.Print($"Region {i}:");
	// 		GD.Print($"  Completion Status: {regionCompletionStatus[i]}");
	// 		GD.Print($"  Result: {regionResults[i].Grid}, Success: {regionResults[i].Success}, Attempts: {regionResults[i].Attempts}, ElapsedMilliseconds: {regionResults[i].ElapsedMilliseconds}");
	// 	}
	// }

	public void VisualizeRegionCoordinates()
	{
		for (int i = 0; i < regions.GetLength(0); i++)
		{
			for (int j = 0; j < regions.GetLength(1); j++)
			{
				WFCRegion region = regions[i, j];
				WFCGrid grid = region.GetGrid();
				GD.Print($"Region ({i},{j}) Grid Coordinates:");
				WFCCell[,] cells = grid.getCellCoordinates();

				for (int y = 0; y < cells.GetLength(1); y++)
				{
					string row = "";

					for (int x = 0; x < cells.GetLength(0); x++)
					{
						WFCCell cell = cells[x, y];
						row += $"({cell.Coordinates.X}, {cell.Coordinates.Y}) ";
					}

					GD.Print(row);
				}
			}
		}
	}
}
