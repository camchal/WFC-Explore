﻿ namespace hamsterbyte.WFC{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Sockets;
	using System.Reflection.Metadata;
	using System.Runtime.Serialization;
	using Godot;
	

	public partial class WFCGrid{

		public int numattempt = 0;
		private EntropyCoordinates Observe(){
			int testRegion = parentRegion.regionIndex.Y;
			if(testRegion == 1 ){
				numattempt++;
				//stop here
						// GD.Print($"region {parentRegion.regionNumber}: cell at 0,0 is collapsed :{cells[0,0].Collapsed}");
						// GD.Print($"cells(0,0) coords x:{cells[0,0].Coordinates.X}");
						// GD.Print($"cells(0,0) coords y:{cells[0,0].Coordinates.Y}");
			}
			// if(numattempt > 256){
			// 	//stop here
			// 	GD.Print("256 debug");
			// }
			//GD.Print($"attempt number:{numattempt}");
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				WFCCell [,] testCells = cells;
				// GD.Print($"region {parentRegion.regionNumber}: cell at 0,0 is collapsed :{cells[0,0].Collapsed}");
				// GD.Print($"cells(0,0) coords x:{cells[0,0].Coordinates.X}");
				// GD.Print($"cells(0,0) coords y:{cells[0,0].Coordinates.Y}");
				// GD.Print($"ent coords x:{coords.Coordinates.X}");
				// GD.Print($"ent coords y:{coords.Coordinates.Y}");
				// GD.Print($"Observing  entropy coordinates: ({coords.Coordinates.X}, {coords.Coordinates.Y}) with entropy {coords.Entropy}");

				// GD.Print("update ent coords");
				//if(currentAttempt == 1)UpdateEntropyCoordinatesOffset(ref coords);
				// GD.Print($"ent coords x:{coords.Coordinates.X}");
				// GD.Print($"ent coords y:{coords.Coordinates.Y}");
				//for cameron coming back, for some reason the entropy coordiantes arent falling with the cell coordinates,
				// need to look how entropy coords are generated, and if i need to update the entropy coordinates with the normal cell coordinates
				// GD.Print($"Observing region {parentRegion.regionNumber}:TESTCELL coordinates: ({testCells[coords.Coordinates.X,coords.Coordinates.Y].Coordinates.X}, {testCells[coords.Coordinates.X,coords.Coordinates.Y].Coordinates.Y}) ");
				// GD.Print($"Observing region {parentRegion.regionNumber}: ENTROPY coordinates: ({coords.Coordinates.X}, {coords.Coordinates.Y}) with entropy {coords.Entropy} and cells.collapsed is {cells[coords.Coordinates.X ,coords.Coordinates.Y].Collapsed}");
				 if(!cells[coords.Coordinates.X, coords.Coordinates.Y].Collapsed){ 
					return coords;}
			}
			GD.Print("Heap was emptied!");
			return EntropyCoordinates.Invalid;
		}
		
		private void Collapse(Coordinates _coords){
			int collapsedIndex = cells[_coords.X, _coords.Y].Collapse();//returns collapsed index
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
			// GD.Print($"Updating Region {parentRegion.regionNumber}'s cells");
			for(int x = 0; x < width; x++){
				for(int y = 0 ; y < height; y++){
				//cells[x, y].UpdateCoordinates(regionOffset.X, regionOffset.Y, parentRegion.regionNumber);
				}
			}
		}
		public WFCCell[,] getCellCoordinates(){
			return cells;
		}
		public void TryCollapse(bool _wrap = true, int _maxAttempts = 100){
				Reset(true);
				Busy = true;
				Stopwatch timer = Stopwatch.StartNew();
				for(int i  = 0; i < _maxAttempts; i++){
					currentAttempt++;
					//GD.Print($"region {parentRegion.regionIndex.Y} is trying a new attempt");
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
				GD.Print($"region {parentRegion.regionIndex.Y} got to the end");
				if(!result.Success){
					ShowResultMetrics(result);
				}
				parentRegion.ChildGridCompleted(result);

				Busy = false;
		}
		public void ShowResultMetrics(WFCResult result)
		{
			GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y} failed max attempts");
			GD.Print($"Grid: {result.Grid}");
			GD.Print($"Success: {result.Success}");
			GD.Print($"Attempts: {result.Attempts}");
			GD.Print($"Elapsed Milliseconds: {result.ElapsedMilliseconds}");
		}
	}
	
}
