using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System;
using System.Linq;


public class RegionManager {
	private List<WFCRegion> regions;
	private List<bool> regionCompletionStatus;
	public event Action<List<WFCRegion>> AllRegionsComplete;

	private Offset regionOffset;
	private Offset regionOffsetConst;
	private int numRegions;
	private int regionNumber;
	private WFCResult[] regionResults;
	private int completedRegions;
	
	public List<WFCRegion> GetRegionList(){
		return regions;
	}

	public RegionManager(int regionWidth, int regionHeight, int _numRegions) {
		//INIT OFFSETS
		//inital offset should be zero so that the first region doesnt move
		regionOffset.X = 0; regionOffset.Y = 0;
		//following regions will be offset by the designated size of regions at beggining(all regions will be same size)
		regionOffsetConst.X = regionWidth; regionOffsetConst.Y = regionHeight;

		//INIT LISTS
		regionCompletionStatus = new List<bool>();
		regions = new List<WFCRegion>();
		regionResults = new WFCResult[_numRegions];
		InitializeLists(_numRegions); //fills regionResults and regionCompstat with empty vars that will be replaced
		
		GD.Print("in constructor!");
			
		completedRegions = 0;
		numRegions = _numRegions;
		regionNumber = 0;
	}
	public void UpdateRegionOffset(){
		regionOffset.X += regionOffsetConst.X;
		//regionOffset.Y += regionOffsetConst.Y;
		regionOffset.Y += 0; //keeping it one row for now

	}

	public List<WFCRegion> GetRegionsList(){
		return regions;
	}
	public void AddRegion(int width, int height, List<WFCRule> rules, bool suppressNotifications = false) {

		WFCRegion newRegion = new WFCRegion(width, height, rules, regionOffset, regionNumber);
		//GD.Print($"Region number {regionNumber} had an offset of {regionOffset.X}"); 
		regions.Add(newRegion);
		newRegion.onRegionComplete += (result, regionNumber) => OnRegionComplete(result, regionNumber);

		regionNumber++;
		//USED TO POSITION NEXT REGION
		UpdateRegionOffset();
		
	}

	private void OnRegionComplete(WFCResult result, int regionNumber)
	{
		//GD.Print("now inside regionmanager.cs");
		//GD.Print($"  Result: {result.Grid}, Success: {result.Success}, Attempts: {result.Attempts}, ElapsedMilliseconds: {result.ElapsedMilliseconds}");
		//GD.Print($"Region number: {regionNumber}, Region Results length: {regionResults.Length}");

		regionResults[regionNumber] = result; //<--- somehow the problem for out of bounds, once done handle animation
		GD.Print($"successfully replaced region result for region {regionNumber}");
		regionCompletionStatus[regionNumber] = true; // Mark region as complete
		GD.Print("Region Completion Status:");
		for (int i = 0; i < regionCompletionStatus.Count; i++)
		{
			GD.Print($"  Region {i}: {regionCompletionStatus[i]}");
		}
		//LogRegionManagerState();
		//failing somewhere around here, but it seems like 

		//GD.Print($"region {regionNumber} called on Region COmplete");

		if (regionCompletionStatus.All(status => status))
		{
			VisualizeRegionCoordinates();
			GD.Print($"regions all completed!");
			OnAllRegionsComplete(); // Raise the AllRegionsComplete event if all regions are complete
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

	public WFCRegion GetRegion(int index) {
		if (index < 0 || index >= regions.Count) {
			throw new IndexOutOfRangeException("Invalid region index");
		}
		return regions[index];
	}

	public bool IsAnyRegionBusy() {
		return regions.Any(region => region.IsBusy());
	}
	//INIT Results list of useless results that will be replaced
	private void InitializeLists(int _numRegions)
	{
		for (int i = 0; i < _numRegions; i++)
		{
			regionCompletionStatus.Add(false);
			WFCResult dummyResult =(new WFCResult()
			{
				
				Grid = new WFCGrid(0,0,new List<WFCRule>(), new WFCRegion(0,0,new List<WFCRule>(),new Offset(0,0),0)), // Placeholder value
				Success = false, // Placeholder value
				Attempts = 0, // Placeholder value
				ElapsedMilliseconds = 0 // Placeholder value
			});
			regionResults[i] = dummyResult;
			GD.Print("Im initializing WFCResults list");
		}
	}
	
   
   public void CollapseRegions(bool wrap)
	{

		foreach (WFCRegion region in regions)
		{
			if(region.regionNumber > 0){
				//GD.Print($"Region {region.regionNumber}: First region succeeded and now I'm starting the second!");

			}
			region.Collapse(wrap);
		}
	}
	public void ResetRegionCellCoordinates()
{
	foreach (WFCRegion region in regions)
	{
		region.InitCellCoordinates(region.regionNumber);
	}
}


/// <summary>
/// DEBUG FUNCTIONS
/// </summary>
			public void LogRegionManagerState()
	{
		GD.Print("RegionManager State:");
		for (int i = 0; i < regions.Count; i++)
		{
			GD.Print($"Region {i}:");
			GD.Print($"  Completion Status: {regionCompletionStatus[i]}");
			GD.Print($"  Result: {regionResults[i].Grid}, Success: {regionResults[i].Success}, Attempts: {regionResults[i].Attempts}, ElapsedMilliseconds: {regionResults[i].ElapsedMilliseconds}");
		}
	}

	public void VisualizeRegionCoordinates(){
		foreach (WFCRegion region in regions)
		{
			WFCGrid grid = region.GetGrid();
			GD.Print($"Region {region.regionNumber} Grid Coordinates:");
			WFCCell [,] cells = grid.getCellCoordinates();

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
