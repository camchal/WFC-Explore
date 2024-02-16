using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;

public partial class Test : TileMap{
	private RegionManager regionManager;
    [Export] private int regionWidth = 50;
    [Export] private int regionHeight = 50;
	[Export(PropertyHint.File)] private string rulePath;
	[Export] private bool wrap;
	private TileSetAtlasSource source;

	public override void _Ready(){
		WFCGrid.onComplete += OnGenerationComplete;
		regionManager = new RegionManager();
		List<WFCRule> rules = WFCRule.FromJSONFile(ProjectSettings.GlobalizePath(rulePath));
		regionManager.AddRegion(regionWidth, regionHeight, rules);
	}

	public override void _Process(double delta){
		if (Input.IsActionJustPressed("Generate")){
			GenerateGrid();
		}
	}

	private void OnGenerationComplete(WFCResult result){
		if (!result.Success) return;
		StartPopulatingTilemap(result.Grid);
	}

	private void GenerateGrid(){
		if (regionManager.IsAnyRegionBusy()) return;
        ClearTilemap();
        regionManager.GetRegion(0).Collapse(wrap);
	}

	private async Task StartPopulatingTilemap(WFCGrid _grid){
		source = TileSet.GetSource(0) as TileSetAtlasSource;
		bool complete = await Task.Run(() => PopulateTilemapAsync(_grid));
		GD.Print(complete);
	}

	private async Task<bool> PopulateTilemapAsync(WFCGrid _grid){
		while (_grid.AnimationCoordinates.Count > 0){
			CallDeferred("SetNextCell", _grid.AnimationCoordinates.Dequeue().AsVector2I);
			await Task.Delay(5);
		}

		return true;
	}

	  private void SetNextCell(Vector2I c) {
        EraseCell(0, c);
    WFCGrid grid = regionManager.GetRegion(0).GetGrid(); // Assuming GetGrid() method in WFCRegion
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
