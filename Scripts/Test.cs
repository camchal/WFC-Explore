using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;

public partial class Test : TileMap{
	public event Action<List<WFCRegion>> AllRegionsComplete;
	private RegionManager regionManager;
	[Export] private int mapWidth = 32;
	[Export] private int mapHeight = 16;
	[Export] private int regionWidth = 16;
	[Export] private int regionHeight = 16;
	[Export] private int numRegions = 2;
	[Export(PropertyHint.File)] private string rulePath;
	[Export] private bool wrap;
	private TileSetAtlasSource source;

	public override void _Ready(){
		//WFCGrid.onComplete += OnGenerationComplete;
		regionManager = new RegionManager(regionWidth, regionHeight, numRegions);
		List<WFCRule> rules = WFCRule.FromJSONFile(ProjectSettings.GlobalizePath(rulePath));
		for(int i = 0; i < numRegions;i++){
		regionManager.AddRegion(regionWidth, regionHeight, rules);
		}
		regionManager.AllRegionsComplete += (regions) => //lambda function
			{
				StartPopulatingTilemap(regions);
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
		if (regionManager.IsAnyRegionBusy()) return;
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

	public void GenerationComplete(List<WFCRegion> regions){
		StartPopulatingTilemap(regions);
	}
	private async Task StartPopulatingTilemap(List<WFCRegion> regions)
	{
		source = TileSet.GetSource(0) as TileSetAtlasSource;
		GD.Print("Before PopulateTilemapAsync");

		Queue<Coordinates> allAnimationCoordinates = new Queue<Coordinates>();

		// Collect animation coordinates from all regions
		foreach (var region in regions)
		{
			WFCGrid grid = region.GetGrid();
			GD.Print($"REGION {region.regionNumber}: COORDS");
			VisualizeAnimationCoords(grid.AnimationCoordinates);
			while (grid.AnimationCoordinates.Count > 0)
			{
				
				allAnimationCoordinates.Enqueue(grid.AnimationCoordinates.Dequeue());
			}
		}
		GD.Print("COMBINED");
		VisualizeAnimationCoords(allAnimationCoordinates);


		// Process animation coordinates
		while (allAnimationCoordinates.Count > 0)
		{
			if(allAnimationCoordinates.Count < 260){
				//stop here
			}
			CallDeferred("SetNextCell", allAnimationCoordinates.Dequeue().AsVector2I);
			await Task.Delay(5);
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

	private void SetNextCell(Vector2I c) {
		EraseCell(0, c);
	WFCGrid grid = regionManager.GetRegion(0).GetGrid(); // Assuming GetGrid() method in WFCRegion
	//this isnt using the animation coordinates at all? i think it goes back to the grid with the coordiantes in mind and puts the tile based on that
	//FUCKK
	int tileIndex = grid[c.X, c.Y].TileIndex;
	if (tileIndex == -1) return; // Assuming -1 indicates no tile
	SetCell(0, c, 0, source.GetTileId(tileIndex));
	}

	private void ClearTilemap(){
		foreach (Vector2I v in GetUsedCells(0)){
			EraseCell(0, v);
		}
	}
}
