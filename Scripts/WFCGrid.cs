 namespace hamsterbyte.WFC{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Sockets;
	using System.Reflection.Metadata;
	using System.Runtime.Serialization;
	using Godot;
	

	public partial class WFCGrid{
		private EntropyCoordinates Observe(){
			int testRegion = parentRegion.regionNumber;
			if(testRegion == 1 ){
				//stop here
			}
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				WFCCell [,] testCells = cells;
				GD.Print($"cell at 16,2 is collapsed :{cells[0,0].Collapsed}");
				GD.Print($"ent coords x:{coords.Coordinates.X}");
				GD.Print($"ent coords y:{coords.Coordinates.Y}");
				GD.Print($"Observing  entropy coordinates: ({coords.Coordinates.X}, {coords.Coordinates.Y}) with entropy {coords.Entropy}");
				//for cameron coming back, for some reason the entropy coordiantes arent falling with the cell coordinates,
				// need to look how entropy coords are generated, and if i need to update the entropy coordinates with the normal cell coordinates
				//GD.Print($"Observing test cell coordinates: ({testCells[coords.Coordinates.X,coords.Coordinates.Y].Coordinates.X}, {testCells[coords.Coordinates.X,coords.Coordinates.Y].Coordinates.Y}) ");
				//GD.Print($"Observing coordinates: ({coords.Coordinates.X}, {coords.Coordinates.Y}) with entropy {coords.Entropy} and cells.collapsed is {cells[coords.Coordinates.X ,coords.Coordinates.Y].Collapsed}");
				if(!cells[coords.Coordinates.X, coords.Coordinates.Y].Collapsed){ 
					return coords;}
			}
			return EntropyCoordinates.Invalid;
		}
		
		private void Collapse(Coordinates _coords){
			int collapsedIndex = cells[_coords.X, _coords.Y].Collapse();
			AnimationCoordinates.Enqueue(_coords);
			removalUpdates.Push(new RemovalUpdate(){
				Coordinates = _coords,
				TileIndex = collapsedIndex
			});
			remainingUncollapsedCells--;
		}
		
		private void Propagate(bool _wrap = true){
			while(removalUpdates.Count > 0){
				RemovalUpdate update = removalUpdates.Pop();
				if (update.TileIndex == -1){
					validCollapse = false;
					return;
				}

				Coordinates[] cardinals = Coordinates.Cardinals;
				for(int d = 0; d < adjacencyRules.GetLength(1); d++){
					Coordinates current = cardinals[d] + update.Coordinates;
					if (_wrap){
						current = current.Wrap(Width, Height);
					} else if (!IsInBounds(current)){
						continue;
					}

					WFCCell currentCell = cells[current.X, current.Y];
					if(currentCell.Collapsed) continue;
					for(int o = 0; o < adjacencyRules.GetLength(2); o++){
						if(adjacencyRules[update.TileIndex, d, o] == 0 && currentCell.Options[o]){
							currentCell.RemoveOption(o);
						}
					}
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
				}
			}
		}
		
		public void updateCellCoordinates(Offset regionOffset){
			GD.Print($"Updating Region {parentRegion.regionNumber}'s cells");
			for(int x = 0; x < width; x++){
				for(int y = 0 ; y < height; y++){
				cells[x, y].UpdateCoordinates(regionOffset.X, regionOffset.Y, parentRegion.regionNumber);
				}
			}
		}
		public WFCCell[,] getCellCoordinates(){
			return cells;
		}
		public void TryCollapse(bool _wrap = true, int _maxAttempts = 100){
				// WFCCell [,] cellCoord = getCellCoordinates();
				// WFCCell testCell = cellCoord[0,0];
				// GD.Print($"region {parentRegion.regionNumber}'s first cell is ({testCell.Coordinates.X + ", " + testCell.Coordinates.Y})");
				Reset(true);
				parentRegion.InitCellCoordinates(parentRegion.regionNumber);//reinitalize the cell coords according to region
				Busy = true;
				// testCell = cellCoord[0,0];
				// GD.Print($"region {parentRegion.regionNumber}'s first cell is ({testCell.Coordinates.X + ", " + testCell.Coordinates.Y})");
				Stopwatch timer = Stopwatch.StartNew();
				for(int i  = 0; i < _maxAttempts; i++){
					currentAttempt++;
					GD.Print($"region {parentRegion.regionNumber} is trying a new attempt");
					WFCCell cell = cells.Random();
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = cell.Coordinates,
						Entropy = cell.Entropy
					});

					while (remainingUncollapsedCells > 0){
						EntropyCoordinates e = Observe();
						Collapse(e.Coordinates);
						Propagate(_wrap);
					}

					if(!validCollapse && i < _maxAttempts - 1){
						Reset();
					} else break;
				}
				timer.Stop();
				WFCResult result = new(){
					Grid = this,
					Success = validCollapse,
					Attempts = currentAttempt,
					ElapsedMilliseconds = timer.ElapsedMilliseconds
				};
				GD.Print($"region {parentRegion.regionNumber} got to the end");
				// WFCCell [,] cellCoord = getCellCoordinates();
				// WFCCell testCell = cellCoord[0,0];
				// GD.Print($"region {parentRegion.regionNumber}'s first cell is ({testCell.Coordinates.X + ", " + testCell.Coordinates.Y})");
				
				//GD.Print($"  Result: {result.Grid}, Success: {result.Success}, Attempts: {result.Attempts}, ElapsedMilliseconds: {result.ElapsedMilliseconds}");

				// need to modify this
				parentRegion.ChildGridCompleted(result);

				Busy = false;
		}
	}
}
