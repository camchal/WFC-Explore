using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;

public partial class Test : TileMap{
	public event Action<WFCRegion[,],Coordinates> AllRegionsComplete;
	private RegionManager regionManager;
	[Export] private int mapWidth = 32;
	[Export] private int mapHeight = 16;
	[Export] private int regionWidth = 16;
	[Export] private int regionHeight = 16;
	[Export] private int numRegionsRows = 1;
	[Export] private int numRegionsCols = 2;
	[Export(PropertyHint.File)] private string rulePath;
	[Export] private bool wrap;
	private TileSetAtlasSource source;

	public override void _Ready(){
		//WFCGrid.onComplete += OnGenerationComplete;
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

	// private void OnGenerationComplete(WFCResult result){
	// 	if (!result.Success) return;
	// 	StartPopulatingTilemap(result.Grid);
	// }

	private void GenerateGrid(){
		if (regionManager.IsAnyRegionBusy()) return; //possible issue here
		ClearTilemap();
		regionManager.CollapseRegions(wrap);

	}

	// private async Task StartPopulatingTilemap(WFCGrid _grid){
	// 	source = TileSet.GetSource(0) as TileSetAtlasSource;
	// 	GD.Print("Before PopulateTilemapAsync");
	// 	bool complete = await Task.Run(() => PopulateTilemapAsync(_grid));
	// 	GD.Print("After PopulateTilemapAsync");
	// 	GD.Print(complete);
	// }
	

	// private async Task<bool> PopulateTilemapAsync(WFCGrid _grid){
	// 	while (_grid.AnimationCoordinates.Count > 0){
	// 		CallDeferred("SetNextCell", _grid.AnimationCoordinates.Dequeue().AsVector2I);
	// 		await Task.Delay(5);
	// 	}
	// private async Task<bool> PopulateTilemapAsync(WFCGrid _grid){
	// 	while (_grid.AnimationCoordinates.Count > 0){
	// 		CallDeferred("SetNextCell", _grid.AnimationCoordinates.Dequeue().AsVector2I);
	// 		await Task.Delay(5);
	// 	}

	// 	return true;
	// }

	public void GenerationComplete(WFCRegion[,] regions){
		//StartPopulatingTilemap(regions);
	}
	private async Task StartPopulatingTilemap(WFCRegion[,] regions, Coordinates _regionDimensions)
	{
		source = TileSet.GetSource(0) as TileSetAtlasSource;
		GD.Print("Before PopulateTilemapAsync");

		
		for(int i = 0; i < _regionDimensions.X; i++){
			for(int j = 0; j < _regionDimensions.Y; j++){
				WFCGrid grid = regions[i,j].GetGrid();
				//offset the animation coordinates
				// Process animation coordinates
				while (grid.AnimationCoordinates.Count > 0)
				{	
					CallDeferred("SetNextCell", grid.AnimationCoordinates.Dequeue().AsVector2I, i, j);
					await Task.Delay(5);
				}
			}
		}
	}
	private void SetNextCell(Vector2I c, int i, int j) {
		//EraseCell(0, c);
	    Coordinates tempIndex = new Coordinates();
		tempIndex.X = i; tempIndex.Y=j;
		WFCGrid grid = regionManager.GetRegion(tempIndex).GetGrid(); // Assuming GetGrid() method in WFCRegion
		//this isnt using the animation coordinates at all? i think it goes back to the grid with the coordiantes in mind and puts the tile based on that
		//FUCKK
		int tileIndex = grid[c.X, c.Y].TileIndex;
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
