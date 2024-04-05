using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System.Runtime.CompilerServices;

public partial class Test : TileMap{
	public event Action<WFCRegion[,],Coordinates> AllRegionsComplete;
	private RegionManager regionManager;
	[Export] private int mapWidth = 32;
	[Export] private int mapHeight = 16;
	[Export] private int regionWidth = 10;
	[Export] private int regionHeight = 10;
	[Export] private int numRegionsRows = 1;
	[Export] private int numRegionsCols = 2;
	[Export(PropertyHint.File)] private string rulePath;
	[Export] private bool wrap;
	private TileSetAtlasSource source;

	public override void _Ready(){
		List<WFCRule> rules = WFCRule.FromJSONFile(ProjectSettings.GlobalizePath(rulePath));
		regionManager = new RegionManager(regionWidth, regionHeight, numRegionsRows,numRegionsCols, rules);
		regionManager.AllRegionsComplete += (regions) => //lambda function
			{
				StartPopulatingTilemap(regions,regionManager.regionDimensions );
			};
		
	}

	public override void _Process(double delta){
		if (Input.IsActionJustPressed("Generate")){
			GenerateGrid();
		}
	}

	

	private void GenerateGrid(){
		if (regionManager.IsAnyRegionBusy()){
			GD.Print("BUSY STAHP IT");
			return; //possible issue here
		} 
		ClearTilemap();
		regionManager.CollapseRegions(wrap);

	}

	public void GenerationComplete(WFCRegion[,] regions){
		//StartPopulatingTilemap(regions);
	}
	private async Task StartPopulatingTilemap(WFCRegion[,] regions, Coordinates _regionDimensions) //await Task - > void
	{
		source = TileSet.GetSource(0) as TileSetAtlasSource;
		
		for(int i = 0; i < _regionDimensions.X; i++){
			for(int j = 0; j < _regionDimensions.Y; j++){
				WFCGrid grid = regions[i,j].GetGrid();
				GD.Print($"region ({regions[i,j].regionIndex.X},{regions[i,j].regionIndex.Y}) is beginning tilemap population");
				//offset the animation coordinates
				// Process animation coordinates  
				while (grid.AnimationCoordinates.Count > 0)
				{	
					CallDeferred("SetNextCell", grid.AnimationCoordinates.Dequeue().AsVector2I, i, j);
					await Task.Delay(1);
				}
			}
		}
	}
	private void SetNextCell(Vector2I c, int i, int j) {
		//EraseCell(0, c); // remember this
		Coordinates tempIndex = new Coordinates();
		tempIndex.X = i; tempIndex.Y=j;
		WFCGrid grid = regionManager.GetRegion(tempIndex).GetGrid(); // Assuming GetGrid() method in WFCRegion
		
		int tileIndex = grid[c.X, c.Y].TileIndex;
		// if(c.Y == 0){
		// 	GD.Print($"Cell({c.X},{c.Y}) set its tile as tile: {tileIndex}");
		// }
		if (tileIndex == -1) return; // Assuming -1 indicates no tile
		Offset regionOffset = regionManager.GetRegion(tempIndex).GetOffset();
		
		c.X += regionOffset.X * j;
		c.Y += regionOffset.Y * i;

		SetCell(0, c, 0, source.GetTileId(tileIndex));
	}

	private void ClearTilemap(){
		foreach (Vector2I v in GetUsedCells(0)){
			EraseCell(0, v);
		}
	}
	
	public void VisualizeAnimationCoords(Queue<Coordinates> allAnimationCoordinates){
		int count = 0;
		foreach (var coord in allAnimationCoordinates)
		{
			Console.Write($"({coord.X}, {coord.Y}) ");
			count++;
			if (count % 16 == 0)
			{
				Console.WriteLine();
			}
		}
	}	
}
